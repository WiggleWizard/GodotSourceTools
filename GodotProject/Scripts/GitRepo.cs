using System;
using Godot;
using GdCollections = Godot.Collections;

using LibGit2Sharp;
using GitCmds = LibGit2Sharp.Commands;

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using GodotAppFramework.Extensions;

namespace GodotAppFramework;

[GlobalClass]
public partial class GitRepo : Node
{
    [Export] private string RepositoryPath { get; set; } = "";
    
    private Repository? _repository = null;

    private readonly object _syncLock = new();
    
    protected ConcurrentDictionary<string, FileStatus> Changelist { get; set; } = new();
    protected RemoteCollection? Remotes { get; set; } = null;

    private ObservableCollection<string> _branchNames = new();

    private bool _initializedState = false;

    protected Timer? GitRepoStatus = null;
    
    [Signal] public delegate void ChangelistChangedEventHandler(GdCollections.Dictionary<string, FileStatus> filesAdded, GdCollections.Dictionary<string, FileStatus> filesDeleted);
    
    [Signal] public delegate void BranchesChangedEventHandler();

    public override void _Ready()
    {
        if (RepositoryPath != "")
        {
            OpenRepo(RepositoryPath);
        }
    }

    public override void _ExitTree()
    {
        
    }

    public bool OpenRepo(string repoPath)
    {
        if (GitRepoStatus != null)
        {
            RemoveChild(GitRepoStatus);
            GitRepoStatus.QueueFree();
            GitRepoStatus = null;
        }

        if (!Repository.IsValid(repoPath))
        {
            return false;
        }
        
        _repository = new Repository(repoPath);
        
        Initialize();

        return true;
    }

    public void CloseRepo()
    {
        if (_repository != null)
        {
            _repository = null;
        }
    }

    public ConcurrentDictionary<string, FileStatus> GetChangelist()
    {
        return Changelist;
    }

    public bool IsInitialized()
    {
        return _initializedState;
    }

    public BranchCollection GetAllBranches()
    {
        return _repository.Branches;
    }

    public RemoteCollection GetRemotes()
    {
        return _repository.Network.Remotes;
    }

    public object GetSynchro()
    {
        return _syncLock;
    }

    /// <summary>
    /// Fetches `remoteBranchName` from `remoteName` into `FETCH_HEAD` branch.
    /// This call is threaded.
    /// </summary>
    /// <param name="remoteName"></param>
    /// <param name="remoteBranchName"></param>
    /// <param name="cbTransferProgress">Called on a separate thread</param>
    /// <param name="cbProgress">Called on a separate thread. Can return false if the desire is to cancel the fetch. Signature: <code>bool Callable(string consoleOut)</code></param>
    /// <param name="cbDone">Called on main thread</param>
    public void Fetch(string remoteName, string remoteBranchName, Callable? cbTransferProgress = null, Callable? cbProgress = null, Callable? cbDone = null)
    {
        if (_repository == null)
        {
            return;
        }
        
        Task.Run(() =>
        {
            FetchOptions fetchOptions = new FetchOptions
            {
                OnProgress = output =>
                {
                    if (cbProgress != null)
                    {
                        Variant response = cbProgress.Value.Call(output);
                        if (response.IsValid())
                        {
                            bool resp = response.AsBool();
                            return resp;
                        }
                    }

                    return true;
                },
                OnTransferProgress = progress =>
                {
                    if (cbTransferProgress != null)
                    {
                        Variant response = cbTransferProgress.Value.Call(progress.IndexedObjects, progress.ReceivedObjects, progress.ReceivedBytes, progress.TotalObjects);
                        if (response.IsValid())
                        {
                            bool resp = response.AsBool();
                            return resp;
                        }
                    }

                    return true;
                }
            };
        
            string[] refSpecs = { $"+refs/heads/{remoteBranchName}:refs/remotes/{remoteName}/{remoteBranchName}" };
            GitCmds.Fetch(_repository, remoteName, refSpecs, fetchOptions, null);
            
            cbDone?.CallDeferred();
        });
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="fromBranchName"></param>
    /// <param name="toBranchName"></param>
    /// <param name="signatureName"></param>
    /// <param name="signatureEmail"></param>
    /// <param name="cbProgress">Called on a separate thread. Signature: <code>void Callable(string path, int steps, int totalSteps)</code></param>
    /// <param name="cbDone">Called on main thread when action is completed. Signature: <code>void Callable(bool successful)</code></param>
    public void Merge(string fromBranchName, string toBranchName, string signatureName, string signatureEmail, Callable? cbProgress = null, Callable? cbDone = null)
    {
        if (_repository == null)
        {
            cbDone?.Call(false);
            return;
        }
        
        Task.Run(() =>
        {
            // Check if we have changes, if we do then we have to abort
            if (HasChanges(_repository))
            {
                cbDone?.CallDeferred(false);
            }
            
            // We first have to check out to the right branch
            CheckoutOptions checkoutOpts = new CheckoutOptions
            {
                OnCheckoutProgress = (path, steps, totalSteps) =>
                {
                    cbProgress?.CallDeferred(path, steps, totalSteps);
                }
            };
            GitCmds.Checkout(_repository, toBranchName, checkoutOpts);

            // Try find the branch we need to pull from and then merge
            var signature = new Signature(new Identity(signatureName, signatureEmail), DateTimeOffset.Now);
            Branch? branch = _repository.Branches[fromBranchName];
            if (branch != null)
            {
                MergeOptions mergeOpts = new MergeOptions
                {
                    CommitOnSuccess = false,
                    FailOnConflict = true,
                    OnCheckoutProgress = (path, steps, totalSteps) =>
                    {
                        cbProgress?.CallDeferred(path, steps, totalSteps);
                    }
                };

                bool successful = true;
                MergeResult mergeResult = _repository.Merge(branch, signature, mergeOpts);
                if (mergeResult.Commit == null)
                {
                    successful = false;
                }

                cbDone?.CallDeferred(successful);
            }
        });
    }

    protected void Initialize()
    {
        if (_repository == null)
        {
            return;
        }
        
        Task.Run(() =>
        {
            lock (GetSynchro())
            {
                Changelist = new ConcurrentDictionary<string, FileStatus>(GetChanges(_repository));
                
                _branchNames.Clear();
                foreach (var branch in _repository.Branches)
                {
                    _branchNames.Add(branch.FriendlyName);
                }
                
                CallDeferred(MethodName.OnInitialized);
            }
        });
    }

    protected void OnInitialized()
    {
        lock (GetSynchro())
        {
            GitRepoStatus = new Timer();
            GitRepoStatus.WaitTime = 5;
            GitRepoStatus.OneShot = false;
            GitRepoStatus.Timeout += OnGitRepoStatusTimerTimeout;
            AddChild(GitRepoStatus);
            GitRepoStatus.Start();

            GdCollections.Dictionary<string, FileStatus> filesAdded = new(Changelist.ToDictionary());
            EmitSignal(SignalName.ChangelistChanged, filesAdded, new());

            _initializedState = true;
        }
    }
    
    protected List<string> ScrapeBranchNames(Repository repo)
    {
        List<string> list = new();

        foreach (var branch in repo.Branches)
        {
            list.Add(branch.FriendlyName);
        }

        return list;
    }

    protected void OnGitRepoStatusTimerTimeout()
    {
        Task.Run(() =>
        {
            // We attempt to lock here, if we cannot lock for what ever reason it isn't the end of the world, we can just spin until we are free
            lock (GetSynchro())
            {
                if (_repository == null)
                {
                    return;
                }

                // Fetch the files that are changed within the repository, compare them to the previous time we checked, spit out
                // the difference between the two and notify the listeners.
                Dictionary<string, FileStatus> filesChanged = GetChanges(_repository);

                Dictionary<string, FileStatus> dictDel = new();
                Dictionary<string, FileStatus> dictAdd = new();
                Utilities.DiffDictionaries(filesChanged, Changelist, dictAdd, dictDel);

                if (dictDel.Count > 0 || dictAdd.Count > 0)
                {
                    Changelist = new ConcurrentDictionary<string, FileStatus>(filesChanged);

                    // Convert the output to a Godot centric type
                    GdCollections.Dictionary<string, FileStatus> filesAdded = new(dictDel);
                    GdCollections.Dictionary<string, FileStatus> filesDeleted = new(dictAdd);

                    CallDeferred(GodotObject.MethodName.EmitSignal, SignalName.ChangelistChanged, filesAdded, filesDeleted);
                }
                
                // Check to see if branch list has changed
                bool branchesChanged = false;
                foreach (var branch in _repository.Branches)
                {
                    if (!_branchNames.Contains(branch.FriendlyName))
                    {
                        _branchNames.Add(branch.FriendlyName);
                        branchesChanged = true;
                    }
                }

                foreach (var branchName in _branchNames.ToList())
                {
                    if (_repository.Branches.Any(branch => branch.FriendlyName == branchName))
                    {
                        _branchNames.Remove(branchName);
                        branchesChanged = true;
                    }
                }

                if (branchesChanged)
                {
                    CallDeferred(GodotObject.MethodName.EmitSignal, SignalName.BranchesChanged);
                }
            }
        });
    }

    protected Dictionary<string, FileStatus> GetChanges(Repository repo)
    {
        Dictionary<string, FileStatus> results = new();

        StatusOptions statusOptions = new()
        {
            IncludeIgnored = false
        };

        foreach (StatusEntry? item in repo.RetrieveStatus(statusOptions))
        {
            if (item == null)
            {
                continue;
            }

            results[item.FilePath] = item.State;
        }

        return results;
    }

    protected bool HasChanges(Repository repo)
    {
        StatusOptions statusOptions = new()
        {
            IncludeIgnored = false
        };
        
        foreach (StatusEntry? item in repo.RetrieveStatus(statusOptions))
        {
            switch (item.State)
            {
                case FileStatus.Unaltered:
                case FileStatus.Ignored:
                    continue;
                default:
                    return false;
            }
        }

        return true;
    }
}
