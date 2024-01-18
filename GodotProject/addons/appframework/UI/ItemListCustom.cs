using System;
using Godot;

namespace GodotAppFramework.UI;

public partial class ItemListCustom : Control
{
    private ItemListCustomItem? _currentSelectedItem = null;
    
    [Signal]
    public delegate void ItemSelectedEventHandler(ItemListCustomItem item);

    public override void _Draw()
    {
        var rect = new Rect2(new Vector2(0, 0), GetRect().Size);
        var stylebox = GetThemeStylebox("panel", "ItemList");
        DrawStyleBox(stylebox, rect);
    }

    public T AddItem<T>(PackedScene packedScene) where T : ItemListCustomItem
    {
        ItemListCustomItem instance = packedScene.Instantiate<ItemListCustomItem>();
        AddChild(instance);
        instance.Owner = this;

        instance.GuiInput += e => OnItemGuiInput(e, instance); 
        
        return (T)Convert.ChangeType(instance, typeof(T));
    }

    public ItemListCustomItem? GetCurrentlySelectedItem()
    {
        return _currentSelectedItem;
    }

    public void SetCurrentlySelectedItem(int idx)
    {
        if (GetChild(idx) is ItemListCustomItem selectingItem)
        {
            foreach (var child in GetChildren())
            {
                if (child is ItemListCustomItem item)
                {
                    item.SetSelected(false);
                }
            }
            
            selectingItem.SetSelected(true);

            _currentSelectedItem = selectingItem;

            EmitSignal(SignalName.ItemSelected, selectingItem);
        }
    }

    public void SelectLastItem()
    {
        SetCurrentlySelectedItem(GetChildCount() - 1);
    }

    private void OnItemGuiInput(InputEvent e, ItemListCustomItem instance)
    {
        if (e is InputEventMouseButton mouseButtonEvent)
        {
            if (!mouseButtonEvent.IsEcho() && !mouseButtonEvent.Pressed)
            {
                if (mouseButtonEvent.ButtonIndex == MouseButton.Left)
                {
                    foreach (var child in GetChildren())
                    {
                        if (child is ItemListCustomItem item)
                        {
                            item.SetSelected(false);
                        }
                    }

                    instance.SetSelected(true);

                    _currentSelectedItem = instance;

                    EmitSignal(SignalName.ItemSelected, instance);
                }
            }
        }
    }
}