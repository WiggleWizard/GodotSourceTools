using Godot;

using System.Collections.Generic;
using System.Reflection;

namespace GodotAppFramework;

public partial class AppConfigDialogContentDefault : AppConfigDialogContent
{
	[Export] public Button? SaveButton { get; set; }
	[Export] public Button? CancelButton { get; set; }
	
	[Export] public Control GeneratedControlsContainer { get; set; }

	public override void _Ready()
	{
		if (SaveButton != null)
		{
			SaveButton.Pressed += () =>
			{
				ApplyChanges();
				var appConfigManager = AppConfigManager.GetInstance();
				appConfigManager?.CloseAppSettingsDialog();
			};
		}

		if (CancelButton != null)
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
