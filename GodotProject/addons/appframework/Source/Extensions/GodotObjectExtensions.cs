using System;
using System.Diagnostics;
using System.Reflection;
using Godot;

namespace GodotAppFramework.Extensions;

public static class GodotObjectExtensions
{
    /// <summary>
    /// Note: Do not use this in hot path.
    /// </summary>
    /// <param name="godotObject"></param>
    /// <param name="exportName"></param>
    /// <returns></returns>
    public static bool EnsureExportValid(this GodotObject thisObject, string exportName, bool mandatory = true)
    {
        var thisType = thisObject.GetType();
        var prop = thisType.GetProperty(exportName);
        if (prop == null)
        {
            GD.PushError($"Property {exportName} does not exist for {thisObject.GetType().Name}");
            return false;
        }

        var value = prop.GetValue(exportName, null);
        if (value is GodotObject godotObject)
        {
            if (GodotObject.IsInstanceValid(godotObject))
            {
                return true;
            }
        }

        if (mandatory)
        {
            GD.PushError($"Mandatory export of {exportName} in {thisType.Name} is not assigned with a valid node");
            throw new Exception();
        }
        
        GD.PushWarning($"Export of {exportName} in {thisType.Name} is not assigned with a valid node");

        return false;
    }
}