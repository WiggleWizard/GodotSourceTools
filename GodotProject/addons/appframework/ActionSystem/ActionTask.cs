using Godot;
using Godot.Collections;

namespace GodotAppFramework.ActionSystem;

[GlobalClass]
public partial class ActionTask : Resource
{
    [Export] public Array<NodePath> Nodes { set; get; } = new();

    public void Execute(Node executor)
    {
        MultiExecute(executor, Nodes);
    }

    public virtual void MultiExecute(Node executor, Array<NodePath> onNodes) {}
}