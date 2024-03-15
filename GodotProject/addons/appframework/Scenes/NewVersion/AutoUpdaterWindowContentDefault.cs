using GodotAppFramework.Extensions;

using System;

using Godot;

namespace GodotAppFramework;

public partial class AutoUpdaterWindowContentDefault : AutoUpdaterWindowContent
{
    [Export] public Label WindowBodyText { get; set; } = null!;
    [Export] public RichTextLabel ChangeLogText { get; set; } = null!;
    
    [Export] public Button ButtonInstall { get; set; } = null!;
    [Export] public Button ButtonDownload { get; set; } = null!;
    [Export] public Button ButtonCancel { get; set; } = null!;
    
    [Export] public ProgressBar DownloadProgress { get; set; } = null!;

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

        if (IsInstanceValid(WindowBodyText))
        {
            WindowBodyText.Text = WindowBodyText.Text.Templated(templateArgs);
        }

        if (IsInstanceValid(ChangeLogText))
        {
            ChangeLogText.Text = versionInfo.ChangeLog;
        }
        
        AutoUpdaterManager? autoUpdaterManager = AutoUpdaterManager.GetInstance();
        if (autoUpdaterManager == null)
        {
            return;
        }

        if (IsInstanceValid(ButtonInstall))
        {
            ButtonInstall.Pressed += () =>
            {
                autoUpdaterManager.UnattendedUpdate(versionInfo);
            };
        }

        if (IsInstanceValid(ButtonDownload))
        {
            ButtonDownload.Pressed += () =>
            {
                OS.ShellOpen(versionInfo.LinkToDownloadPage);
            };
        }

        if (IsInstanceValid(ButtonCancel))
        {
            ButtonCancel.Pressed += () =>
            {
                autoUpdaterManager.IgnoreUpdate(versionInfo);
            };
        }
        
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