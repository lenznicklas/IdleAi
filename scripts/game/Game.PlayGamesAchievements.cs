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

		_playGamesAchievements.SyncLeaderboards(
			_state,
			_progression
		);

		_statsLeaderboardButtons?.Refresh();

		_singularityMapExtension?.Refresh();
	}
}
