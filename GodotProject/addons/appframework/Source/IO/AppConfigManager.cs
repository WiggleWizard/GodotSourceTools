using GodotAppFramework.Globals;
using GodotAppFramework.Extensions;

using Godot;

using System;
using System.Linq;
using System.Reflection;

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
        
        var assembly = Assembly.GetExecutingAssembly();
        foreach (Type type in assembly.GetTypes())
        {
            Config? typeAttr = type.GetCustomAttributes<Config>().SingleOrDefault();
            if (typeAttr == null)
            {
                continue;
            }
            
            foreach (PropertyInfo property in type.GetProperties())
            {
                var attributes = property.GetCustomAttributes<Config>();
                foreach (var propertyAttr in attributes)
                {
                    // Only allow on static properties
                    if (!property.GetAccessors(nonPublic: true)[0].IsStatic)
                    {
                        GD.PrintErr("Property must the static accessor in order for it to be saved/loaded from the application config");
                        continue;
                    }

                    // Store defaults into a temporary memory mapped config file
                    object? propVal = property.GetValue(null);
                    Variant gdVariant = Utilities.CSharpObj2GdVariant(propVal);
                    _defaults.SetValue(type.Name, property.Name, gdVariant);

                    // Load the value from the config file if it has the property
                    if (configFile.HasSectionKey(type.Name, property.Name))
                    {
                        Variant v = configFile.GetValue(type.Name, property.Name);
                        property.SetValue(null, v.ToCSharpObject());
                    }
                }
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

    public static void SaveChanges()
    {
        var appConfigInstance = GetInstance();
        appConfigInstance?.SaveConfig();
    }

    public void SaveConfig()
    {
        ConfigFile configFile = new ConfigFile();
        
        var assembly = Assembly.GetExecutingAssembly();
        foreach (Type type in assembly.GetTypes())
        {
            Config? typeAttr = type.GetCustomAttributes<Config>().SingleOrDefault();
            if (typeAttr == null)
            {
                continue;
            }
			
            foreach (PropertyInfo property in type.GetProperties())
            {
                var attributes = property.GetCustomAttributes<Config>();
                foreach (var propertyAttr in attributes)
                {
                    // Only allow on static properties
                    if (!property.GetAccessors(nonPublic: true)[0].IsStatic)
                    {
                        GD.PrintErr("Property must the static accessor in order for it to be saved/loaded from the application config");
                        continue;
                    }

                    object? propVal = property.GetValue(null);
                    Variant gdVariant = Utilities.CSharpObj2GdVariant(propVal);

                    // Only save to config file if this property has changed from defaults
                    Variant defaultVal = _defaults.GetValue(type.Name, property.Name);
                    if (!gdVariant.IsEqualTo(defaultVal))
                    {
                        configFile.SetValue(type.Name, property.Name, gdVariant);
                    }
                }
            }
        }
        
        configFile.Save(AppConfigPath);
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, Inherited = false)]
public class Config : Attribute {}