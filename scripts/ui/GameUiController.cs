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
	private readonly BotSkinService _skinService;

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
	private BotSkinShopController? _skinShop;

	private bool _shopInitialized;

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

		/*
		 * Cosmetic data is loaded before the room UI is built.
		 * BotCatalog therefore already exposes the equipped
		 * textures when MachineSlot / MachineDetails render.
		 */
		_skinService =
			new BotSkinService(
				_state
			);
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
		 * ShopController is intentionally NOT initialized here.
		 * Its initialization starts AdMob. On Android cold starts
		 * that extra native initialization is unnecessary during
		 * the loading -> game transition, so the shop is lazily
		 * initialized the first time the player opens it.
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

		/*
		 * Do not call _shop.Initialize() here.
		 * It initializes the native AdMob SDK. We delay this
		 * until the player opens the shop for the first time.
		 */
	}

	private void EnsureShopInitialized()
	{
		if (_shopInitialized)
			return;

		_shop.Initialize();

		_skinShop =
			new BotSkinShopController(
				_root,
				_state,
				_skinService
			);

		_skinShop.MessageRequested +=
			SetMessage;

		_skinShop.StateChanged +=
			() =>
			{
				/*
				 * BotSkinService already persisted cosmetic
				 * ownership/equipment. This event saves the
				 * Data Shard deduction through the normal game
				 * save and refreshes every visible bot texture.
				 */
				StateChanged?.Invoke();

				UpdateAll();

				_skinShop?.Refresh();
			};

		_skinShop.Initialize();

		_shopInitialized =
			true;
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

		/*
		 * First shop open is the only place where AdMob and
		 * the dynamic shop UI are initialized.
		 */
		EnsureShopInitialized();

		if (_shop.Visible)
		{
			_shop.Hide();

			SyncRoomChromeVisibility();

			return;
		}

		CloseTransientOverlays();

		_shop.Open();

		_skinShop?.Refresh();

		SyncRoomChromeVisibility();

		_bottomBar.MoveToFront();
	}

	private async void BringTopBarInfoPopupsToFront()
	{
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

		/*
		 * Safe before lazy initialization because ShopController
		 * uses null-conditional access in Hide().
		 */
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

		SyncRoomChromeVisibility();

		_pipeline.UpdateAll();
		_infrastructure.UpdateAll();
		_quantum.UpdateAll();

		_topBar.SetRoomName(
			_state.CurrentRoom.Name
		);

		_topBar.UpdateValues();
		_map.Refresh();

		/*
		 * Refresh is safe before lazy initialization.
		 */
		_shop.Refresh();
		_skinShop?.Refresh();

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

		_lastDisplayedRoom =
			currentRoom;

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
		{
			_shop.Refresh();
			_skinShop?.Refresh();
		}

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

		_lastThemeRoom =
			roomIndex;

		RoomThemeTextures theme =
			RoomThemePalette.Create(
				roomIndex
			);

		_topBar.ApplyTheme(
			theme
		);

		_bottomBar.ApplyTheme(
			theme
		);
	}

	public void SetMessage(
		string message)
	{
		_bottomBar.SetMessage(
			message
		);
	}
}
