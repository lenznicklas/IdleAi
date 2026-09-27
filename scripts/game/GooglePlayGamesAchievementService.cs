using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace IdleAi;


public sealed record GooglePlayLeaderboardEntry(
	long Rank,
	string DisplayRank,
	long RawScore,
	string DisplayScore,
	string PlayerName
);


public sealed class GooglePlayGamesAchievementService
{
	private const string PluginName =
		"GodotPlayGameServices";


	// ==================================================
	// ACHIEVEMENT IDS - GOOGLE PLAY CONSOLE
	// ==================================================

	public const string HelloWorldId =
		"CgkI7res5usVEAIQAQ";

	public const string AutomatedIntelligenceId =
		"CgkI7res5usVEAIQAg";

	public const string MovingUpId =
		"CgkI7res5usVEAIQAw";

	public const string ResearcherId =
		"CgkI7res5usVEAIQBQ";

	public const string PrestigiousId =
		"CgkI7res5usVEAIQBA";

	public const string ServerOnlineId =
		"CgkI7res5usVEAIQBg";

	public const string QuantumLeapId =
		"CgkI7res5usVEAIQBw";


	// ==================================================
	// LEADERBOARD IDS - GOOGLE PLAY CONSOLE
	// ==================================================

	public const string HighestTotalLevelLeaderboardId =
		"CgkI7res5usVEAIQCA";

	public const string MostPrestigesLeaderboardId =
		"CgkI7res5usVEAIQCQ";


	private const ulong AuthenticationRetryMilliseconds =
		30_000;

	/*
	 * Google Play Games LeaderboardVariant constants:
	 *
	 * TIME_SPAN_ALL_TIME = 2
	 * COLLECTION_PUBLIC  = 0
	 */
	private const int LeaderboardTimeSpanAllTime =
		2;

	private const int LeaderboardCollectionPublic =
		0;

	private const int LeaderboardTopResultCount =
		25;


	private GodotObject? _plugin;


	private readonly HashSet<string>
		_pendingUnlocks =
			new(
				StringComparer.Ordinal
			);


	private readonly HashSet<string>
		_confirmedUnlocks =
			new(
				StringComparer.Ordinal
			);


	private bool _initialized;

	private bool _authenticated;

	private bool _manualSignInAttempted;

	private ulong _nextAuthenticationCheckAt;

	private long _lastSubmittedLevel =
		-1;

	private long _lastSubmittedPrestiges =
		-1;

	private string? _pendingLeaderboardToShow;

	private string? _pendingLeaderboardDataRequest;


	public event Action<
		string,
		IReadOnlyList<GooglePlayLeaderboardEntry>
	>? LeaderboardTopScoresLoaded;

	public event Action<
		string,
		GooglePlayLeaderboardEntry?
	>? LeaderboardPlayerScoreLoaded;

	public event Action<
		string,
		string
	>? LeaderboardLoadFailed;


	public bool IsAvailable =>
		OS.GetName() == "Android"
		&& _plugin != null;


	public bool IsAuthenticated =>
		_authenticated;


	// ==================================================
	// INITIALIZATION
	// ==================================================

	public void Initialize()
	{
		if (_initialized)
			return;


		_initialized =
			true;


		/*
		 * Google Play Games Services only exists in the Android build.
		 * Keeping this a no-op on desktop means the Godot editor can run
		 * normally without the Android singleton.
		 */
		if (OS.GetName() != "Android")
		{
			GD.Print(
				"Google Play Games: desktop/editor mode - integration disabled."
			);

			return;
		}


		if (!Engine.HasSingleton(PluginName))
		{
			GD.PushWarning(
				"Google Play Games plugin not found. "
				+ "Enable GodotPlayGameServices in Project Settings > Plugins "
				+ "and enable it in the Android export preset."
			);

			return;
		}


		_plugin =
			Engine.GetSingleton(
				PluginName
			);


		if (_plugin == null)
		{
			GD.PushWarning(
				"Google Play Games singleton returned null."
			);

			return;
		}


		ConnectSignals();


		/*
		 * The current Godot Play Games Services plugin requires manual
		 * initialization. After that we explicitly check authentication.
		 */
		_plugin.Call(
			"initialize"
		);


		_plugin.Call(
			"isAuthenticated"
		);


		_nextAuthenticationCheckAt =
			Time.GetTicksMsec()
			+ AuthenticationRetryMilliseconds;


		GD.Print(
			"Google Play Games: initialized."
		);
	}


	private void ConnectSignals()
	{
		if (_plugin == null)
			return;


		_plugin.Connect(
			"userAuthenticated",
			Callable.From<bool>(
				OnUserAuthenticated
			)
		);


		_plugin.Connect(
			"achievementUnlocked",
			Callable.From<bool, string>(
				OnAchievementUnlocked
			)
		);


		_plugin.Connect(
			"achievementsLoaded",
			Callable.From<string>(
				OnAchievementsLoaded
			)
		);


		_plugin.Connect(
			"scoreSubmitted",
			Callable.From<bool, string>(
				OnScoreSubmitted
			)
		);


		_plugin.Connect(
			"topScoresLoaded",
			Callable.From<string, string>(
				OnTopScoresLoaded
			)
		);


		_plugin.Connect(
			"scoreLoaded",
			Callable.From<string, string>(
				OnPlayerScoreLoaded
			)
		);
	}


	// ==================================================
	// AUTHENTICATION
	// ==================================================

	private void OnUserAuthenticated(
		bool authenticated)
	{
		_authenticated =
			authenticated;


		if (authenticated)
		{
			GD.Print(
				"Google Play Games: player authenticated."
			);


			/*
			 * Load the server state once so we do not unnecessarily request
			 * achievements that Google already knows are unlocked.
			 */
			_plugin?.Call(
				"loadAchievements",
				true
			);


			if (
				!string.IsNullOrWhiteSpace(
					_pendingLeaderboardDataRequest
				)
			)
			{
				string leaderboardId =
					_pendingLeaderboardDataRequest;

				_pendingLeaderboardDataRequest =
					null;

				LoadLeaderboardData(
					leaderboardId
				);
			}


			if (
				!string.IsNullOrWhiteSpace(
					_pendingLeaderboardToShow
				)
			)
			{
				string leaderboardId =
					_pendingLeaderboardToShow;

				_pendingLeaderboardToShow =
					null;

				ShowLeaderboard(
					leaderboardId
				);
			}


			return;
		}


		GD.Print(
			"Google Play Games: player is not authenticated."
		);


		/*
		 * First try the normal interactive sign-in once.
		 *
		 * We deliberately do NOT continuously pop the sign-in UI if the
		 * player cancels. Later authentication checks are silent.
		 */
		if (
			!_manualSignInAttempted
			&& _plugin != null
		)
		{
			_manualSignInAttempted =
				true;


			_plugin.Call(
				"signIn"
			);
		}
	}


	private void RetryAuthenticationIfNeeded()
	{
		if (
			_authenticated
			|| _plugin == null
		)
		{
			return;
		}


		ulong now =
			Time.GetTicksMsec();


		if (
			now
			< _nextAuthenticationCheckAt
		)
		{
			return;
		}


		_nextAuthenticationCheckAt =
			now
			+ AuthenticationRetryMilliseconds;


		/*
		 * Silent state check only.
		 * signIn() is not repeated automatically after the initial attempt.
		 */
		_plugin.Call(
			"isAuthenticated"
		);
	}


	// ==================================================
	// SERVER ACHIEVEMENT STATE
	// ==================================================

	private void OnAchievementsLoaded(
		string achievementsJson)
	{
		if (
			string.IsNullOrWhiteSpace(
				achievementsJson
			)
		)
		{
			return;
		}


		try
		{
			using JsonDocument document =
				JsonDocument.Parse(
					achievementsJson
				);


			if (
				document.RootElement.ValueKind
				!= JsonValueKind.Array
			)
			{
				return;
			}


			foreach (
				JsonElement achievement
				in document.RootElement
					.EnumerateArray()
			)
			{
				if (
					!achievement.TryGetProperty(
						"achievementId",
						out JsonElement idElement
					)
					|| !achievement.TryGetProperty(
						"state",
						out JsonElement stateElement
					)
				)
				{
					continue;
				}


				string? achievementId =
					idElement.GetString();


				string? state =
					stateElement.GetString();


				if (
					string.IsNullOrWhiteSpace(
						achievementId
					)
				)
				{
					continue;
				}


				if (
					state == "STATE_UNLOCKED"
				)
				{
					_confirmedUnlocks.Add(
						achievementId
					);


					_pendingUnlocks.Remove(
						achievementId
					);
				}
			}


			GD.Print(
				"Google Play Games: loaded achievement state. Already unlocked=",
				_confirmedUnlocks.Count
			);
		}
		catch (Exception exception)
		{
			/*
			 * Failure to parse the list is not fatal.
			 * unlockAchievement() is idempotent on Google's side, so normal
			 * condition syncing can still safely continue.
			 */
			GD.PushWarning(
				"Google Play Games: could not parse achievements: "
					+ exception.Message
			);
		}
	}


	private void OnAchievementUnlocked(
		bool unlocked,
		string achievementId)
	{
		_pendingUnlocks.Remove(
			achievementId
		);


		if (unlocked)
		{
			_confirmedUnlocks.Add(
				achievementId
			);


			GD.Print(
				"Google Play achievement unlocked: ",
				GetAchievementName(
					achievementId
				),
				" [",
				achievementId,
				"]"
			);


			return;
		}


		/*
		 * False can mean the request failed. Do not permanently remember it
		 * locally, so the next sync can try again.
		 */
		GD.PushWarning(
			"Google Play achievement unlock was not confirmed: "
				+ achievementId
		);
	}


	// ==================================================
	// GAME -> GOOGLE PLAY SYNC
	// ==================================================

	public void SyncAchievements(
		GameState state,
		ProgressionService progression)
	{
		if (!_initialized)
		{
			Initialize();
		}


		if (_plugin == null)
			return;


		if (!_authenticated)
		{
			RetryAuthenticationIfNeeded();

			return;
		}


		// --------------------------------------------------
		// HELLO, WORLD!
		// Start/use the first machine.
		//
		// TotalEarned makes this work retroactively for old saves.
		// IsRunning lets it unlock immediately after START is pressed,
		// before the first production cycle necessarily completes.
		// --------------------------------------------------

		if (
			state.Stats.TotalEarned > 0.0
			|| HasRunningMachine(
				state
			)
		)
		{
			TryUnlock(
				HelloWorldId
			);
		}


		// --------------------------------------------------
		// AUTOMATED INTELLIGENCE
		// Own/buy the first bot.
		//
		// Bot spending survives selling/prestige, so existing players who
		// owned a bot in the past are still detected.
		// --------------------------------------------------

		if (
			state.Stats.GetMachineSpending(
				"Bots"
			) > 0.0
			|| HasAnyBot(
				state
			)
		)
		{
			TryUnlock(
				AutomatedIntelligenceId
			);
		}


		// --------------------------------------------------
		// MOVING UP
		// Reach total Level 150.
		//
		// HighestClaimedLevelReward also survives Prestige, allowing an
		// existing save that already claimed Level 150 to be recognized.
		// --------------------------------------------------

		if (
			progression.GetTotalLevel()
				>= 150
			|| state.Shop.HighestClaimedLevelReward
				>= 150
		)
		{
			TryUnlock(
				MovingUpId
			);
		}


		// --------------------------------------------------
		// RESEARCHER
		// Complete first research.
		// --------------------------------------------------

		if (
			state.Lab.CompletedResearch.Count
				> 0
		)
		{
			TryUnlock(
				ResearcherId
			);
		}


		// --------------------------------------------------
		// PRESTIGIOUS
		// Perform first Prestige.
		// --------------------------------------------------

		if (
			state.Prestige.PrestigeCount
				>= 1
		)
		{
			TryUnlock(
				PrestigiousId
			);
		}


		// --------------------------------------------------
		// SERVER ONLINE
		// Unlock Server Room.
		//
		// DataShardUnlockRewardClaimed is retained for old-save/history
		// compatibility and remains true after Prestige.
		// --------------------------------------------------

		if (
			HasEverUnlockedRoom(
				state,
				"Server Room"
			)
		)
		{
			TryUnlock(
				ServerOnlineId
			);
		}


		// --------------------------------------------------
		// QUANTUM LEAP
		// Unlock Quantum Lab.
		// --------------------------------------------------

		if (
			HasEverUnlockedRoom(
				state,
				"Quantum Lab"
			)
		)
		{
			TryUnlock(
				QuantumLeapId
			);
		}
	}


	private void TryUnlock(
		string achievementId)
	{
		if (
			_plugin == null
			|| !_authenticated
			|| _confirmedUnlocks.Contains(
				achievementId
			)
			|| _pendingUnlocks.Contains(
				achievementId
			)
		)
		{
			return;
		}


		_pendingUnlocks.Add(
			achievementId
		);


		try
		{
			_plugin.Call(
				"unlockAchievement",
				achievementId
			);


			GD.Print(
				"Google Play achievement requested: ",
				GetAchievementName(
					achievementId
				)
			);
		}
		catch (Exception exception)
		{
			_pendingUnlocks.Remove(
				achievementId
			);


			GD.PushWarning(
				"Google Play achievement request failed: "
					+ exception.Message
			);
		}
	}


	// ==================================================
	// LEADERBOARDS
	// ==================================================

	public void SyncLeaderboards(
		GameState state,
		ProgressionService progression)
	{
		if (!_initialized)
		{
			Initialize();
		}


		if (
			_plugin == null
			|| !_authenticated
		)
		{
			return;
		}


		long totalLevel =
			progression.GetTotalLevel();


		long prestigeCount =
			Math.Max(
				0,
				state.Prestige.PrestigeCount
			);


		/*
		 * Google Play is configured with "larger is better".
		 *
		 * We only submit when the local value changed during this app
		 * session. Google itself keeps the player's best score, so a
		 * Prestige that temporarily lowers the current total level can
		 * never overwrite a previously higher level score.
		 */
		if (
			totalLevel
			!= _lastSubmittedLevel
		)
		{
			SubmitScore(
				HighestTotalLevelLeaderboardId,
				totalLevel
			);


			_lastSubmittedLevel =
				totalLevel;
		}


		if (
			prestigeCount
			!= _lastSubmittedPrestiges
		)
		{
			SubmitScore(
				MostPrestigesLeaderboardId,
				prestigeCount
			);


			_lastSubmittedPrestiges =
				prestigeCount;
		}
	}


	private void SubmitScore(
		string leaderboardId,
		long score)
	{
		if (
			_plugin == null
			|| !_authenticated
			|| score < 0
		)
		{
			return;
		}


		try
		{
			_plugin.Call(
				"submitScore",
				leaderboardId,
				score
			);


			GD.Print(
				"Google Play leaderboard score submitted: ",
				GetLeaderboardName(
					leaderboardId
				),
				" = ",
				score
			);
		}
		catch (Exception exception)
		{
			GD.PushWarning(
				"Google Play leaderboard score submission failed: "
					+ exception.Message
			);
		}
	}


	private void OnScoreSubmitted(
		bool submitted,
		string leaderboardId)
	{
		if (submitted)
		{
			GD.Print(
				"Google Play leaderboard confirmed: ",
				GetLeaderboardName(
					leaderboardId
				)
			);


			return;
		}


		GD.PushWarning(
			"Google Play leaderboard score was not confirmed: "
				+ leaderboardId
		);


		/*
		 * Allow a retry on the next synchronization tick.
		 */
		if (
			leaderboardId
			== HighestTotalLevelLeaderboardId
		)
		{
			_lastSubmittedLevel =
				-1;
		}


		if (
			leaderboardId
			== MostPrestigesLeaderboardId
		)
		{
			_lastSubmittedPrestiges =
				-1;
		}
	}


	public bool RequestLeaderboardData(
		string leaderboardId)
	{
		if (!_initialized)
		{
			Initialize();
		}


		if (_plugin == null)
		{
			LeaderboardLoadFailed?.Invoke(
				leaderboardId,
				"Google Play Games is not available in this build."
			);

			return false;
		}


		if (_authenticated)
		{
			LoadLeaderboardData(
				leaderboardId
			);

			return true;
		}


		/*
		 * Keep the requested leaderboard and continue immediately after
		 * Google Play authentication succeeds.
		 */
		_pendingLeaderboardDataRequest =
			leaderboardId;


		try
		{
			_manualSignInAttempted =
				true;


			_plugin.Call(
				"signIn"
			);


			return true;
		}
		catch (Exception exception)
		{
			_pendingLeaderboardDataRequest =
				null;


			LeaderboardLoadFailed?.Invoke(
				leaderboardId,
				"Google Play sign-in could not be started: "
					+ exception.Message
			);


			return false;
		}
	}


	private void LoadLeaderboardData(
		string leaderboardId)
	{
		if (
			_plugin == null
			|| !_authenticated
		)
		{
			return;
		}


		try
		{
			/*
			 * Load the public all-time Top 25 and the signed-in player's
			 * own score/rank as two independent requests.
			 */
			_plugin.Call(
				"loadTopScores",
				leaderboardId,
				LeaderboardTimeSpanAllTime,
				LeaderboardCollectionPublic,
				LeaderboardTopResultCount,
				true
			);


			_plugin.Call(
				"loadPlayerScore",
				leaderboardId,
				LeaderboardTimeSpanAllTime,
				LeaderboardCollectionPublic
			);
		}
		catch (Exception exception)
		{
			LeaderboardLoadFailed?.Invoke(
				leaderboardId,
				"Leaderboard could not be loaded: "
					+ exception.Message
			);
		}
	}


	private void OnTopScoresLoaded(
		string leaderboardId,
		string scoresJson)
	{
		try
		{
			IReadOnlyList<GooglePlayLeaderboardEntry> entries =
				ParseLeaderboardScores(
					scoresJson
				);


			LeaderboardTopScoresLoaded?.Invoke(
				leaderboardId,
				entries
			);
		}
		catch (Exception exception)
		{
			GD.PushWarning(
				"Google Play top scores parse failed: "
					+ exception.Message
			);


			LeaderboardLoadFailed?.Invoke(
				leaderboardId,
				"Could not read Google Play leaderboard data."
			);
		}
	}


	private void OnPlayerScoreLoaded(
		string leaderboardId,
		string scoreJson)
	{
		try
		{
			GooglePlayLeaderboardEntry? entry =
				ParseSingleLeaderboardScore(
					scoreJson
				);


			LeaderboardPlayerScoreLoaded?.Invoke(
				leaderboardId,
				entry
			);
		}
		catch (Exception exception)
		{
			GD.PushWarning(
				"Google Play player score parse failed: "
					+ exception.Message
			);


			LeaderboardPlayerScoreLoaded?.Invoke(
				leaderboardId,
				null
			);
		}
	}


	private static IReadOnlyList<GooglePlayLeaderboardEntry>
		ParseLeaderboardScores(
			string json)
	{
		List<GooglePlayLeaderboardEntry> entries =
			[];


		if (
			string.IsNullOrWhiteSpace(
				json
			)
			|| json == "null"
		)
		{
			return entries;
		}


		using JsonDocument document =
			JsonDocument.Parse(
				json
			);


		if (
			document.RootElement.ValueKind
				!= JsonValueKind.Object
			|| !document.RootElement.TryGetProperty(
				"scores",
				out JsonElement scores
			)
			|| scores.ValueKind
				!= JsonValueKind.Array
		)
		{
			return entries;
		}


		foreach (
			JsonElement score
				in scores.EnumerateArray()
		)
		{
			GooglePlayLeaderboardEntry? entry =
				ParseScoreElement(
					score
				);


			if (entry != null)
			{
				entries.Add(
					entry
				);
			}
		}


		return entries;
	}


	private static GooglePlayLeaderboardEntry?
		ParseSingleLeaderboardScore(
			string json)
	{
		if (
			string.IsNullOrWhiteSpace(
				json
			)
			|| json == "null"
		)
		{
			return null;
		}


		using JsonDocument document =
			JsonDocument.Parse(
				json
			);


		if (
			document.RootElement.ValueKind
				!= JsonValueKind.Object
		)
		{
			return null;
		}


		return ParseScoreElement(
			document.RootElement
		);
	}


	private static GooglePlayLeaderboardEntry?
		ParseScoreElement(
			JsonElement score)
	{
		long rank =
			GetJsonInt64(
				score,
				"rank"
			);


		long rawScore =
			GetJsonInt64(
				score,
				"rawScore"
			);


		string displayRank =
			GetJsonString(
				score,
				"displayRank"
			);


		if (
			string.IsNullOrWhiteSpace(
				displayRank
			)
		)
		{
			displayRank =
				rank > 0
					? "#"
						+ rank
					: "-";
		}


		string displayScore =
			GetJsonString(
				score,
				"displayScore"
			);


		if (
			string.IsNullOrWhiteSpace(
				displayScore
			)
		)
		{
			displayScore =
				rawScore.ToString();
		}


		string playerName =
			GetJsonString(
				score,
				"scoreHolderDisplayName"
			);


		if (
			string.IsNullOrWhiteSpace(
				playerName
			)
		)
		{
			playerName =
				"Player";
		}


		return new GooglePlayLeaderboardEntry(
			rank,
			displayRank,
			rawScore,
			displayScore,
			playerName
		);
	}


	private static string GetJsonString(
		JsonElement element,
		string propertyName)
	{
		if (
			!element.TryGetProperty(
				propertyName,
				out JsonElement property
			)
			|| property.ValueKind
				== JsonValueKind.Null
		)
		{
			return "";
		}


		return property.ValueKind
				== JsonValueKind.String
			? property.GetString()
				?? ""
			: property.ToString();
	}


	private static long GetJsonInt64(
		JsonElement element,
		string propertyName)
	{
		if (
			!element.TryGetProperty(
				propertyName,
				out JsonElement property
			)
		)
		{
			return 0;
		}


		if (
			property.ValueKind
				== JsonValueKind.Number
			&& property.TryGetInt64(
				out long numericValue
			)
		)
		{
			return numericValue;
		}


		if (
			property.ValueKind
				== JsonValueKind.String
			&& long.TryParse(
				property.GetString(),
				out long stringValue
			)
		)
		{
			return stringValue;
		}


		return 0;
	}


	public void ShowLevelLeaderboard()
	{
		ShowLeaderboardOrSignIn(
			HighestTotalLevelLeaderboardId
		);
	}


	public void ShowPrestigeLeaderboard()
	{
		ShowLeaderboardOrSignIn(
			MostPrestigesLeaderboardId
		);
	}


	private void ShowLeaderboardOrSignIn(
		string leaderboardId)
	{
		if (!_initialized)
		{
			Initialize();
		}


		if (_plugin == null)
		{
			GD.PushWarning(
				"Google Play Games is not available on this device/build."
			);

			return;
		}


		if (_authenticated)
		{
			ShowLeaderboard(
				leaderboardId
			);


			return;
		}


		/*
		 * Remember what the player wanted. The authentication callback opens
		 * it immediately after a successful sign-in.
		 */
		_pendingLeaderboardToShow =
			leaderboardId;


		_manualSignInAttempted =
			true;


		_plugin.Call(
			"signIn"
		);
	}


	private void ShowLeaderboard(
		string leaderboardId)
	{
		if (
			_plugin == null
			|| !_authenticated
		)
		{
			return;
		}


		_plugin.Call(
			"showLeaderboard",
			leaderboardId
		);
	}


	private static string GetLeaderboardName(
		string leaderboardId)
	{
		return leaderboardId switch
		{
			HighestTotalLevelLeaderboardId =>
				"Highest Total Level",

			MostPrestigesLeaderboardId =>
				"Most Prestiges",

			_ =>
				leaderboardId
		};
	}


	// ==================================================
	// OPTIONAL UI METHOD
	// ==================================================

	public void ShowAchievements()
	{
		if (
			_plugin == null
			|| !_authenticated
		)
		{
			return;
		}


		_plugin.Call(
			"showAchievements"
		);
	}


	// ==================================================
	// CONDITION HELPERS
	// ==================================================

	private static bool HasRunningMachine(
		GameState state)
	{
		foreach (
			RoomState room
				in state.RoomStates
		)
		{
			foreach (
				SlotData slot
					in room.Slots
			)
			{
				if (
					slot.Unlocked
					&& slot.IsRunning
				)
				{
					return true;
				}
			}
		}


		return false;
	}


	private static bool HasAnyBot(
		GameState state)
	{
		foreach (
			RoomState room
				in state.RoomStates
		)
		{
			foreach (
				SlotData slot
					in room.Slots
			)
			{
				if (slot.HasBot)
					return true;
			}
		}


		return false;
	}


	private static bool HasEverUnlockedRoom(
		GameState state,
		string roomName)
	{
		for (
			int roomIndex = 0;
			roomIndex < state.Rooms.Count
				&& roomIndex
					< state.RoomStates.Count;
			roomIndex++
		)
		{
			if (
				!state.Rooms[
					roomIndex
				].Name.Equals(
					roomName,
					StringComparison.Ordinal
				)
			)
			{
				continue;
			}


			RoomState roomState =
				state.RoomStates[
					roomIndex
				];


			return roomState.Unlocked
				|| roomState
					.DataShardUnlockRewardClaimed;
		}


		return false;
	}


	private static string GetAchievementName(
		string achievementId)
	{
		return achievementId switch
		{
			HelloWorldId =>
				"Hello World!",

			AutomatedIntelligenceId =>
				"Automated Intelligence",

			MovingUpId =>
				"Moving Up",

			ResearcherId =>
				"Researcher",

			PrestigiousId =>
				"Prestigious",

			ServerOnlineId =>
				"Server Online",

			QuantumLeapId =>
				"Quantum Leap",

			_ =>
				achievementId
		};
	}
}
