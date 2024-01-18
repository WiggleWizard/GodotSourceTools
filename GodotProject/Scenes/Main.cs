using Godot;
using System;

using NativeFileDialogs.Net;

public partial class Main : Node
{
    [Export] public Node TabContainer { set; get; }
    [Export] public MenuBar MainMenu { set; get; }

    [Export] public String TabScenesPath { set; get; } = "res://Scenes/Tabs";

    public override void _Ready()
    {
        var d = DirAccess.Open(TabScenesPath);
        foreach (var tabDir in d.GetDirectories())
        {
            d.ChangeDir(tabDir);
            foreach (var f in d.GetFiles())
            {
                if (f.EndsWith(".tscn"))
                {
                    String fullFilePath = $"{d.GetCurrentDir()}/{f}";
                    PackedScene scene = ResourceLoader.Load<PackedScene>(fullFilePath);
                    Node newTabScene = scene.Instantiate();
                }
            }
            d.ChangeDir("..");
        }
    }

    public void OnFileIdPressed(int id)
    {
        if (id == 0)
        {
            AppConfig appConfig = AppConfig.GetInstance();
            String lastOpenedDir = appConfig.GetConfigVarString("last_opened_dir", "C:/");
            
            String outPath = "";
            NfdStatus status = Nfd.PickFolder(out outPath, lastOpenedDir);
            if (status == NfdStatus.Cancelled)
                return;
            
            appConfig.SetConfigVar("last_opened_dir", outPath);
            
            SourceManager sourceManager = SourceManager.GetInstance();
            sourceManager.OpenSourceDir(outPath);
        }
    }
}
