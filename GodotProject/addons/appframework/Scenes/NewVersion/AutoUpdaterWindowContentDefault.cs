using GodotAppFramework.Extensions;

using System;

using Godot;

namespace GodotAppFramework;

public partial class AutoUpdaterWindowContentDefault : AutoUpdaterWindowContent
{
    [Export] public Label WindowBodyText { get; set; }
    [Export] public RichTextLabel ChangeLogText { get; set; }
    
    [Export] public Button ButtonInstall { get; set; }
    [Export] public Button ButtonDownload { get; set; }
    [Export] public Button ButtonCancel { get; set; }
    
    [Export] public ProgressBar DownloadProgress { get; set; }

    public override void Initialize(AppVersionInfo versionInfo)
    {
        AppFrameworkManager? appFrameworkManager = AppFrameworkManager.GetInstance();
        if (appFrameworkManager == null)
        {
            return;
        }

        TimeSpan age = (versionInfo.Time - DateTime.Now).Duration();
        
        var templateArgs = versionInfo.ToTemplatedArgs();
        templateArgs["AppName"] = AppFrameworkManager.GetAppName();
        templateArgs["CurrentVer"] = AppFrameworkManager.GetAppVersion().ToString();
        templateArgs["Age"] = StringFormatting.GetReadableTimespan(age);

        WindowBodyText.Text = WindowBodyText.Text.Templated(templateArgs);
        ChangeLogText.Text = versionInfo.ChangeLog;
        
        AutoUpdaterManager? autoUpdaterManager = AutoUpdaterManager.GetInstance();
        if (autoUpdaterManager == null)
        {
            return;
        }
        
        ButtonInstall.Pressed += () =>
        {
            autoUpdaterManager.UnattendedUpdate(versionInfo);
        };

        ButtonDownload.Pressed += () =>
        {
            OS.ShellOpen(versionInfo.LinkToDownloadPage);
        };

        ButtonCancel.Pressed += () =>
        {
            autoUpdaterManager.IgnoreUpdate(versionInfo);
        };

        autoUpdaterManager.UpdateStatusChanged += (info, status) =>
        {
            GD.Print(status);
        };

        autoUpdaterManager.DownloadProgress += (info, progress) =>
        {
            DownloadProgress.Value = progress;
        };
    }
}