using Godot;

using GodotAppFramework;
using GodotAppFramework.UI;
using GodotSourceTools;

public partial class BuildEntry : ItemListCustomItem
{
    [Export] public Label TitleNode { get; set; }
    [Export] public Label TimeNode { get; set; }

    public ThreadedProcess ThreadedProcess { get; set; }

    private Timer _timer = new Timer();

    public override void _Ready()
    {
        AddChild(_timer);
        _timer.Timeout += TimerTimeout;
        _timer.OneShot = false;
        _timer.Start(1);
    }

    public void TimerTimeout()
    {
        var timeElapsed = StringFormatting.GetReadableTimespan(ThreadedProcess.TimeRunning());
        if (ThreadedProcess.IsRunning())
        {
            TimeNode.Text = $"Elapsed: {timeElapsed}";
        }
        else
        {
            TimeNode.Text = $"Completed in: {timeElapsed}";
            _timer.Stop();
        }
    }
}
