using Godot;

using System.Collections.Generic;
using System.Reflection;

namespace GodotAppFramework;

public partial class AppConfigDialogContentDefault : AppConfigDialogContent
{
	[Export] public Button SaveButton { get; set; } = null!;
	[Export] public Button CancelButton { get; set; } = null!;

	[Export] public Control GeneratedControlsContainer { get; set; } = null!;

	public override void _Ready()
	{
		if (IsInstanceValid(SaveButton))
		{
			SaveButton.Pressed += () =>
			{
				ApplyChanges();
				var appConfigManager = AppConfigManager.GetInstance();
				appConfigManager?.CloseAppSettingsDialog();
			};
		}

		if (IsInstanceValid(CancelButton))
		{
			CancelButton.Pressed += () =>
			{
				AppConfigManager.GetInstance()?.CloseAppSettingsDialog();
			};
		}
	}

	protected override void Initialize(List<Control> generatedControls)
	{
		if (!IsInstanceValid(GeneratedControlsContainer))
		{
			return;
		}
		
		foreach (var control in generatedControls)
		{
			GeneratedControlsContainer.AddChild(control);
		}
	}
}
