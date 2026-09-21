using Godot;
using System;

namespace IdleAi;

public sealed class PrestigeOverlayController
{
	private readonly Game _root;

	private readonly GameState _state;

	private readonly PrestigeService _prestige;


	private Control _overlay =
		null!;


	private PanelContainer _panel =
		null!;


	private Label _info =
		null!;


	private Button _confirm =
		null!;


	private Button _oldCancel =
		null!;


	public event Action? Confirmed;

	public event Action? Cancelled;


	public bool Visible =>
		_overlay.Visible;


	public PrestigeOverlayController(
		Game root,
		GameState state,
		PrestigeService prestige)
	{
		_root =
			root;


		_state =
			state;


		_prestige =
			prestige;
	}


	// ==================================================
	// INITIALIZE
	// ==================================================

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


		/*
		 * Remove old bottom CANCEL button.
		 */
		_oldCancel.Hide();


		_oldCancel.MouseFilter =
			Control.MouseFilterEnum.Ignore;


		/*
		 * New X icon in top-right corner.
		 */
		OverlayCloseButton.Add(
			_panel,
			Cancel
		);


		ConfigureOutsideClose();


		Hide();
	}


	// ==================================================
	// OUTSIDE CLOSE
	// ==================================================

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


	// ==================================================
	// OPEN
	// ==================================================

	public bool Open()
	{
		long reward =
			_prestige.GetAvailableAiCores();


		if (reward <= 0)
			return false;


		long coresAfter =
			_state.Prestige.AiCores
			+ reward;


		double multiplier =
			1.0
			+ coresAfter
			* GameConfig
				.ProductionBoostPerAiCore;


		_info.Text =
			$"You gain +{reward} AI Cores.\n\n"
			+ $"AI Cores after prestige: {coresAfter}\n"
			+ $"Production after prestige: x{multiplier:F2}";


		_overlay.Show();

		_overlay.MoveToFront();


		return true;
	}


	// ==================================================
	// CLOSE
	// ==================================================

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
