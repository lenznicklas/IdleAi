using Godot;
using System;

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
	private readonly PrestigeService _prestigeService;
	private readonly ShopService _shopService;
	private readonly PipelineService _pipelineService;
	private readonly InfrastructureService _infrastructureService;
	private readonly QuantumService _quantumService;

	private RoomUiController _room = null!;
	private PipelineUiController _pipeline = null!;
	private InfrastructureUiController _infrastructure = null!;
	private QuantumUiController _quantum = null!;
	private TopBarController _topBar = null!;
	private BottomBarController _bottomBar = null!;
	private MapController _map = null!;
	private StatsOverlayController _stats = null!;
	private PrestigeOverlayController _prestige = null!;
	private MachineDetailsOverlay _details = null!;
	private ShopController _shop = null!;

	private int _lastThemeRoom = -1;
	private int _lastDisplayedRoom = -1;

	public event Action<int>? SlotActionRequested;
	public event Action<int>? RoomSelectedRequested;
	public event Action? StateChanged;
	public event Action? PrestigeRequested;

	public GameUiController(
		Game root,
		GameState state,
		EconomyService economy,
		ProgressionService progression,
		ProductionService production,
		BotService bots,
		PrestigeService prestige,
		ShopService shopService,
		PipelineService pipelineService,
		InfrastructureService infrastructureService,
		QuantumService quantumService)
	{
		_root = root;
		_state = state;
		_economy = economy;
		_progression = progression;
		_production = production;
		_bots = bots;
		_prestigeService = prestige;
		_shopService = shopService;
		_pipelineService = pipelineService;
		_infrastructureService = infrastructureService;
		_quantumService = quantumService;
	}

	public void Initialize()
	{
		_root.Theme = MainTheme;

		/*
		 * Important initialization order:
		 *
		 * 1. All normal room/special-room controllers create
		 *    their UI using the original scene hierarchy.
		 *
		 * 2. TopBar / BottomBar controllers cache their nodes.
		 *
		 * 3. Only then RoomUiController reparents the room
		 *    content and chrome into the new overlay layout.
		 *
		 * Cached Control references remain valid after Reparent,
		 * so Pipeline / Infrastructure / Quantum do not need to
		 * know about the new hierarchy.
		 */
		CreateRoomController();
		CreatePipelineController();
		CreateInfrastructureController();
		CreateQuantumController();
		CreateTopBarController();
		CreateBottomBarController();
		CreateMapController();
		CreateStatsController();
		CreatePrestigeController();
		CreateShopController();
		CreateMachineDetails();

		_room.EnableOverlayLayout();

		SyncRoomChromeVisibility();

		ApplyRoomTheme(true);
		UpdateAll();
	}

	private void CreateRoomController()
	{
		_room =
			new RoomUiController(
				_root,
				_state,
				_progression,
				_production
			);

		_room.SlotActionRequested +=
			index =>
				SlotActionRequested?.Invoke(index);

		_room.MessageRequested += SetMessage;
		_room.DetailsRequested += OpenMachineDetails;

		_room.Initialize();
	}

	private void CreatePipelineController()
	{
		_pipeline =
			new PipelineUiController(
				_root,
				_state,
				_pipelineService
			);

		_pipeline.MessageRequested += SetMessage;

		_pipeline.StateChanged += () =>
		{
			StateChanged?.Invoke();
			UpdateAll();
		};

		_pipeline.Initialize();
	}

	private void CreateInfrastructureController()
	{
		_infrastructure =
			new InfrastructureUiController(
				_root,
				_state,
				_infrastructureService
			);

		_infrastructure.MessageRequested += SetMessage;

		_infrastructure.StateChanged += () =>
		{
			StateChanged?.Invoke();
			UpdateAll();
		};

		_infrastructure.Initialize();
	}

	private void CreateQuantumController()
	{
		_quantum =
			new QuantumUiController(
				_root,
				_state,
				_quantumService
			);

		_quantum.MessageRequested += SetMessage;

		_quantum.StateChanged += () =>
		{
			StateChanged?.Invoke();
			UpdateAll();
		};

		_quantum.Initialize();
	}

	private void CreateTopBarController()
	{
		_topBar =
			new TopBarController(
				_root,
				_state,
				_progression
			);

		_topBar.StatsRequested += OpenStats;
		_topBar.Initialize();


		TextureButton tokenCard =
			_root.GetNode<TextureButton>(
				"MarginContainer/VBoxContainer/TopBar/Margin/VBox/TopStats/TokenCard"
			);


		TextureButton levelCard =
			_root.GetNode<TextureButton>(
				"MarginContainer/VBoxContainer/TopBar/Margin/VBox/TopStats/LevelCard"
			);


		tokenCard.Pressed +=
			BringTopBarInfoPopupsToFront;


		levelCard.Pressed +=
			BringTopBarInfoPopupsToFront;
	}

	private void CreateBottomBarController()
	{
		_bottomBar =
			new BottomBarController(
				_root
			);

		_bottomBar.MapRequested += ToggleMapPage;
		_bottomBar.ShopRequested += ToggleShopPage;
		_bottomBar.Initialize();
	}

	private void CreateMapController()
	{
		_map =
			new MapController(
				_root,
				_state,
				_progression
			);

		_map.RoomSelectedRequested +=
			roomIndex =>
			{
				bool validRoom =
					roomIndex >= 0
					&& roomIndex
						< _state.RoomStates.Count;


				bool wasUnlocked =
					validRoom
					&& _state.RoomStates[
						roomIndex
					].Unlocked;


				/*
				 * Game handles the request synchronously.
				 * After it returns we can tell whether this
				 * exact tap unlocked a new room.
				 */
				RoomSelectedRequested?.Invoke(
					roomIndex
				);


				bool isUnlocked =
					validRoom
					&& _state.RoomStates[
						roomIndex
					].Unlocked;


				bool enteredUnlockedRoom =
					validRoom
					&& !wasUnlocked
					&& isUnlocked
					&& _state.CurrentRoomIndex
						== roomIndex;


				if (enteredUnlockedRoom)
				{
					_room.PlayRoomUnlockAnimation();
				}
			};

		_map.Initialize();
	}

	private void CreateStatsController()
	{
		_stats =
			new StatsOverlayController(
				_root,
				_state,
				_economy,
				_progression,
				_prestigeService
			);

		_stats.PrestigeRequested +=
			OpenPrestigeConfirmation;

		_stats.Initialize();
	}

	private void CreatePrestigeController()
	{
		_prestige =
			new PrestigeOverlayController(
				_root,
				_state,
				_prestigeService
			);

		_prestige.Confirmed +=
			() => PrestigeRequested?.Invoke();

		_prestige.Cancelled +=
			() => _stats.Open();

		_prestige.Initialize();
	}

	private void CreateShopController()
	{
		_shop =
			new ShopController(
				_root,
				_state,
				_shopService
			);

		_shop.MessageRequested += SetMessage;

		_shop.StateChanged += () =>
		{
			StateChanged?.Invoke();
			UpdateAll();
		};

		_shop.Initialize();
	}

	private void CreateMachineDetails()
	{
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

	private void OpenMachineDetails(
		int slotIndex)
	{
		if (
			slotIndex < 0
			|| slotIndex >= _state.CurrentRoomState.Slots.Count
		)
		{
			return;
		}

		SlotData slot =
			_state.CurrentRoomState.Slots[slotIndex];

		if (!slot.Unlocked)
			return;

		_details.Open(
			_state.CurrentRoomIndex,
			slotIndex
		);
	}

	private void OnDetailsStateChanged(
		string message)
	{
		SetMessage(message);


		/*
		 * Save the changed gameplay state BEFORE any room/UI
		 * refresh. The overlay layout added more work to
		 * UpdateAll(); persistence must not depend on that
		 * presentation code finishing successfully.
		 */
		StateChanged?.Invoke();


		UpdateAll();
	}

	private void ToggleMapPage()
	{
		_shop.Hide();

		if (_map.Visible)
		{
			_map.Hide();

			SyncRoomChromeVisibility();

			return;
		}

		CloseTransientOverlays();

		_map.Open();

		SyncRoomChromeVisibility();

		_bottomBar.MoveToFront();
	}

	private void ToggleShopPage()
	{
		_map.Hide();

		if (_shop.Visible)
		{
			_shop.Hide();

			SyncRoomChromeVisibility();

			return;
		}

		CloseTransientOverlays();

		_shop.Open();

		SyncRoomChromeVisibility();

		_bottomBar.MoveToFront();
	}

	private async void BringTopBarInfoPopupsToFront()
	{
		/*
		 * TokenPopup / LevelPopup are root-level siblings of
		 * RoomOverlayLayer. Since the room TopBar is itself
		 * inside RoomOverlayLayer and that layer is normally
		 * moved to the front, the info popups could render
		 * behind the TopBar.
		 *
		 * Wait one frame because TopBarController shows and
		 * positions the popup asynchronously, then explicitly
		 * move the visible info popup to the highest level.
		 */
		await _root.ToSignal(
			_root.GetTree(),
			SceneTree.SignalName.ProcessFrame
		);


		PanelContainer? tokenPopup =
			_root.GetNodeOrNull<PanelContainer>(
				"TokenPopup"
			);


		if (
			tokenPopup != null
			&& tokenPopup.Visible
		)
		{
			tokenPopup.MoveToFront();
		}


		PanelContainer? levelPopup =
			_root.GetNodeOrNull<PanelContainer>(
				"LevelPopup"
			);


		if (
			levelPopup != null
			&& levelPopup.Visible
		)
		{
			levelPopup.MoveToFront();
		}
	}


	private void OpenStats()
	{
		ClosePages();
		_details.Close();
		_topBar.HidePopups();
		_prestige.Hide();
		_stats.Open();
	}

	private void OpenPrestigeConfirmation()
	{
		if (!_prestige.Open())
			return;

		_stats.Hide();
	}

	private void CloseTransientOverlays()
	{
		_details.Close();
		_stats.Hide();
		_prestige.Hide();
		_topBar.HidePopups();
	}

	public void ClosePages()
	{
		_map.Hide();

		_shop.Hide();

		SyncRoomChromeVisibility();
	}


	private void SyncRoomChromeVisibility()
	{
		Control? roomChrome =
			_root.GetNodeOrNull<Control>(
				"RoomOverlayLayer"
			);


		if (roomChrome == null)
			return;


		bool labVisible =
			_root.GetNodeOrNull<Control>(
				"LabPage"
			)?.Visible
			== true;


		bool pageVisible =
			_map.Visible
			|| _shop.Visible
			|| labVisible;


		roomChrome.Visible =
			!pageVisible;
	}

	public void UpdateAll()
	{
		ResetRoomScrollIfRoomChanged();


		bool modalOverlayVisible =
			_details.Visible
			|| _stats.Visible
			|| _root.GetNode<Control>(
				"PrestigeConfirmOverlay"
			).Visible;


		bool labVisible =
			_root.GetNodeOrNull<Control>(
				"LabPage"
			)?.Visible
			== true;


		bool pageVisible =
			_map.Visible
			|| _shop.Visible
			|| labVisible;


		_room.UpdateAll(
			bringChromeToFront:
				!modalOverlayVisible
				&& !pageVisible
		);


		/*
		 * Map/Shop are full page views.
		 *
		 * RoomOverlayLayer contains the TopBar and the
		 * MACHINES / PIPELINE / INFRASTRUCTURE / QUANTUM
		 * navigation. It must stay hidden while one of those
		 * pages is open.
		 *
		 * This is especially important when a locked room is
		 * tapped in the map: Game.OnRoomSelectedRequested()
		 * may call UpdateAll() after an unsuccessful unlock.
		 * Without this guard, RoomUiController would bring
		 * the TopBar back in front of MapPage.
		 */
		SyncRoomChromeVisibility();


		_pipeline.UpdateAll();
		_infrastructure.UpdateAll();
		_quantum.UpdateAll();

		_topBar.SetRoomName(
			_state.CurrentRoom.Name
		);

		_topBar.UpdateValues();
		_map.Refresh();
		_shop.Refresh();

		ApplyRoomTheme();

		if (_stats.Visible)
			_stats.Refresh();

		if (_details.Visible)
			_details.Refresh();
	}

	private void ResetRoomScrollIfRoomChanged()
	{
		int currentRoom =
			_state.CurrentRoomIndex;

		if (currentRoom == _lastDisplayedRoom)
			return;

		_lastDisplayedRoom = currentRoom;
		_room.ScrollToTop();
	}

	public void UpdateRuntime()
	{
		_topBar.UpdateValues();
		_room.UpdateRuntime();
		_pipeline.UpdateRuntime();
		_infrastructure.UpdateRuntime();
		_quantum.UpdateRuntime();

		if (_shop.Visible)
			_shop.Refresh();

		if (_details.Visible)
			_details.Refresh();

		if (_stats.Visible)
			_stats.RefreshRuntime();
	}

	private void ApplyRoomTheme(
		bool force = false)
	{
		int roomIndex =
			_state.CurrentRoomIndex;

		if (
			!force
			&& roomIndex == _lastThemeRoom
		)
		{
			return;
		}

		_lastThemeRoom = roomIndex;

		RoomThemeTextures theme =
			RoomThemePalette.Create(roomIndex);

		_topBar.ApplyTheme(theme);
		_bottomBar.ApplyTheme(theme);
	}

	public void SetMessage(
		string message)
	{
		_bottomBar.SetMessage(message);
	}
}
