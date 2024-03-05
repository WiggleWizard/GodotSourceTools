using Godot;

using System;
using System.Reflection;
using WinRT;

namespace GodotAppFramework.Extentions;

public static class VariantExtentions
{
    // Returns false if the variant is at all invalid (nil, nullptr, deleted, etc)
    public static bool IsValid(this Variant v)
    {
        return v.VariantType == Variant.Type.Nil || v.Obj == null || !GodotObject.IsInstanceValid(v.AsGodotObject());
    }

    public static object? ToCSharpObject(this Variant v)
    {
        return v.Obj;
    }
    
    // Sets a static property of a type using the variant as the "value"
    public static void SetStaticProp(this Variant v, Type t, PropertyInfo propInfo)
    {
        if (!v.IsValid())
        {
            return;
        }
        
        switch (v.VariantType)
        {
            case Variant.Type.Int:
                propInfo.SetValue(null, v.AsInt32());
                break;
            case Variant.Type.Float:
                propInfo.SetValue(null, v.AsDouble());
                break;
            default:
                propInfo.SetValue(null, v.ToCSharpObject());
                break;
        }
    }
}