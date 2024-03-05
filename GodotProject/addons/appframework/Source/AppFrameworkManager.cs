using GodotAppFramework.Extentions;

using Godot;
using Godot.Collections;

using System;
using System.Linq;
using System.Reflection;

namespace GodotAppFramework;

// Designed to be the first thing that loads in your application, handles all things AppFramework
public partial class AppFrameworkManager : Node
{
    private static AppFrameworkManager? Instance { get; set; } = null;

    private Dictionary<string, Node> _managers = new();
    
    public static AppFrameworkManager? GetInstance()
    {
        return Instance;
    }
    
    public override void _Ready()
    {
        Instance = this;
        
        InjectAndInitializeBoundProjectSettings();

        InitializeMandatoryManagers();
        InitializeOptionalManagers();
    }

    public string GetAppVersion()
    {
        return ProjectSettings.GetSetting("application/config/version").AsString();
    }

    public string GetAppName()
    {
        return ProjectSettings.GetSetting("application/config/name").AsString();
    }

    private void InitializeMandatoryManagers()
    {
        InitManager<AppConfigManager>();
    }

    private void InitializeOptionalManagers()
    {
        if (AutoUpdaterManager.IsEnabled)
        {
            InitManager<AutoUpdaterManager>();
        }
    }

    private T InitManager<T>(string managerName = "") where T : Node, new()
    {
        var newManager = new T();
        
        AddChild(newManager);
        newManager.Name = managerName.Length != 0 ? managerName : newManager.GetType().Name;

        _managers.Add(newManager.Name, newManager);

        return newManager;
    }

    public void InjectAndInitializeBoundProjectSettings()
    {
        var assembly = Assembly.GetExecutingAssembly();
        foreach (Type type in assembly.GetTypes())
        {
            AFXProjectSetting? projectSetting = type.GetCustomAttributes<AFXProjectSetting>().SingleOrDefault();

            if (projectSetting == null)
            {
                continue;
            }
			
            foreach (PropertyInfo property in type.GetProperties())
            {
                // Only allow on static properties
                if (!property.GetAccessors(nonPublic: true)[0].IsStatic)
                {
                    continue;
                }
                
                var attributes = property.GetCustomAttributes<AFXProjectSettingProperty>();
                foreach (var projectSettingProperty in attributes)
                {
                    Variant settingValue = new();
                    
                    string finalProjectSettingName = projectSetting.Root + "/" + projectSetting.SecondaryPath + "/" + projectSettingProperty.SettingsName;
                    if (ProjectSettings.HasSetting(finalProjectSettingName))
                    {
                        settingValue = ProjectSettings.GetSetting(finalProjectSettingName);
                    }
                    else
                    {
                        settingValue = projectSettingProperty.DefaultValue;
                    }
                    
                    settingValue.SetStaticProp(type, property);
                }
            }
        }
    }

    
}