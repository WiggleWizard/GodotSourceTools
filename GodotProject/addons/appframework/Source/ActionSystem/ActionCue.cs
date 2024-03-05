using Godot;

namespace GodotAppFramework.ActionSystem;

// A cue is something that tells the task to run
[GlobalClass]
public partial class ActionCue : Resource
{
    [Signal] public delegate void ExecutedEventHandler();

    public void ExecutorReady(Node executor)
    {
        OnExecutorReady(executor);
    }
    
    public virtual void OnExecutorReady(Node executor) {}

    public void Trigger()
    {
        EmitSignal(SignalName.Executed);
    }
}