namespace IdleAi;

public readonly record struct PrestigeResult(
	bool Success,
	string Message
);


public sealed class PrestigeService
{
	private const double RequiredRunTokens =
		1_000_000_000_000_000_000.0;

	private readonly GameState _state;

	public PrestigeService(
		GameState state)
	{
		_state =
			state;
	}

	public double GetRequiredRunTokens()
	{
		return RequiredRunTokens;
	}

	public bool CanPrestige()
	{
		return _state.RunEarnedTokens
			>= RequiredRunTokens;
	}

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

	public double GetProductionMultiplierAfterNextPrestige()
	{
		int nextCount =
			_state.Prestige.PrestigeCount
				== int.MaxValue
					? int.MaxValue
					: _state.Prestige.PrestigeCount
						+ 1;

		return PrestigeData
			.GetProductionMultiplierForPrestigeCount(
				nextCount
			);
	}

	public PrestigeResult Prestige()
	{
		if (!CanPrestige())
		{
			double remaining =
				System.Math.Max(
					0.0,
					RequiredRunTokens
						- _state.RunEarnedTokens
				);

			return new PrestigeResult(
				false,
				"Earn "
					+ NumberFormatter.Format(
						remaining
					)
					+ " more Tokens this run to prestige."
			);
		}

		int totalLevelBeforePrestige =
			ProgressionService
				.CalculateTotalLevel(
					_state
				);

		if (
			_state.Prestige.PrestigeCount
				< int.MaxValue
		)
		{
			_state.Prestige.PrestigeCount++;
		}

		ResetNormalProgress();

		int resetRunLevel =
			ProgressionService
				.CalculateCurrentRunLevel(
					_state
				);

		_state.LifetimeLevelBase =
			System.Math.Max(
				0,
				totalLevelBeforePrestige
					- resetRunLevel
			);

		return new PrestigeResult(
			true,
			"Prestige complete! Total Level remains "
				+ ProgressionService
					.CalculateTotalLevel(
						_state
					)
				+ ". Permanent production x"
				+ GetProductionMultiplier()
					.ToString(
						"F2"
					)
		);
	}

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
					room.Slots[
						slotIndex
					];

				slot.Unlocked =
					roomIndex == 0
					&& slotIndex == 0;

				slot.MachineTier =
					0;

				slot.MachineLevel =
					1;

				slot.ResetDataShardMilestones();

				/*
				 * ClearBot also resets durability state so a future Bot cannot
				 * inherit durability from the previous prestige run.
				 */
				slot.ClearBot();

				slot.IsRunning =
					false;

				slot.CycleRemaining =
					0.0;

				slot.RuntimeCycleDuration =
					0.0;
			}
		}
	}
}
