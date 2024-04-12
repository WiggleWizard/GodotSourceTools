using Godot;
using GodotAppFramework;

public partial class BurgerButton : Button
{
    private PopupMenu _dropdownMenu = new();

    private enum DropdownMenuOptions
    {
        Wizard,
        LoadProject,
        Sep1,
        AppSettings,
    }

    public override void _Ready()
    {
        _dropdownMenu.Clear();
        _dropdownMenu.InitialPosition = Window.WindowInitialPosition.Absolute;

        _dropdownMenu.AddItem("Source Wizard", (int)DropdownMenuOptions.Wizard);
        _dropdownMenu.AddItem("Open Project", (int)DropdownMenuOptions.LoadProject);
        _dropdownMenu.AddSeparator();
        _dropdownMenu.AddItem("Settings", (int)DropdownMenuOptions.AppSettings);

        _dropdownMenu.IdPressed += OnIdPressed;
        
        AddChild(_dropdownMenu);
        
        _dropdownMenu.ResetSize();
    }

    public override void _Pressed()
    {
        Vector2 thisPos = GetScreenPosition();
        thisPos.Y += Size.Y;
        
        _dropdownMenu.Position = new Vector2I((int)thisPos.X, (int)thisPos.Y);
        _dropdownMenu.Show();
    }

    private void OnIdPressed(long id)
    {
        if (id == (long)DropdownMenuOptions.LoadProject)
        {
            FileDialog fileDialog = new();
            fileDialog.UseNativeDialog = true;
            fileDialog.FileMode = FileDialog.FileModeEnum.OpenDir;
            fileDialog.Access = FileDialog.AccessEnum.Filesystem;
            fileDialog.DirSelected += OnProjectDirSelected;

            fileDialog.Show();
        }
        else if (id == (long)DropdownMenuOptions.AppSettings)
        {
            AppConfigManager.GetInstance()?.ShowAppSettingsDialog();
        }
        else if (id == (long)DropdownMenuOptions.Wizard)
        {
            var window = WindowManager.Instance?.CreateExclusiveWindow(GD.Load<PackedScene>("res://Scenes/SourceWizard/Main.tscn"), "Source Wizard");
        }
    }

    private void OnProjectDirSelected(string dir)
    {
        SourceManager.LastOpenedSourceDir = dir;
        var sourceManager = SourceManager.GetInstance();
        sourceManager?.OpenSourceDir(dir);
    }
}
