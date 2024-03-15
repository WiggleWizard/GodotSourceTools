using Godot;

namespace GodotAppFramework.UI;

[Tool, GlobalClass]
public partial class LoadingScreen : Control
{
    [Export] public AnimationTree AnimationTree { get; set; } = null!;
    [Export] public string ExitCondition = "done";

    [Signal] public delegate void ExitTransitionCompletedEventHandler();

    public void OnExitTransitionCompleted()
    {
        EmitSignal(SignalName.ExitTransitionCompleted);
    }

    public void PlayExitAnimation()
    {
        if (!IsInstanceValid(AnimationTree))
        {
            return;
        }
        
        AnimationTree.Set($"parameters/conditions/{ExitCondition}", true);
    }
}