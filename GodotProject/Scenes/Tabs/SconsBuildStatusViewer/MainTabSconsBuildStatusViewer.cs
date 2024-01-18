using System.Linq;
using Godot;
using GodotAppFramework;
using GodotAppFramework.UI;

public partial class MainTabSconsBuildStatusViewer : MainTabBase
{
    [Export] public ItemListCustom BuildListContainer { set; get; }
    [Export] public TextEdit BuildArgsDisplay { get; set; }
    [Export] public LargeScrollableBuildStatus ScrollableTextBox { set; get; }
    
    [Export] public PackedScene BuildEntryScene { get; set; }
    
    public override void _Ready()
    {
        VisibilityChanged += () =>
        {
            if (BuildListContainer.GetCurrentlySelectedItem() == null)
            {
                BuildListContainer.SetCurrentlySelectedItem(0);
            }
        };
        
        BuildManager buildManager = BuildManager.GetInstance();
        buildManager.StatusChanged += OnBuildManagerStateChanged;

        BuildListContainer.ItemSelected += (item) =>
        {
            if (item is BuildEntry entry)
            {
                ScrollableTextBox.SetThreadedProcess(entry.ThreadedProcess);
                BuildArgsDisplay.Text = entry.ThreadedProcess.ExecFile + " " + string.Join(" ", entry.ThreadedProcess.Args);
            }
        };
    }

    private void OnBuildManagerStateChanged(BuildManagerStatus status, ThreadedProcess tp)
    {
        int itemIdx = -1;
        
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
