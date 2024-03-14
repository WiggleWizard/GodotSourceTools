using Godot;

namespace GodotAppFramework;

public partial class AppConfigDialogContentDefault : AppConfigDialogContent
{
	[Export] public Button? SaveButton { get; set; }
	[Export] public Button? CancelButton { get; set; }

	public override void _Ready()
	{
		if (SaveButton != null)
		{
			SaveButton.Pressed += () =>
			{
				var appConfigManager = AppConfigManager.GetInstance();
				appConfigManager?.SaveConfig();
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
}
