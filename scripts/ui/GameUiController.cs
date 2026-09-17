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

	public event Action<int>? RoomSelectedRequested;

	public event Action? StateChanged;

	public event Action? PrestigeRequested;


	private TextureRect _background = null!;

	private Label _messageLabel = null!;


	private TextureButton _tokenCard = null!;

	private TextureButton _statsCard = null!;

	private TextureButton _levelCard = null!;

	private Label _tokenLabel = null!;

	private Label _totalLevelLabel = null!;


	private Label _roomLabel = null!;

	private GridContainer _slotGrid = null!;


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

	private Label _aiCoresLabel = null!;

	private Label _prestigeBoostLabel = null!;

	private Label _prestigeProgressLabel = null!;

	private Button _prestigeButton = null!;

	private Button _statsCloseButton = null!;


	private Control _prestigeConfirmOverlay = null!;

	private Label _prestigeConfirmInfo = null!;

	private Button _prestigeConfirmButton = null!;

	private Button _prestigeCancelButton = null!;


	private Control _mapPage = null!;

	private VBoxContainer _mapRoomButtons = null!;


	private Control _shopPage = null!;


	private TextureRect _bottomBackground = null!;

	private Button _mapButton = null!;

	private Button _shopButton = null!;


	private MachineDetailsOverlay _details = null!;


	private int _lastThemeRoom =
		-1;


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


	public void Initialize()
	{
		CacheNodes();


		_root.Theme =
			MainTheme;


		SetupBackground();

		SetupTopbar();

		SetupBottomBar();

		SetupStats();

		SetupPrestigeConfirmation();

		SetupPages();

		CreateSlotViews();

		CreateMapButtons();


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


		ApplyRoomTheme(
			true
		);
	}


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


		_slotGrid =
			_root.GetNode<GridContainer>(
                "MarginContainer/VBoxContainer/RoomPanel/RoomVBox/ScrollContainer/SlotGrid"
			);


		_tokenPopup =
			_root.GetNode<PanelContainer>(
                "TokenPopup"
			);


		_levelPopup =
			_root.GetNode<PanelContainer>(
                "LevelPopup"
			);


		_messageLabel =
			_root.GetNode<Label>(
                "BottomBar/Margin/HBox/MessageLabel"
			);


		_bottomBackground =
			_root.GetNode<TextureRect>(
                "BottomBar/Background"
			);


		_mapButton =
			_root.GetNode<Button>(
                "BottomBar/Margin/HBox/MapButton"
			);


		_shopButton =
			_root.GetNode<Button>(
                "BottomBar/Margin/HBox/ShopButton"
			);


		_mapPage =
			_root.GetNode<Control>(
                "MapPage"
			);


		_mapRoomButtons =
			_root.GetNode<VBoxContainer>(
                "MapPage/Margin/VBox/RoomButtons"
			);


		_shopPage =
			_root.GetNode<Control>(
                "ShopPage"
			);


		_statsOverlay =
			_root.GetNode<Control>(
                "StatsOverlay"
			);


		const string stats =
			"StatsOverlay/StatsPanel/Margin/Scroll/VBox/";


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


	private void SetupBackground()
	{
		_background.ExpandMode =
			TextureRect.ExpandModeEnum.IgnoreSize;


		_background.StretchMode =
			TextureRect.StretchModeEnum.KeepAspectCovered;
	}


	private void SetupTopbar()
	{
		_tokenCard.Pressed +=
			ToggleTokenPopup;


		_statsCard.Pressed +=
			OpenStats;


		_levelCard.Pressed +=
			ToggleLevelPopup;
	}


	private void SetupBottomBar()
	{
		_mapButton.Pressed +=
			ToggleMapPage;


		_shopButton.Pressed +=
			ToggleShopPage;
	}


	private void SetupPages()
	{
		_mapPage.Hide();

		_shopPage.Hide();
	}


	private void SetupStats()
	{
		_statsCloseButton.Pressed +=
			CloseStats;


		_prestigeButton.Pressed +=
			OpenPrestigeConfirmation;


		ColorRect dim =
			_root.GetNode<ColorRect>(
                "StatsOverlay/Dim"
			);


		dim.GuiInput +=
			@event =>
			{
				if (
					@event is InputEventMouseButton mouse
					&& mouse.Pressed
					&& mouse.ButtonIndex == MouseButton.Left
				)
				{
					CloseStats();
				}


				if (
					@event is InputEventScreenTouch touch
					&& touch.Pressed
				)
				{
					CloseStats();
				}
			};
	}


	private void SetupPrestigeConfirmation()
	{
		_prestigeConfirmButton.Pressed +=
			ConfirmPrestige;


		_prestigeCancelButton.Pressed +=
			ClosePrestigeConfirmation;


		ColorRect dim =
			_root.GetNode<ColorRect>(
                "PrestigeConfirmOverlay/Dim"
			);


		dim.GuiInput +=
			@event =>
			{
				if (
					@event is InputEventMouseButton mouse
					&& mouse.Pressed
				)
				{
					ClosePrestigeConfirmation();
				}


				if (
					@event is InputEventScreenTouch touch
					&& touch.Pressed
				)
				{
					ClosePrestigeConfirmation();
				}
			};
	}


	// ==================================================
	// MAP
	// ==================================================

	private void CreateMapButtons()
	{
		foreach (
			Node child
			in _mapRoomButtons.GetChildren()
		)
		{
			child.QueueFree();
		}


		for (
			int roomIndex = 0;
			roomIndex < _state.Rooms.Count;
			roomIndex++
		)
		{
			int target =
				roomIndex;


			Button button =
				new()
				{
					CustomMinimumSize =
						new Vector2(
							0,
							82
						)
				};


			button.Pressed +=
				() =>
					SelectRoomFromMap(
						target
					);


			_mapRoomButtons.AddChild(
				button
			);
		}


		UpdateMapButtons();
	}


	private void UpdateMapButtons()
	{
		int count =
			Math.Min(
				_state.Rooms.Count,
				_mapRoomButtons.GetChildCount()
			);


		for (
			int i = 0;
			i < count;
			i++
		)
		{
			Button button =
				(Button)
				_mapRoomButtons.GetChild(
					i
				);


			RoomData room =
				_state.Rooms[
					i
				];


			bool unlocked =
				_state.RoomStates[
					i
				].Unlocked;


			if (
				i == _state.CurrentRoomIndex
			)
			{
				button.Text =
					$"{room.Name}\nCURRENT";
			}
			else if (unlocked)
			{
				button.Text =
					$"{room.Name}\nENTER";
			}
			else
			{
				button.Text =
					$"{room.Name}\nUNLOCK • "
					+ NumberFormatter.Format(
						room.UnlockCost
					);
			}
		}
	}


	private void SelectRoomFromMap(
		int roomIndex)
	{
		RoomSelectedRequested?.Invoke(
			roomIndex
		);
	}


	private void ToggleMapPage()
	{
		_shopPage.Hide();


		if (_mapPage.Visible)
		{
			_mapPage.Hide();

			return;
		}


		_details?.Close();

		_statsOverlay.Hide();

		_prestigeConfirmOverlay.Hide();


		UpdateMapButtons();


		_mapPage.Show();

		_mapPage.MoveToFront();


		_root.GetNode<Control>(
            "BottomBar"
		).MoveToFront();
	}


	private void ToggleShopPage()
	{
		_mapPage.Hide();


		if (_shopPage.Visible)
		{
			_shopPage.Hide();

			return;
		}


		_details?.Close();

		_statsOverlay.Hide();

		_prestigeConfirmOverlay.Hide();


		_shopPage.Show();

		_shopPage.MoveToFront();


		_root.GetNode<Control>(
            "BottomBar"
		).MoveToFront();
	}


	public void ClosePages()
	{
		_mapPage.Hide();

		_shopPage.Hide();
	}


	// ==================================================
	// SLOTS
	// ==================================================

	private void CreateSlotViews()
	{
		for (
			int i = 0;
			i < 8;
			i++
		)
		{
			MachineSlot slot =
				new();


			slot.Setup(
				i
			);


			slot.UnlockPressed +=
				index =>
					SlotActionRequested?.Invoke(
						index
					);


			slot.ManualStartPressed +=
				OnManualStart;


			slot.DetailsPressed +=
				OpenMachineDetails;


			_slotGrid.AddChild(
				slot
			);
		}
	}


	private void OnManualStart(
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
	}


	private void OpenMachineDetails(
		int slotIndex)
	{
		if (
			!_state.CurrentRoomState
				.Slots[
					slotIndex
				]
				.Unlocked
		)
		{
			return;
		}


		_details.Open(
			_state.CurrentRoomIndex,
			slotIndex
		);
	}


	// ==================================================
	// UPDATE
	// ==================================================

	public void UpdateAll()
	{
		_roomLabel.Text =
			_state.CurrentRoom.Name;


		_background.Texture =
			_state.CurrentRoom.Background;


		UpdateTopBar();

		ApplyRoomTheme();

		UpdateMapButtons();


		for (
			int i = 0;
			i < _slotGrid.GetChildCount();
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


		if (_details.Visible)
		{
			_details.Refresh();
		}
	}


	private void UpdateSlot(
		int index)
	{
		SlotData slot =
			_state.CurrentRoomState
				.Slots[
					index
				];


		MachineSlot view =
			(MachineSlot)
			_slotGrid.GetChild(
				index
			);


		if (!slot.Unlocked)
		{
			double cost =
				GameConfig
					.GetSlotUnlockCosts(
						_state.CurrentRoomIndex
					)[
						index
					];


			view.ShowLocked(
				NumberFormatter.Format(
					cost
				),

				_state.CurrentRoom
					.EmptyTexture
			);


			return;
		}


		MachineData machine =
			_state.CurrentRoom
				.Machines[
					slot.MachineTier
				];


		BotDefinition? bot =
			slot.HasBot
				? BotCatalog.Get(
					slot.BotRarity!.Value
				)
				: null;


		view.ShowMachine(
			machine,
			slot,
			bot
		);
	}


	public void UpdateRuntime()
	{
		UpdateTopBar();


		for (
			int i = 0;
			i < _slotGrid.GetChildCount();
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


			((MachineSlot)
				_slotGrid.GetChild(
					i
				))
				.UpdateRuntime(
					slot
				);
		}


		if (_details.Visible)
		{
			_details.Refresh();
		}


		if (_statsOverlay.Visible)
		{
			UpdatePrestigeSection();
		}
	}


	// ==================================================
	// TOP / BOTTOM COLORS
	// ==================================================

	private void ApplyRoomTheme(
		bool force = false)
	{
		int room =
			_state.CurrentRoomIndex;


		if (
			!force
			&& room == _lastThemeRoom
		)
		{
			return;
		}


		_lastThemeRoom =
			room;


		(
			Color main,
			Color secondary
		) =
			GetRoomColors(
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


		_bottomBackground.Texture =
			CreateGradientTexture(
				main,
				secondary
			);
	}


	private static (
		Color,
		Color
	) GetRoomColors(
		int room)
	{
		return room switch
		{
			0 =>
				(
					new Color(
						0.015f,
						0.27f,
						0.52f,
						1
					),

					new Color(
						0.01f,
						0.22f,
						0.44f,
						1
					)
				),

			1 =>
				(
					new Color(
						0.50f,
						0.07f,
						0.10f,
						1
					),

					new Color(
						0.41f,
						0.05f,
						0.08f,
						1
					)
				),

			2 =>
				(
					new Color(
						0.04f,
						0.39f,
						0.20f,
						1
					),

					new Color(
						0.03f,
						0.32f,
						0.16f,
						1
					)
				),

			3 =>
				(
					new Color(
						0.33f,
						0.10f,
						0.52f,
						1
					),

					new Color(
						0.27f,
						0.08f,
						0.44f,
						1
					)
				),

			_ =>
				(
					new Color(
						0.1f,
						0.1f,
						0.1f,
						1
					),

					new Color(
						0.08f,
						0.08f,
						0.08f,
						1
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
					0.06f
				),

				second.Lightened(
					0.06f
				)
			);


		button.TexturePressed =
			CreateGradientTexture(
				first.Darkened(
					0.06f
				),

				second.Darkened(
					0.06f
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


		return new GradientTexture2D
		{
			Gradient =
				gradient,

			Width =
				512,

			Height =
				96,

			Fill =
				GradientTexture2D.FillEnum.Linear,

			FillFrom =
				new Vector2(
					0,
					0.5f
				),

			FillTo =
				new Vector2(
					1,
					0.5f
				)
		};
	}


	// ==================================================
	// TOP BAR
	// ==================================================

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


	private void ToggleTokenPopup()
	{
		_levelPopup.Hide();


		if (_tokenPopup.Visible)
		{
			_tokenPopup.Hide();

			return;
		}


		PositionPopup(
			_tokenPopup,
			_tokenCard
		);
	}


	private void ToggleLevelPopup()
	{
		_tokenPopup.Hide();


		if (_levelPopup.Visible)
		{
			_levelPopup.Hide();

			return;
		}


		PositionPopup(
			_levelPopup,
			_levelCard
		);
	}


	private async void PositionPopup(
		Control popup,
		Control card)
	{
		popup.Show();


		await _root.ToSignal(
			_root.GetTree(),
			SceneTree.SignalName.ProcessFrame
		);


		popup.GlobalPosition =
			new Vector2(
				card.GlobalPosition.X
				+ card.Size.X / 2
				- popup.Size.X / 2,

				card.GlobalPosition.Y
				+ card.Size.Y
				+ 6
			);
	}


	// ==================================================
	// STATS
	// ==================================================

	private void OpenStats()
	{
		ClosePages();

		_details?.Close();


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


		_statsSlotsLabel.Text =
            "Unlocked slots: "
			+ _progression.GetUnlockedSlotCount(
				_state.CurrentRoomIndex
			)
			+ " / "
			+ _state.CurrentRoomState.Slots.Count;


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


		_statsMachineSpendLabel.Text =
			string.Join(
				"\n",
				lines
			);


		UpdatePrestigeSection();
	}


	private void UpdatePrestigeSection()
	{
		long available =
			_prestige.GetAvailableAiCores();


		_aiCoresLabel.Text =
			$"AI Cores: {_state.Prestige.AiCores}";


		_prestigeBoostLabel.Text =
            "Permanent production: x"
			+ _prestige
				.GetProductionMultiplier()
				.ToString(
                    "F2"
				);


		if (available > 0)
		{
			_prestigeProgressLabel.Text =
                "Current run: "
				+ NumberFormatter.Format(
					_state.RunEarnedTokens
				)
				+ $"\nReward: +{available} AI Cores";


			_prestigeButton.Text =
				$"PRESTIGE +{available}";


			_prestigeButton.Disabled =
				false;
		}
		else
		{
			double remaining =
				Math.Max(
					0,
					GameConfig.PrestigeTokensPerCore
					- _state.RunEarnedTokens
				);


			_prestigeProgressLabel.Text =
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


	// ==================================================
	// PRESTIGE CONFIRM
	// ==================================================

	private void OpenPrestigeConfirmation()
	{
		long reward =
			_prestige.GetAvailableAiCores();


		if (reward <= 0)
			return;


		long coresAfter =
			_state.Prestige.AiCores
			+ reward;


		double multiplier =
			1.0
			+ coresAfter
			* GameConfig
				.ProductionBoostPerAiCore;


		_prestigeConfirmInfo.Text =
			$"You gain +{reward} AI Cores.\n\n"
			+ $"AI Cores after prestige: {coresAfter}\n"
			+ $"Production after prestige: x{multiplier:F2}";


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


	// ==================================================
	// DETAILS EVENT
	// ==================================================

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
}
