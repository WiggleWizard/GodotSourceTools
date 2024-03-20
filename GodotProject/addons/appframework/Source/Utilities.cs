using Godot;
using GdCollections = Godot.Collections;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.ComponentModel;

namespace GodotAppFramework;

public class Utilities
{
    public static bool EnsureManager<T>(object? manager)
    {
        if (manager == null)
        {
            GD.PrintErr($"The manager {typeof(T)} does not exist, ensure it's loaded at least once");
            return false;
        }
        
        return true;
    }

    public static T? GetAutoload<T>(bool fatal = false, string customAutoloadName = "")
    {
        var manager = default(T);
        
        return manager;
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

        if (oType == typeof(GdCollections.Dictionary))
        {
            return (GdCollections.Dictionary)o;
        }
        
        Variant variantValue = o switch
        {
            int v => Variant.From(v),
            float v => Variant.From(v),
            string v => Variant.From(v),
            Int64 v => Variant.From(v),
            _ => throw new NotSupportedException(),
        };

        return variantValue;
    }
    
    public static void DiffDictionaries<T, U>(IDictionary<T, U> dicA, IDictionary<T, U> dicB, IDictionary<T, U> dicAdd, IDictionary<T, U> dicDel)
    {
        // dicDel has entries that are in A, but not in B, 
        // ie they were deleted when moving from A to B
        DiffDicSub<T, U>(dicA, dicB, dicDel);

        // dicAdd has entries that are in B, but not in A,
        // ie they were added when moving from A to B
        DiffDicSub<T, U>(dicB, dicA, dicAdd);
    }

    private static void DiffDicSub<T, U>(IDictionary<T, U> dicA, IDictionary<T, U> dicB, IDictionary<T, U> dicAExceptB)
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