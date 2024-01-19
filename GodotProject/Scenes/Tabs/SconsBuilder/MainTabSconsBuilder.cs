using Godot;
using Godot.Collections;

using System;
using System.Reflection;

using ModuleCheckBox = Godot.CheckButton;

public partial class MainTabSconsBuilder : MainTabBase
{
    [Export] private Control ModuleListVanillaContainer { set; get; } = null;
    
    [Export] private Control ModuleListCustomContainer { set; get; } = null;
    [Export] private OptionButton TargetOptionButton { set; get; } = null;
    [Export] private OptionButton PlatformOptionButton { set; get; } = null;
    [Export] private OptionButton PrecisionOptionButton { set; get; } = null;
    [Export] private OptionButton ConfigSelector { set; get; } = null;
    [Export] private LineEdit ConfigEditor { set; get; } = null;
    [Export] private Array<Control?> ConfigControlContainers { set; get; } = new();

    [Export] private SourceManagerConfigEntry? CurrentlySelectedConfig { set; get; } = null;

    public override void _Ready()
    {
        foreach (Node? n in ConfigControlContainers)
        {
            if (n == null)
            {
                GD.PrintErr($"A control in {nameof(ConfigControlContainers)} is null. Are you sure the tab is set up correctly in editor? Some issues may occur if continuing to use tool");
                break;
            }
        }
        
        foreach (var v in Enum.GetValues(typeof(BuildTarget)))
        {
            TargetOptionButton.AddItem(v.ToString());
        }

        foreach (var v in Enum.GetValues(typeof(BuildPlatform)))
        {
            PlatformOptionButton.AddItem(v.ToString());
        }

        foreach (var v in Enum.GetValues(typeof(BuildPrecision)))
        {
            PrecisionOptionButton.AddItem(v.ToString());
        }

        var sourceManager = SourceManager.GetInstance();
        sourceManager.NewSourceLoaded += OnNewSourceLoaded;
        sourceManager.NewConfig += OnNewConfigAdded;

        var buildManager = BuildManager.GetInstance();
        buildManager.StatusChanged += (status, tp) =>
        {
        };

        if (sourceManager.CurrentSourceDir != "")
        {
            OnNewSourceLoaded(sourceManager.CurrentSourceDir);
        }
    }

    public void OnNewSourceLoaded(String dirPath)
    {
        ConfigSelector.Clear();
        
        SourceManager sourceManager = SourceManager.GetInstance();
        foreach (var entry in sourceManager.Config)
        {
            ConfigSelector.AddItem(entry.Name);
        }
        
        // Clear then add the modules to the container, including listening for when they are toggled so that we can modify the
        // config when they are pressed.
        foreach (var child in ModuleListVanillaContainer.GetChildren())
        {
            ModuleListVanillaContainer.RemoveChild(child);
        }
        foreach (var child in ModuleListCustomContainer.GetChildren())
        {
            ModuleListCustomContainer.RemoveChild(child);
        }
        
        Array<String> availableModules = sourceManager.GetAvailableModules();
        Array<string> vanillaModules = sourceManager.GetVanillaModules();
        foreach (String availableModuleName in availableModules)
        {
            ModuleCheckBox checkbox = new ModuleCheckBox();
            checkbox.Name = availableModuleName;
            checkbox.Text = availableModuleName;
            checkbox.Toggled += on => OnConfigModuleToggled(on, availableModuleName);

            if (vanillaModules.Contains(availableModuleName))
            {
                ModuleListVanillaContainer.AddChild(checkbox);
                checkbox.Owner = ModuleListVanillaContainer;
            }
            else
            {
                ModuleListCustomContainer.AddChild(checkbox);
                checkbox.Owner = ModuleListCustomContainer;
            }
        }
        
        // Select first config in the list
        ConfigSelector.Selected = 0;
        OnConfigSelectorItemSelected(0);
    }

    public void OnAddPressed()
    {
        var sourceManager = SourceManager.GetInstance();
        sourceManager.CreateNewConfigEntry("New Config");
    }

    public void OnSaveConfigPressed()
    {
        var sourceManager = SourceManager.GetInstance();
        sourceManager.SaveConfig();
    }

    public void OnConfigSelectorItemSelected(int index)
    {
        var sourceManager = SourceManager.GetInstance();
        CurrentlySelectedConfig = sourceManager.GetConfigEntry(index);
        if (CurrentlySelectedConfig == null)
        {
            GD.Print("No sources loaded");
            return;
        }
        
        foreach (Control? container in ConfigControlContainers)
        {
            if (container?.FindChild("Control") is Control c)
            {
                GenerativeUIControl.SetControlValueFromProperty(CurrentlySelectedConfig, container.Name, c);
            }
        }
        
        // Set all the module checkbox statuses
        foreach (Node child in ModuleListVanillaContainer.GetChildren())
        {
            if (child is ModuleCheckBox checkbox)
                checkbox.ButtonPressed = CurrentlySelectedConfig.EnabledModules.Contains(child.Name);
        }
        
        foreach (Node child in ModuleListCustomContainer.GetChildren())
        {
            if (child is ModuleCheckBox checkbox)
                checkbox.ButtonPressed = CurrentlySelectedConfig.EnabledModules.Contains(child.Name);
        }
    }

    public void OnNewConfigAdded(SourceManagerConfigEntry newConfig)
    {
        ConfigSelector.AddItem(newConfig.Name);
    }

    public void OnOptionButtonItemSelected(int index, String propName)
    {
        if (CurrentlySelectedConfig == null)
        {
            GD.Print("No config selected");
            return;
        }
        
        PropertyInfo? propInfo = CurrentlySelectedConfig.GetType().GetProperty(propName);
        if (propInfo == null)
            return;
        
        Type propType = propInfo.PropertyType;
        System.Array values = Enum.GetValues(propType);
        object? value = values.GetValue(index);
        propInfo.SetValue(CurrentlySelectedConfig, value);
    }

    public void OnConfigCheckButtonToggled(bool on, String propName)
    {
        if (CurrentlySelectedConfig == null)
        {
            GD.Print("No config selected");
            return;
        }
        
        PropertyInfo? propInfo = CurrentlySelectedConfig.GetType().GetProperty(propName);
        if (propInfo == null)
            return;
        
        propInfo.SetValue(CurrentlySelectedConfig, on);
    }

    public void OnConfigLineEditChanged(String newText, String propName)
    {
        if (CurrentlySelectedConfig == null)
        {
            GD.Print("No config selected");
            return;
        }
        
        PropertyInfo? propInfo = CurrentlySelectedConfig.GetType().GetProperty(propName);
        if (propInfo == null)
            return;
        
        propInfo.SetValue(CurrentlySelectedConfig, newText);
    }

    public void OnConfigModuleToggled(bool on, String moduleName)
    {
        if (CurrentlySelectedConfig == null)
        {
            GD.Print("No config selected");
            return;
        }
        
        CurrentlySelectedConfig.EnabledModules.Remove(moduleName);
        if (on)
            CurrentlySelectedConfig.EnabledModules.Add(moduleName);
    }

    public void OnEditConfigPressed()
    {
        if (CurrentlySelectedConfig == null)
            return;

        ConfigEditor.Text = CurrentlySelectedConfig.Name;
        ConfigEditor.Show();
        ConfigSelector.Hide();
    }

    public void OnConfigEditTextSubmitted(String newText)
    {
        if (CurrentlySelectedConfig == null)
            return;
        
        CurrentlySelectedConfig.Name = newText;
        
        ConfigEditor.Hide();
        ConfigSelector.Show();
        ConfigSelector.SetItemText(ConfigSelector.Selected, newText);
    }

    public void ForceModuleListPreset(String presetName)
    {
        var sourceManager = SourceManager.GetInstance();
        
        switch (presetName)
        {
            case "EnableAll":
            {
                // Vanilla module list
                foreach (Node child in ModuleListVanillaContainer.GetChildren())
                {
                    if (child is ModuleCheckBox checkbox)
                        checkbox.ButtonPressed = true;
                }
                // Custom module list
                foreach (Node child in ModuleListCustomContainer.GetChildren())
                {
                    if (child is ModuleCheckBox checkbox)
                        checkbox.ButtonPressed = true;
                }
            } break;
            case "DisableAll":
            {
                // Vanilla module list
                foreach (Node child in ModuleListVanillaContainer.GetChildren())
                {
                    if (child is ModuleCheckBox checkbox)
                        checkbox.ButtonPressed = false;
                }
                // Custom module list
                foreach (Node child in ModuleListCustomContainer.GetChildren())
                {
                    if (child is ModuleCheckBox checkbox)
                        checkbox.ButtonPressed = false;
                }
            } break;
            case "VanillaOnly":
            {
                ForceModuleListPreset("DisableAll");
                
                foreach (String moduleName in sourceManager.GetVanillaModules())
                {
                    if (ModuleListVanillaContainer.FindChild(moduleName) is ModuleCheckBox checkbox)
                        checkbox.ButtonPressed = true;
                }
            } break;
            case "ToggleMono":
            {
                if (ModuleListVanillaContainer.FindChild("mono") is ModuleCheckBox checkbox)
                    checkbox.ButtonPressed = !checkbox.ButtonPressed;
            } break;
        }
    }

    public void OnBuildPressed()
    {
        if (CurrentlySelectedConfig == null)
        {
            GD.PrintErr("No config selected");
            return;
        }
        
        SourceManager sourceManager = SourceManager.GetInstance();
        sourceManager.StartBuildingConfig(CurrentlySelectedConfig);
    }
    
    public void OnCleanPressed()
    {
        if (CurrentlySelectedConfig == null)
        {
            GD.PrintErr("No config selected");
            return;
        }
        
        SourceManager sourceManager = SourceManager.GetInstance();
        sourceManager.StartClean();
    }

    public void OnCancelPressed()
    {
        BuildManager buildManager = BuildManager.GetInstance();
        buildManager.TerminateCurrentBuild();
    }
}
