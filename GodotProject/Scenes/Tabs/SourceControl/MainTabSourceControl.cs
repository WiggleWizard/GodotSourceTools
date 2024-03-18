using GodotAppFramework;

using Godot;
using GdCollections = Godot.Collections;

using LibGit2Sharp;

using System.Security.Cryptography;
using System;
using System.Linq;
using GodotSourceTools;

[Config]
public partial class MainTabSourceControl : MainTabBase
{
    [Config] public static GdCollections.Dictionary LastBranchSelection { get; set; } = new();
    
    [Export] public Control ChangeListControl { get; set; } = null!;

    [Export] public Button ButtonAddUpstream { get; set; } = null!;
    [Export] public Button ButtonFetchAndMerge { get; set; } = null!;
    [Export] public Button ButtonRebaseChanges { get; set; } = null!;
    
    [Export] public LineEdit FetchAndMergeRemoteBranchName { get; set; } = null!;
    [Export] public OptionButton FetchAndMergeLocalBranchName { get; set; } = null!;
    [Export] public OptionButton RebaseFromBranchName { get; set; } = null!;
    [Export] public OptionButton RebaseToBranchName { get; set; } = null!;

    public override void _Ready()
    {
        var sourceManager = SourceManager.GetInstance();
        if (sourceManager == null)
        {
            return;
        }

        sourceManager.NewSourceLoaded += OnNewSourceLoaded;

        if (sourceManager.CurrentSourceDir != "")
        {
            OnNewSourceLoaded(sourceManager.CurrentSourceDir);
        }

        ButtonFetchAndMerge.Pressed += OnFetchAndMergePressed;
        ButtonRebaseChanges.Pressed += OnRebaseChangesPressed;

        if (IsInstanceValid(FetchAndMergeRemoteBranchName))
        {
            FetchAndMergeRemoteBranchName.TextChanged += text =>
            {
                CacheInfoForProject(nameof(FetchAndMergeRemoteBranchName), FetchAndMergeRemoteBranchName.Text);
            };
        }

        if (IsInstanceValid(FetchAndMergeLocalBranchName))
        {
            FetchAndMergeLocalBranchName.ItemSelected += index =>
            {
                CacheInfoForProject(nameof(FetchAndMergeLocalBranchName),
                    FetchAndMergeLocalBranchName.GetItemText((int)index));
            };
        }

        if (IsInstanceValid(RebaseFromBranchName))
        {
            RebaseFromBranchName.ItemSelected += index =>
            {
                CacheInfoForProject(nameof(RebaseFromBranchName), RebaseFromBranchName.GetItemText((int)index));
            };
        }

        if (IsInstanceValid(RebaseToBranchName))
        {
            RebaseToBranchName.ItemSelected += index =>
            {
                CacheInfoForProject(nameof(RebaseToBranchName), RebaseToBranchName.GetItemText((int)index));
            };
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
        
        // Massage the config data into the relevant selectors and line edits
        PushCachedValueToControl(FetchAndMergeRemoteBranchName, nameof(FetchAndMergeRemoteBranchName));
        PushCachedValueToControl(FetchAndMergeLocalBranchName, nameof(FetchAndMergeLocalBranchName));
        PushCachedValueToControl(RebaseFromBranchName, nameof(RebaseFromBranchName));
        PushCachedValueToControl(RebaseToBranchName, nameof(RebaseToBranchName));
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

        if (!IsInstanceValid(FetchAndMergeRemoteBranchName) || !IsInstanceValid(FetchAndMergeLocalBranchName))
        {
            return;
        }

        var mergeStatusGuid = StatusManager.Instance?.QueueStatus("Merge Rebasing");
        var cbMergeDone = Callable.From((bool successful) =>
        {
            StatusManager.Instance?.DequeueStatus(mergeStatusGuid);
        });

        var onProgressCb = Callable.From((string s) =>
        {
            CallDeferred(MethodName.OnFetchProgress, s);
        });
        
        var onTransferProgressCb = Callable.From((int indexedObjects, int receivedObjects, int receivedBytes, int totalObjects) =>
        {
            CallDeferred(MethodName.OnFetchTransferProgress, indexedObjects);
        });
        
        var fetchStatusGuid = StatusManager.Instance?.QueueStatus("Fetching");
        var cbFetchDone = Callable.From(() =>
        {
            StatusManager.Instance?.DequeueStatus(fetchStatusGuid);

            var fromRemoteBranch = FetchAndMergeRemoteBranchName.Text;
            var toLocalBranch = FetchAndMergeLocalBranchName.GetItemText(FetchAndMergeLocalBranchName.Selected);
            repo.Merge("upstream/" + fromRemoteBranch, toLocalBranch, SourceManager.GitSignatureUsername, SourceManager.GitSignatureEmail, null, cbMergeDone);
        });
        
        repo.Fetch("upstream", FetchAndMergeRemoteBranchName.Text, onTransferProgressCb, onProgressCb, cbFetchDone);
    }

    public void OnRebaseChangesPressed()
    {
        GitRepo? repo = GetRepo();
        if (repo == null)
        {
            return;
        }

        if (!IsInstanceValid(RebaseFromBranchName) || !IsInstanceValid(RebaseToBranchName))
        {
            return;
        }
        
        var rebaseStatusGuid = StatusManager.Instance?.QueueStatus("Rebasing");
        
        Callable cbProgress = Callable.From((int step, int totalSteps) =>
        {
            
        });
        
        Callable cbDone = Callable.From((bool successful) =>
        {
            GD.Print(successful);
            StatusManager.Instance?.DequeueStatus(rebaseStatusGuid);
        });

        string rebaseFromBranchStr = RebaseFromBranchName.GetItemText(RebaseFromBranchName.Selected);
        string rebaseToBranchStr = RebaseToBranchName.GetItemText(RebaseToBranchName.Selected);
        repo.Rebase(rebaseFromBranchStr, rebaseToBranchStr, SourceManager.GitSignatureUsername, SourceManager.GitSignatureEmail, cbProgress, cbDone);
    }

    protected void FillLocalBranchSelectors()
    {
        GitRepo? repo = GetRepo();
        if (repo == null)
        {
            return;
        }

        if (!IsInstanceValid(RebaseFromBranchName) || !IsInstanceValid(RebaseToBranchName) || !IsInstanceValid(FetchAndMergeLocalBranchName))
        {
            return;
        }

        lock (repo.GetSynchro())
        {
            var branches = repo.GetAllBranches();
            if (branches != null)
            {
                foreach (var branch in branches)
                {
                    if (!branch.IsRemote)
                    {
                        FetchAndMergeLocalBranchName.AddItem(branch.FriendlyName);
                        RebaseFromBranchName.AddItem(branch.FriendlyName);
                        RebaseToBranchName.AddItem(branch.FriendlyName);
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

    private void CacheInfoForProject(string controlName, string value)
    {
        var sourceManager = SourceManager.GetInstance();
        if (sourceManager == null)
        {
            return;
        }
        
        if (!LastBranchSelection.ContainsKey(sourceManager.CurrentSourceDir))
        {
            LastBranchSelection.Add(sourceManager.CurrentSourceDir, new GdCollections.Dictionary());
        }
        
        GdCollections.Dictionary branchNames = LastBranchSelection[sourceManager.CurrentSourceDir].AsGodotDictionary();
        branchNames[controlName] = value;
    }

    private void PushCachedValueToControl(Control control, string controlName)
    {
        var sourceManager = SourceManager.GetInstance();
        if (sourceManager == null)
        {
            return;
        }
        
        if (!LastBranchSelection.ContainsKey(sourceManager.CurrentSourceDir))
        {
            return;
        }
        GdCollections.Dictionary branchNames = LastBranchSelection[sourceManager.CurrentSourceDir].AsGodotDictionary();
        if (!branchNames.ContainsKey(controlName))
        {
            return;
        }
        
        string value = branchNames[controlName].AsString();
        
        if (control is LineEdit lineEdit)
        {
            lineEdit.Text = value;
        }
        else if (control is OptionButton optionButton)
        {
            for (int i = 0; i < optionButton.ItemCount; ++i)
            {
                if (optionButton.GetItemText(i) == value)
                {
                    optionButton.Select(i);
                    break;
                }
            }
        }
    }
}
