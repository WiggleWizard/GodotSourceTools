using Godot;

namespace GodotAppFramework.UI;

public partial class ItemListCustomItem : Control
{
    private bool _isSelected = false;
    
    [Signal] public delegate void SelectedEventHandler();
    
    public override void _Draw()
    {
        var rect = new Rect2(new Vector2(0, 0), GetRect().Size);
        StyleBox? stylebox = null;

        if (_isSelected)
        {
            stylebox = GetThemeStylebox("selected", "ItemList");
        }

        if (stylebox != null)
        {
            DrawStyleBox(stylebox, rect);
        }
    }

    public void SetSelected(bool isSelected)
    {
        if (isSelected)
            EmitSignal(SignalName.Selected);
        
        _isSelected = isSelected;
        
        QueueRedraw();
    }
}