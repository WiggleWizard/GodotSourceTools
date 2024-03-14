using Godot;

using System;
using System.Reflection;
using WinRT;

namespace GodotAppFramework.Extensions;

public static class VariantExtensions
{
    // Returns false if the variant is at all invalid (nil, nullptr, deleted, etc)
    public static bool IsValid(this Variant v)
    {
        if (v.VariantType == Variant.Type.Nil)
        {
            return false;
        }

        if (v.Obj == null)
        {
            return false;
        }

        if (v.VariantType == Variant.Type.Object && !GodotObject.IsInstanceValid(v.AsGodotObject()))
        {
            return false;
        }

        return true;
    }

    // Holder method, awaiting more advanced implementation
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

    // Compares variant's contents to check if they are equal
    public static bool IsEqualTo(this Variant v, Variant other)
    {
        if (v.VariantType != other.VariantType)
        {
            return false;
        }
        
        switch (v.VariantType)
        {
            case Variant.Type.Int: return v.AsInt64() == other.AsInt64();
            case Variant.Type.Float: return v.AsDouble() == other.AsDouble();
            case Variant.Type.String: return v.AsString() == other.AsString();
            case Variant.Type.Array:
            {
                var gdArray = v.AsGodotArray();
                var otherGdArray = other.AsGodotArray();
                foreach (var e in gdArray)
                {
                    if (!otherGdArray.Contains(e))
                    {
                        return false;
                    }
                }

                return true;
            }
            default: return false;
        }
    }
}