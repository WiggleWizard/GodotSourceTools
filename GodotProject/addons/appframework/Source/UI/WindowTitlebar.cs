using Godot;
using System;

public partial class WindowTitlebar : Control
{
    [Export] private Button MinWindowControl { get; set; } = null!;
    [Export] private Button MaxWindowControl { get; set; } = null!;
    [Export] private Button CloseWindowControl { get; set; } = null!;

    private bool IsDragging { get; set; } = false;
    public Vector2I DragDelta { get; set; }

    public override void _Ready()
    {
        if (IsInstanceValid(MinWindowControl))
        {
            MinWindowControl.Pressed += OnMinWindowControlPressed;
        }

        if (IsInstanceValid(MaxWindowControl))
        {
            MaxWindowControl.Pressed += OnMaxWindowControlPressed;
        }

        if (IsInstanceValid(CloseWindowControl))
        {
            CloseWindowControl.Pressed += OnCloseWindowControlPressed;
        }
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
        GetTree().Root.PropagateNotification((int)NotificationWMCloseRequest);
        GetTree().Quit();
    }
}
