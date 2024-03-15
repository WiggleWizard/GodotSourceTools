using System.Linq;
using Godot;
using GodotAppFramework;
using GodotAppFramework.UI;

public partial class MainTabSconsBuildStatusViewer : MainTabBase
{
    [Export] public ItemListCustom BuildListContainer { set; get; } = null!;
    [Export] public TextEdit BuildArgsDisplay { get; set; } = null!;
    [Export] public LargeScrollableBuildStatus ScrollableTextBox { set; get; } = null!;
    
    [Export] public PackedScene BuildEntryScene { get; set; } = null!;
    
    public override void _Ready()
    {
        VisibilityChanged += () =>
        {
            if (BuildListContainer.GetCurrentlySelectedItem() == null)
            {
                BuildListContainer.SetCurrentlySelectedItem(0);
            }
        };
        
        var buildManager = BuildManager.GetInstance();
        if (buildManager != null)
        {
            buildManager.StatusChanged += OnBuildManagerStateChanged;
        }

        BuildListContainer.ItemSelected += (item) =>
        {
            if (item is BuildEntry entry)
            {
                if (entry.ThreadedProcess != null)
                {
                    ScrollableTextBox.SetThreadedProcess(entry.ThreadedProcess);
                    BuildArgsDisplay.Text = entry.ThreadedProcess.ExecFile + " " + string.Join(" ", entry.ThreadedProcess.Args);
                }
            }
        };
    }

    private void OnBuildManagerStateChanged(BuildManagerStatus status, ThreadedProcess? tp)
    {
        if (status == BuildManagerStatus.Building)
        {
            var newEntry = BuildListContainer.AddItem<BuildEntry>(BuildEntryScene);
            newEntry.ThreadedProcess = tp;
        }
        else if (status == BuildManagerStatus.Cleaning)
        {
            var newEntry = BuildListContainer.AddItem<BuildEntry>(BuildEntryScene);
            newEntry.ThreadedProcess = tp;
        }
    }
}
