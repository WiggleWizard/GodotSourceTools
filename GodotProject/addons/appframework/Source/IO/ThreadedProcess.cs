using Godot;
using Godot.Collections;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace GodotAppFramework;

public class ThreadedProcessTimestampedOut
{
    public ThreadedProcessTimestampedOut(string line)
    {
        Line = line;
    }
    
    public string Line;
    public DateTime DT { get; set; } = DateTime.Now;
}

[Tool]
public partial class ThreadedProcess : Node
{
    public string WorkingDirectory = "";
    public string ExecFile = "";
    public Array<string> Args = new();
    
    public DateTime TimeStarted { get; set; }
    public DateTime TimeCompleted { get; set; } = DateTime.MinValue;
    
    public List<ThreadedProcessTimestampedOut> StandardOut = new();
    public List<ThreadedProcessTimestampedOut> StandardError = new();

    private Process _proc = new();
    private Thread? _procThread = null;
    private bool _isRunning = false;

    [Signal] public delegate void OnCompletedEventHandler();
    //[Signal] public delegate void OnNewStandardOutTextEventHandler();

    public ThreadedProcess()
    {
        
    }
    
    public ThreadedProcess(string workingDirectory, string execFile, Array<string> args)
    {
        WorkingDirectory = workingDirectory;
        ExecFile = execFile;
        Args = args;
    }

    public void Start()
    {
        if (_procThread != null && _procThread.IsAlive)
        {
            Terminate();
        }

        StandardError.Clear();
        StandardOut.Clear();
        
        _procThread = new Thread(ThreadedStart);
        _procThread.Start();
        
        TimeStarted = DateTime.Now;
    }

    public void Terminate()
    {
        if (!_proc.HasExited)
        {
            _proc.Kill();
            _procThread?.Join();

            Exited();
        }
    }

    public void LockForRead()
    {
        Monitor.Enter(StandardError);
        Monitor.Enter(StandardOut);
    }

    public void UnlockForRead()
    {
        Monitor.Exit(StandardError);
        Monitor.Exit(StandardOut);
    }

    public bool IsRunning()
    {
        return _isRunning;
    }

    public TimeSpan TimeRunning()
    {
        if (IsRunning())
            return DateTime.Now - TimeStarted;

        return TimeCompleted - TimeStarted;
    }

    protected void ThreadedStart()
    {
        _isRunning = true;
        
        _proc = new Process();
        _proc.StartInfo.RedirectStandardOutput = true;
        _proc.OutputDataReceived += HandleProcessStdOut;
        _proc.ErrorDataReceived += HandleProcessStdErr;
        
        _proc.StartInfo.UseShellExecute = false;
        _proc.StartInfo.CreateNoWindow = true;
        _proc.StartInfo.RedirectStandardOutput = true;
        _proc.StartInfo.RedirectStandardError = true;

        _proc.StartInfo.WorkingDirectory = WorkingDirectory;
        _proc.StartInfo.FileName = ExecFile;
        _proc.StartInfo.Arguments = string.Join(" ", Args);

        _proc.Start();
        
        _proc.BeginOutputReadLine();
        _proc.BeginErrorReadLine();
        
        _proc.WaitForExit();

        Exited();
    }

    private void HandleProcessStdErr(object sender, DataReceivedEventArgs e)
    {
        if (String.IsNullOrEmpty(e.Data))
            return;

        Monitor.Enter(StandardError);
        StandardError.Add(new ThreadedProcessTimestampedOut(e.Data));
        Monitor.Exit(StandardError);
    }

    protected void HandleProcessStdOut(object sender, DataReceivedEventArgs e)
    {
        if (String.IsNullOrEmpty(e.Data))
            return;
        
        Monitor.Enter(StandardOut);
        StandardOut.Add(new ThreadedProcessTimestampedOut(e.Data));
        Monitor.Exit(StandardOut);
    }

    private void Exited()
    {
        TimeCompleted = DateTime.Now;
        
        _isRunning = false;
        CallDeferred(MethodName.OnCompletedDeferred);
    }

    private void OnCompletedDeferred()
    {
        EmitSignal(SignalName.OnCompleted);
    }
}