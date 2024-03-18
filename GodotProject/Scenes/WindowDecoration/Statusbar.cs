using GodotAppFramework;

using Godot;

namespace GodotSourceTools;

public partial class Statusbar : Panel
{
	[Export] private Control _statusTextContainer = null!;
	[Export] private Label _versionText = null!;

	private Label _readyLabel = new();
	
	public override void _Ready()
	{
		if (!IsInstanceValid(_statusTextContainer))
		{
			GD.Print($"{nameof(_statusTextContainer)} is null, statusbar isn't going to work as expected");
			return;
		}

		RebuildStatusDisplay();
		
		if (StatusManager.Instance != null)
		{
			StatusManager.Instance.StatusLocked += (guid, name) =>
			{
				RebuildStatusDisplay();
			};
			
			StatusManager.Instance.StatusUnlocked += (guid) =>
			{
				RebuildStatusDisplay();
			};
		}
		else
		{
			GD.Print($"{nameof(StatusManager)} needs to be part of autoloads for the statusbar to operate correctly");
		}
		
		// Show the application version
		_versionText.Text = AppFrameworkManager.GetAppVersion().ToString();
	}

	private void ClearStatusContainer()
	{
		foreach (var child in _statusTextContainer.GetChildren())
		{
			_statusTextContainer.RemoveChild(child);
		}
	}

	private void RebuildStatusDisplay()
	{
		ClearStatusContainer();

		if (StatusManager.Instance == null)
		{
			return;
		}
		
		if (StatusManager.Instance.StatusStack.Count == 0)
		{
			_readyLabel = new Label();
			_readyLabel.Name = "Ready";
			_readyLabel.Text = "Ready";
			_statusTextContainer.AddChild(_readyLabel);

			return;
		}

		bool first = true;
		foreach (var status in StatusManager.Instance.StatusStack)
		{
			if (!first)
			{
				_statusTextContainer.AddChild(new VSeparator());
			}
			
			var label = new Label();
			label.Text = status.Value;
			label.Name = status.Key.ToString("N");
			_statusTextContainer.AddChild(label);
			
			first = false;
		}
	}
}
