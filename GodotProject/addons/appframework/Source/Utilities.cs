using Godot;

using System;
using System.Reflection;
using System.ComponentModel;

namespace GodotAppFramework;

public class Utilities
{
    public static Variant PropToVariant(PropertyInfo propInfo)
    {
        Variant result = new();
        
        Type propType = propInfo.PropertyType;
        switch (propType)
        {
            // Boolean
            case Type when propType == typeof(bool):
            {
                result = (bool)(GetDefaultValue(propInfo) ?? false);
            } break;
            
            // String
            case Type when propType == typeof(string):
            {
                result = (string)(GetDefaultValue(propInfo) ?? "");
            } break;
			
            // Int
            case Type when propType == typeof(int):
            {
                result = (int)(GetDefaultValue(propInfo) ?? 0);
            } break;

            default: break;
        }

        return result;
    }
    
    public static object? GetDefaultValue(PropertyInfo prop)
    {
        var attributes = prop.GetCustomAttributes(typeof(DefaultValueAttribute), true);
        if (attributes.Length > 0)
        {
            var defaultAttr = (DefaultValueAttribute)attributes[0];
            return defaultAttr.Value;
        }

        // Attribute not found, fall back to default value for the type
        if (prop.PropertyType.IsValueType)
        {
            return Activator.CreateInstance(prop.PropertyType);
        }
        
        return null;
    }

    public static Variant CSharpObj2GdVariant(object? o)
    {
        if (o == null)
        {
            return new Variant();
        }
        
        Variant variantValue = o switch
        {
            int v => Variant.From(v),
            float v => Variant.From(v),
            string v => Variant.From(v),
            _ => throw new NotSupportedException(),
        };

        return variantValue;
    }
}