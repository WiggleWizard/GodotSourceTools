using Godot;
using GdCollections = Godot.Collections;

using LibGit2Sharp;

using System.Security.Cryptography;
using System;
using System.Linq;
using GodotAppFramework;

public partial class MainTabSourceControl : MainTabBase
{
    [Export] public Control ChangeListControl;

    [Export] public Button ButtonAddUpstream { get; set; }
    [Export] public Button ButtonFetchAndMerge { get; set; }
    [Export] public Button ButtonRebaseChanges { get; set; }
    
    [Export] public GdCollections.Array<OptionButton> RemoteBranchSelectors { get; set; }
    [Export] public GdCollections.Array<OptionButton> LocalBranchSelectors { get; set; }

    public override void _Ready()
    {
        var sourceManager = SourceManager.GetInstance();
        sourceManager.NewSourceLoaded += OnNewSourceLoaded;

        if (sourceManager.CurrentSourceDir != "")
        {
            OnNewSourceLoaded(sourceManager.CurrentSourceDir);
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
        Remote? upstreamRemote = remotes["upstream"];
        if (upstreamRemote == null)
        {
            
        }

        FillLocalBranchSelectors();
        FillRemoteBranchSelectors();
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
        
    }

    public void OnRebaseChanges()
    {
        
    }

    protected void FillLocalBranchSelectors()
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

        var branches = repo.GetLocalBranches();
        foreach (var selector in LocalBranchSelectors)
        {
            foreach (var branch in branches)
            {
                selector.AddItem(branch.FriendlyName);
            }
        }
    }

    protected void FillRemoteBranchSelectors()
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

        var branches = repo.GetRemoteBranches();
        foreach (var selector in RemoteBranchSelectors)
        {
            foreach (var branch in branches)
            {
                selector.AddItem(branch.FriendlyName);
            }
        }
    }
}
