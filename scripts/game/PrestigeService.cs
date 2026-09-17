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


	public PrestigeService(
		GameState state)
	{
		_state =
			state;
	}


	// ==================================================
	// REWARD
	// ==================================================

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


	// ==================================================
	// BOOST
	// ==================================================

	public double GetProductionMultiplier()
	{
		return _state.Prestige
			.GetProductionMultiplier();
	}


	public double GetProductionBonusPercent()
	{
		return _state.Prestige
			.GetProductionBonusPercent();
	}


	// ==================================================
	// PRESTIGE
	// ==================================================

	public PrestigeResult Prestige()
	{
		long reward =
			GetAvailableAiCores();


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


		// Permanent prestige progress.
		_state.Prestige.AiCores +=
			reward;


		_state.Prestige.PrestigeCount++;


		ResetNormalProgress();


		return new PrestigeResult(
			true,
			reward,
			$"Prestige complete! +{reward} AI Cores"
		);
	}


	// ==================================================
	// RESET NORMAL GAME
	// ==================================================

	private void ResetNormalProgress()
	{
		_state.Tokens =
			0.0;


		_state.RunEarnedTokens =
			0.0;


		_state.CurrentRoomIndex =
			0;


		for (
			int roomIndex = 0;
			roomIndex < _state.RoomStates.Count;
			roomIndex++
		)
		{
			RoomState room =
				_state.RoomStates[
					roomIndex
				];


			// Only room 1 stays unlocked.
			room.Unlocked =
				roomIndex == 0;


			for (
				int slotIndex = 0;
				slotIndex < room.Slots.Count;
				slotIndex++
			)
			{
				SlotData slot =
					room.Slots[
						slotIndex
					];


				// Only the first slot of room 1
				// starts unlocked again.
				slot.Unlocked =
					roomIndex == 0
					&& slotIndex == 0;


				slot.MachineTier =
					0;


				slot.MachineLevel =
					1;
			}
		}
	}
}
