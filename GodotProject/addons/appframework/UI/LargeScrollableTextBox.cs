using System;
using Godot;
using Godot.Collections;

namespace GodotAppFramework.UI;

[Tool]
public partial class LargeScrollableTextBox : Control
{
    private Array<string> _lines = new();
    private VScrollBar _vScrollBar = new();

    [Export] private float LineHeight { set; get; } = 14;
    [Export] public bool FollowLatest { set; get; } = false;
    [Export] public bool StopFollowLatestWhenScrolling { set; get; } = true;

    public override void _Ready()
    {
        ClipContents = true;
        
        AddChild(_vScrollBar);
        _vScrollBar.Owner = this;
        _vScrollBar.SetAnchorsAndOffsetsPreset(LayoutPreset.RightWide);
        _vScrollBar.MaxValue = 1;
        _vScrollBar.Page = 1;
    }

    public override void _Process(double delta)
    {
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (GetLineCount() <= 0)
        {
            return;
        }
        
        _vScrollBar.MaxValue = GetLineCount();
        
        // Figure out how many lines we can dump here
        int lineCount = Math.Min((int)Math.Round(GetRect().Size.Y / LineHeight), GetLineCount());
        _vScrollBar.Page = lineCount;

        // Figure out, from the scrollbar, what line we are one
        float v = (float)_vScrollBar.Value;
        int lineStart = (int)Math.Floor(v);
        int lineEnd = lineStart + lineCount + 1;
        float offset = (v * LineHeight) % LineHeight;

        int renderLine = 0;
        for (int i = lineStart; i < lineEnd; ++i)
        {
            Vector2 pos = new Vector2(0, LineHeight * renderLine + LineHeight - offset);
            DrawStringLine(pos, i);
            renderLine++;
        }

        if (FollowLatest)
            _vScrollBar.Value = _vScrollBar.MaxValue;
    }

    public override void _GuiInput(InputEvent @event)
    {
        InputEventMouseButton? eventMouseButton = @event as InputEventMouseButton;
        if (eventMouseButton != null)
        {
            if (eventMouseButton.Pressed && !eventMouseButton.IsEcho())
            {
                if (eventMouseButton.ButtonIndex == MouseButton.WheelDown)
                {
                    _vScrollBar.Value += 4;

                    if (_vScrollBar.Value + _vScrollBar.Page == _vScrollBar.MaxValue)
                        FollowLatest = true;
                }
                else if (eventMouseButton.ButtonIndex == MouseButton.WheelUp)
                {
                    if (StopFollowLatestWhenScrolling)
                        FollowLatest = false;
                    
                    _vScrollBar.Value -= 4;
                }
            }
        }
    }

    public virtual void DrawStringLine(Vector2 position, int lineNumber)
    {
        Font font = GetThemeFont("font", "LargeScrollableTextBox");
        if (font == null)
            font = GetThemeDefaultFont();

        int fontSize = GetThemeFontSize("font_size", "LargeScrollableTextBox");
        
        DrawString(font, position, GetLine(lineNumber), HorizontalAlignment.Left, -1, fontSize);
    }
    
    public virtual string GetLine(int lineNumber)
    {
        if (lineNumber >= _lines.Count || lineNumber < 0)
            return "";
        
        return _lines[lineNumber];
    }

    public virtual int GetLineCount()
    {
        return _lines.Count;
    }

    public void SetLineArray(Array<string> lineArray)
    {
        _lines = lineArray;
    }
}