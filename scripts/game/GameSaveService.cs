using System;
using System.Linq;

namespace IdleAi;

/// <summary>Hydration diagnostics for Game's load logging; no offline settlement.</summary>
public readonly record struct GameLoadStateResult(int RestoredLifetimeLevelBase, bool LifetimeLevelMigrated);

/// <summary>Maps normal-game state and applies legacy compatibility. No files, UI or offline settlement.</summary>
public sealed class GameSaveService
{
	public const int SaveVersion = 21;
	private readonly GameState _state;
	private readonly EconomyService _economy;

	public GameSaveService(GameState state, EconomyService economy)
	{
		_state = state;
		_economy = economy;
	}

	public SaveGameData CreateSave(long nowUnix)
	{
		return new SaveGameData
			{
				SaveVersion = SaveVersion,
				Tokens = _state.Tokens,
				RunEarnedTokens = _state.RunEarnedTokens,
				LifetimeLevelBase = _state.LifetimeLevelBase,
				LastSaveUnix = nowUnix,
				IncomePerSecond =
					_economy
						.GetTotalIncomeWithoutTemporaryShopBoost(),
				CurrentRoomIndex = _state.CurrentRoomIndex,
				AiCores = _state.Prestige.AiCores,
				PrestigeCount = _state.Prestige.PrestigeCount,
				LabUnlocked = _state.Lab.Unlocked,
				ResearchPoints = _state.Lab.ResearchPoints,
				CompletedResearch =
					_state.Lab.CompletedResearch
						.ToList(),
				ActiveResearchId = _state.Lab.ActiveResearchId,
				ActiveResearchEndUnix = _state.Lab.ActiveResearchEndUnix,
				DataShards = _state.Shop.DataShards,
				HighestClaimedLevelReward =
					_state.Shop.HighestClaimedLevelReward,
				ShopProductionBoostEndUnix = _state.Shop.ProductionBoostEndUnix,
				ShopBotLuckBoostEndUnix = _state.Shop.BotLuckBoostEndUnix,
				ShopProductionUpgradeLevel = _state.Shop.ProductionUpgradeLevel,
				ShopOfflineUpgradeLevel = _state.Shop.OfflineUpgradeLevel,
				Rooms =
					_state.RoomStates
						.Select(
							room =>
								new RoomSaveData
								{
									Unlocked = room.Unlocked,
									DataShardUnlockRewardClaimed =
										room.DataShardUnlockRewardClaimed,
									Pipeline = room.Pipeline.ToSaveData(),
									Infrastructure = room.Infrastructure.ToSaveData(),
									Quantum = room.Quantum.ToSaveData(),
									Slots =
										room.Slots
											.Select(
												slot =>
													slot.ToSaveData()
											)
											.ToList()
								}
						)
						.ToList(),
				Stats = _state.Stats.ToSaveData()
			};

	}

	// Apply only after the existing room/slot defaults have been initialized.
	// Balances are hydrated directly: loading is not a gameplay transaction.
	public GameLoadStateResult ApplySave(SaveGameData save)
	{
		_state.Tokens = save.Tokens;
		_state.RunEarnedTokens = save.RunEarnedTokens;
		_state.LifetimeLevelBase =
			save.SaveVersion >= 21
				? Math.Max(
					0,
					save.LifetimeLevelBase
				)
				: 0;
		_state.Prestige.AiCores = save.AiCores;
		_state.Prestige.PrestigeCount = save.PrestigeCount;
		_state.Lab.Unlocked = save.LabUnlocked;
		_state.Lab.ResearchPoints = save.ResearchPoints;

		_state.Lab.LoadCompletedResearch(
			save.CompletedResearch
		);

		_state.Lab.ActiveResearchId =
			save.ActiveResearchId;

		_state.Lab.ActiveResearchEndUnix =
			save.ActiveResearchEndUnix;

		_state.Shop.DataShards =
			save.SaveVersion < 13
				? GameConfig.InitialDataShards
				: save.DataShards;

		_state.Shop.HighestClaimedLevelReward =
			save.SaveVersion < 20
				? 0
				: Math.Max(
					0,
					save.HighestClaimedLevelReward
				);

		int restoredLifetimeLevelBase = _state.LifetimeLevelBase;

		_state.Shop.ProductionBoostEndUnix =
			save.ShopProductionBoostEndUnix;

		_state.Shop.BotLuckBoostEndUnix =
			save.ShopBotLuckBoostEndUnix;

		_state.Shop.ProductionUpgradeLevel =
			save.ShopProductionUpgradeLevel;

		_state.Shop.OfflineUpgradeLevel =
			save.ShopOfflineUpgradeLevel;

		for (
			int roomIndex = 0;
			roomIndex < Math.Min(
				save.Rooms.Count,
				_state.RoomStates.Count
			);
			roomIndex++
		)
		{
			RoomSaveData savedRoom =
				save.Rooms[
					roomIndex
				];

			RoomState room =
				_state.RoomStates[
					roomIndex
				];

			room.Unlocked =
				savedRoom.Unlocked;

			if (save.SaveVersion < 14)
			{
				room.DataShardUnlockRewardClaimed =
					roomIndex == 0
					|| savedRoom.Unlocked;
			}
			else
			{
				room.DataShardUnlockRewardClaimed =
					savedRoom
						.DataShardUnlockRewardClaimed;
			}

			if (
				save.SaveVersion >= 16
				&& savedRoom.Pipeline != null
			)
			{
				room.Pipeline.LoadFromSaveData(
					savedRoom.Pipeline
				);
			}
			else
			{
				room.Pipeline.Reset();
			}

			if (
				save.SaveVersion >= 18
				&& savedRoom.Infrastructure != null
			)
			{
				room.Infrastructure.LoadFromSaveData(
					savedRoom.Infrastructure
				);
			}
			else
			{
				room.Infrastructure.Reset();
			}

			if (
				save.SaveVersion >= 19
				&& savedRoom.Quantum != null
			)
			{
				room.Quantum.LoadFromSaveData(
					savedRoom.Quantum
				);
			}
			else
			{
				room.Quantum.Reset();
			}

			for (
				int slotIndex = 0;
				slotIndex < Math.Min(
					savedRoom.Slots.Count,
					room.Slots.Count
				);
				slotIndex++
			)
			{
				room.Slots[
					slotIndex
				].LoadFromSaveData(
					savedRoom.Slots[
						slotIndex
					]
				);
			}
		}

		_state.CurrentRoomIndex =
			Math.Clamp(
				save.CurrentRoomIndex,
				0,
				_state.Rooms.Count - 1
			);

		if (
			!_state.RoomStates[
				_state.CurrentRoomIndex
			].Unlocked
		)
		{
			_state.CurrentRoomIndex =
				0;
		}

		if (save.Stats != null)
		{
			_state.Stats.LoadFromSaveData(
				save.Stats
			);
		}

		bool lifetimeLevelMigrated = false;

		/*
		 * Migration for pre-v21 saves:
		 *
		 * Older saves did not remember the exact amount lost at previous
		 * prestiges. We cannot reconstruct that exact historical value, but
		 * the Level Reward Road remembers the highest claimed 150-level
		 * milestone. Use it as a safe lower bound so an old player is never
		 * migrated below a reward level they had already reached.
		 */
		if (
			save.SaveVersion < 21
			&& _state.Prestige.PrestigeCount > 0
			&& _state.Shop.HighestClaimedLevelReward > 0
		)
		{
			lifetimeLevelMigrated = true;
			int currentRunLevel =
				ProgressionService
					.CalculateCurrentRunLevel(
						_state
					);

			_state.LifetimeLevelBase =
				Math.Max(
					0,
					_state.Shop
						.HighestClaimedLevelReward
						- currentRunLevel
				);

		}

		return new GameLoadStateResult(restoredLifetimeLevelBase, lifetimeLevelMigrated);
	}
}
