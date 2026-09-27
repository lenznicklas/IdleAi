using Godot;

namespace IdleAi;


/*
 * Game.cs is already declared as "partial", so Play Games can be integrated
 * without replacing the large existing Game.cs file.
 *
 * Godot calls _EnterTree() before the existing Game._Ready(). We defer setup
 * by one idle step, then use a small Timer to synchronize achievement
 * conditions with Google Play Games.
 */
public partial class Game
{
	private GooglePlayGamesAchievementService?
		_playGamesAchievements;


	private Timer?
		_playGamesAchievementTimer;

	private LeaderboardOverlayController?
		_leaderboardOverlay;

	private StatsLeaderboardButtonsController?
		_statsLeaderboardButtons;


	public override void _EnterTree()
	{
		/*
		 * Do not add child nodes while the parent is still entering the tree.
		 * Deferred setup also means the normal Game._Ready() has time to
		 * create/load GameState before the first synchronization tick.
		 */
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


		/*
		 * Native Idle AI leaderboard overlay. Google Play still provides all
		 * leaderboard data; only the presentation remains inside the game.
		 */
		_leaderboardOverlay =
			new LeaderboardOverlayController(
				this,
				_playGamesAchievements
			);


		_leaderboardOverlay.Initialize();


		/*
		 * Stats already exists in the scene. This controller inserts the two
		 * leaderboard cards as the first section of the existing Stats VBox.
		 */
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
		/*
		 * The timer may tick very early on a slow device.
		 * The existing Game._Ready() initializes these fields.
		 */
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
