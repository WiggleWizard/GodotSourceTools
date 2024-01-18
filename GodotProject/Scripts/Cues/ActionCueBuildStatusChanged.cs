using Godot;

using GodotAppFramework.ActionSystem;

[GlobalClass]
public partial class ActionCueBuildStatusChanged : ActionCue
{
    [Export] public BuildManagerStatus OnStatusChangedTo;

    public override void OnExecutorReady(Node executor)
    {
        BuildManager buildManager = BuildManager.GetInstance();
        buildManager.StatusChanged += (status, tp) =>
        {
            if (status == OnStatusChangedTo)
            {
                Trigger();
            }
        };
    }
}
