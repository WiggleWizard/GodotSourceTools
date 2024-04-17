using Godot;
using GDArray = Godot.Collections.Array;

using System;
using System.Collections.Generic;

[Tool, GlobalClass]
public partial class FilesystemPathSelector : Control
{
    private string _initialPath = "";
    [Export] public string InitialPath
    {
        get => _initialPath;
        set { _initialPath = value; PropertiesChanged(); }
    }
    
    private string _openButtonText = "...";
    [Export] public string OpenButtonText
    {
        get => _openButtonText;
        set { _openButtonText = value; PropertiesChanged(); }
    }
    
    private Font _openButtonFont = null!;
    [Export] public Font OpenButtonFont
    {
        get => _openButtonFont;
        set { _openButtonFont = value; PropertiesChanged(); }
    }

    [Export] private FileDialog.FileModeEnum DialogMode { get; set; } = FileDialog.FileModeEnum.OpenFile;

    [Signal] public delegate void PathSelectedEventHandler(string[] paths, FileDialog.FileModeEnum mode);
    
    protected LineEdit TextEditor = new();
    protected Button ExplorerButton = new();
        
    public override void _Ready()
    {
        HBoxContainer hBoxContainer = new();
        hBoxContainer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        
        TextEditor.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        
        hBoxContainer.AddChild(TextEditor);
        hBoxContainer.AddChild(ExplorerButton);
        AddChild(hBoxContainer);

        ExplorerButton.Pressed += OnExplorerButtonPressed;

        PathSelected += (paths, mode) =>
        {
            TextEditor.Text = paths[0];
        };
        
        PropertiesChanged();
    }

    public void PropertiesChanged()
    {
        ExplorerButton.Text = OpenButtonText;
        TextEditor.Text = InitialPath;
        ExplorerButton.AddThemeFontOverride("font", OpenButtonFont);
    }

    private void OnExplorerButtonPressed()
    {
        FileDialog fileDialog = new();
        fileDialog.UseNativeDialog = true;
        fileDialog.FileMode = DialogMode;
        fileDialog.Access = FileDialog.AccessEnum.Filesystem;
        
        fileDialog.DirSelected += path =>
        {
            string[] arr = { path };
            EmitSignal(SignalName.PathSelected, arr, (int)DialogMode);
        };
        
        fileDialog.FileSelected += path =>
        {
            string[] arr = { path };
            EmitSignal(SignalName.PathSelected, arr, (int)DialogMode);
        };
        
        fileDialog.FilesSelected += paths =>
        {
            EmitSignal(SignalName.PathSelected, paths, (int)DialogMode);
        };

        fileDialog.Show();
    }
}
