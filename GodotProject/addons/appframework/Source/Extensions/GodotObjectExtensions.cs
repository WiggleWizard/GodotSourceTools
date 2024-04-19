using Godot;

using System.Reflection;

namespace GodotAppFramework.Extensions;

public static class GodotObjectExtensions
{
    public static bool IsExportValid(this GodotObject thisObject, string exportName)
    {
        var thisType = thisObject.GetType();
        var prop = thisType.GetProperty(exportName);
        if (prop == null)
        {
            return false;
        }

        return thisObject.IsExportValid(prop);
    }

    public static bool IsExportValid(this GodotObject thisObject, PropertyInfo prop)
    {
        var value = prop.GetValue(thisObject, null);
        if (value is GodotObject godotObject)
        {
            if (GodotObject.IsInstanceValid(godotObject))
            {
                return true;
            }
        }

        return false;   
    }
}