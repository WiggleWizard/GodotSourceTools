using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GodotAppFramework.Globals;

public static class Constants
{
    public const string AddonPath = "res://addons/appframework";
    public const string ProjectSettingsPrefix = "godot_app_framework";
}

public static class MiscNames
{
    public const string VcRedistRegPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64";
}

public class AttributeInfo<T> where T : Attribute
{

    public Type TypeInfo { get; set; }
    public PropertyInfo PropInfo { get; set; }
    public T Attribute { get; set; }
    
    public AttributeInfo(Type typeInfo, PropertyInfo propInfo, T attribute)
    {
        TypeInfo = typeInfo;
        PropInfo = propInfo;
        Attribute = attribute;
    }

    public static List<AttributeInfo<T>> GetAllStaticPropertyAttributes(Assembly assembly)
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

                    list.Add(new(type, property, propertyAttr));
                }
            }
        }

        return list;
    }
}


[AttributeUsage(AttributeTargets.Property)]
public class ValidateExport : Attribute {}