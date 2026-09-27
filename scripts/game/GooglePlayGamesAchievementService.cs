using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace IdleAi;


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


	private const ulong AuthenticationRetryMilliseconds =
		30_000;


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
