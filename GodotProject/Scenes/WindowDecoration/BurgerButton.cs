using Godot;

public partial class BurgerButton : Button
{
    private PopupMenu _dropdownMenu = new();

    private enum DropdownMenuOptions
    {
        LoadProject
    }

    public override void _Ready()
    {
        _dropdownMenu.Clear();
        _dropdownMenu.InitialPosition = Window.WindowInitialPosition.Absolute;

        _dropdownMenu.AddItem("Open Project...", (int)DropdownMenuOptions.LoadProject);

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
    }

    private void OnProjectDirSelected(string dir)
    {
        AppConfig appConfig = AppConfig.GetInstance();
        appConfig.SetConfigVar("last_opened_dir", dir);
			
        SourceManager sourceManager = SourceManager.GetInstance();
        sourceManager.OpenSourceDir(dir);
    }
}
