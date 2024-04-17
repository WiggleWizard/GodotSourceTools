using Godot;

namespace GodotAppFramework.Extensions;

public static class GodotObjectExtensions
{
    public static bool EnsureInstanceValid(this GodotObject godotObject)
    {
        if (GodotObject.IsInstanceValid(godotObject))
        {
            GD.PrintErr($"{}");
            return true;
        }

        return false;
    }
}