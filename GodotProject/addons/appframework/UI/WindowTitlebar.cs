using Godot;
using System;

public partial class WindowTitlebar : Control
{
    [Export] private Button MinWindowControl;
    [Export] private Button MaxWindowControl;
    [Export] private Button CloseWindowControl;

    private bool IsDragging { get; set; } = false;
    public Vector2I DragDelta { get; set; }

    public override void _Ready()
    {
        MinWindowControl.Pressed += OnMinWindowControlPressed;
        MaxWindowControl.Pressed += OnMaxWindowControlPressed;
        CloseWindowControl.Pressed += OnCloseWindowControlPressed;
    }

    public override void _Process(double delta)
    {
        if (IsDragging)
        {
            GetTree().Root.Position += DragDelta;
        }
        
        DragDelta = new();
    }

    public override void _GuiInput(InputEvent e)
    {
        if (e is InputEventMouseButton mouseButton)
        {
            if (mouseButton.ButtonIndex == MouseButton.Left)
            {
                IsDragging = mouseButton.Pressed;
            }
        }

        if (e is InputEventMouseMotion mouseMotion)
        {
            DragDelta += (Vector2I)mouseMotion.Relative;
        }
    }

    private void OnMinWindowControlPressed()
    {
        GetTree().Root.Mode = Window.ModeEnum.Minimized;
    }

    private void OnMaxWindowControlPressed()
    {
        GetTree().Root.Mode = Window.ModeEnum.Maximized;
    }

    private void OnCloseWindowControlPressed()
    {
        GetTree().Root.EmitSignal(Window.SignalName.CloseRequested);
    }
}
