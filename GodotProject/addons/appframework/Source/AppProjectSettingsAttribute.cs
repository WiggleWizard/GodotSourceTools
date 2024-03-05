using System;

using Godot;

namespace GodotAppFramework;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class AFXProjectSetting : Attribute
{
	public string Root { get; set; }
	public string SecondaryPath { get; set; }
	
	public AFXProjectSetting(string root, string secondaryPath)
	{
		Root = root;
		SecondaryPath = secondaryPath;
	}
}

[AttributeUsage(AttributeTargets.Property)]
public class AFXProjectSettingProperty : Attribute
{
	public string SettingsName { get; set; }
	public Variant.Type VariantType { get; set; } = Variant.Type.Nil;
	public PropertyHint HintType { get; set; } = PropertyHint.None;
	public string HintString { get; set; } = "";
		
	public Variant DefaultValue { get; set; }

	public AFXProjectSettingProperty() {}
	
	public AFXProjectSettingProperty(string name, string defaultValue)
	{
		InitializeWithVariant(name, defaultValue);
	}
	
	public AFXProjectSettingProperty(string name, bool defaultValue)
	{
		InitializeWithVariant(name, defaultValue);
	}
	
	public AFXProjectSettingProperty(string name, int defaultValue)
	{
		InitializeWithVariant(name, defaultValue);
	}

	protected void InitializeWithVariant(string name, Variant defaultValue)
	{
		SettingsName = name;
		DefaultValue = defaultValue;
		VariantType = DefaultValue.VariantType;
	}
}

[AttributeUsage(AttributeTargets.Property)]
public class AFXProjectSettingPropertyFile : AFXProjectSettingProperty
{
	public AFXProjectSettingPropertyFile(string name, string initialPath, string filter)
	{
		SettingsName = name;
		VariantType = Variant.Type.String;
		DefaultValue = initialPath;
		HintString = filter;
		HintType = PropertyHint.File;
	}
}

[AttributeUsage(AttributeTargets.Property)]
public class AFXProjectSettingPropertyScene : AFXProjectSettingProperty
{
	public AFXProjectSettingPropertyScene(string name, string initialPath)
	{
		SettingsName = name;
		VariantType = Variant.Type.String;
		DefaultValue = initialPath;
		HintString = "*tscn,*.scn";
		HintType = PropertyHint.File;
	}
}
