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


	// ==================================================
	// MAIN LAYOUT
	// ==================================================

	private VBoxContainer _mainLayout =
		null!;


	private ScrollContainer _roomScroll =
		null!;


	// ==================================================
	// BACKGROUND
	// ==================================================

	private TextureRect _background =
		null!;


	private AmbientBackgroundController _ambient =
		null!;


	// ==================================================
	// TOP BAR
	// ==================================================

	private TextureRect _topBarBackground =
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


	private Label _roomInTopBarLabel =
		null!;


	// ==================================================
	// SLOT AREA
	// ==================================================

	private GridContainer _slotGrid =
		null!;


	// ==================================================
	// POPUPS
	// ==================================================

	private PanelContainer _tokenPopup =
		null!;


	private PanelContainer _levelPopup =
		null!;


	// ==================================================
	// STATS
	// ==================================================

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


	private Label _aiCoresLabel =
		null!;


	private Label _prestigeBoostLabel =
		null!;


	private Label _prestigeProgressLabel =
		null!;


	private Button _prestigeButton =
		null!;


	private Button _statsCloseButton =
		null!;


	// ==================================================
	// PRESTIGE CONFIRM
	// ==================================================

	private Control _prestigeConfirmOverlay =
		null!;


	private Label _prestigeConfirmInfo =
		null!;


	private Button _prestigeConfirmButton =
		null!;


	private Button _prestigeCancelButton =
		null!;


	// ==================================================
	// PAGES
	// ==================================================

	private Control _mapPage =
		null!;


	private VBoxContainer _mapRoomButtons =
		null!;


	private Control _shopPage =
		null!;


	// ==================================================
	// BOTTOM BAR
	// ==================================================

	private Label _messageLabel =
		null!;


	private TextureRect _bottomBackground =
		null!;


	private TextureButton _mapButton =
		null!;


	private TextureButton _shopButton =
		null!;


	// ==================================================
	// DETAILS
	// ==================================================

	private MachineDetailsOverlay _details =
		null!;


	private int _lastThemeRoom =
		-1;


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


		/*
		 * Important:
		 *
		 * Removes the unwanted gap between
		 * TopBar and the room ScrollContainer.
		 */
		SetupRoomScrollLayout();


		_root.Theme =
			MainTheme;


		SetupBackground();

		SetupAmbientBackground();

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


		_ambient.SetRoom(
			_state.CurrentRoomIndex
		);
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


		_mainLayout =
			_root.GetNode<VBoxContainer>(
				"MarginContainer/VBoxContainer"
			);


		_roomScroll =
			_root.GetNode<ScrollContainer>(
				"MarginContainer/VBoxContainer/RoomPanel/RoomVBox/ScrollContainer"
			);


		_topBarBackground =
			_root.GetNode<TextureRect>(
				"MarginContainer/VBoxContainer/TopBar/Background"
			);


		_tokenCard =
			_root.GetNode<TextureButton>(
				"MarginContainer/VBoxContainer/TopBar/Margin/VBox/TopStats/TokenCard"
			);


		_statsCard =
			_root.GetNode<TextureButton>(
				"MarginContainer/VBoxContainer/TopBar/Margin/VBox/TopStats/StatsCard"
			);


		_levelCard =
			_root.GetNode<TextureButton>(
				"MarginContainer/VBoxContainer/TopBar/Margin/VBox/TopStats/LevelCard"
			);


		_tokenLabel =
			_root.GetNode<Label>(
				"MarginContainer/VBoxContainer/TopBar/Margin/VBox/TopStats/TokenCard/TokenLabel"
			);


		_totalLevelLabel =
			_root.GetNode<Label>(
				"MarginContainer/VBoxContainer/TopBar/Margin/VBox/TopStats/LevelCard/TotalLevelLabel"
			);


		_roomInTopBarLabel =
			_root.GetNode<Label>(
				"MarginContainer/VBoxContainer/TopBar/Margin/VBox/RoomInTopBarLabel"
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
			_root.GetNode<TextureButton>(
				"BottomBar/Margin/HBox/MapButton"
			);


		_shopButton =
			_root.GetNode<TextureButton>(
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
				stats
				+ "IncomeLabel"
			);


		_statsEarnedLabel =
			_root.GetNode<Label>(
				stats
				+ "EarnedLabel"
			);


		_statsSpentLabel =
			_root.GetNode<Label>(
				stats
				+ "SpentLabel"
			);


		_statsSlotsLabel =
			_root.GetNode<Label>(
				stats
				+ "SlotsLabel"
			);


		_statsLevelLabel =
			_root.GetNode<Label>(
				stats
				+ "LevelLabel"
			);


		_statsUnlockSpendLabel =
			_root.GetNode<Label>(
				stats
				+ "UnlockSpendLabel"
			);


		_statsMachineSpendLabel =
			_root.GetNode<Label>(
				stats
				+ "MachineSpendLabel"
			);


		_aiCoresLabel =
			_root.GetNode<Label>(
				stats
				+ "AiCoresLabel"
			);


		_prestigeBoostLabel =
			_root.GetNode<Label>(
				stats
				+ "PrestigeBoostLabel"
			);


		_prestigeProgressLabel =
			_root.GetNode<Label>(
				stats
				+ "PrestigeProgressLabel"
			);


		_prestigeButton =
			_root.GetNode<Button>(
				stats
				+ "PrestigeButton"
			);


		_statsCloseButton =
			_root.GetNode<Button>(
				stats
				+ "CloseButton"
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
	// ROOM SCROLL LAYOUT
	// ==================================================

	private void SetupRoomScrollLayout()
	{
		/*
		 * game.tscn currently uses a VBox separation
		 * between TopBar and RoomPanel.
		 *
		 * This creates the visible strip when the
		 * ScrollContainer is completely at the top.
		 *
		 * Remove it so the room starts directly
		 * underneath the TopBar.
		 */

		_mainLayout.AddThemeConstantOverride(
			"separation",
			0
		);


		/*
		 * Keep scrolling enabled but hide the scrollbar.
		 */

		_roomScroll.HorizontalScrollMode =
			ScrollContainer.ScrollMode.Disabled;


		_roomScroll.VerticalScrollMode =
			ScrollContainer.ScrollMode.ShowNever;
	}


	// ==================================================
	// BACKGROUND
	// ==================================================

	private void SetupBackground()
	{
		_background.ExpandMode =
			TextureRect.ExpandModeEnum.IgnoreSize;


		_background.StretchMode =
			TextureRect.StretchModeEnum.KeepAspectCovered;


		_background.MouseFilter =
			Control.MouseFilterEnum.Ignore;
	}


	private void SetupAmbientBackground()
	{
		_ambient =
			new AmbientBackgroundController(
				_root
			);


		_ambient.Initialize();
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


	// ==================================================
	// BOTTOM BAR
	// ==================================================

	private void SetupBottomBar()
	{
		_mapButton.Pressed +=
			ToggleMapPage;


		_shopButton.Pressed +=
			ToggleShopPage;
	}


	// ==================================================
	// PAGES
	// ==================================================

	private void SetupPages()
	{
		_mapPage.Hide();

		_shopPage.Hide();
	}


	// ==================================================
	// STATS
	// ==================================================

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
					&& mouse.ButtonIndex
					== MouseButton.Left
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


	// ==================================================
	// PRESTIGE CONFIRM
	// ==================================================

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
					&& mouse.ButtonIndex
					== MouseButton.Left
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
				i
				== _state.CurrentRoomIndex
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
				double cost =
					_progression
						.GetRoomUnlockCost(
							i
						);


				button.Text =
					$"{room.Name}\nUNLOCK • "
					+ NumberFormatter.Format(
						cost
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

		_tokenPopup.Hide();

		_levelPopup.Hide();


		UpdateMapButtons();


		_mapPage.Show();

		_mapPage.MoveToFront();


		MoveBottomBarToFront();
	}


	// ==================================================
	// SHOP
	// ==================================================

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

		_tokenPopup.Hide();

		_levelPopup.Hide();


		_shopPage.Show();

		_shopPage.MoveToFront();


		MoveBottomBarToFront();
	}


	private void MoveBottomBarToFront()
	{
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
	// SLOT VIEWS
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


		/*
		 * The grid has two columns.
		 *
		 * Keep one invisible row below the final
		 * machines so the last unlock button can be
		 * scrolled completely above the BottomBar.
		 *
		 * This bottom padding is intentional.
		 */

		for (
			int i = 0;
			i < 2;
			i++
		)
		{
			Control spacer =
				new()
				{
					CustomMinimumSize =
						new Vector2(
							0,
							70
						),

					MouseFilter =
						Control.MouseFilterEnum.Ignore
				};


			_slotGrid.AddChild(
				spacer
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


	// ==================================================
	// UPDATE
	// ==================================================

	public void UpdateAll()
	{
		_background.Texture =
			_state.CurrentRoom.Background;


		_roomInTopBarLabel.Text =
			_state.CurrentRoom.Name;


		UpdateTopBar();


		ApplyRoomTheme();


		_ambient.SetRoom(
			_state.CurrentRoomIndex
		);


		UpdateMapButtons();


		int count =
			Math.Min(
				8,
				_state.CurrentRoomState.Slots.Count
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
				_progression
					.GetSlotUnlockCost(
						_state.CurrentRoomIndex,
						index
					);


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


		int count =
			Math.Min(
				8,
				_state.CurrentRoomState.Slots.Count
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


			MachineSlot view =
				(MachineSlot)
				_slotGrid.GetChild(
					i
				);


			view.UpdateRuntime(
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


	// ==================================================
	// ROOM THEME
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


		Texture2D barTexture =
			CreateGradientTexture(
				main,
				secondary
			);


		Texture2D cardTexture =
			CreateGradientTexture(
				main.Darkened(
					0.18f
				),

				secondary.Darkened(
					0.18f
				)
			);


		Texture2D cardHoverTexture =
			CreateGradientTexture(
				main.Darkened(
					0.10f
				),

				secondary.Darkened(
					0.10f
				)
			);


		Texture2D cardPressedTexture =
			CreateGradientTexture(
				main.Darkened(
					0.28f
				),

				secondary.Darkened(
					0.28f
				)
			);


		_topBarBackground.Texture =
			barTexture;


		_bottomBackground.Texture =
			barTexture;


		ApplyCardTexture(
			_tokenCard,
			cardTexture,
			cardHoverTexture,
			cardPressedTexture
		);


		ApplyCardTexture(
			_statsCard,
			cardTexture,
			cardHoverTexture,
			cardPressedTexture
		);


		ApplyCardTexture(
			_levelCard,
			cardTexture,
			cardHoverTexture,
			cardPressedTexture
		);
	}


	private static (
		Color Main,
		Color Secondary
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
						0.10f,
						0.10f,
						0.10f,
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


	private static void ApplyCardTexture(
		TextureButton button,
		Texture2D normal,
		Texture2D hover,
		Texture2D pressed)
	{
		button.TextureNormal =
			normal;


		button.TextureHover =
			hover;


		button.TexturePressed =
			pressed;
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
	// TOP BAR VALUES
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


	// ==================================================
	// SMALL POPUPS
	// ==================================================

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


		_statsMachineSpendLabel.Text =
			string.Join(
				"\n",
				lines
			);


		UpdatePrestigeSection();
	}


	// ==================================================
	// PRESTIGE
	// ==================================================

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


	// ==================================================
	// MESSAGE
	// ==================================================

	public void SetMessage(
		string message)
	{
		_messageLabel.Text =
			message;
	}
}
