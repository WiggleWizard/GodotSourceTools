using Godot;

using Microsoft.Toolkit.Uwp.Notifications;

public partial class ToastNotificationManager : Node
{
    private static ToastNotificationManager? _instance = null;
    
    public override void _Ready()
    {
        _instance = this;

        ToastNotificationManagerCompat.OnActivated += toastArgs =>
        {
            CallDeferred("OnNotificationActivated", toastArgs.Argument);
        };
    }

    public static ToastNotificationManager? GetInstance()
    {
        return _instance;
    }

    public void ShowSimpleNotification(string title, string desc, ToastDuration? expirationTime = null)
    {
        var builder = new ToastContentBuilder()
            .AddArgument("action", "bringToFront")
            .AddText(title)
            .AddText(desc);

        if (expirationTime != null)
            builder.SetToastDuration(expirationTime.GetValueOrDefault());

        builder.Show();
    }

    private void OnNotificationActivated(string action)
    {
        GetTree().Root.GrabFocus();
    }
}