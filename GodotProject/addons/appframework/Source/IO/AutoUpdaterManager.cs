using GodotAppFramework.Globals;
using GodotAppFramework.Serializers.Github;
using GodotAppFramework.Extensions;
using GodotAppFramework.System;

using Godot;
using GDCollections = Godot.Collections;
using GDFileAccess = Godot.FileAccess;

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

using CSHttpClient = System.Net.Http.HttpClient;
using CSHttpProgressMessageHandler = System.Net.Http.HttpMessageHandler;
using Environment = System.Environment;
using FileAccess = System.IO.FileAccess;

namespace GodotAppFramework;

public partial class AppVersionInfo : Resource 
{
    public Version Ver = new();
    public string ZipUrl = "";
    public DateTime Time = DateTime.MinValue;
    public string ChangeLog = "";
    public string LinkToDownloadPage = "";

    public Dictionary<BetterPlatformID, Tuple<string, string>> BinaryDownloads = new();

    public Dictionary<string, string> ToTemplatedArgs()
    {
        return new()
        {
            [nameof(Ver)] = Ver.ToString(),
            [nameof(ZipUrl)] = ZipUrl,
            [nameof(Time)] = Time.ToLongTimeString(),
            [nameof(ChangeLog)] = ChangeLog,
            [nameof(LinkToDownloadPage)] = LinkToDownloadPage
        };
    }
}

public enum UpdateStatus
{
    Downloading,
    Installing,
    Complete
}

[AFXProjectSetting(Constants.ProjectSettingsPrefix, "auto_updater"), Config]
public partial class AutoUpdaterManager : Node
{
    private static AutoUpdaterManager? Instance { set; get; }
        
    [AFXProjectSettingProperty("enabled", false)]
    public static bool IsEnabled { get; set; }
        
    [AFXProjectSettingProperty("show_update_prompt", true)]
    public static bool ShowUpdatePrompt { get; set; }
    
    [AFXProjectSettingProperty("check_for_update_interval", -1)]
    public static int CheckForUpdateIntervalSec { get; set; }

    [AFXProjectSettingProperty("version_check_url", "")]
    public static string VersionCheckUrl { get; set; } = "";

    [AFXProjectSettingProperty("download_temp_path", "user://Downloads")]
    public static string DownloadTempPath { get; set; } = "user://Downloads";

    [AFXProjectSettingPropertyScene("prompt_scene", "res://addons/appframework/Scenes/NewVersion/Scene.tscn")]
    public static string PromptScenePath { get; set; } = "";

    [Config] public static string IgnoreUpdateToVersion { get; set; } = "";

    private CSHttpClient _downloadHttpClient = new();

    private static string _versionFilePath = "user://version";

    private Window? _updateWindow;

    // Triggers once an update to the application is available
    [Signal] public delegate void UpdateAvailableEventHandler(AppVersionInfo releaseInfo);

    [Signal] public delegate void UpdateStatusChangedEventHandler(AppVersionInfo appVersionInfo, UpdateStatus status);
    
    [Signal] public delegate void DownloadProgressEventHandler(AppVersionInfo appVersionInfo, float progress);
    
    [Signal] public delegate void InstallProgressEventHandler(float progress);

    private Timer? _checkForUpdateTimer;

    private ColorRect _greyOut = new();

    public static AutoUpdaterManager? GetInstance()
    {
        return Instance;
    }

    public override void _Ready()
    {
        Instance = this;
        
        _greyOut.Color = Color.Color8(12, 12, 12, 120);
        
        // On startup, we fetch the version file first. If this contains our current version then we all good. If it doesn't exist
        // at all then make one and write the current version. If it is a lower version then trigger the upgrade routine(s).
        Version verFileVersion = AppFrameworkManager.GetAppVersion();
        if (!GDFileAccess.FileExists(_versionFilePath))
        {
            var initialVerFile = GDFileAccess.Open(_versionFilePath, GDFileAccess.ModeFlags.Write);
            initialVerFile.StoreString(verFileVersion.ToString());
            initialVerFile.Close();
        }
        var verFile = GDFileAccess.Open(_versionFilePath, GDFileAccess.ModeFlags.Read);
        verFileVersion = verFile.GetAsText().ToVersion();

        if (verFileVersion < AppFrameworkManager.GetAppVersion())
        {
            OnAppStartupAfterUpdateSuccessful(verFileVersion);
        }
        
        if (CheckForUpdateIntervalSec > 0)
        {
            _checkForUpdateTimer = new();
            _checkForUpdateTimer.Autostart = true;
            _checkForUpdateTimer.OneShot = false;
            _checkForUpdateTimer.WaitTime = CheckForUpdateIntervalSec;
            _checkForUpdateTimer.Timeout += CheckForUpdate;
            
            AddChild(_checkForUpdateTimer);
        }

        CheckForUpdate();
    }

    public void CheckForUpdate()
    {
        Task.Run(async () =>
        {
            // Fetch the latest version information
            var releaseEntry = await GetLatestVersionInfo();
            
            // If we actually have an entry, then we need to pass that on to the main thread to do something with it
            CallDeferred(MethodName.OnNewVersionAvailable, releaseEntry!);
        });
    }
    
    public void UnattendedUpdate(AppVersionInfo to)
    {
        // Download the ZIP file
        Task.Run(async () =>
        {
            var progress = new Progress<float>();
            progress.ProgressChanged += (sender, f) =>
            {
                CallDeferred(GodotObject.MethodName.EmitSignal, SignalName.DownloadProgress, to, f);
            };

            string downloadTempPath = ProjectSettings.GlobalizePath(DownloadTempPath);
            
            // Ensure that the download dir path exists
            Directory.CreateDirectory(downloadTempPath);

            OperatingSystem os = Environment.OSVersion;
            BetterPlatformID platformId = os.GetBetterPlatformID();
            if (!to.BinaryDownloads.ContainsKey(platformId))
            {
                return;
            }

            Tuple<string, string> downloadInfo = to.BinaryDownloads[platformId];
            var fullPath = downloadTempPath + "/" + downloadInfo.Item2;
            using (var file = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                CallDeferred(GodotObject.MethodName.EmitSignal, SignalName.UpdateStatusChanged, to, (int)UpdateStatus.Downloading);
                await _downloadHttpClient.DownloadDataAsync(downloadInfo.Item1, file, progress);
                
                CallDeferred(GodotObject.MethodName.EmitSignal, SignalName.UpdateStatusChanged, to, (int)UpdateStatus.Installing);
                InstallUpdate(to, fullPath);
            }
        });
    }

    private void InstallUpdate(AppVersionInfo versionInfo, string zipFilePath)
    {
    }

    public void IgnoreUpdate(AppVersionInfo releaseInfo)
    {
        CloseVersionWindow();
        IgnoreUpdateToVersion = releaseInfo.Ver.ToString();
    }

    public void OnAppStartupAfterUpdateSuccessful(Version prevVersion)
    {
        
    }

    private void OnNewVersionAvailable(AppVersionInfo? versionInfo)
    {
        // No new version info...bail out, no need to continue
        if (versionInfo == null)
        {
            return;
        }
        
        // Check if we need to ignore this version, as per the user's request
        try
        {
            var ignoreVersion = IgnoreUpdateToVersion.ToVersion();
            if (ignoreVersion == versionInfo.Ver)
            {
                return;
            }
        }
        catch (Exception)
        {
            // ignored
        }

        // Let interested parties know about this new version
        EmitSignal(SignalName.UpdateAvailable, versionInfo);
        
        // If dev has specified that the update window should not show (they might be doing something custom), then don't
        // continue.
        if (!ShowUpdatePrompt)
        {
            return;
        }

        // Check to see if we have a valid scene path in project settings
        if (string.IsNullOrWhiteSpace(PromptScenePath))
        {
            GD.PrintErr("For auto updater to work, you must specify a valid scene to load into the version download window space");
            return;
        }
        
        // Create the contents of the window, then create the window itself and show it to the user
        AutoUpdaterWindowContent? windowContents = GD.Load<PackedScene>(PromptScenePath).Instantiate<AutoUpdaterWindowContent>();
        if (windowContents == null)
        {
            GD.PrintErr($"Auto updater window content root node script must inherit from {nameof(AutoUpdaterWindowContent)}");
            return;
        }
        
        windowContents.InternalInitialize(versionInfo);
        
        _updateWindow = new Window();
        _updateWindow.Borderless = false;
        _updateWindow.InitialPosition = Window.WindowInitialPosition.CenterMainWindowScreen;
        _updateWindow.Unresizable = true;
        _updateWindow.Title = "Update Available";
        _updateWindow.Size = new Vector2I(650, 300);
        _updateWindow.Transient = true;
        _updateWindow.Exclusive = true;
        _updateWindow.CloseRequested += CloseVersionWindow;

        _updateWindow.AddChild(windowContents);
        AddChild(_updateWindow);

        _greyOut.TopLevel = true;
        _greyOut.ZIndex = 125;
        _greyOut.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(_greyOut);
    }

    private static void DeleteFiles(List<string> files)
    {
        foreach (var filePath in files)
        {
            try
            {
                File.Delete(filePath);
            }
            catch (Exception e)
            {
                GD.PrintErr(e.Message);
            }
        }
    }

    private async Task<AppVersionInfo?> GetLatestVersionInfo()
    {
        // If the specified URL is a Github URL then we fetch the Json and serialize it into a version info object
        if (VersionCheckUrl.StartsWith("https://api.github.com"))
        {
            using (var client = new CSHttpClient())
            {
                client.BaseAddress = new Uri(VersionCheckUrl);
                client.DefaultRequestHeaders.Add("User-Agent", "GodotSourceTools");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await client.GetAsync(VersionCheckUrl);
                response.EnsureSuccessStatusCode();
                List<JsonGithubReleaseEntry>? entries = response.Content.ReadFromJsonAsync<List<JsonGithubReleaseEntry>>().Result;

                if (entries == null)
                {
                    return null;
                }

                // Go through each release info and check its release version against the app version, if there is a new release available
                // then return the info.
                string appVersionStr = ProjectSettings.GetSetting("application/config/version").AsString();
                var appVersion = appVersionStr.ToVersion();
                foreach (var entry in entries)
                {
                    var releaseVersion = entry.GetVersion();
                    if (releaseVersion > appVersion)
                    {
                        var appVersionInfo = entry.ToAppVersionInfo();

                        foreach (var asset in entry.Assets)
                        {
                            string[] split = asset.Browser_Download_Url.Split(".");
                            if (split.Length >= 3)
                            {
                                string forOsStr = split[split.Length - 2];
                                if (Enum.TryParse(forOsStr, true, out BetterPlatformID forOs))
                                {
                                    appVersionInfo.BinaryDownloads[forOs] = new Tuple<string, string>(asset.Browser_Download_Url, asset.Name);
                                }
                            }
                        }
                        
                        return appVersionInfo;
                    }
                }
            }
        }

        return null;
    }

    private static List<string> RenameDirectoryContents(string root, string prefix = "", string suffix = "_bak")
    {
        List<string> filesRenamed = new();
        
        foreach (var directory in Directory.GetDirectories(root))
        {
            var newDirectory = Path.Combine(root, Path.GetFileName(directory));
            List<string> result = RenameDirectoryContents(newDirectory, prefix, suffix);
            filesRenamed.AddRange(result);
        }

        foreach (var file in Directory.GetFiles(root))
        {
            try
            {
                string fileName = Path.GetFileName(file);
                string finalDest = Path.Combine(root, prefix + fileName + suffix);
                File.Move(file, finalDest);
                filesRenamed.Add(finalDest);
            }
            catch (Exception e)
            {
                GD.PrintErr(e.Message);
            }
        }

        return filesRenamed;
    }
    
    private static void CloneDirectory(string root, string dest)
    {
        foreach (var directory in Directory.GetDirectories(root))
        {
            var newDirectory = Path.Combine(dest, Path.GetFileName(directory));
            Directory.CreateDirectory(newDirectory);
            CloneDirectory(directory, newDirectory);
        }

        foreach (var file in Directory.GetFiles(root))
        {
            try
            {
                string finalDest = Path.Combine(dest, Path.GetFileName(file));
                File.Copy(file, finalDest);
            }
            catch (Exception e)
            {
                GD.PrintErr(e.Message);
            }
        }
    }

    private void CloseVersionWindow()
    {
        _updateWindow?.QueueFree();
        _updateWindow = null;
        
        _greyOut.Hide();

        OnVersionWindowClosed();
    }

    protected virtual void OnVersionWindowClosed()
    {
        
    }
}