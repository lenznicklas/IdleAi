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
	}
}
