using System.Drawing.Printing;
using Godot;
using Godot.Collections;

namespace GodotSourceTools;

public class Margins
{
    public int Left = 0;
    public int Right = 0;
    public int Top = 0;
    public int Bottom = 0;
    
    public Margins(int left, int right, int top, int bottom)
    {
        Left = left;
        Right = right;
        Top = top;
        Bottom = bottom;
    }

    public Margins(int leftRight, int topBottom)
    {
        Left = Right = leftRight;
        Top = Bottom = topBottom;
    }

    public Margins(int allMargins)
    {
        Left = Right = Top = Bottom = allMargins;
    }
}

public class UIDrawing
{
    static void DrawCenteredRect(Control control, Vector2 position, Vector2 size, Color color, bool fill, float outlineThickness = -1)
    {
        Rect2 rect = new(position + size / 2, size);
        control.DrawRect(rect, color, fill, outlineThickness);
    }

    static void DrawCenteredStyleBox(StyleBox stylebox, Control control, Vector2 position, Vector2 size, Margins margins)
    {
        
    }
}