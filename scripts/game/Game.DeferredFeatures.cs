using Godot;

namespace IdleAi;


/// <summary>Deferred gameplay/UI startup; online ownership lives in its coordinator.</summary>
public partial class Game
{
    internal Color ActiveAreaAccent => RoomThemePalette.GetAreaAccentColor(
        _state.CurrentRoomIndex, _singularityController?.Visible ?? false);
    internal bool IsSingularityUnlockedForArchive => _singularityService?.Unlocked ?? false;

	private OnlineIntegrationCoordinator? _onlineIntegrations;

	private SingularityService?
		_singularityService;

	private SingularityController?
		_singularityController;

	private SingularityMapExtension?
		_singularityMapExtension;

	private UiReadabilityController?
		_uiReadability;

	private BotDurabilityUiController?
		_botDurabilityUi;

	private NavigationPolishController?
		_navigationPolish;

	private StartupInfoOverlayController?
		_startupInfoOverlay;



	public override void _EnterTree()
	{
		Callable
			.From(
				InitializeDeferredFeatures
			)
			.CallDeferred();
	}


	private void InitializeDeferredFeatures()
	{
		if (
			_onlineIntegrations
				!= null
		)
		{
			return;
		}

		InitializeReadability();
		InitializeBotDurabilityUi();
		InitializePlayGamesAndFirebase();
		InitializeSingularity();
		InitializeEvents();

		/*
		 * These are deliberately initialized after the normal UI and
		 * Singularity have been created. Shop is lazy and Lab is built later,
		 * therefore NavigationPolishController keeps discovering dynamic pages
		 * at runtime instead of assuming a fixed startup order.
		 */
		InitializeNavigationPolish();
		InitializeStartupInfoOverlay();
	}


	private void InitializeReadability()
	{
		if (_uiReadability != null)
			return;

		_uiReadability =
			new UiReadabilityController
			{
				Name =
					"UiReadabilityController"
			};

		AddChild(
			_uiReadability
		);

		_uiReadability.Initialize(
			this
		);
	}


	private void InitializeBotDurabilityUi()
	{
		if (_botDurabilityUi != null)
			return;

		_botDurabilityUi =
			new BotDurabilityUiController(
				this,
				_state
			)
			{
				Name =
					"BotDurabilityUiController"
			};

		AddChild(
			_botDurabilityUi
		);
	}


	private void InitializePlayGamesAndFirebase()
	{
		_onlineIntegrations = new OnlineIntegrationCoordinator { Name = "OnlineIntegrations" };
		AddChild(_onlineIntegrations);
		_onlineIntegrations.Initialize(this, _state, _progression);
		_onlineIntegrations.TickCompleted += () => _singularityMapExtension?.Refresh();
	}

	private void InitializeSingularity()
	{
		/*
		 * Passing GameState lets Singularity Bots reuse the same Lab Bot-power
		 * and rarity research as normal machine Bots.
		 */
		_singularityService =
			new SingularityService(
				_state
			);

		_singularityController =
			new SingularityController(
				this,
				_state,
				_singularityService
			);

		_singularityController.MessageRequested +=
			_ui.SetMessage;

		_singularityController.Initialize();

		_singularityController.EnableSmoothPanPhysics();

		_singularityController.EnableBotUi();

		/*
		 * Slightly enlarge the Sector frame around the existing Node network
		 * without changing map coordinates, snapping or cross-Sector links.
		 */
		_singularityController.EnableLargerSectorFrames();

		_singularityMapExtension =
			new SingularityMapExtension(
				this,
				_state,
				_singularityService,
				_singularityController,
				_ui.Map,
				_economy
			);

		_singularityMapExtension.MessageRequested +=
			_ui.SetMessage;

		_singularityMapExtension.GameStateChanged +=
			() =>
			{
				_ui.UpdateAll();
				SaveGame();
			};

		_singularityMapExtension.Initialize();
	}


	private void InitializeNavigationPolish()
	{
		if (_navigationPolish != null)
			return;

		_navigationPolish =
			new NavigationPolishController(
				this,
				_state,
				_singularityService
			)
			{
				Name =
					"NavigationPolishController"
			};

		AddChild(
			_navigationPolish
		);
	}


	private void InitializeStartupInfoOverlay()
	{
		if (_startupInfoOverlay != null)
			return;

		_startupInfoOverlay =
			new StartupInfoOverlayController(
				this
			)
			{
				Name =
					"StartupInfoOverlayController"
			};

		AddChild(
			_startupInfoOverlay
		);

		/*
		 * Shows once for this running process. Resume from Android/iOS standby
		 * does not recreate Game and therefore does not show the overlay again.
		 * A real app/process restart resets the static cold-start flag.
		 */
		_startupInfoOverlay
			.InitializeAndShow();
	}


}
