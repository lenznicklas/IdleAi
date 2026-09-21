using Godot;
using System;
using System.Collections.Generic;

namespace IdleAi;

public sealed class StatsOverlayController
{
	private const float OpenStartScale =
		0.94f;

	private const float OpenOvershootScale =
		1.015f;

	private const double OpenGrowDuration =
		0.22;

	private const double OpenSettleDuration =
		0.14;

	private const float OpenStartAlpha =
		0.82f;


	private readonly Game _root;
	private readonly GameState _state;
	private readonly EconomyService _economy;
	private readonly ProgressionService _progression;
	private readonly PrestigeService _prestige;


	private Control _overlay =
		null!;

	private PanelContainer _panel =
		null!;

	private ScrollContainer _scroll =
		null!;

	private MobileScrollController _mobileScroll =
		null!;


	private Label _income =
		null!;

	private Label _earned =
		null!;

	private Label _spent =
		null!;

	private Label _slots =
		null!;

	private Label _level =
		null!;

	private Label _unlockSpend =
		null!;

	private Label _machineSpend =
		null!;

	private Label _aiCores =
		null!;

	private Label _prestigeBoost =
		null!;

	private Label _prestigeProgress =
		null!;


	private Button _prestigeButton =
		null!;

	private Button _oldCloseButton =
		null!;


	private Tween? _openTween;


	public event Action? PrestigeRequested;


	public bool Visible =>
		_overlay.Visible;


	public StatsOverlayController(
		Game root,
		GameState state,
		EconomyService economy,
		ProgressionService progression,
		PrestigeService prestige)
	{
		_root =
			root;

		_state =
			state;

		_economy =
			economy;

		_progression =
			progression;

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
				"StatsOverlay"
			);


		_panel =
			_root.GetNode<PanelContainer>(
				"StatsOverlay/StatsPanel"
			);


		_scroll =
			_root.GetNode<ScrollContainer>(
				"StatsOverlay/StatsPanel/Margin/Scroll"
			);


		const string path =
			"StatsOverlay/StatsPanel/Margin/Scroll/VBox/";


		_income =
			_root.GetNode<Label>(
				path
				+ "IncomeLabel"
			);


		_earned =
			_root.GetNode<Label>(
				path
				+ "EarnedLabel"
			);


		_spent =
			_root.GetNode<Label>(
				path
				+ "SpentLabel"
			);


		_slots =
			_root.GetNode<Label>(
				path
				+ "SlotsLabel"
			);


		_level =
			_root.GetNode<Label>(
				path
				+ "LevelLabel"
			);


		_unlockSpend =
			_root.GetNode<Label>(
				path
				+ "UnlockSpendLabel"
			);


		_machineSpend =
			_root.GetNode<Label>(
				path
				+ "MachineSpendLabel"
			);


		_aiCores =
			_root.GetNode<Label>(
				path
				+ "AiCoresLabel"
			);


		_prestigeBoost =
			_root.GetNode<Label>(
				path
				+ "PrestigeBoostLabel"
			);


		_prestigeProgress =
			_root.GetNode<Label>(
				path
				+ "PrestigeProgressLabel"
			);


		_prestigeButton =
			_root.GetNode<Button>(
				path
				+ "PrestigeButton"
			);


		_oldCloseButton =
			_root.GetNode<Button>(
				path
				+ "CloseButton"
			);


		_oldCloseButton.Hide();


		_oldCloseButton.MouseFilter =
			Control.MouseFilterEnum.Ignore;


		OverlayCloseButton.Add(
			_panel,
			Hide
		);


		_prestigeButton.Pressed +=
			() =>
				PrestigeRequested?.Invoke();


		ConfigureOutsideClose();

		CreateMobileScrolling();


		Hide();
	}


	// ==================================================
	// MOBILE SCROLL
	// ==================================================

	private void CreateMobileScrolling()
	{
		_mobileScroll =
			new MobileScrollController
			{
				Name =
					"StatsMobileScroll"
			};


		_overlay.AddChild(
			_mobileScroll
		);


		_mobileScroll.Setup(
			_scroll
		);
	}


	// ==================================================
	// OUTSIDE CLOSE
	// ==================================================

	private void ConfigureOutsideClose()
	{
		ColorRect dim =
			_root.GetNode<ColorRect>(
				"StatsOverlay/Dim"
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
						Hide
					)
					.CallDeferred();
			};
	}


	// ==================================================
	// OPEN / CLOSE
	// ==================================================

	public void Open()
	{
		Refresh();


		_overlay.Show();

		_overlay.MoveToFront();


		PlayOpenAnimation();
	}


	public void Hide()
	{
		_openTween?.Kill();

		_openTween =
			null;


		_mobileScroll?.ResetMotion();


		ResetPanelTransform();


		_overlay.Hide();
	}


	// ==================================================
	// OPEN ANIMATION
	// ==================================================

	private void PlayOpenAnimation()
	{
		_openTween?.Kill();


		_panel.PivotOffset =
			_panel.Size
			/ 2.0f;


		_panel.Scale =
			new Vector2(
				OpenStartScale,
				OpenStartScale
			);


		_panel.Modulate =
			new Color(
				1.0f,
				1.0f,
				1.0f,
				OpenStartAlpha
			);


		_openTween =
			_root.CreateTween();


		/*
		 * Scale and fade at the same time.
		 */
		_openTween.SetParallel(
			true
		);


		_openTween.TweenProperty(
			_panel,
			"scale",
			new Vector2(
				OpenOvershootScale,
				OpenOvershootScale
			),
			OpenGrowDuration
		)
		.SetTrans(
			Tween.TransitionType.Cubic
		)
		.SetEase(
			Tween.EaseType.Out
		);


		_openTween.TweenProperty(
			_panel,
			"modulate:a",
			1.0f,
			OpenGrowDuration
		)
		.SetTrans(
			Tween.TransitionType.Sine
		)
		.SetEase(
			Tween.EaseType.Out
		);


		/*
		 * Tiny settle back to normal size.
		 */
		_openTween
			.Chain()
			.TweenProperty(
				_panel,
				"scale",
				Vector2.One,
				OpenSettleDuration
			)
			.SetTrans(
				Tween.TransitionType.Sine
			)
			.SetEase(
				Tween.EaseType.Out
			);
	}


	private void ResetPanelTransform()
	{
		if (
			_panel == null
			|| !GodotObject.IsInstanceValid(
				_panel
			)
		)
		{
			return;
		}


		_panel.Scale =
			Vector2.One;


		_panel.Modulate =
			Colors.White;
	}


	// ==================================================
	// REFRESH
	// ==================================================

	public void Refresh()
	{
		_income.Text =
			"Automated Tokens / sec: "
			+ NumberFormatter.Format(
				_economy.GetTotalIncome()
			);


		_earned.Text =
			"Total earned: "
			+ NumberFormatter.Format(
				_state.Stats.TotalEarned
			)
			+ "\nOffline earned: "
			+ NumberFormatter.Format(
				_state.Stats.OfflineEarned
			);


		_spent.Text =
			"Total spent: "
			+ NumberFormatter.Format(
				_state.Stats.TotalSpent
			);


		_slots.Text =
			"Unlocked slots: "
			+ _progression.GetUnlockedSlotCount(
				_state.CurrentRoomIndex
			)
			+ " / "
			+ _state.CurrentRoomState.Slots.Count;


		_level.Text =
			"Total level: "
			+ _progression.GetTotalLevel();


		_unlockSpend.Text =
			"Slot unlocks: "
			+ NumberFormatter.Format(
				_state.Stats.SlotUnlockSpent
			);


		RefreshMachineSpending();

		RefreshPrestige();
	}


	// ==================================================
	// MACHINE SPENDING
	// ==================================================

	private void RefreshMachineSpending()
	{
		List<string> lines =
			[];


		foreach (
			RoomData room
			in _state.Rooms
		)
		{
			lines.Add(
				room.Name
					.ToUpperInvariant()
			);


			foreach (
				MachineData machine
				in room.Machines
			)
			{
				lines.Add(
					machine.MachineName
					+ ": "
					+ NumberFormatter.Format(
						_state.Stats
							.GetMachineSpending(
								machine.MachineName
							)
					)
				);
			}


			lines.Add(
				""
			);
		}


		lines.Add(
			"BOTS: "
			+ NumberFormatter.Format(
				_state.Stats
					.GetMachineSpending(
						"Bots"
					)
			)
		);


		_machineSpend.Text =
			string.Join(
				"\n",
				lines
			);
	}


	public void RefreshRuntime()
	{
		RefreshPrestige();
	}


	// ==================================================
	// PRESTIGE
	// ==================================================

	private void RefreshPrestige()
	{
		long available =
			_prestige.GetAvailableAiCores();


		_aiCores.Text =
			$"AI Cores: {_state.Prestige.AiCores}";


		_prestigeBoost.Text =
			"Permanent production: x"
			+ _prestige
				.GetProductionMultiplier()
				.ToString(
					"F2"
				);


		if (available > 0)
		{
			_prestigeProgress.Text =
				"Current run: "
				+ NumberFormatter.Format(
					_state.RunEarnedTokens
				)
				+ $"\nReward: +{available} AI Cores";


			_prestigeButton.Text =
				$"PRESTIGE +{available}";


			_prestigeButton.Disabled =
				false;


			return;
		}


		double remaining =
			Math.Max(
				0,
				GameConfig.PrestigeTokensPerCore
				- _state.RunEarnedTokens
			);


		_prestigeProgress.Text =
			"Next AI Core in: "
			+ NumberFormatter.Format(
				remaining
			);


		_prestigeButton.Text =
			"PRESTIGE";


		_prestigeButton.Disabled =
			true;
	}
}
