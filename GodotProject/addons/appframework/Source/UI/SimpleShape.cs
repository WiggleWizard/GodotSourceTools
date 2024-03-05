using System;
using Godot;

namespace GodotAppFramework.UI;

public enum SimpleShapeType 
{
    Circle,
    Cross
}

[GlobalClass, Tool]
public partial class SimpleShape : Control
{
    private SimpleShapeType _simpleShapeType = SimpleShapeType.Circle;
    [Export] public SimpleShapeType SimpleShapeType
    {
        get => _simpleShapeType;
        set { _simpleShapeType = value; PropChanged(); }
    }
    
    private Color _color = Godot.Colors.White;
    [Export] public Color Color
    {
        get => _color;
        set { _color = value; PropChanged(); }
    }

    public override void _Draw()
    {
        float size = Math.Min(GetRect().Size.X, GetRect().Size.Y);
        
        switch (_simpleShapeType)
        {
            case SimpleShapeType.Cross:
            {
                DrawLine(new Vector2(0, 0), new Vector2(size, size), _color);
                DrawLine(new Vector2(size, 0), new Vector2(0, size), _color);
            } break;
            default:
            {
            } break;
        }
    }

    private void PropChanged()
    {
        QueueRedraw();
    }
}
