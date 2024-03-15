using System;
using Godot;
using Godot.Collections;

[Tool]
public partial class Sidebar : Control
{
    private const string ThemeType = "Sidebar";
    
    private Array<string> _items = new();
    [Export] public Array<string> Items
    {
        get => _items;
        set { _items = value; PropChanged(); }
    }
    
    private int _selectedIndex = 0;
    [Export] public int SelectedIndex
    {
        get => _selectedIndex;
        set { _selectedIndex = value; PropChanged(); }
    }
    
    private Control _switchContainer = null!;
    [Export] public Control SwitchContainer
    {
        get => _switchContainer;
        set { _switchContainer = value; PropChanged(); }
    }

    [Signal] public delegate void TabChangedEventHandler(int index);

    private int _indexHovering = -1;

    public override void _Ready()
    {
        OnNewIndexSelected(_selectedIndex);
        
        Connect(SignalName.MouseExited, Callable.From(() =>
        {
            _indexHovering = -1;
            QueueRedraw();
        }));
    }

    public override void _Draw()
    {
        var backgroundStylebox = GetThemeStylebox("background", ThemeType);
        if (backgroundStylebox != null)
        {
            var localRect = GetRect();
            localRect.Position = Vector2.Zero;
            DrawStyleBox(backgroundStylebox, localRect);
        }

        var itemFont = GetThemeFont("item", ThemeType);
        if (itemFont == null)
            return;
        
        int itemMargin = GetThemeConstant("item_margin", ThemeType);
        int itemHeight = GetThemeConstant("item_height", ThemeType);
        int itemYOffset = GetThemeConstant("item_y_offset", ThemeType);
        int itemFontSize = GetThemeFontSize("item", ThemeType);
        Color itemFontColor = GetThemeColor("item", ThemeType);
        Color itemHoveredFontColor = GetThemeColor("item_hovered", ThemeType);
        Color itemSelectedFontColor = GetThemeColor("item_selected", ThemeType);
        
        int hoveredBgSize = GetThemeConstant("hovered_bg_size", ThemeType);
        StyleBox hoveredBgStyleBox = GetThemeStylebox("hovered_background", ThemeType);

        StyleBox selectedBgStyleBox = GetThemeStylebox("selected_background", ThemeType);
        int selectedBgSize = GetThemeConstant("selected_bg_size", ThemeType);

        StyleBox selectedPipStyleBox = GetThemeStylebox("selected_pip", ThemeType);
        int selectedPipWidth = GetThemeConstant("selected_pip_width", ThemeType);
        
        float sidebarWidth = Size.X;
        for (int i = 0; i < _items.Count; ++i)
        {
            bool itemHovered = i == _indexHovering;
            bool itemSelected = i == _selectedIndex;
            
            var item = _items[i];
            var fontHeight = itemFont.GetHeight(itemFontSize);
            Vector2 p = new Vector2(0, itemHeight * i + itemMargin * i);
            Rect2 rect = new Rect2(p, new Vector2(sidebarWidth, itemHeight));
            
            // Draw highlight background
            if (itemHovered)
            {
                //DrawRect(rect, Colors.White);
            }

            // Highlight selected entry background
            if (itemHovered)
            {
                Rect2 highlightedRect = new Rect2(rect.Position.X + rect.Size.X / 2, rect.Position.Y + rect.Size.Y / 2, 0, 0);
                highlightedRect = highlightedRect.Grow(selectedBgSize / 2F);
                hoveredBgStyleBox.Draw(GetCanvasItem(), highlightedRect);
            }
            
            // Draw the actual item icon
            Color iconColor = itemFontColor;
            if (itemSelected) iconColor = itemSelectedFontColor;
            if (itemHovered) iconColor = itemHoveredFontColor;
            Vector2 p2 = p;
            p2.Y += fontHeight / 2f + itemHeight / 2f + itemYOffset;
            DrawString(itemFont, p2, item, HorizontalAlignment.Center, sidebarWidth, itemFontSize, iconColor);

            // Draw the pip
            if (itemSelected)
            {
                Rect2 pipRect = new Rect2(sidebarWidth - selectedPipWidth, rect.Position.Y, selectedPipWidth, rect.Size.Y);
                selectedPipStyleBox.Draw(GetCanvasItem(), pipRect);
            }
        }
    }
    
    private void PropChanged()
    {
        QueueRedraw();
    }

    public override void _GuiInput(InputEvent e)
    {
        if (e is InputEventMouseMotion mouseMotion)
        {
            int prevIndex = _indexHovering;
            _indexHovering = GetIndexFromPosition(mouseMotion.Position.Y);
            if (prevIndex != _indexHovering)
            {
                QueueRedraw();
            }
        }

        if (e is InputEventMouseButton mouseButton)
        {
            if (!e.IsEcho() && mouseButton.ButtonIndex == MouseButton.Left)
            {
                int prevIndex = _selectedIndex;
                int newIndex = GetIndexFromPosition(mouseButton.Position.Y);
                if (newIndex != -1)
                {
                    _selectedIndex = newIndex;
                }
                
                if (prevIndex != _selectedIndex)
                {
                    OnNewIndexSelected(_selectedIndex);
                    QueueRedraw();
                }
            }
        }
    }

    private int GetIndexFromPosition(float posY)
    {
        int itemMargin = GetThemeConstant("item_margin", ThemeType);
        int itemHeight = GetThemeConstant("item_height", ThemeType);
        
        float totalHeight = itemHeight * _items.Count + itemMargin * _items.Count;
        float actualItemHeight = totalHeight / _items.Count;
        int newIndex = (int)Math.Floor(posY / actualItemHeight);
       
        if (newIndex < _items.Count && newIndex >= 0)
        {
            return newIndex;
        }

        return -1;
    }

    private void OnNewIndexSelected(int idx)
    {
        if (_switchContainer == null)
        {
            return;
        }

        foreach (var child in _switchContainer.GetChildren())
        {
            if (child is Control control)
            {
                control.Hide();
                control.QueueRedraw();
            }
        }

        Node? selectedChild = _switchContainer.GetChild(idx);
        if (selectedChild != null && selectedChild is Control selectedControl)
        {
            selectedControl.Show();
            selectedControl.QueueRedraw();
        }
    }
}
