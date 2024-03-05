#if TOOLS
using Godot;
using Godot.Collections;

using System;
using System.Linq;
using System.Reflection;

using GodotAppFramework.Globals;
using GodotAppFramework;

[Tool]
public partial class AppFrameworkPlugin : EditorPlugin
{
	public override void _EnterTree()
	{
		GD.Print("Enabling App Framework");
		
		// Register all specifically requested project settings that are bound to property attributes
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
					string finalProjectSettingName = projectSetting.Root + "/" + projectSetting.SecondaryPath + "/" + projectSettingProperty.SettingsName;
					AddCustomProjectSetting(finalProjectSettingName, projectSettingProperty.VariantType, projectSettingProperty.DefaultValue, projectSettingProperty.HintType, projectSettingProperty.HintString);
				}
			}
		}
		
		// Finally, register the autoload singleton if it isn't registered anyways as the first load in the list
		AddAutoloadSingleton("AFXManager", Constants.AddonPath + "/Source/AppFrameworkManager.cs");
	}

	public override void _ExitTree()
	{
		GD.Print("Disabling App Framework");
	}

	public void AddCustomProjectSetting(string name, Variant.Type type, Variant defaultValue, PropertyHint hintType = PropertyHint.None, string hintString = "")
	{
		if (!ProjectSettings.HasSetting(name))
		{
			var settingInfo = new Dictionary();
			settingInfo.Add("name", name);
			settingInfo.Add("type", (int)type);
		
			if (hintType != PropertyHint.None)
			{
				settingInfo.Add("hint", (int)hintType);
				settingInfo.Add("hint_string", hintString);
			}

			ProjectSettings.SetSetting(name, defaultValue);
			ProjectSettings.SetInitialValue(name, defaultValue);
			ProjectSettings.AddPropertyInfo(settingInfo);
			ProjectSettings.SetAsBasic(name, true);
			ProjectSettings.Save();
			
			GD.Print($"Added Project Setting {name}");
		}
	}
}
#endif
