using Godot;
using Godot.Collections;

namespace GodotAppFramework.ActionSystem;

[GlobalClass]
public partial class ActionTaskSetNodeProperty : ActionTask
{
    [Export] public Dictionary<StringName, Variant> Properties { set; get; }

    public override void MultiExecute(Node executor, Array<NodePath> onNodes)
    {
        foreach (var nodePath in onNodes)
        {
            foreach (var (key, value) in Properties)
            {
                Node n = executor.GetNode(nodePath);
                if (n == null)
                    continue;
                
                n.Set(key, value);
            }
        }
    }
}