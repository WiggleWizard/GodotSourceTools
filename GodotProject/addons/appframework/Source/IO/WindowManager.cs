using Godot;

using System.Collections.Generic;

namespace GodotAppFramework;

public partial class WindowManager : Node
{
    public static WindowManager? Instance { get; private set; } = null;

    private List<Window> _activeWindows = new();

    public override void _Ready()
    {
        Instance = this;
    }

    /// <summary>
    /// Creates an exclusive window (main window stop taking inputs).
    /// </summary>
    /// <param name="content"></param>
    /// <param name="title"></param>
    /// <returns></returns>
    public Window? CreateExclusiveWindow(PackedScene? content, string title)
    {
        if (content == null)
        {
            GD.PrintErr($"Creation of window could not be done, content is null");
            return null;
        }
        
        Control? instance = content.Instantiate<Control>();
        if (instance == null)
        {
            GD.PrintErr($"Creation of window could not be done, content is not a control");
            return null;
        }

        return CreateExclusiveWindow(instance, title);
    }

    public Window? CreateExclusiveWindow(Control content, string title)
    {
        Window window = new Window();

        window.Borderless = false;
        window.InitialPosition = Window.WindowInitialPosition.CenterMainWindowScreen;
        window.Unresizable = true;
        window.Title = title;
        window.Size = new Vector2I((int)content.Size.X, (int)content.Size.Y);
        window.Transient = true;
        window.Exclusive = true;
        window.CloseRequested += () =>
        {
            _activeWindows.Remove(window);
            window.QueueFree();
        };

        window.AddChild(content);
        AddChild(window);

        return window;
    }

    public void AddCloseButton(Window window, Button button)
    {
        button.Pressed += () =>
        {
            _activeWindows.Remove(window);
            window.QueueFree();
        };
    }
}