using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GodotAppFramework.Globals;

public class Constants
{
    public const string AddonPath = "res://addons/appframework";
    public const string ProjectSettingsPrefix = "godot_app_framework";

}

public class ProjectSettingName
{
}

public class MiscNames
{
    public const string VCRedistRegPath = "HKEY_LOCAL_MACHINE\\SOFTWARE\\Microsoft\\VisualStudio\\14.0\\VC\\Runtimes\\x64";
}

public class AttributeInfo<T> where T : Attribute
{
    public Type TypeInfo { get; set; }
    public PropertyInfo PropInfo { get; set; }
    public T Attribute { get; set; }

    public static List<AttributeInfo<T>> GetAllStaticAttributesOfType(Assembly assembly)
    {
        List<AttributeInfo<T>> list = new();

        foreach (Type type in assembly.GetTypes())
        {
            T? typeAttr = type.GetCustomAttributes<T>().SingleOrDefault();
            if (typeAttr == null)
            {
                continue;
            }

            foreach (PropertyInfo property in type.GetProperties())
            {
                var attributes = property.GetCustomAttributes<T>();
                foreach (T propertyAttr in attributes)
                {
                    // Only allow on static properties
                    if (!property.GetAccessors(nonPublic: true)[0].IsStatic)
                    {
                        continue;
                    }

                    list.Add(new()
                    {
                        Attribute = propertyAttr,
                        PropInfo = property,
                        TypeInfo = type
                    });
                }
            }
        }

        return list;
    }
}