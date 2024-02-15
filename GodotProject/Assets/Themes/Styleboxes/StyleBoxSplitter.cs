using Godot;
using System;

[GlobalClass]
public partial class StyleBoxSplitter : StyleBoxFlat
{
    public override void _Draw(Rid toCanvasItem, Rect2 rect)
    {
        RenderingServer.CanvasItemAddRect(toCanvasItem, rect, Colors.Red);
    }
}
