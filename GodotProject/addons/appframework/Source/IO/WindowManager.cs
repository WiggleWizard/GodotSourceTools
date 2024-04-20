using Godot;

using System.Collections.Generic;

namespace GodotAppFramework;

public partial class WindowManager : Node
{
    public static WindowManager? Instance { get; private set; } = null;

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
            CloseManagedWindow(window);
        };

        window.AddChild(content);
        window.Name = title;
        AddChild(window);
        
        return window;
    }

    /// <summary>
    /// Allows any input button to be able to close the window it belongs to.
    /// </summary>
    /// <param name="button"></param>
    public void AddCloseButton(Button button)
    {
        var buttonTree = button.GetTree();
        var window = buttonTree.Root;
        
        button.Pressed += () =>
        {
            CloseManagedWindow(window);
        };
    }

    public void CloseManagedWindow(Window? window)
    {
        if (window == null)
        {
            return;
        }
        
        if (window.GetParent() == this)
        {
            RemoveChild(window);
            window.QueueFree();   
        }
    }

    public Window? GetManagedWindowOfNode(Node node)
    {
        var window = node.GetWindow();
        if (window.GetParent() == this)
        {
            return window;
        }

        return null;
    }
}