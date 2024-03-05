using Godot;

using GodotAppFramework.Globals;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using CSHttpClient = System.Net.Http.HttpClient;

namespace GodotAppFramework;

public partial class JsonGithubReleaseEntry : Resource
{
    public string Name { get; set; }
    public string Tag_Name { get; set; }
    public string ZipBall_Url { get; set; }
    public bool Prerelease { get; set; }
    public DateTime Created_At { get; set; }
    public DateTime Published_At { get; set; }
    public string Body { get; set; }
    public string Html_Url { get; set; }

    public string GetVersionStr()
    {
        return Tag_Name;
    }

    public Version GetVersion()
    {
        var versionStr = GetVersionStr();
        return versionStr.ToVersion();
    }
}

[AFXProjectSetting(Constants.ProjectSettingsPrefix, "auto_updater"), Config]
public partial class AutoUpdaterManager : Node
{
    private static AutoUpdaterManager? Instance { set; get; }
        
    [Export, AFXProjectSettingProperty("enabled", false)]
    public static bool IsEnabled { get; set; }
        
    [Export, AFXProjectSettingProperty("show_update_prompt", true)]
    public static bool ShowUpdatePrompt { get; set; }
    
    [Export, AFXProjectSettingProperty("check_for_update_interval", -1)]
    public static int CheckForUpdateIntervalSec { get; set; }

    [Export, AFXProjectSettingProperty("version_check_url", "")]
    public static string VersionCheckUrl { get; set; }

    [Export, AFXProjectSettingProperty("download_temp_path", "")]
    public static string DownloadTempPath { get; set; } = "";

    [Export, AFXProjectSettingPropertyScene("prompt_scene", "res://addons/appframework/Scenes/NewVersion/Scene.tscn")]
    public static string PromptScenePath { get; set; } = "";

    [Config] public static string IgnoreUpdateToVersion { get; set; } = "";

    private Window? _updateWindow;

    // Triggers once an update to the application is available
    [Signal] public delegate void UpdateAvailableEventHandler(JsonGithubReleaseEntry releaseInfo);
    
    [Signal] public delegate void DownloadProgressEventHandler(float progress);
    
    [Signal] public delegate void InstallProgressEventHandler(float progress);

    private Timer? _checkForUpdateTimer;

    public static AutoUpdaterManager? GetInstance()
    {
        return Instance;
    }

    public override void _Ready()
    {
        Instance = this;
        
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
            CallDeferred(nameof(OnNewVersionAvailable), releaseEntry);
        });
    }
    
    public void UnattendedUpdate()
    {
    }

    public void IgnoreUpdate(JsonGithubReleaseEntry releaseInfo)
    {
        // Close the update window if it's open
        if (_updateWindow != null)
        {
            _updateWindow.QueueFree();
            _updateWindow = null;
        }

        IgnoreUpdateToVersion = releaseInfo.GetVersionStr();
    }

    public void OnAppStartupAfterUpdateSuccessful(string prevVersion)
    {
        
    }

    private void OnNewVersionAvailable(JsonGithubReleaseEntry? releaseInfo)
    {
        // No new version info...bail out, no need to continue
        if (releaseInfo == null)
        {
            return;
        }
        
        // Check if we need to ignore this version, as per the user's request
        try
        {
            var ignoreVersion = IgnoreUpdateToVersion.ToVersion();
            if (ignoreVersion == releaseInfo.GetVersion())
            {
                return;
            }
        }
        catch (Exception e)
        {
            
        }
        
        // Let interested parties know about this new version
        EmitSignal(SignalName.UpdateAvailable, releaseInfo);
        
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
        
        windowContents.InternalInitialize(releaseInfo);
        
        _updateWindow = new Window();
        _updateWindow.Borderless = false;
        _updateWindow.InitialPosition = Window.WindowInitialPosition.CenterMainWindowScreen;
        _updateWindow.Unresizable = true;
        _updateWindow.Title = "Update Available";
        _updateWindow.Size = new Vector2I(650, 300);
        _updateWindow.AddChild(windowContents);

        _updateWindow.CloseRequested += () =>
        {
            _updateWindow.QueueFree();
            _updateWindow = null;
        };

        AddChild(_updateWindow);
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

    private async Task<JsonGithubReleaseEntry?> GetLatestVersionInfo()
    {
        string version = "";

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
                        return entry;
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
}