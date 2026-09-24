namespace IdleAi;

public readonly record struct PrestigeResult(
	bool Success,
	int DataShardsGained,
	string Message
);


public sealed class PrestigeService
{
	private const double RequiredRunTokens =
		1_000_000_000_000_000_000.0;


	private const int DataShardReward =
		10;


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


	public int GetDataShardReward()
	{
		return DataShardReward;
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
				0,
				"Earn "
					+ NumberFormatter.Format(
						remaining
					)
					+ " more Tokens this run to prestige."
			);
		}


		if (
			_state.Prestige.PrestigeCount
				< int.MaxValue
		)
		{
			_state.Prestige.PrestigeCount++;
		}


		_state.Shop.DataShards +=
			DataShardReward;


		ResetNormalProgress();


		return new PrestigeResult(
			true,
			DataShardReward,
			"Prestige complete! +"
				+ DataShardReward
				+ " Data Shards"
				+ "  •  Permanent production x"
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


				slot.BotRarity =
					null;


				slot.BotPurchasePrice =
					0.0;


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
