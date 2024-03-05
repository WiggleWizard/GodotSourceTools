using Godot;

namespace GodotAppFramework.UI;

[Tool, GlobalClass]
public partial class LoadingScreen : Control
{
    [Export] public AnimationTree AnimationTree { get; set; }
    [Export] public string ExitCondition = "done";

    [Signal] public delegate void ExitTransitionCompletedEventHandler();

    public void OnExitTransitionCompleted()
    {
        EmitSignal(SignalName.ExitTransitionCompleted);
    }

    public void PlayExitAnimation()
    {
        AnimationTree.Set($"parameters/conditions/{ExitCondition}", true);
    }
}