using System;

namespace IdleAi;

public readonly record struct PrestigeResult(
	bool Success,
	long CoresGained,
	string Message
);

public sealed class PrestigeService
{
	private readonly GameState _state;

	public PrestigeService(GameState state)
	{
		_state = state;
	}

	public long GetAvailableAiCores()
	{
		if (
			_state.RunEarnedTokens
			< GameConfig.PrestigeTokensPerCore
		)
		{
			return 0;
		}

		double result =
			Math.Floor(
				_state.RunEarnedTokens
				/ GameConfig.PrestigeTokensPerCore
			);

		if (result >= long.MaxValue)
			return long.MaxValue;

		return (long)result;
	}

	public bool CanPrestige()
	{
		return GetAvailableAiCores() > 0;
	}

	public double GetProductionMultiplier()
	{
		return _state.Prestige.GetProductionMultiplier();
	}

	public double GetProductionBonusPercent()
	{
		return _state.Prestige.GetProductionBonusPercent();
	}

	public PrestigeResult Prestige()
	{
		long reward = GetAvailableAiCores();

		if (reward <= 0)
		{
			return new PrestigeResult(
				false,
				0,
				$"Earn at least "
				+ $"{NumberFormatter.Format(GameConfig.PrestigeTokensPerCore)} "
				+ "Tokens this run."
			);
		}

		_state.Prestige.AiCores += reward;
		_state.Prestige.PrestigeCount++;

		_state.Shop.DataShards +=
			GameConfig.PrestigeDataShardReward;

		ResetNormalProgress();

		return new PrestigeResult(
			true,
			reward,
			$"Prestige complete! +{reward} AI Cores"
			+ " +"
			+ NumberFormatter.Format(
				GameConfig.PrestigeDataShardReward
			)
			+ " Data Shards!"
		);
	}

	private void ResetNormalProgress()
	{
		_state.Tokens = 0.0;
		_state.RunEarnedTokens = 0.0;
		_state.CurrentRoomIndex = 0;

		for (
			int roomIndex = 0;
			roomIndex < _state.RoomStates.Count;
			roomIndex++
		)
		{
			RoomState room =
				_state.RoomStates[roomIndex];

			room.Unlocked =
				roomIndex == 0;

			room.Pipeline.Reset();
			room.Infrastructure.Reset();
			room.Quantum.Reset();

			for (
				int slotIndex = 0;
				slotIndex < room.Slots.Count;
				slotIndex++
			)
			{
				SlotData slot =
					room.Slots[slotIndex];

				slot.Unlocked =
					roomIndex == 0
					&& slotIndex == 0;

				slot.MachineTier = 0;
				slot.MachineLevel = 1;

				slot.ResetDataShardMilestones();

				slot.BotRarity = null;
				slot.BotPurchasePrice = 0.0;
				slot.IsRunning = false;
				slot.CycleRemaining = 0.0;
				slot.RuntimeCycleDuration = 0.0;
			}
		}
	}
}
