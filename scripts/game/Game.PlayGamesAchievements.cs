using Godot;

namespace IdleAi;


/*
 * Play Games + Firebase alias integration and lightweight feature bootstraps
 * live in this partial Game class so the large main Game.cs stays focused on
 * the primary gameplay loop.
 */
public partial class Game
{
	private GooglePlayGamesAchievementService?
		_playGamesAchievements;

	private FirebaseAliasService?
		_firebaseAliases;

	private Timer?
		_playGamesAchievementTimer;

	private UsernameSetupOverlayController?
		_usernameSetupOverlay;

	private LeaderboardOverlayController?
		_leaderboardOverlay;

	private StatsLeaderboardButtonsController?
		_statsLeaderboardButtons;

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

	private bool _roomSelectionGuardInstalled;


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
			_playGamesAchievements
				!= null
		)
		{
			return;
		}

		InitializeReadability();
		InitializeBotDurabilityUi();
		InitializePlayGamesAndFirebase();
		InitializeSingularity();
		InstallSequentialRoomSelectionGuard();

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
		_firebaseAliases =
			new FirebaseAliasService
			{
				Name =
					"FirebaseAliasService"
			};

		AddChild(
			_firebaseAliases
		);

		_firebaseAliases.Initialize();

		_playGamesAchievements =
			new GooglePlayGamesAchievementService();

		_playGamesAchievements.Initialize();

		_playGamesAchievementTimer =
			new Timer
			{
				Name =
					"PlayGamesAchievementSyncTimer",

				WaitTime =
					1.0,

				OneShot =
					false,

				Autostart =
					true
			};

		_playGamesAchievementTimer.Timeout +=
			SyncPlayGamesAchievements;

		AddChild(
			_playGamesAchievementTimer
		);

		_usernameSetupOverlay =
			new UsernameSetupOverlayController(
				this,
				_firebaseAliases
			);

		_usernameSetupOverlay.Initialize();

		_leaderboardOverlay =
			new LeaderboardOverlayController(
				this,
				_playGamesAchievements,
				_firebaseAliases,
				_usernameSetupOverlay
			);

		_leaderboardOverlay.Initialize();

		_statsLeaderboardButtons =
			new StatsLeaderboardButtonsController(
				this,
				_state,
				_progression,
				_leaderboardOverlay
			);

		_statsLeaderboardButtons.Initialize();
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
				_singularityController
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


	private void InstallSequentialRoomSelectionGuard()
	{
		if (
			_roomSelectionGuardInstalled
			|| _ui == null
		)
		{
			return;
		}

		/*
		 * Game.cs originally subscribes OnRoomSelectedRequested directly.
		 * Replace that subscription with a guarded wrapper so the progression
		 * rule is enforced BEFORE UnlockRoom can spend Tokens.
		 */
		_ui.RoomSelectedRequested -=
			OnRoomSelectedRequested;

		_ui.RoomSelectedRequested +=
			OnGuardedRoomSelectedRequested;

		_roomSelectionGuardInstalled =
			true;
	}


	private void OnGuardedRoomSelectedRequested(
		int targetRoom)
	{
		if (
			targetRoom > 0
			&& targetRoom < _state.RoomStates.Count
			&& !_state.RoomStates[
				targetRoom
			].Unlocked
		)
		{
			if (
				!RoomUnlockRules.CanUnlockRoom(
					_state,
					targetRoom,
					out string reason
				)
			)
			{
				_ui.SetMessage(
					reason
				);

				_ui.UpdateAll();

				return;
			}
		}

		OnRoomSelectedRequested(
			targetRoom
		);
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


	private void SyncPlayGamesAchievements()
	{
		if (
			_playGamesAchievements
				== null
			|| _state == null
			|| _progression == null
		)
		{
			return;
		}

		_playGamesAchievements.SyncAchievements(
			_state,
			_progression
		);

		/*
		 * Keep the official Google Play leaderboards exactly as before.
		 */
		_playGamesAchievements.SyncLeaderboards(
			_state,
			_progression
		);

		/*
		 * Additionally persist the same values beside the unique Idle AI
		 * username. The custom in-game Top 25 is rendered from this source,
		 * so every row has a real Firebase username and an exact rank.
		 */
		_firebaseAliases?.SyncInGameLeaderboardScores(
			_progression.GetTotalLevel(),
			_state.Prestige.PrestigeCount
		);

		_statsLeaderboardButtons?.Refresh();

		_singularityMapExtension?.Refresh();
	}
}
