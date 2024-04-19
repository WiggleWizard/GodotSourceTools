using GodotAppFramework.Globals;

using Godot;

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace GodotAppFramework.Extensions;

public static class NodeExtensions
{
    /// <summary>
    /// Note: Do not use this in hot path.
    /// </summary>
    /// <param name="thisObject"></param>
    /// <param name="exportName"></param>
    /// <returns></returns>
    public static bool EnsureExportValid(this Node thisObject, string exportName, bool isFatal = true)
    {
        var thisType = thisObject.GetType();
        var prop = thisType.GetProperty(exportName);
        if (prop == null)
        {
            AppFrameworkManager.FatalError($"Property {exportName} does not exist for {thisObject.GetType().Name}");
            return false;
        }

        return thisObject.EnsureExportValid(prop, isFatal);
    }

    public static bool EnsureExportValid(this Node thisObject, PropertyInfo prop, bool isFatal = true)
    {
        if (thisObject.IsExportValid(prop))
        {
            return true;
        }

        if (isFatal)
        {
            AppFrameworkManager.FatalError($"{thisObject.GetPath()}: Export `{prop.Name}` in {thisObject.GetType().Name} is not assigned with a valid node");
            //throw new Exception();
        }

        return false;
    }

    public static bool EnsureExportsAreValid(this Node node)
    {
        var errStr = "";
        var foundInvalidExport = false;
        
        var validateExportProps = node.GetType().GetProperties().Where(prop => Attribute.IsDefined(prop, typeof(ValidateExport)));
        foreach (var prop in validateExportProps)
        {
            var result = node.IsExportValid(prop);
            if (!result)
            {
                foundInvalidExport = true;
                errStr += $"{node.GetPath()}: Export `{prop.Name}` in {node.GetType().Name} is not assigned with a valid node\n";
            }
        }

        if (foundInvalidExport)
        {
            AppFrameworkManager.FatalError(errStr);
            return false;
        }
        
        return true;
    }

    public static void RemoveAllChildren(this Node node)
    {
        foreach (var child in node.GetChildren())
        {
            child.QueueFree();
        }
    }
}