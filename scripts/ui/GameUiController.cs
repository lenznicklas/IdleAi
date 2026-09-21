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


	// ==================================================
	// SUB CONTROLLERS
	// ==================================================

	private RoomUiController _room =
		null!;


	private TopBarController _topBar =
		null!;


	private BottomBarController _bottomBar =
		null!;


	private MapController _map =
		null!;


	private StatsOverlayController _stats =
		null!;


	private PrestigeOverlayController _prestige =
		null!;


	private MachineDetailsOverlay _details =
		null!;


	// ==================================================
	// SHOP
	// ==================================================

	private Control _shopPage =
		null!;


	// ==================================================
	// THEME
	// ==================================================

	private int _lastThemeRoom =
		-1;


	// ==================================================
	// EVENTS
	// ==================================================

	public event Action<int>? SlotActionRequested;

	public event Action<int>? RoomSelectedRequested;

	public event Action? StateChanged;

	public event Action? PrestigeRequested;


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


		_prestigeService =
			prestige;
	}


	// ==================================================
	// INITIALIZE
	// ==================================================

	public void Initialize()
	{
		_root.Theme =
			MainTheme;


		CreateRoomController();

		CreateTopBarController();

		CreateBottomBarController();

		CreateMapController();

		CreateStatsController();

		CreatePrestigeController();

		CreateShopController();

		CreateMachineDetails();


		ApplyRoomTheme(
			true
		);


		UpdateAll();
	}


	// ==================================================
	// ROOM CONTROLLER
	// ==================================================

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
				SlotActionRequested?.Invoke(
					index
				);


		_room.MessageRequested +=
			SetMessage;


		_room.DetailsRequested +=
			OpenMachineDetails;


		_room.Initialize();
	}


	// ==================================================
	// TOP BAR CONTROLLER
	// ==================================================

	private void CreateTopBarController()
	{
		_topBar =
			new TopBarController(
				_root,
				_state,
				_progression
			);


		_topBar.StatsRequested +=
			OpenStats;


		_topBar.Initialize();
	}


	// ==================================================
	// BOTTOM BAR CONTROLLER
	// ==================================================

	private void CreateBottomBarController()
	{
		_bottomBar =
			new BottomBarController(
				_root
			);


		_bottomBar.MapRequested +=
			ToggleMapPage;


		_bottomBar.ShopRequested +=
			ToggleShopPage;


		_bottomBar.Initialize();
	}


	// ==================================================
	// MAP CONTROLLER
	// ==================================================

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
				RoomSelectedRequested?.Invoke(
					roomIndex
				);


		_map.Initialize();
	}


	// ==================================================
	// STATS CONTROLLER
	// ==================================================

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


	// ==================================================
	// PRESTIGE CONTROLLER
	// ==================================================

	private void CreatePrestigeController()
	{
		_prestige =
			new PrestigeOverlayController(
				_root,
				_state,
				_prestigeService
			);


		_prestige.Confirmed +=
			() =>
			{
				PrestigeRequested?.Invoke();
			};


		_prestige.Cancelled +=
			() =>
			{
				_stats.Open();
			};


		_prestige.Initialize();
	}


	// ==================================================
	// SHOP
	// ==================================================

	private void CreateShopController()
	{
		_shopPage =
			_root.GetNode<Control>(
				"ShopPage"
			);


		_shopPage.Hide();
	}


	// ==================================================
	// MACHINE DETAILS
	// ==================================================

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
			|| slotIndex
			>= _state.CurrentRoomState.Slots.Count
		)
		{
			return;
		}


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
	// MAP PAGE
	// ==================================================

	private void ToggleMapPage()
	{
		_shopPage.Hide();


		if (_map.Visible)
		{
			_map.Hide();

			return;
		}


		CloseTransientOverlays();


		_map.Open();


		_bottomBar.MoveToFront();
	}


	// ==================================================
	// SHOP PAGE
	// ==================================================

	private void ToggleShopPage()
	{
		_map.Hide();


		if (_shopPage.Visible)
		{
			_shopPage.Hide();

			return;
		}


		CloseTransientOverlays();


		_shopPage.Show();

		_shopPage.MoveToFront();


		_bottomBar.MoveToFront();
	}


	// ==================================================
	// STATS
	// ==================================================

	private void OpenStats()
	{
		ClosePages();


		_details.Close();

		_topBar.HidePopups();

		_prestige.Hide();


		_stats.Open();
	}


	// ==================================================
	// PRESTIGE
	// ==================================================

	private void OpenPrestigeConfirmation()
	{
		if (
			!_prestige.Open()
		)
		{
			return;
		}


		_stats.Hide();
	}


	// ==================================================
	// TRANSIENT OVERLAYS
	// ==================================================

	private void CloseTransientOverlays()
	{
		_details.Close();

		_stats.Hide();

		_prestige.Hide();

		_topBar.HidePopups();
	}


	// ==================================================
	// PAGE CONTROL
	// ==================================================

	public void ClosePages()
	{
		_map.Hide();

		_shopPage.Hide();
	}


	// ==================================================
	// FULL UPDATE
	// ==================================================

	public void UpdateAll()
	{
		_room.UpdateAll();


		_topBar.SetRoomName(
			_state.CurrentRoom.Name
		);


		_topBar.UpdateValues();


		_map.Refresh();


		ApplyRoomTheme();


		if (_stats.Visible)
		{
			_stats.Refresh();
		}


		if (_details.Visible)
		{
			_details.Refresh();
		}
	}


	// ==================================================
	// RUNTIME UPDATE
	// ==================================================

	public void UpdateRuntime()
	{
		_topBar.UpdateValues();


		_room.UpdateRuntime();


		if (_details.Visible)
		{
			_details.Refresh();
		}


		if (_stats.Visible)
		{
			_stats.RefreshRuntime();
		}
	}


	// ==================================================
	// ROOM THEME
	// ==================================================

	private void ApplyRoomTheme(
		bool force = false)
	{
		int roomIndex =
			_state.CurrentRoomIndex;


		if (
			!force
			&& roomIndex
			== _lastThemeRoom
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


	// ==================================================
	// MESSAGE
	// ==================================================

	public void SetMessage(
		string message)
	{
		_bottomBar.SetMessage(
			message
		);
	}
}
