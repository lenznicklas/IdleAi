using Godot;
using System;

namespace IdleAi;

public sealed class PrestigeOverlayController
{
	private readonly Game _root;
	private readonly GameState _state;
	private readonly PrestigeService _prestige;

	private Control _overlay = null!;
	private PanelContainer _panel = null!;
	private Label _info = null!;
	private Button _confirm = null!;
	private Button _oldCancel = null!;

	public event Action? Confirmed;
	public event Action? Cancelled;

	public bool Visible =>
		_overlay.Visible;


	public PrestigeOverlayController(
		Game root,
		GameState state,
		PrestigeService prestige)
	{
		_root = root;
		_state = state;
		_prestige = prestige;
	}


	public void Initialize()
	{
		_overlay =
			_root.GetNode<Control>(
				"PrestigeConfirmOverlay"
			);

		_panel =
			_root.GetNode<PanelContainer>(
				"PrestigeConfirmOverlay/Panel"
			);

		_info =
			_root.GetNode<Label>(
				"PrestigeConfirmOverlay/Panel/Margin/VBox/InfoLabel"
			);

		_confirm =
			_root.GetNode<Button>(
				"PrestigeConfirmOverlay/Panel/Margin/VBox/ConfirmButton"
			);

		_oldCancel =
			_root.GetNode<Button>(
				"PrestigeConfirmOverlay/Panel/Margin/VBox/CancelButton"
			);

		_confirm.Pressed +=
			Confirm;

		_oldCancel.Hide();

		_oldCancel.MouseFilter =
			Control.MouseFilterEnum.Ignore;

		OverlayCloseButton.Add(
			_panel,
			Cancel
		);

		ConfigureOutsideClose();

		Hide();
	}


	private void ConfigureOutsideClose()
	{
		ColorRect dim =
			_root.GetNode<ColorRect>(
				"PrestigeConfirmOverlay/Dim"
			);

		dim.GuiInput +=
			@event =>
			{
				bool released =
					@event
						is InputEventScreenTouch touch
					&& !touch.Pressed;

				released |=
					@event
						is InputEventMouseButton mouse
					&& !mouse.Pressed
					&& mouse.ButtonIndex
						== MouseButton.Left;

				if (!released)
					return;

				dim.GetViewport()
					.SetInputAsHandled();

				Callable
					.From(
						Cancel
					)
					.CallDeferred();
			};
	}


	public bool Open()
	{
		if (!_prestige.CanPrestige())
			return false;


		int nextPrestige =
			_state.Prestige.PrestigeCount
				== int.MaxValue
					? int.MaxValue
					: _state.Prestige.PrestigeCount
						+ 1;


		double currentMultiplier =
			_prestige.GetProductionMultiplier();


		double nextMultiplier =
			_prestige
				.GetProductionMultiplierAfterNextPrestige();


		int shardReward =
			_prestige.GetDataShardReward();


		_info.Text =
			"PRESTIGE #"
				+ nextPrestige
				+ "\n\n"
				+ "Reward: +"
				+ shardReward
				+ " Data Shards\n"
				+ "Permanent production: x"
				+ currentMultiplier.ToString(
					"F2"
				)
				+ "  →  x"
				+ nextMultiplier.ToString(
					"F2"
				)
				+ "\nMaximum prestige production: x"
				+ GameConfig
					.PrestigeMaximumProductionMultiplier
					.ToString(
						"F1"
					)
				+ "\n\nPrestige resets Tokens, rooms, machines, bots and room systems."
				+ "\nShop upgrades, Research, skins and Data Shards stay.";


		_confirm.Text =
			"PRESTIGE  •  +"
				+ shardReward
				+ " SHARDS";


		_overlay.Show();

		_overlay.MoveToFront();

		return true;
	}


	public void Hide()
	{
		_overlay.Hide();
	}


	private void Confirm()
	{
		Hide();

		Confirmed?.Invoke();
	}


	private void Cancel()
	{
		Hide();

		Cancelled?.Invoke();
	}
}
