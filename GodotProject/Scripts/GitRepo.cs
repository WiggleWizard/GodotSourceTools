using Godot;
using GdCollections = Godot.Collections;

using LibGit2Sharp;

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GodotAppFramework;

[GlobalClass]
public partial class GitRepo : Node
{
    [Export] private string RepositoryPath { get; set; } = "";
    
    private Repository? _repository { set; get; } = null;

    protected Mutex RepoMutex = new();
    
    protected ConcurrentDictionary<string, FileStatus> Changelist { get; set; } = new();
    protected RemoteCollection? Remotes { get; set; } = null;

    private bool _initializedState = false;

    protected Timer? GitRepoStatus = null;
    
    [Signal] public delegate void ChangelistChangedEventHandler(GdCollections.Dictionary<string, FileStatus> filesAdded, GdCollections.Dictionary<string, FileStatus> filesDeleted);

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

    public List<Branch> GetLocalBranches()
    {
        List<Branch> list = new();

        foreach (var branch in GetAllBranches())
        {
            if (!branch.IsRemote)
            {
                list.Add(branch);
            }
        }

        return list;
    }

    public List<Branch> GetRemoteBranches()
    {
        List<Branch> list = new();

        foreach (var branch in GetAllBranches())
        {
            if (branch.IsRemote)
            {
                list.Add(branch);
            }
        }

        return list;
    }

    public RemoteCollection GetRemotes()
    {
        return _repository.Network.Remotes;
    }

    protected void Initialize()
    {
        if (_repository == null)
        {
            return;
        }
        
        Task.Run(() =>
        {
            RepoMutex.Lock();
            
            Changelist = new ConcurrentDictionary<string, FileStatus>(GetChanges(_repository));
            CallDeferred(MethodName.OnInitialized);
            
            RepoMutex.Unlock();
        });
    }

    protected void OnInitialized()
    {
        GitRepoStatus = new Timer();
        GitRepoStatus.WaitTime = 5;
        GitRepoStatus.OneShot = false;
        GitRepoStatus.Timeout += () => { Task.Run(OnGitRepoStatusTimerTimeout); };
        AddChild(GitRepoStatus);
        GitRepoStatus.Start();

        GdCollections.Dictionary<string, FileStatus> filesAdded = new(Changelist.ToDictionary());
        EmitSignal(SignalName.ChangelistChanged, filesAdded, new());

        _initializedState = true;
    }

    protected void OnGitRepoStatusTimerTimeout()
    {
        if (!RepoMutex.TryLock())
        {
            return;
        }

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
        
        RepoMutex.Unlock();
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
}
