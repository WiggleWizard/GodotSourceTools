using GodotAppFramework.Serializers.Github;

using Godot;

namespace GodotAppFramework;

public partial class AutoUpdaterWindowContentDefault : AutoUpdaterWindowContent
{
    [Export] public Label WindowBodyText { get; set; }
    [Export] public RichTextLabel ChangeLogText { get; set; }
    
    [Export] public Button ButtonInstall { get; set; }
    [Export] public Button ButtonDownload { get; set; }
    [Export] public Button ButtonCancel { get; set; }

    public override void Initialize(JsonGithubReleaseEntry releaseInfo)
    {
        AppFrameworkManager? appFrameworkManager = AppFrameworkManager.GetInstance();
        if (appFrameworkManager == null)
        {
            return;
        }
        
        var appName = AppFrameworkManager.GetAppName();
        var newVersion = releaseInfo.GetVersionStr();
        var currentVersion = AppFrameworkManager.GetAppVersion();
        var changeLog = releaseInfo.Body;
        
        WindowBodyText.Text = WindowBodyText.Text.Templated(new { appName, newVersion, currentVersion });
        ChangeLogText.Text = changeLog;
        
        AutoUpdaterManager? autoUpdaterManager = AutoUpdaterManager.GetInstance();
        if (autoUpdaterManager == null)
        {
            return;
        }
        
        ButtonInstall.Pressed += () =>
        {
            autoUpdaterManager.UnattendedUpdate();
        };

        ButtonDownload.Pressed += () =>
        {
            OS.ShellOpen(releaseInfo.Html_Url);
        };

        ButtonCancel.Pressed += () =>
        {
            autoUpdaterManager.IgnoreUpdate(releaseInfo);
        };
    }
}