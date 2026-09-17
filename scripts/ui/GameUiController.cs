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

	private readonly ProductionService _production;

	private readonly BotService _bots;


	public event Action<int>? SlotActionRequested;

	public event Action<int>? RoomChangeRequested;

	public event Action? StateChanged;


	private TextureRect _background =
		null!;


	private Label _messageLabel =
		null!;


	private TextureButton _tokenCard =
		null!;


	private TextureButton _statsCard =
		null!;


	private TextureButton _levelCard =
		null!;


	private Label _tokenLabel =
		null!;


	private Label _totalLevelLabel =
		null!;


	private Label _roomLabel =
		null!;


	private Button _previousRoomButton =
		null!;


	private Button _nextRoomButton =
		null!;


	private Label _roomPageLabel =
		null!;


	private GridContainer _slotGrid =
		null!;


	private PanelContainer _tokenPopup =
		null!;


	private PanelContainer _levelPopup =
		null!;


	private Control _statsOverlay =
		null!;


	private Label _statsIncomeLabel =
		null!;


	private Label _statsEarnedLabel =
		null!;


	private Label _statsSpentLabel =
		null!;


	private Label _statsSlotsLabel =
		null!;


	private Label _statsLevelLabel =
		null!;


	private Label _statsUnlockSpendLabel =
		null!;


	private Label _statsMachineSpendLabel =
		null!;


	private Button _statsCloseButton =
		null!;


	private MachineDetailsOverlay _details =
		null!;


	public GameUiController(
		Game root,
		GameState state,
		EconomyService economy,
		ProgressionService progression,
		ProductionService production,
		BotService bots)
	{
		_root =
			root;

		_state =
			state;

		_economy =
			economy;

		_progression =
			progression;

		_production =
			production;

		_bots =
			bots;
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


		_details =
			new MachineDetailsOverlay(
				_root,
				_state,
				_economy,
				_progression,
				_bots
			);


		_details.Initialize();


		_details.StateChanged +=
			OnDetailsStateChanged;
	}


	// ==================================================
	// CACHE
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
	// TOP BAR
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
	// ROOMS
	// ==================================================

	private void SetupRoomNavigation()
	{
		_previousRoomButton.Pressed +=
			() =>
			{
				_details?.Close();

				RoomChangeRequested?.Invoke(
					-1
				);
			};


		_nextRoomButton.Pressed +=
			() =>
			{
				_details?.Close();

				RoomChangeRequested?.Invoke(
					1
				);
			};
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


		_previousRoomButton.Disabled =
			roomIndex <= 0;


		_previousRoomButton.Text =
			"<";


		if (
			roomIndex >=
			_state.Rooms.Count - 1
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


		if (
			_state.RoomStates[
				nextRoomIndex
			].Unlocked
		)
		{
			_nextRoomButton.Text =
				">";
		}
		else
		{
			_nextRoomButton.Text =
                "> "
				+ NumberFormatter.Format(
					_state.Rooms[
						nextRoomIndex
					].UnlockCost
				);
		}
	}


	// ==================================================
	// SLOTS
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


			slotUi.UnlockPressed +=
				OnUnlockPressed;


			slotUi.ManualStartPressed +=
				OnManualStartPressed;


			slotUi.DetailsPressed +=
				OnDetailsPressed;


			_slotGrid.AddChild(
				slotUi
			);
		}
	}


	private void OnUnlockPressed(
		int slotIndex)
	{
		SlotActionRequested?.Invoke(
			slotIndex
		);
	}


	private void OnManualStartPressed(
		int slotIndex)
	{
		ManualStartResult result =
			_production.TryStartManual(
				_state.CurrentRoomIndex,
				slotIndex
			);


		SetMessage(
			result.Message
		);


		UpdateRuntime();
	}


	private void OnDetailsPressed(
		int slotIndex)
	{
		SlotData slot =
			_state.CurrentRoomState
				.Slots[
					slotIndex
				];


		if (!slot.Unlocked)
			return;


		_details.Open(
			_state.CurrentRoomIndex,
			slotIndex
		);
	}


	private void UpdateSlot(
		int slotIndex)
	{
		int roomIndex =
			_state.CurrentRoomIndex;


		RoomData room =
			_state.CurrentRoom;


		SlotData slot =
			_state.CurrentRoomState
				.Slots[
					slotIndex
				];


		MachineSlot slotUi =
			(MachineSlot)
			_slotGrid.GetChild(
				slotIndex
			);


		if (!slot.Unlocked)
		{
			double cost =
				GameConfig
					.GetSlotUnlockCosts(
						roomIndex
					)[
						slotIndex
					];


			slotUi.ShowLocked(
				NumberFormatter.Format(
					cost
				),

				room.EmptyTexture
			);


			return;
		}


		MachineData machine =
			room.Machines[
				slot.MachineTier
			];


		BotDefinition? bot =
			slot.BotRarity.HasValue
				? BotCatalog.Get(
					slot.BotRarity.Value
				)
				: null;


		slotUi.ShowMachine(
			machine,
			slot,
			bot
		);
	}


	// ==================================================
	// UPDATES
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


		if (
			_details != null
			&& _details.Visible
		)
		{
			_details.Refresh();
		}
	}


	public void UpdateRuntime()
	{
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
			SlotData slot =
				_state.CurrentRoomState
					.Slots[
						i
					];


			if (!slot.Unlocked)
				continue;


			MachineSlot slotUi =
				(MachineSlot)
				_slotGrid.GetChild(
					i
				);


			slotUi.UpdateRuntime(
				slot
			);
		}


		if (
			_details != null
			&& _details.Visible
		)
		{
			_details.Refresh();
		}
	}


	private void OnDetailsStateChanged(
		string message)
	{
		SetMessage(
			message
		);


		UpdateAll();


		StateChanged?.Invoke();
	}


	public void SetMessage(
		string message)
	{
		_messageLabel.Text =
			message;
	}


	// ==================================================
	// POPUPS
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
	// STATS
	// ==================================================

	private void SetupStatsOverlay()
	{
		_statsCloseButton.Pressed +=
			CloseStats;
	}


	private void OpenStats()
	{
		_tokenPopup.Hide();

		_levelPopup.Hide();

		_details?.Close();


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
            "Automated Tokens / sec: "
			+ NumberFormatter.Format(
				_economy.GetTotalIncome()
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


		int unlockedSlots =
			_progression.GetUnlockedSlotCount(
				_state.CurrentRoomIndex
			);


		_statsSlotsLabel.Text =
			$"Unlocked slots: "
			+ $"{unlockedSlots} / "
			+ $"{_state.CurrentRoomState.Slots.Count}"
			+ $" ({_state.CurrentRoom.Name})";


		_statsLevelLabel.Text =
            "Total level: "
			+ _progression.GetTotalLevel();


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
					_state.Stats.GetMachineSpending(
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


		lines.Add(
            "BOTS: "
			+ NumberFormatter.Format(
				_state.Stats.GetMachineSpending(
                    "Bots"
				)
			)
		);


		_statsMachineSpendLabel.Text =
			string.Join(
				"\n",
				lines
			);
	}
}
