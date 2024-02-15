using Godot;

namespace StylesheetUI;

[Tool]
public partial class PluginStylesheetUI : EditorPlugin
{
    private StylesheetImporter? _stylesheetImporter = null;
    
    public override void _EnterTree()
    {
        Script scriptStylesheet = GD.Load<Script>("res://addons/StylesheetUI/Stylesheet.cs");
        AddCustomType("Stylesheet", "Resource", scriptStylesheet, new PlaceholderTexture2D());
        Script scriptStylesheetUITheme = GD.Load<Script>("res://addons/StylesheetUI/StylesheetUITheme.cs");
        AddCustomType("StylesheetUITheme", "Theme", scriptStylesheetUITheme, new PlaceholderTexture2D());
        
        _stylesheetImporter = new();
        AddImportPlugin(_stylesheetImporter);
    }

    public override void _ExitTree()
    {
        RemoveCustomType("Stylesheet");
        RemoveCustomType("StylesheetUITheme");
        
        RemoveImportPlugin(_stylesheetImporter);
        _stylesheetImporter = null;
    }
}