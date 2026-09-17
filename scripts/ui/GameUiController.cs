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
	private readonly PrestigeService _prestige;


	public event Action<int>? SlotActionRequested;
	public event Action<int>? RoomChangeRequested;
	public event Action? StateChanged;
	public event Action? PrestigeRequested;


	// ==================================================
	// GENERAL
	// ==================================================

	private TextureRect _background = null!;
	private Label _messageLabel = null!;


	// ==================================================
	// TOP BAR
	// ==================================================

	private TextureButton _tokenCard = null!;
	private TextureButton _statsCard = null!;
	private TextureButton _levelCard = null!;

	private Label _tokenLabel = null!;
	private Label _totalLevelLabel = null!;

	private int _lastTopBarRoom =
		-1;


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

	private Label _aiCoresLabel = null!;
	private Label _prestigeBoostLabel = null!;
	private Label _prestigeProgressLabel = null!;

	private Button _prestigeButton = null!;
	private Button _statsCloseButton = null!;


	// ==================================================
	// PRESTIGE CONFIRM
	// ==================================================

	private Control _prestigeConfirmOverlay = null!;
	private Label _prestigeConfirmInfo = null!;
	private Button _prestigeConfirmButton = null!;
	private Button _prestigeCancelButton = null!;


	// ==================================================
	// DETAILS
	// ==================================================

	private MachineDetailsOverlay _details = null!;


	// ==================================================
	// CONSTRUCTOR
	// ==================================================

	public GameUiController(
		Game root,
		GameState state,
		EconomyService economy,
		ProgressionService progression,
		ProductionService production,
		BotService bots,
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

		_production =
			production;

		_bots =
			bots;

		_prestige =
			prestige;
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

		SetupPrestigeConfirmation();

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


		const string stats =
			"StatsOverlay/StatsPanel/Margin/Scroll/VBox/";


		_statsOverlay =
			_root.GetNode<Control>(
                "StatsOverlay"
			);


		_statsIncomeLabel =
			_root.GetNode<Label>(
				stats + "IncomeLabel"
			);


		_statsEarnedLabel =
			_root.GetNode<Label>(
				stats + "EarnedLabel"
			);


		_statsSpentLabel =
			_root.GetNode<Label>(
				stats + "SpentLabel"
			);


		_statsSlotsLabel =
			_root.GetNode<Label>(
				stats + "SlotsLabel"
			);


		_statsLevelLabel =
			_root.GetNode<Label>(
				stats + "LevelLabel"
			);


		_statsUnlockSpendLabel =
			_root.GetNode<Label>(
				stats + "UnlockSpendLabel"
			);


		_statsMachineSpendLabel =
			_root.GetNode<Label>(
				stats + "MachineSpendLabel"
			);


		_aiCoresLabel =
			_root.GetNode<Label>(
				stats + "AiCoresLabel"
			);


		_prestigeBoostLabel =
			_root.GetNode<Label>(
				stats + "PrestigeBoostLabel"
			);


		_prestigeProgressLabel =
			_root.GetNode<Label>(
				stats + "PrestigeProgressLabel"
			);


		_prestigeButton =
			_root.GetNode<Button>(
				stats + "PrestigeButton"
			);


		_statsCloseButton =
			_root.GetNode<Button>(
				stats + "CloseButton"
			);


		_prestigeConfirmOverlay =
			_root.GetNode<Control>(
                "PrestigeConfirmOverlay"
			);


		_prestigeConfirmInfo =
			_root.GetNode<Label>(
                "PrestigeConfirmOverlay/Panel/Margin/VBox/InfoLabel"
			);


		_prestigeConfirmButton =
			_root.GetNode<Button>(
                "PrestigeConfirmOverlay/Panel/Margin/VBox/ConfirmButton"
			);


		_prestigeCancelButton =
			_root.GetNode<Button>(
                "PrestigeConfirmOverlay/Panel/Margin/VBox/CancelButton"
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


		ApplyRoomTopBarTheme(
			true
		);
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


		ApplyRoomTopBarTheme();
	}


	private void ApplyRoomTopBarTheme(
		bool force = false)
	{
		int room =
			_state.CurrentRoomIndex;


		if (
			!force
			&& room == _lastTopBarRoom
		)
		{
			return;
		}


		_lastTopBarRoom =
			room;


		(Color main, Color secondary) =
			GetRoomTopBarColors(
				room
			);


		ApplyGradient(
			_tokenCard,
			main,
			secondary
		);


		ApplyGradient(
			_statsCard,
			main,
			secondary
		);


		ApplyGradient(
			_levelCard,
			main,
			secondary
		);
	}


	private static (
		Color Main,
		Color Secondary
	) GetRoomTopBarColors(
		int roomIndex)
	{
		return roomIndex switch
		{
			// Garage - blue
			0 =>
				(
					new Color(
						0.015f,
						0.27f,
						0.52f,
						0.96f
					),

					new Color(
						0.01f,
						0.21f,
						0.42f,
						0.96f
					)
				),

			// Server room - red
			1 =>
				(
					new Color(
						0.50f,
						0.07f,
						0.10f,
						0.96f
					),

					new Color(
						0.39f,
						0.045f,
						0.075f,
						0.96f
					)
				),

			// Data center - green
			2 =>
				(
					new Color(
						0.04f,
						0.39f,
						0.20f,
						0.96f
					),

					new Color(
						0.025f,
						0.30f,
						0.15f,
						0.96f
					)
				),

			// Quantum lab - purple
			3 =>
				(
					new Color(
						0.33f,
						0.10f,
						0.52f,
						0.96f
					),

					new Color(
						0.25f,
						0.07f,
						0.41f,
						0.96f
					)
				),

			_ =>
				(
					new Color(
						0.08f,
						0.12f,
						0.18f,
						0.96f
					),

					new Color(
						0.05f,
						0.08f,
						0.13f,
						0.96f
					)
				)
		};
	}


	private static void ApplyGradient(
		TextureButton button,
		Color first,
		Color second)
	{
		button.TextureNormal =
			CreateGradientTexture(
				first,
				second
			);


		button.TextureHover =
			CreateGradientTexture(
				first.Lightened(
					0.08f
				),

				second.Lightened(
					0.08f
				)
			);


		button.TexturePressed =
			CreateGradientTexture(
				first.Darkened(
					0.08f
				),

				second.Darkened(
					0.08f
				)
			);
	}


	private static GradientTexture2D CreateGradientTexture(
		Color first,
		Color second)
	{
		Gradient gradient =
			new();


		gradient.SetColor(
			0,
			first
		);


		gradient.SetColor(
			1,
			second
		);


		GradientTexture2D texture =
			new()
			{
				Gradient =
					gradient,

				Width =
					256,

				Height =
					64,

				Fill =
					GradientTexture2D.FillEnum.Linear,

				FillFrom =
					new Vector2(
						0.0f,
						0.5f
					),

				FillTo =
					new Vector2(
						1.0f,
						0.5f
					)
			};


		return texture;
	}


	// ==================================================
	// ROOM NAVIGATION
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


		ApplyRoomTopBarTheme();


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
	// UPDATE
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


		if (_statsOverlay.Visible)
		{
			UpdatePrestigeSection();
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
	// TOKEN / LEVEL POPUPS
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


		popup.GlobalPosition =
			new Vector2(
				card.GlobalPosition.X
				+ card.Size.X / 2.0f
				- popup.Size.X / 2.0f,

				card.GlobalPosition.Y
				+ card.Size.Y
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


		_prestigeButton.Pressed +=
			OpenPrestigeConfirmation;
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
			$"Unlocked slots: {unlockedSlots} / "
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


		UpdatePrestigeSection();
	}


	// ==================================================
	// PRESTIGE IN STATS
	// ==================================================

	private void UpdatePrestigeSection()
	{
		long cores =
			_state.Prestige.AiCores;


		long available =
			_prestige.GetAvailableAiCores();


		double multiplier =
			_prestige.GetProductionMultiplier();


		_aiCoresLabel.Text =
			$"AI Cores: {cores}";


		_prestigeBoostLabel.Text =
			$"Permanent production: x{multiplier:F2}";


		if (available > 0)
		{
			_prestigeProgressLabel.Text =
                "Current run: "
				+ NumberFormatter.Format(
					_state.RunEarnedTokens
				)
				+ $"\nPrestige reward: +{available} AI Cores";


			_prestigeButton.Text =
				$"PRESTIGE +{available} AI CORES";


			_prestigeButton.Disabled =
				false;


			return;
		}


		double remaining =
			Math.Max(
				0.0,
				GameConfig.PrestigeTokensPerCore
				- _state.RunEarnedTokens
			);


		_prestigeProgressLabel.Text =
            "Current run: "
			+ NumberFormatter.Format(
				_state.RunEarnedTokens
			)
			+ "\nNext AI Core in: "
			+ NumberFormatter.Format(
				remaining
			);


		_prestigeButton.Text =
			"PRESTIGE";


		_prestigeButton.Disabled =
			true;
	}


	// ==================================================
	// PRESTIGE CONFIRM
	// ==================================================

	private void SetupPrestigeConfirmation()
	{
		_prestigeConfirmButton.Pressed +=
			ConfirmPrestige;


		_prestigeCancelButton.Pressed +=
			ClosePrestigeConfirmation;
	}


	private void OpenPrestigeConfirmation()
	{
		long reward =
			_prestige.GetAvailableAiCores();


		if (reward <= 0)
			return;


		long totalCores =
			_state.Prestige.AiCores
			+ reward;


		double multiplierAfter =
			1.0
			+ totalCores
			* GameConfig.ProductionBoostPerAiCore;


		_prestigeConfirmInfo.Text =
			$"You will receive +{reward} AI Cores.\n\n"
			+ $"AI Cores after prestige: {totalCores}\n"
			+ $"Permanent production: x{multiplierAfter:F2}\n\n"
			+ "AI Cores are permanent.";


		_statsOverlay.Hide();


		_prestigeConfirmOverlay.Show();

		_prestigeConfirmOverlay.MoveToFront();
	}


	private void ClosePrestigeConfirmation()
	{
		_prestigeConfirmOverlay.Hide();


		UpdateStatsOverlay();


		_statsOverlay.Show();

		_statsOverlay.MoveToFront();
	}


	private void ConfirmPrestige()
	{
		_prestigeConfirmOverlay.Hide();


		PrestigeRequested?.Invoke();
	}
}
