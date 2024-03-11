using Godot;
using GdCollections = Godot.Collections;

using System;
using System.Collections.Generic;
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

        var oType = o.GetType();
        if (oType == typeof(GdCollections.Array<string>))
        {
            return (GdCollections.Array<string>)o;
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
    
    public static void DiffDictionaries<T, U>(IDictionary<T, U> dicA, IDictionary<T, U> dicB, IDictionary<T, U> dicAdd, IDictionary<T, U> dicDel)
    {
        // dicDel has entries that are in A, but not in B, 
        // ie they were deleted when moving from A to B
        diffDicSub<T, U>(dicA, dicB, dicDel);

        // dicAdd has entries that are in B, but not in A,
        // ie they were added when moving from A to B
        diffDicSub<T, U>(dicB, dicA, dicAdd);
    }

    private static void diffDicSub<T, U>(IDictionary<T, U> dicA, IDictionary<T, U> dicB, IDictionary<T, U> dicAExceptB)
    {
        // Walk A, and if any of the entries are not
        // in B, add them to the result dictionary.

        foreach (KeyValuePair<T, U> kvp in dicA)
        {
            if (!dicB.ContainsKey(kvp.Key))
            {
                dicAExceptB[kvp.Key] = (U)kvp.Value;
            }
        }
    }
}