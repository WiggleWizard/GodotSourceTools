using GodotAppFramework;

using Godot;
using GdCollections = Godot.Collections;

using LibGit2Sharp;

using System.Security.Cryptography;
using System;
using System.Linq;

public partial class MainTabSourceControl : MainTabBase
{
    [Export] public Control ChangeListControl { get; set; } = null!;

    [Export] public Button ButtonAddUpstream { get; set; } = null!;
    [Export] public Button ButtonFetchAndMerge { get; set; } = null!;
    [Export] public Button ButtonRebaseChanges { get; set; } = null!;
    
    [Export] public LineEdit FetchAndMergeRemoteBranchName { get; set; } = null!;
    [Export] public OptionButton FetchAndMergeLocalBranchName { get; set; } = null!;
    [Export] public GdCollections.Array<OptionButton> LocalBranchSelectors { get; set; } = new();

    public override void _Ready()
    {
        var sourceManager = SourceManager.GetInstance();
        if (sourceManager != null)
        {
            sourceManager.NewSourceLoaded += OnNewSourceLoaded;

            if (sourceManager.CurrentSourceDir != "")
            {
                OnNewSourceLoaded(sourceManager.CurrentSourceDir);
            }

            ButtonFetchAndMerge.Pressed += OnFetchAndMergePressed;
        }
    }

    public void OnChangelistChanged(GdCollections.Dictionary<string, FileStatus> filesAdded, GdCollections.Dictionary<string, FileStatus> filesDeleted)
    {
        foreach (var file in filesAdded)
        {
            string uid = CreateMD5(file.Key);
            
            Label newLabel = new();
            newLabel.Text = file.Key;
            newLabel.Name = uid;
            ChangeListControl.AddChild(newLabel);
        }

        foreach (var file in filesDeleted)
        {
            string uid = CreateMD5(file.Key);
            
            Node? n = ChangeListControl.FindChild(uid);
            if (n != null)
            {
                n.QueueFree();
            }
        }
    }

    private void OnNewSourceLoaded(string dirpath)
    {
        SourceManager? sourceManager = SourceManager.GetInstance();
        if (sourceManager == null)
        {
            return;
        }
        
        GitRepo? repo = sourceManager.GetRepo();
        if (repo == null)
        {
            return;
        }

        // If the repository has been initialized, then we need to gather the "changes" ourselves as it we would be too
        // late for the change hook.
        if (repo.IsInitialized())
        {
            GdCollections.Dictionary<string, FileStatus> filesAdded = new(repo.GetChangelist().ToDictionary());
            OnChangelistChanged(filesAdded, new());
        }

        repo.ChangelistChanged += OnChangelistChanged;

        // Check if we have an upstream
        var remotes = repo.GetRemotes();
        Remote? upstreamRemote = remotes?["upstream"];
        if (upstreamRemote == null)
        {
            
        }

        FillLocalBranchSelectors();
    }
    
    public static string CreateMD5(string input)
    {
        // Use input string to calculate MD5 hash
        using (MD5 md5 = MD5.Create())
        {
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            return Convert.ToHexString(hashBytes);
        }
    }

    public void OnAddUpstreamPressed()
    {
        
    }

    public void OnFetchAndMergePressed()
    {
        GitRepo? repo = GetRepo();
        if (repo == null)
        {
            return;
        }

        var onProgressCb = Callable.From((string s) =>
        {
            CallDeferred(MethodName.OnFetchProgress, s);
        });
        
        var onTransferProgressCb = Callable.From((int indexedObjects, int receivedObjects, int receivedBytes, int totalObjects) =>
        {
            CallDeferred(MethodName.OnFetchTransferProgress, indexedObjects);
        });

        if (IsInstanceValid(FetchAndMergeRemoteBranchName) && IsInstanceValid(FetchAndMergeLocalBranchName))
        {
            repo.Fetch("upstream", FetchAndMergeRemoteBranchName.Text, onTransferProgressCb, onProgressCb, Callable.From(() =>
            {
                var fromRemoteBranch = FetchAndMergeRemoteBranchName.Text;
                var toLocalBranch = FetchAndMergeLocalBranchName.GetItemText(FetchAndMergeLocalBranchName.Selected);
                repo.Merge("upstream/" + fromRemoteBranch, toLocalBranch, "WiggleWizard", "");
            }));
        }
    }

    public void OnRebaseChangesPressed()
    {
        
    }

    protected void FillLocalBranchSelectors()
    {
        GitRepo? repo = GetRepo();
        if (repo == null)
        {
            return;
        }

        lock (repo.GetSynchro())
        {
            var branches = repo.GetAllBranches();
            if (branches != null)
            {
                foreach (var selector in LocalBranchSelectors)
                {
                    foreach (var branch in branches)
                    {
                        if (!branch.IsRemote)
                        {
                            selector.AddItem(branch.FriendlyName);
                        }
                    }
                }
            }
        }
    }

    private GitRepo? GetRepo()
    {
        SourceManager? sourceManager = SourceManager.GetInstance();
        if (sourceManager == null)
        {
            return null;
        }
        
        return sourceManager.GetRepo();
    }

    private void OnFetchProgress(string s)
    {
        GD.Print(s);
    }
    
    private void OnFetchTransferProgress(int indexedObjects, int receivedObjects, int receivedBytes, int totalObjects)
    {
        GD.Print(indexedObjects);
    }

    private void OnFetchAndMergeCompleted(bool successful)
    {
        
    }
}
