using Godot;

using GodotAppFramework.ActionSystem;

[GlobalClass]
public partial class ActionCueBuildStatusChanged : ActionCue
{
    [Export] public BuildManagerStatus OnStatusChangedTo;

    public override void OnExecutorReady(Node executor)
    {
        var buildManager = BuildManager.GetInstance();
        if (buildManager != null)
        {
            buildManager.StatusChanged += (status, tp) =>
            {
                if (status == OnStatusChangedTo)
                {
                    Trigger();
                }
            };
        }
    }
}
