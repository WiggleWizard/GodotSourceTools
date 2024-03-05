using Godot;
using System;

public partial class MainTabSourceControl : MainTabBase
{
    [Export] public Control ChangeListControl;

    public override void _Ready()
    {
        var sourceManager = SourceManager.GetInstance();
        sourceManager.NewSourceLoaded += OnNewSourceLoaded;

        if (sourceManager.CurrentSourceDir != "")
        {
            OnNewSourceLoaded(sourceManager.CurrentSourceDir);
        }
    }

    private void OnNewSourceLoaded(string dirpath)
    {
    }
}
