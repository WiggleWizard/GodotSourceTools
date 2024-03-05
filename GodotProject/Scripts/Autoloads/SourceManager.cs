using Godot;
using Godot.Collections;
using FileAccess = Godot.FileAccess;

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using GodotAppFramework;
using GodotSourceTools;
using CSHttpClient = System.Net.Http.HttpClient;

public partial class SourceManagerConfigEntry : Resource
{
    [Export]
    public string Name { get; set; } = "";

    [Export, GenerativeCLIArg("platform")]
    public BuildPlatform Platform { get; set; } = BuildPlatform.Windows;
    
    [Export, GenerativeCLIArg("target")]
    public BuildTarget Target { get; set; } = BuildTarget.Editor;
    
    [Export, GenerativeCLIArg("precision")]
    public BuildPrecision Precision { get; set; } = BuildPrecision.Single;
    
    [Export, GenerativeCLIArg("dev_build")]
    public bool IsDevBuild { get; set; } = false;
    
    [Export, GenerativeCLIArg("tools")]
    public bool BuildWithTools { get; set; } = true;

    [Export, GenerativeCLIArg("vsproj")]
    public bool GenerateVsProj { get; set; } = true;
    
    [Export, GenerativeCLIArg("extra_suffix")]
    public string Suffix { get; set; } = string.Empty;
    
    [Export]
    public Array<String> EnabledModules { get; set; } = new();
    
    [Export]
    public string AdditionalArgs { get; set; } = string.Empty;

    public Dictionary Serialize()
    {
        Dictionary result = new();

        result["name"]            = Name;
        result["platform"]        = (int)Platform;
        result["target"]          = (int)Target;
        result["precision"]       = (int)Precision;
        result["dev_build"]       = IsDevBuild;
        result["tools"]           = BuildWithTools;
        result["vsproj"]          = GenerateVsProj;
        result["suffix"]          = Suffix;
        result["enabled_modules"] = EnabledModules;
        result["additional_args"] = AdditionalArgs;

        return result;
    }

    public static SourceManagerConfigEntry Deserialize(Dictionary entryData)
    {
        SourceManagerConfigEntry newEntry = new();

        newEntry.Name           = GetValueOrDefault(entryData, "name", "").AsString();
        newEntry.Platform       = (BuildPlatform)GetValueOrDefault(entryData, "platform", 0).AsInt32();
        newEntry.Target         = (BuildTarget)GetValueOrDefault(entryData, "target", 0).AsInt32();
        newEntry.Precision      = (BuildPrecision)GetValueOrDefault(entryData, "precision", 0).AsInt32();
        newEntry.IsDevBuild     = GetValueOrDefault(entryData, "dev_build", false).AsBool();
        newEntry.BuildWithTools = GetValueOrDefault(entryData, "tools", false).AsBool();
        newEntry.GenerateVsProj = GetValueOrDefault(entryData, "vsproj", false).AsBool();
        newEntry.Suffix         = GetValueOrDefault(entryData, "suffix", "").AsString();
        newEntry.EnabledModules = new Array<string>(GetValueOrDefault(entryData, "enabled_modules", new Array<string>()).AsStringArray());
        newEntry.AdditionalArgs = GetValueOrDefault(entryData, "additional_args", "").AsString();

        return newEntry;
    }

    public static Variant GetValueOrDefault(Dictionary o, string key, Variant d)
    {
        if (!o.ContainsKey(key))
            return d;

        return o[key];
    }

    public Array<string> GenerateCliArgs()
    {
        Array<string> args = new();
        
        // General generative stuff
        foreach (PropertyInfo propInfo in GetType().GetProperties())
        {
            GenerativeCLIArg? buildOption = propInfo.GetCustomAttribute<GenerativeCLIArg>();
            if (buildOption == null)
                continue;
            
            args.Add(buildOption.BuildArg(this, propInfo.Name));
        }
        
        // Enabled modules
        foreach (var module in EnabledModules)
        {
            args.Add("module_" + module + "_enabled=yes");
        }
        
        args.Add("modules_enabled_by_default=no");
        
        // TODO: Additional custom args

        return args;
    }
}

internal class RepoFileEntry
{
    public String Name { set; get; }
    public String Type { set; get; }
}

[Config]
public partial class SourceManager : Node
{
    private static SourceManager _instance = null;

    private static String _vanillaModulesFetchUrl = "https://api.github.com";
    private static String _vanillaModulesFetchUri = "repos/godotengine/godot/contents/modules";

    [Config] public static string LastOpenedSourceDir { get; set; } = "";
    
    public static String ConfigName { set; get; } = "godotsourcetools.tres";

    [Export] public Array<SourceManagerConfigEntry> Config { set; get; } = new();
    
    [Export] public String CurrentSourceDir { set; get; } = "";

    [Export] private Array<String> _availableModules = new();
    [Export] private Array<String> _vanillaModules = new();
    
    [Signal] public delegate void NewSourceLoadedEventHandler(String dirPath);
    [Signal] public delegate void NewConfigEventHandler(SourceManagerConfigEntry newConfig);
    [Signal] public delegate void FetchedVanillaModulesEventHandler();

    public override void _Ready()
    {
        _instance = this;

        AppStartupManager.GetInstance()?.WaitForNode(this);

        FetchGitDirs();
    }

    async void FetchGitDirs()
    {
        // Fetch the vanilla module names from Github to be able to reliably pick out which modules are the user's
        // and which ones come with Godot.
        List<RepoFileEntry> files;
        using (var client = new CSHttpClient())
        {
            client.BaseAddress = new Uri(_vanillaModulesFetchUrl);
            client.DefaultRequestHeaders.Add("User-Agent", "GodotSourceTools");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.GetAsync(_vanillaModulesFetchUri);
            response.EnsureSuccessStatusCode();
            files = response.Content.ReadFromJsonAsync<List<RepoFileEntry>>().Result;
        }

        foreach (RepoFileEntry repoFileEntry in files)
        {
            if (repoFileEntry.Type == "dir")
                _vanillaModules.Add(repoFileEntry.Name);
        }

        CallDeferred(nameof(GitDirsFetched));
    }

    private void GitDirsFetched()
    {
        FinallyReady();
    }

    /// <summary>
    /// Called when this singletons is finally ready after asynchronously loading necessary data
    /// </summary>
    private void FinallyReady()
    {
        if (LastOpenedSourceDir != "")
            OpenSourceDir(LastOpenedSourceDir);
        
        // Tell the startup manager that this node is actually done loading
        var appStartupManager = AppStartupManager.GetInstance();
        appStartupManager?.NodeIsReady(this);
    }

    public static SourceManager GetInstance()
    {
        return _instance;
    }

    public void OpenSourceDir(String dirPath)
    {
        CurrentSourceDir = dirPath;
        
        LoadConfig();
        
        _availableModules.Clear();
        GatherAvailableModules(dirPath, ref _availableModules);

        GetTree().Root.Title = $"Godot Source Tools ({dirPath})";
        
        EmitSignal(SignalName.NewSourceLoaded, dirPath);
    }

    public Array<String> GetAvailableModules()
    {
        return _availableModules;
    }

    public Array<String> GetVanillaModules()
    {
        return _vanillaModules;
    }

    public SourceManagerConfigEntry CreateNewConfigEntry(String entryName)
    {
        SourceManagerConfigEntry newConfigEntry = new();

        if (entryName == String.Empty || entryName == "")
            entryName = $"Config {Config.Count}";
        
        newConfigEntry.Name = entryName;
        Config.Add(newConfigEntry);
        
        EmitSignal(SignalName.NewConfig, newConfigEntry);
        
        return newConfigEntry;
    }

    public SourceManagerConfigEntry? GetConfigEntry(int index)
    {
        if (Config.Count <= index)
        {
            return null;
        }
        
        return Config[index];
    }

    public void SaveConfig()
    {
        String configPath = Path.Combine(CurrentSourceDir, ConfigName);
        FileAccess configFile = FileAccess.Open(configPath, FileAccess.ModeFlags.Write);

        Array<Dictionary> data = new Array<Dictionary>();
        foreach (var config in Config)
        {
            data.Add(config.Serialize());
        }
        
        String fileStr = Json.Stringify(data, "    ");
        configFile.StoreString(fileStr);
        configFile.Close();
    }

    public void LoadConfig()
    {
        Config.Clear();
        
        String configPath = Path.Combine(CurrentSourceDir, ConfigName);
        if (FileAccess.FileExists(configPath))
        {
            FileAccess configFile = FileAccess.Open(configPath, FileAccess.ModeFlags.Read);
            String fileStr = "";
            while (!configFile.EofReached())
            {
                fileStr += configFile.GetLine();
            }

            Json jsonParser = new Json();
            Error err = jsonParser.Parse(fileStr);
            if (err == Error.Ok)
            {
                Array<Dictionary> array = jsonParser.Data.AsGodotArray<Dictionary>();
                foreach (Dictionary configData in array)
                {
                    Config.Add(SourceManagerConfigEntry.Deserialize(configData));
                }
            }
        }
    }

    public Array<string> BuildArgListFromConfig(SourceManagerConfigEntry config)
    {
        Array<string> args = new();

        args.AddRange(config.GenerateCliArgs());

        return args;
    }

    public void StartBuildingConfig(SourceManagerConfigEntry config, Array<string>? additionalArgs = null)
    {
        Array<string> args = BuildArgListFromConfig(config);
        if (additionalArgs != null)
            args.AddRange(additionalArgs);
        
        BuildManager buildManager = BuildManager.GetInstance();
        ThreadedProcess? tp = buildManager.StartBuild(CurrentSourceDir, "scons", args);

        tp.OnCompleted += () =>
        {
            ToastNotificationManager notificationManager = ToastNotificationManager.GetInstance();
            notificationManager.ShowSimpleNotification("Build Completed", config.Name);   
        };
    }

    public void StartClean()
    {
        BuildManager buildManager = BuildManager.GetInstance();
        ThreadedProcess? tp = buildManager.StartClean(CurrentSourceDir, "scons");

        tp.OnCompleted += () =>
        {
            ToastNotificationManager notificationManager = ToastNotificationManager.GetInstance();
            notificationManager.ShowSimpleNotification("Clean Completed", "");   
        };
    }

    private bool GatherAvailableModules(String godotSourcePath, ref Array<String> availableModulesList)
    {
        bool newModuleAdded = false;
        
        _availableModules.Clear();

        var fullPath = Path.Join(godotSourcePath, "modules");
        if (Directory.Exists(fullPath))
        {
            string[] moduleDirs = Directory.GetDirectories(fullPath);
            foreach (string path in moduleDirs)
            {
                string scsubPath = Path.Join(path, "SCsub");
                if (File.Exists(scsubPath))
                {
                    String moduleName = Path.GetFileName(path);
                    if (!availableModulesList.Contains(moduleName))
                    {
                        newModuleAdded = true;
                        _availableModules.Add(moduleName);
                    }
                }
            }
        }
        
        return newModuleAdded;
    }
}
