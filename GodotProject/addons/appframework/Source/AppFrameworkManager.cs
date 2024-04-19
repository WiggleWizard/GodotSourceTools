using GodotAppFramework.Extensions;
using GodotAppFramework.Globals;

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

    public static Version GetAppVersion()
    {
        return ProjectSettings.GetSetting("application/config/version").AsString().ToVersion();
    }

    public static string GetAppName()
    {
        return ProjectSettings.GetSetting("application/config/name").AsString();
    }

    public static void FatalError(string message)
    {
        GD.PushError(message);

        var finalAlertMessage = $"{message}\n\nApplication will now terminate";
        OS.Alert(finalAlertMessage, "Script Fatal Error");
        OS.Crash(message);

        return;
        
        Window window = new Window();
        
        var content = new VBoxContainer();
        content.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        
        var lblMessage = new Label();
        lblMessage.Text = message;
        lblMessage.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
        lblMessage.AutowrapMode = TextServer.AutowrapMode.Word;
        content.AddChild(lblMessage);

        var buttonBox = new HBoxContainer();
        content.AddChild(buttonBox);
        
        Button btnContinue = new Button();
        btnContinue.Text = "Continue";
        btnContinue.Pressed += () =>
        {
            window.QueueFree();
        };
        content.AddChild(btnContinue);
        Button btnTerminate = new Button();
        btnTerminate.Text = "Terminate";
        btnTerminate.Pressed += () =>
        {
            // Be nice
            Instance?.GetTree().Root.PropagateNotification((int)NotificationWMCloseRequest);
            // GTFO
            Instance?.GetTree().Quit(0);
        };
        buttonBox.AddChild(btnTerminate);

        window.Borderless = false;
        window.InitialPosition = Window.WindowInitialPosition.CenterMainWindowScreen;
        window.Unresizable = true;
        window.Title = "Script: Fatal Error Occurred";
        window.Size = new Vector2I(500, 200);
        window.Transient = true;
        window.Exclusive = true;
        window.ForceNative = true;
        window.CloseRequested += () =>
        {
            // Deliberately empty
        };
        
        window.AddChild(content);
        Instance?.AddChild(window);
    }

    private void InitializeMandatoryManagers()
    {
        InitManager<WindowManager>();
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