using Godot;

using System;
using System.Collections.Concurrent;

namespace GodotSourceTools;

public partial class StatusManager : Node
{
    public static StatusManager? Instance { get; private set; } = null;
    
    public ConcurrentDictionary<Guid, string> StatusStack { get; private set; } = new();

    [Signal] public delegate void StatusLockedEventHandler(string guid, string statusName);
    [Signal] public delegate void StatusUnlockedEventHandler(string guid);

    public override void _Ready()
    {
        Instance = this;
    }

    public Guid QueueStatus(string statusName)
    {
        Guid guid = Guid.NewGuid();
        if (!StatusStack.TryAdd(guid, statusName))
        {
            guid = QueueStatus(statusName);
            CallDeferred(GodotObject.MethodName.EmitSignal, SignalName.StatusLocked, guid.ToString("N"), statusName);
        }
        
        return guid;
    }

    public void DequeueStatus(Guid? guid)
    {
        if (guid == null)
        {
            return;
        }
        
        if (StatusStack.ContainsKey(guid.Value))
        {
            StatusStack.TryRemove(guid.Value, out var v);
            CallDeferred(GodotObject.MethodName.EmitSignal, SignalName.StatusUnlocked, guid.Value.ToString("N"));
        }
    }
}