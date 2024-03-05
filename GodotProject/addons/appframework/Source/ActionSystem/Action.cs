using Godot;
using Godot.Collections;

namespace GodotAppFramework.ActionSystem;

[GlobalClass]
public partial class Action : Resource
{
    [Export] public Array<ActionCue> Cues { set; get; } = new();
    [Export] public Array<ActionTask> Tasks { set; get; } = new();

    public void ExecutorReady(Node executor)
    {
        foreach (ActionCue cue in Cues)
        {
            cue.ExecutorReady(executor);
            cue.Executed += () =>
            {
                foreach (ActionTask task in Tasks)
                {
                    task.Execute(executor);
                }
            };
        }
    }

    public virtual void OnExecutorReady(Node executor, Node parent){}
}
