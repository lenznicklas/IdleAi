using Godot;

namespace IdleAi;


/*
 * Play Games + Firebase alias integration lives in this partial Game class so
 * the large main Game.cs stays focused on gameplay.
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


	public override void _EnterTree()
	{
		Callable
			.From(
				InitializePlayGamesAchievements
			)
			.CallDeferred();
	}


	private void InitializePlayGamesAchievements()
	{
		if (
			_playGamesAchievements
				!= null
		)
		{
			return;
		}


		/*
		 * Connect FirebaseAliasService first so it can capture the raw
		 * topScoresLoaded/scoreLoaded JSON and cache Play Games player IDs
		 * before the visible leaderboard needs them. The UI also handles the
		 * reverse callback order, so this is only an extra safety measure.
		 */
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
	}
}
