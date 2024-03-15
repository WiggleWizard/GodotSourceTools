using Godot;
using Godot.Collections;

using GodotAppFramework;

public enum BuildManagerStatus
{
    Idle, Building, Cleaning
}

public partial class BuildManager : Node
{
    private static BuildManager? _instance = null;

    private ThreadedProcess? _currentlyExecutingThreadedProcess = null;
    private BuildManagerStatus _status = BuildManagerStatus.Idle;
    
    [Signal] public delegate void StatusChangedEventHandler(BuildManagerStatus newStatus, ThreadedProcess? tp);
    
    public override void _Ready()
    {
        _instance = this;
    }

    public static BuildManager? GetInstance()
    {
        return _instance;
    }

    public ThreadedProcess? StartBuild(string workingDirectory, string execFile, Array<string> args)
    {
        // Ensure there is no currently active build
        if (GetActiveBuild() != null)
            return null;
        
        ThreadedProcess tp = new(workingDirectory, execFile, args);
        AddChild(tp);
        tp.Owner = this;
        tp.OnCompleted += OnBuildCompleted;
        tp.Start();

        SetCurrentlyExecutingThreadedProcessAndStatus(tp, BuildManagerStatus.Building);

        return tp;
    }

    public ThreadedProcess? StartClean(string workingDirectory, string execFile)
    {
        // Ensure there is no currently active build
        if (GetActiveBuild() != null)
            return null;
        
        ThreadedProcess tp = new(workingDirectory, execFile, new() { "--clean" });
        AddChild(tp);
        tp.Owner = this;
        tp.OnCompleted += OnCleanCompleted;
        tp.Start();

        SetCurrentlyExecutingThreadedProcessAndStatus(tp, BuildManagerStatus.Cleaning);

        return tp;
    }

    public ThreadedProcess? GetActiveBuild()
    {
        ThreadedProcess? tp = null;
        
        // Iterate through all children nodes to see which one is currently running/executing
        foreach (Node node in GetChildren())
        {
            if (node is ThreadedProcess tpChild && tpChild.IsRunning())
            {
                tp = tpChild;
                break;
            }
        }

        return tp;
    }
    
    public Array<ThreadedProcess> GetAllBuilds()
    {
        Array<ThreadedProcess> builds = new();
        return builds;
    }

    public ThreadedProcess? GetBuild(int index)
    {
        return (ThreadedProcess)GetChildren()[index];
    }

    public void TerminateCurrentBuild()
    {
        ThreadedProcess? currentBuild = GetActiveBuild();
        if (currentBuild != null)
        {
            currentBuild.Terminate();
        }
    }

    private void SetCurrentlyExecutingThreadedProcessAndStatus(ThreadedProcess? newThreadedProcess, BuildManagerStatus newStatus)
    {
        _currentlyExecutingThreadedProcess = newThreadedProcess;
        _status = newStatus;
        
        EmitSignal(SignalName.StatusChanged, (int)_status, _currentlyExecutingThreadedProcess!);
    }

    private void OnBuildCompleted()
    {
        SetCurrentlyExecutingThreadedProcessAndStatus(null, BuildManagerStatus.Idle);
    }

    private void OnCleanCompleted()
    {
        SetCurrentlyExecutingThreadedProcessAndStatus(null, BuildManagerStatus.Idle);
    }
}