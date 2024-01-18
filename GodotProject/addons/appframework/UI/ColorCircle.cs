using System;
using Godot;

namespace GodotAppFramework.UI;

[Tool, GlobalClass]
public partial class ColorCircle : Control
{
    private Color _color = Godot.Colors.White;
    [Export] public Color Color
    {
        get => _color;
        set { _color = value; PropChanged(); }
    }

    private bool _outline = false;
    [Export] public bool Outline
    {
        get => _outline;
        set { _outline = value; PropChanged(); }
    }

    private float _outlineWidth = 1;
    [Export] public float OutlineWidth
    {
        get => _outlineWidth;
        set { _outlineWidth = value; PropChanged(); }
    }

    private bool _centerPivot = true;
    [Export] public bool CenterPivot
    {
        get => _centerPivot;
        set { _centerPivot = value; PropChanged(); }
    }
    
    private float _arcStartAngle = 0;
    [Export] public float ArcStartAngle
    {
        get => _arcStartAngle;
        set { _arcStartAngle = value; PropChanged(); }
    }
    
    private float _arcEndAngle = 360;
    [Export] public float ArcEndAngle
    {
        get => _arcEndAngle;
        set { _arcEndAngle = value; PropChanged(); }
    }
    
    private int _resolution = 32;
    [Export] public int Resolution
    {
        get => _resolution;
        set { _resolution = value; PropChanged(); }
    }
    
    public override void _Ready()
    {
        ItemRectChanged += OnItemRectChanged;
    }

    public override void _Draw()
    {
        var rect = GetRect();
        
        // Get the shortest side of the rect so we can use that as our circle radius
        float radius = Math.Min(rect.Size.X, rect.Size.Y) / 2;
        Vector2 center = rect.Size / 2;
        float outlineThickness = _outline ? Math.Min(_outlineWidth, radius) : radius;

        float arcStartAngleRad = float.DegreesToRadians(_arcStartAngle - 90f);
        float arcEndAngleRad = float.DegreesToRadians(_arcEndAngle - 90f);

        DrawArc(center, radius - outlineThickness / 2, arcStartAngleRad, arcEndAngleRad, _resolution, _color, outlineThickness, true);
    }

    private void PropChanged()
    {
        OnItemRectChanged();
        QueueRedraw();
    }

    private void OnItemRectChanged()
    {
        if (_centerPivot)
        {
            PivotOffset = GetRect().Size / 2;
        }
    }
}