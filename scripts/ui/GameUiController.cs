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


	private readonly Game _root;

	private readonly GameState _state;

	private readonly EconomyService _economy;

	private readonly ProgressionService _progression;


	public event Action<int>? SlotActionRequested;

	public event Action<int>? RoomChangeRequested;


	// ==================================================
	// GENERAL
	// ==================================================

	private TextureRect _background = null!;

	private Label _messageLabel = null!;


	// ==================================================
	// TOPBAR
	// ==================================================

	private TextureButton _tokenCard = null!;

	private TextureButton _statsCard = null!;

	private TextureButton _levelCard = null!;

	private Label _tokenLabel = null!;

	private Label _totalLevelLabel = null!;


	// ==================================================
	// ROOM
	// ==================================================

	private Label _roomLabel = null!;

	private Button _previousRoomButton = null!;

	private Button _nextRoomButton = null!;

	private Label _roomPageLabel = null!;

	private GridContainer _slotGrid = null!;


	// ==================================================
	// POPUPS
	// ==================================================

	private PanelContainer _tokenPopup = null!;

	private PanelContainer _levelPopup = null!;


	// ==================================================
	// STATS
	// ==================================================

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
		ProgressionService progression)
	{
		_root =
			root;


		_state =
			state;


		_economy =
			economy;


		_progression =
			progression;
	}


	// ==================================================
	// INITIALIZE
	// ==================================================

	public void Initialize()
	{
		CacheNodes();


		_root.Theme =
			MainTheme;


		SetupBackground();

		SetupTopbar();

		SetupRoomNavigation();

		SetupStatsOverlay();

		CreateSlotViews();
	}


	// ==================================================
	// CACHE NODES
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


		_roomLabel =
			_root.GetNode<Label>(
                "MarginContainer/VBoxContainer/RoomPanel/RoomVBox/RoomLabel"
			);


		_previousRoomButton =
			_root.GetNode<Button>(
                "MarginContainer/VBoxContainer/RoomPanel/RoomVBox/RoomNavigation/PreviousRoomButton"
			);


		_roomPageLabel =
			_root.GetNode<Label>(
                "MarginContainer/VBoxContainer/RoomPanel/RoomVBox/RoomNavigation/RoomPageLabel"
			);


		_nextRoomButton =
			_root.GetNode<Button>(
                "MarginContainer/VBoxContainer/RoomPanel/RoomVBox/RoomNavigation/NextRoomButton"
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
	// TOPBAR
	// ==================================================

	private void SetupTopbar()
	{
		_tokenCard.Pressed +=
			ToggleTokenPopup;


		_statsCard.Pressed +=
			OpenStats;


		_levelCard.Pressed +=
			ToggleLevelPopup;
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


	// ==================================================
	// ROOM NAVIGATION
	// ==================================================

	private void SetupRoomNavigation()
	{
		_previousRoomButton.Pressed +=
			OnPreviousRoomPressed;


		_nextRoomButton.Pressed +=
			OnNextRoomPressed;


		SetupRoundButton(
			_previousRoomButton
		);


		SetupRoundButton(
			_nextRoomButton
		);
	}


	private void OnPreviousRoomPressed()
	{
		RoomChangeRequested?.Invoke(
			-1
		);
	}


	private void OnNextRoomPressed()
	{
		RoomChangeRequested?.Invoke(
			1
		);
	}


	private void UpdateRoom()
	{
		int roomIndex =
			_state.CurrentRoomIndex;


		RoomData room =
			_state.CurrentRoom;


		_roomLabel.Text =
			room.Name;


		_roomPageLabel.Text =
			$"{roomIndex + 1} / {_state.Rooms.Count}";


		_background.Texture =
			room.Background;


		// PREVIOUS

		_previousRoomButton.Disabled =
			roomIndex <= 0;


		_previousRoomButton.Text =
			"<";


		// NEXT

		if (
			roomIndex
			>= _state.Rooms.Count - 1
		)
		{
			_nextRoomButton.Disabled =
				true;


			_nextRoomButton.Text =
				">";


			return;
		}


		_nextRoomButton.Disabled =
			false;


		int nextRoomIndex =
			roomIndex + 1;


		RoomState nextRoomState =
			_state.RoomStates[
				nextRoomIndex
			];


		if (nextRoomState.Unlocked)
		{
			_nextRoomButton.Text =
				">";


			return;
		}


		double unlockCost =
			_state.Rooms[
				nextRoomIndex
			].UnlockCost;


		_nextRoomButton.Text =
			$"> {NumberFormatter.Format(unlockCost)}";
	}


	// ==================================================
	// SLOT VIEWS
	// ==================================================

	private void CreateSlotViews()
	{
		for (
			int i = 0;
			i < _state.CurrentRoomState.Slots.Count;
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
	// UPDATE ALL
	// ==================================================

	public void UpdateAll()
	{
		UpdateRoom();

		UpdateTopBar();


		int count =
			Math.Min(
				_state.CurrentRoomState.Slots.Count,
				_slotGrid.GetChildCount()
			);


		for (
			int i = 0;
			i < count;
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


	// ==================================================
	// UPDATE SLOT
	// ==================================================

	private void UpdateSlot(
		int slotIndex)
	{
		int roomIndex =
			_state.CurrentRoomIndex;


		RoomData room =
			_state.CurrentRoom;


		RoomState roomState =
			_state.CurrentRoomState;


		SlotData slot =
			roomState.Slots[
				slotIndex
			];


		MachineSlot slotUi =
			(MachineSlot)
			_slotGrid.GetChild(
				slotIndex
			);


		// ==================================================
		// LOCKED
		// ==================================================

		if (!slot.Unlocked)
		{
			double unlockCost =
				GameConfig
					.GetSlotUnlockCosts(
						roomIndex
					)[
						slotIndex
					];


			slotUi.ShowLocked(
				NumberFormatter.Format(
					unlockCost
				),

				room.EmptyTexture
			);


			return;
		}


		// ==================================================
		// MACHINE
		// ==================================================

		MachineData machine =
			room.Machines[
				slot.MachineTier
			];


		string buttonText;


		// LEVEL UPGRADE

		if (
			slot.MachineLevel
			< machine.MaxLevel
		)
		{
			double cost =
				_economy.GetLevelUpgradeCost(
					roomIndex,
					slot,
					slotIndex
				);


			buttonText =
                "Upgrade\n"
				+ NumberFormatter.Format(
					cost
				)
				+ " Tokens";
		}

		// TIER UPGRADE

		else if (
			slot.MachineTier
			< room.Machines.Count - 1
		)
		{
			MachineData nextMachine =
				room.Machines[
					slot.MachineTier
					+ 1
				];


			double cost =
				_economy.GetTierUpgradeCost(
					roomIndex,
					slot,
					slotIndex
				);


			buttonText =
				$"Upgrade: {nextMachine.MachineName}\n"
				+ NumberFormatter.Format(
					cost
				)
				+ " Tokens";
		}

		// MAX

		else
		{
			buttonText =
				"MAX";
		}


		double income =
			_economy.GetSlotIncome(
				roomIndex,
				slot
			);


		slotUi.ShowMachine(
			machine,

			slot.MachineLevel,

			NumberFormatter.Format(
				income
			),

			EconomyService.GetMilestoneText(
				slot.MachineLevel
			),

			buttonText
		);
	}


	// ==================================================
	// MESSAGE
	// ==================================================

	public void SetMessage(
		string message)
	{
		_messageLabel.Text =
			message;
	}


	// ==================================================
	// TOKEN POPUP
	// ==================================================

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


	// ==================================================
	// LEVEL POPUP
	// ==================================================

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


	// ==================================================
	// POPUP POSITION
	// ==================================================

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
	// STATS
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
		double totalIncome =
			_economy.GetTotalIncome();


		_statsIncomeLabel.Text =
            "Tokens / sec: "
			+ NumberFormatter.Format(
				totalIncome
			);


		_statsEarnedLabel.Text =
            "Total earned: "
			+ NumberFormatter.Format(
				_state.Stats.TotalEarned
			)
			+ "\nOffline earned: "
			+ NumberFormatter.Format(
				_state.Stats.OfflineEarned
			);


		_statsSpentLabel.Text =
            "Total spent: "
			+ NumberFormatter.Format(
				_state.Stats.TotalSpent
			);


		int roomIndex =
			_state.CurrentRoomIndex;


		int unlockedSlots =
			_progression.GetUnlockedSlotCount(
				roomIndex
			);


		int totalSlots =
			_state.CurrentRoomState
				.Slots
				.Count;


		_statsSlotsLabel.Text =
			$"Unlocked slots: "
			+ $"{unlockedSlots} / {totalSlots}"
			+ $" ({_state.CurrentRoom.Name})";


		_statsLevelLabel.Text =
            "Total level: "
			+ _progression
				.GetTotalLevel()
				.ToString();


		_statsUnlockSpendLabel.Text =
            "Slot unlocks: "
			+ NumberFormatter.Format(
				_state.Stats.SlotUnlockSpent
			);


		List<string> lines =
			[];


		foreach (
			RoomData room
			in _state.Rooms
		)
		{
			lines.Add(
				room.Name.ToUpperInvariant()
			);


			foreach (
				MachineData machine
				in room.Machines
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


			lines.Add(
                ""
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


		StyleBoxFlat disabled =
			(StyleBoxFlat)
			normal.Duplicate();


		disabled.BgColor =
			new Color(
				0.02f,
				0.025f,
				0.035f,
				0.7f
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


		button.AddThemeStyleboxOverride(
			"disabled",
			disabled
		);
	}
}
