#if TOOLS
using Godot;

using System;
using Godot.Collections;
using AppFramework.Globals;

[Tool]
public partial class AppFrameworkPlugin : EditorPlugin
{
	public override void _EnterTree()
	{
		GD.Print("Enabling App Framework");
		
		AddCustomProjectSetting(ProjectSettingName.LoadingScreenScene, "", PropertyHint.File, "*.tscn,*.scn,*.res");
		AddCustomProjectSetting(ProjectSettingName.PromptForVCRedist, false);
	}

	public override void _ExitTree()
	{
		GD.Print("Disabling App Framework");
	}

	public void AddCustomProjectSetting(string name, Variant defaultValue, PropertyHint hintType = PropertyHint.None, string hintString = "")
	{
		if (ProjectSettings.HasSetting(name))
		{
			return;
		}

		var settingInfo = new Dictionary();
		settingInfo.Add("name", name);
		settingInfo.Add("type", (int)defaultValue.VariantType);
		settingInfo.Add("hint", (int)hintType);
		settingInfo.Add("hint_string", hintString);

		ProjectSettings.SetSetting(name, defaultValue);
		ProjectSettings.AddPropertyInfo(settingInfo);
		ProjectSettings.SetInitialValue(name, defaultValue);
		ProjectSettings.SetAsBasic(name, true);
	}
}
#endif
