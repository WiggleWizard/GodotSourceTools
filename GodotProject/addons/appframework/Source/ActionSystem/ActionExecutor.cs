using Godot;
using Godot.Collections;

namespace GodotAppFramework.ActionSystem;

[GlobalClass]
public partial class ActionExecutor : Node
{
    [Export] public Array<Action> Actions = new();

    public override void _Ready()
    {
        foreach (Action action in Actions)
        {
            action.ExecutorReady(this);
        }
    }
}
