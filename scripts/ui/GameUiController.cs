using Godot;

using System;
using System.Collections.Generic;

namespace IdleAi;

public sealed class GameUiController
{
	private static readonly Theme MainTheme =
		GD.Load<Theme>(
            "res://assets/themes/main_theme.tres"
		);


	private static readonly Texture2D BackgroundTexture =
		GD.Load<Texture2D>(
            "res://assets/background/bg.png"
		);


	private readonly Game _root;

	private readonly GameState _state;

	private readonly EconomyService _economy;

	private readonly ProgressionService _progression;

	private readonly double[] _slotUnlockCosts;


	public event Action<int>? SlotActionRequested;


	private TextureRect _background = null!;

	private TextureButton _tokenCard = null!;

	private TextureButton _statsCard = null!;

	private TextureButton _levelCard = null!;

	private Label _tokenLabel = null!;

	private Label _totalLevelLabel = null!;

	private GridContainer _slotGrid = null!;

	private Label _messageLabel = null!;

	private PanelContainer _tokenPopup = null!;

	private PanelContainer _levelPopup = null!;

	private Control _statsOverlay = null!;

	private Label _statsIncomeLabel = null!;

	private Label _statsEarnedLabel = null!;

	private Label _statsSpentLabel = null!;

	private Label _statsSlotsLabel = null!;

	private Label _statsLevelLabel = null!;

	private Label _statsUnlockSpendLabel = null!;

	private Label _statsMachineSpendLabel = null!;

	private Button _statsCloseButton = null!;


	public GameUiController(
		Game root,
		GameState state,
		EconomyService economy,
		ProgressionService progression,
		double[] slotUnlockCosts)
	{
		_root =
			root;


		_state =
			state;


		_economy =
			economy;


		_progression =
			progression;


		_slotUnlockCosts =
			slotUnlockCosts;
	}


	public void Initialize()
	{
		CacheNodes();

		_root.Theme =
			MainTheme;


		SetupBackground();

		SetupTopbar();

		SetupStatsOverlay();

		CreateSlotViews();
	}


	// ==================================================
	// NODE CACHE
	// ==================================================

	private void CacheNodes()
	{
		_background =
			_root.GetNode<TextureRect>(
                "Background"
			);


		_tokenCard =
			_root.GetNode<TextureButton>(
                "MarginContainer/VBoxContainer/TopStats/TokenCard"
			);


		_statsCard =
			_root.GetNode<TextureButton>(
                "MarginContainer/VBoxContainer/TopStats/StatsCard"
			);


		_levelCard =
			_root.GetNode<TextureButton>(
                "MarginContainer/VBoxContainer/TopStats/LevelCard"
			);


		_tokenLabel =
			_root.GetNode<Label>(
                "MarginContainer/VBoxContainer/TopStats/TokenCard/TokenLabel"
			);


		_totalLevelLabel =
			_root.GetNode<Label>(
                "MarginContainer/VBoxContainer/TopStats/LevelCard/TotalLevelLabel"
			);


		_slotGrid =
			_root.GetNode<GridContainer>(
                "MarginContainer/VBoxContainer/RoomPanel/RoomVBox/ScrollContainer/SlotGrid"
			);


		_messageLabel =
			_root.GetNode<Label>(
                "MarginContainer/VBoxContainer/MessageLabel"
			);


		_tokenPopup =
			_root.GetNode<PanelContainer>(
                "TokenPopup"
			);


		_levelPopup =
			_root.GetNode<PanelContainer>(
                "LevelPopup"
			);


		_statsOverlay =
			_root.GetNode<Control>(
                "StatsOverlay"
			);


		_statsIncomeLabel =
			_root.GetNode<Label>(
                "StatsOverlay/StatsPanel/Margin/VBox/IncomeLabel"
			);


		_statsEarnedLabel =
			_root.GetNode<Label>(
                "StatsOverlay/StatsPanel/Margin/VBox/EarnedLabel"
			);


		_statsSpentLabel =
			_root.GetNode<Label>(
                "StatsOverlay/StatsPanel/Margin/VBox/SpentLabel"
			);


		_statsSlotsLabel =
			_root.GetNode<Label>(
                "StatsOverlay/StatsPanel/Margin/VBox/SlotsLabel"
			);


		_statsLevelLabel =
			_root.GetNode<Label>(
                "StatsOverlay/StatsPanel/Margin/VBox/LevelLabel"
			);


		_statsUnlockSpendLabel =
			_root.GetNode<Label>(
                "StatsOverlay/StatsPanel/Margin/VBox/UnlockSpendLabel"
			);


		_statsMachineSpendLabel =
			_root.GetNode<Label>(
                "StatsOverlay/StatsPanel/Margin/VBox/MachineSpendLabel"
			);


		_statsCloseButton =
			_root.GetNode<Button>(
                "StatsOverlay/StatsPanel/Margin/VBox/CloseButton"
			);
	}


	// ==================================================
	// BACKGROUND
	// ==================================================

	private void SetupBackground()
	{
		_background.Texture =
			BackgroundTexture;


		_background.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);


		_background.ExpandMode =
			TextureRect.ExpandModeEnum.IgnoreSize;


		_background.StretchMode =
			TextureRect.StretchModeEnum.KeepAspectCovered;


		_background.MouseFilter =
			Control.MouseFilterEnum.Ignore;
	}


	// ==================================================
	// MACHINE SLOT VIEWS
	// ==================================================

	private void CreateSlotViews()
	{
		for (
			int i = 0;
			i < _state.Slots.Count;
			i++
		)
		{
			MachineSlot slotUi =
				new();


			slotUi.Setup(
				i
			);


			slotUi.ActionPressed +=
				OnSlotPressed;


			_slotGrid.AddChild(
				slotUi
			);
		}
	}


	private void OnSlotPressed(
		int slotIndex)
	{
		SlotActionRequested?.Invoke(
			slotIndex
		);
	}


	// ==================================================
	// UPDATE
	// ==================================================

	public void UpdateAll()
	{
		UpdateTopBar();


		for (
			int i = 0;
			i < _state.Slots.Count;
			i++
		)
		{
			UpdateSlot(
				i
			);
		}


		if (_statsOverlay.Visible)
		{
			UpdateStatsOverlay();
		}
	}


	public void UpdateTopBar()
	{
		_tokenLabel.Text =
			NumberFormatter.Format(
				_state.Tokens
			);


		_totalLevelLabel.Text =
			_progression
				.GetTotalLevel()
				.ToString();
	}


	private void UpdateSlot(
		int slotIndex)
	{
		SlotData slot =
			_state.Slots[
				slotIndex
			];


		MachineSlot slotUi =
			(MachineSlot)
			_slotGrid.GetChild(
				slotIndex
			);


		if (!slot.Unlocked)
		{
			slotUi.ShowLocked(
				NumberFormatter.Format(
					_slotUnlockCosts[
						slotIndex
					]
				)
			);

			return;
		}


		MachineData machine =
			_state.Machines[
				slot.MachineTier
			];


		string buttonText;


		if (
			slot.MachineLevel
			< machine.MaxLevel
		)
		{
			double cost =
				_economy.GetLevelUpgradeCost(
					slot,
					slotIndex
				);


			buttonText =
				$"Upgrade\n"
				+ $"{NumberFormatter.Format(cost)} Tokens";
		}
		else if (
			slot.MachineTier
			< _state.Machines.Count - 1
		)
		{
			MachineData nextMachine =
				_state.Machines[
					slot.MachineTier
					+ 1
				];


			double cost =
				_economy.GetTierUpgradeCost(
					slot,
					slotIndex
				);


			buttonText =
				$"Upgrade: {nextMachine.MachineName}\n"
				+ $"{NumberFormatter.Format(cost)} Tokens";
		}
		else
		{
			buttonText =
				"MAX";
		}


		slotUi.ShowMachine(
			machine,

			slot.MachineLevel,

			NumberFormatter.Format(
				_economy.GetSlotIncome(
					slot
				)
			),

			EconomyService.GetMilestoneText(
				slot.MachineLevel
			),

			buttonText
		);
	}


	public void SetMessage(
		string message)
	{
		_messageLabel.Text =
			message;
	}


	// ==================================================
	// TOPBAR
	// ==================================================

	private void SetupTopbar()
	{
		_tokenCard.Pressed +=
			ToggleTokenPopup;


		_levelCard.Pressed +=
			ToggleLevelPopup;


		_statsCard.Pressed +=
			OpenStats;
	}


	private void ToggleTokenPopup()
	{
		_levelPopup.Hide();


		if (_tokenPopup.Visible)
		{
			_tokenPopup.Hide();

			return;
		}


		PositionPopupBelow(
			_tokenPopup,
			_tokenCard
		);


		_tokenPopup.Show();
	}


	private void ToggleLevelPopup()
	{
		_tokenPopup.Hide();


		if (_levelPopup.Visible)
		{
			_levelPopup.Hide();

			return;
		}


		PositionPopupBelow(
			_levelPopup,
			_levelCard
		);


		_levelPopup.Show();
	}


	private async void PositionPopupBelow(
		Control popup,
		Control card)
	{
		await _root.ToSignal(
			_root.GetTree(),
			SceneTree.SignalName.ProcessFrame
		);


		Vector2 cardPosition =
			card.GlobalPosition;


		Vector2 cardSize =
			card.Size;


		popup.GlobalPosition =
			new Vector2(
				cardPosition.X
				+ cardSize.X / 2.0f
				- popup.Size.X / 2.0f,

				cardPosition.Y
				+ cardSize.Y
				+ 6.0f
			);
	}


	// ==================================================
	// STATS OVERLAY
	// ==================================================

	private void SetupStatsOverlay()
	{
		_statsCloseButton.Pressed +=
			CloseStats;


		SetupRoundButton(
			_statsCloseButton
		);
	}


	private void OpenStats()
	{
		_tokenPopup.Hide();

		_levelPopup.Hide();


		UpdateStatsOverlay();


		_statsOverlay.Show();

		_statsOverlay.MoveToFront();
	}


	private void CloseStats()
	{
		_statsOverlay.Hide();
	}


	private void UpdateStatsOverlay()
	{
		_statsIncomeLabel.Text =
			$"Tokens / sec: "
			+ NumberFormatter.Format(
				_economy.GetTotalIncome(
					_state.Slots
				)
			);


		_statsEarnedLabel.Text =
			$"Total earned: "
			+ NumberFormatter.Format(
				_state.Stats.TotalEarned
			)
			+ "\nOffline earned: "
			+ NumberFormatter.Format(
				_state.Stats.OfflineEarned
			);


		_statsSpentLabel.Text =
			$"Total spent: "
			+ NumberFormatter.Format(
				_state.Stats.TotalSpent
			);


		_statsSlotsLabel.Text =
			$"Unlocked slots: "
			+ $"{_progression.GetUnlockedSlotCount()}"
			+ $" / {_state.Slots.Count}";


		_statsLevelLabel.Text =
			$"Total level: "
			+ $"{_progression.GetTotalLevel()}";


		_statsUnlockSpendLabel.Text =
			$"Slot unlocks: "
			+ NumberFormatter.Format(
				_state.Stats.SlotUnlockSpent
			);


		List<string> lines =
			[];


		foreach (
			MachineData machine
			in _state.Machines
		)
		{
			double spent =
				_state.Stats
					.GetMachineSpending(
						machine.MachineName
					);


			lines.Add(
				$"{machine.MachineName}: "
				+ NumberFormatter.Format(
					spent
				)
			);
		}


		_statsMachineSpendLabel.Text =
			string.Join(
				"\n",
				lines
			);
	}


	// ==================================================
	// BUTTON STYLE
	// ==================================================

	private static void SetupRoundButton(
		Button button)
	{
		StyleBoxFlat normal =
			new()
			{
				BgColor =
					new Color(
						0.025f,
						0.04f,
						0.065f,
						0.96f
					),

				CornerRadiusTopLeft =
					12,

				CornerRadiusTopRight =
					12,

				CornerRadiusBottomLeft =
					12,

				CornerRadiusBottomRight =
					12
			};


		StyleBoxFlat hover =
			(StyleBoxFlat)
			normal.Duplicate();


		hover.BgColor =
			new Color(
				0.03f,
				0.12f,
				0.2f,
				1.0f
			);


		StyleBoxFlat pressed =
			(StyleBoxFlat)
			normal.Duplicate();


		pressed.BgColor =
			new Color(
				0.02f,
				0.18f,
				0.3f,
				1.0f
			);


		button.AddThemeStyleboxOverride(
			"normal",
			normal
		);


		button.AddThemeStyleboxOverride(
			"hover",
			hover
		);


		button.AddThemeStyleboxOverride(
			"pressed",
			pressed
		);
	}
}
