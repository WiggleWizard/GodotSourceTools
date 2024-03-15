using Godot;
using GodotAppFramework;
using GodotAppFramework.UI;

[Tool]
public partial class LargeScrollableBuildStatus : LargeScrollableTextBox
{
    private ThreadedProcess? _threadedProcess = null;
    
    public override string GetLine(int lineNumber)
    {
        if (_threadedProcess == null || lineNumber >= _threadedProcess.StandardOut.Count)
        {
            return "";
        }

        ThreadedProcessTimestampedOut t = _threadedProcess.StandardOut[lineNumber];
        return $"[{t.DT:MM/dd/yy HH:mm:ss}] {t.Line}";
    }

    public override int GetLineCount()
    {
        if (_threadedProcess == null)
            return 0;
        return _threadedProcess.StandardOut.Count;
    }

    public void SetThreadedProcess(ThreadedProcess? tp)
    {
        _threadedProcess = tp;
    }
}