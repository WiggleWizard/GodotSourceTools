using Godot;
using System;

public partial class Main : Node
{
	[Export] public Node TabContainer { set; get; } = null!;
	[Export] public MenuBar MainMenu { set; get; } = null!;

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
}
