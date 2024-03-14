using GodotAppFramework.Globals;
using GodotAppFramework.Extensions;

using Godot;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using InvalidCastException = System.InvalidCastException;

namespace GodotAppFramework;

/// <summary>
/// Automates the application configuration; properties set during the runtime of the application that persist across restarts.
/// </summary>
[AFXProjectSetting(Constants.ProjectSettingsPrefix, "app_framework")]
public partial class AppConfigManager : Node
{
    private static AppConfigManager? _instance = null;

    private static ConfigFile _defaults = new ConfigFile();
    
    [AFXProjectSettingProperty("app_config_path", "user://config.ini")]
    public static string AppConfigPath { get; set; } = "";
    
    [Export, AFXProjectSettingPropertyScene("settings_dialog_scene", "res://addons/appframework/Scenes/AppConfigDialog/Scene.tscn")]
    public static string AppConfigDialogScenePath { get; set; } = "";

    [AFXProjectSettingPropertyScene("settings_dialog_title", "Settings")]
    public static string AppConfigDialogTitle { get; set; } = "";

    private Window? _appConfigWindow = null;

    public static AppConfigManager? GetInstance()
    {
        return _instance;
    }
    
    public override void _Ready()
    {
        _instance = this;
        
        ConfigFile? configFile = new ConfigFile();
        
        // Load the application's config from disk
        if (!FileAccess.FileExists(AppConfigPath))
        {
            configFile.Save(AppConfigPath);
        }
        
        configFile.Load(AppConfigPath);

        var attributeInfos = AttributeInfo<Config>.GetAllStaticPropertyAttributes(Assembly.GetExecutingAssembly());
        foreach (var attributeInfo in attributeInfos)
        {
            // Store defaults into a temporary memory mapped config file
            object? propVal = attributeInfo.PropInfo.GetValue(null);
            Variant gdVariant = Utilities.CSharpObj2GdVariant(propVal);
            _defaults.SetValue(attributeInfo.TypeInfo.Name, attributeInfo.PropInfo.Name, gdVariant);

            // Load the value from the config file if it has the property
            if (configFile.HasSectionKey(attributeInfo.TypeInfo.Name, attributeInfo.PropInfo.Name))
            {
                Variant v = configFile.GetValue(attributeInfo.TypeInfo.Name, attributeInfo.PropInfo.Name);
                attributeInfo.PropInfo.SetValue(null, v.ToCSharpObject());
            }
        }
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
        {
            // When the application is about to exit, then we try save all properties that are marked as `Config`
            SaveConfig();
        }
    }
    
    public void ShowAppSettingsDialog()
    {
        if (AppConfigDialogScenePath == string.Empty)
        {
            return;
        }
        
        PackedScene packedScene = GD.Load<PackedScene>(AppConfigDialogScenePath);
        if (!IsInstanceValid(packedScene))
        {
            GD.PrintErr($"Could not load packed scene {AppConfigDialogScenePath} from disk");
            return;
        }

        AppConfigDialogContent? windowContents = null;
        try
        {
            windowContents = packedScene.Instantiate<AppConfigDialogContent>();
            windowContents.InternalInitialize();
        }
        catch (InvalidCastException e)
        {
            GD.PrintErr($"Could not load packed scene {AppConfigDialogScenePath}. Cannot cast to a {nameof(AppConfigDialogContent)}");
            return;
        }

        _appConfigWindow = new Window();
        _appConfigWindow.Borderless = false;
        _appConfigWindow.InitialPosition = Window.WindowInitialPosition.CenterMainWindowScreen;
        _appConfigWindow.Unresizable = true;
        _appConfigWindow.Title = AppConfigDialogTitle;
        _appConfigWindow.Size = new Vector2I(650, 300);
        _appConfigWindow.Transient = true;
        _appConfigWindow.Exclusive = true;
        _appConfigWindow.CloseRequested += CloseAppSettingsDialog;

        _appConfigWindow.AddChild(windowContents);
        AddChild(_appConfigWindow);
    }

    public void CloseAppSettingsDialog()
    {
        if (_appConfigWindow != null)
        {
            _appConfigWindow.QueueFree();
            _appConfigWindow = null;
        }
    }

    public static void SaveChanges()
    {
        var appConfigInstance = GetInstance();
        appConfigInstance?.SaveConfig();
    }

    public void SaveConfig()
    {
        ConfigFile configFile = new ConfigFile();
        
        var attributeInfos = AttributeInfo<Config>.GetAllStaticPropertyAttributes(Assembly.GetExecutingAssembly());
        foreach (var attributeInfo in attributeInfos)
        {
            object? propVal = attributeInfo.PropInfo.GetValue(null);
            Variant gdVariant = Utilities.CSharpObj2GdVariant(propVal);

            // Only save to config file if this property has changed from defaults
            Variant defaultVal = _defaults.GetValue(attributeInfo.TypeInfo.Name, attributeInfo.PropInfo.Name);
            if (!gdVariant.IsEqualTo(defaultVal))
            {
                configFile.SetValue(attributeInfo.TypeInfo.Name, attributeInfo.PropInfo.Name, gdVariant);
            }
        }
        
        configFile.Save(AppConfigPath);
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, Inherited = false)]
public class Config : Attribute
{
    public bool ShowInSettingsDialog = false;
    public string Category = "";
    public string FriendlyName = "";
    
    public Config()
    {
        
    }

    public Config(string category, string friendlyName)
    {
        ShowInSettingsDialog = true;
        Category = category;
        FriendlyName = friendlyName;
    }
}