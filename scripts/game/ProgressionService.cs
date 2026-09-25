using System;
using System.Linq;

namespace IdleAi;


public readonly record struct ProgressionResult(
	bool Changed,
	string Message
);


public readonly record struct MachineUpgradeQuote(
	int Levels,
	int TargetLevel,
	double Cost,
	bool CanAfford
);


public sealed class ProgressionService
{
	private readonly GameState _state;

	private readonly EconomyService _economy;


	public ProgressionService(
		GameState state,
		EconomyService economy)
	{
		_state =
			state;

		_economy =
			economy;
	}


	public ProgressionResult HandleSlotAction(
		int slotIndex)
	{
		int roomIndex =
			_state.CurrentRoomIndex;

		RoomState roomState =
			_state.CurrentRoomState;

		if (
			slotIndex < 0
			|| slotIndex >= roomState.Slots.Count
		)
		{
			return new ProgressionResult(
				false,
				"Invalid slot."
			);
		}

		SlotData slot =
			roomState.Slots[
				slotIndex
			];

		if (!slot.Unlocked)
		{
			return UnlockSlot(
				roomIndex,
				slotIndex
			);
		}

		return UpgradeSlot(
			roomIndex,
			slot,
			slotIndex
		);
	}


	public double GetSlotUnlockCost(
		int roomIndex,
		int slotIndex)
	{
		double[] costs =
			GameConfig.GetSlotUnlockCosts(
				roomIndex
			);

		if (
			slotIndex < 0
			|| slotIndex >= costs.Length
		)
		{
			return 0.0;
		}

		return costs[
			slotIndex
		]
		* _state.Lab
			.GetUnlockCostMultiplier();
	}


	private ProgressionResult UnlockSlot(
		int roomIndex,
		int slotIndex)
	{
		SlotData slot =
			_state.RoomStates[
				roomIndex
			].Slots[
				slotIndex
			];

		double cost =
			GetSlotUnlockCost(
				roomIndex,
				slotIndex
			);

		if (_state.Tokens < cost)
		{
			return new ProgressionResult(
				false,
				"Not enough Tokens."
			);
		}

		_state.Tokens -=
			cost;

		_state.Stats.AddSlotSpending(
			cost
		);

		slot.Unlocked =
			true;

		slot.MachineTier =
			0;

		slot.MachineLevel =
			1;

		/*
		 * Legacy machine Shard milestone data is intentionally no longer
		 * rewarded. Free Data Shards now only come from the Level Reward Road.
		 */
		slot.ResetDataShardMilestones();

		return new ProgressionResult(
			true,
			$"Slot {slotIndex + 1} unlocked!"
		);
	}


	private ProgressionResult UpgradeSlot(
		int roomIndex,
		SlotData slot,
		int slotIndex)
	{
		RoomData room =
			_state.Rooms[
				roomIndex
			];

		MachineData machine =
			room.Machines[
				slot.MachineTier
			];

		if (
			slot.MachineLevel
			< machine.MaxLevel
		)
		{
			return UpgradeMachineLevels(
				roomIndex,
				slotIndex,
				1
			);
		}

		return UpgradeMachineTier(
			roomIndex,
			slot,
			machine,
			slotIndex
		);
	}


	public MachineUpgradeQuote GetMachineUpgradeQuote(
		int roomIndex,
		int slotIndex,
		int requestedLevels)
	{
		if (
			roomIndex < 0
			|| roomIndex >= _state.RoomStates.Count
		)
		{
			return new MachineUpgradeQuote(
				0,
				0,
				0,
				false
			);
		}

		RoomState roomState =
			_state.RoomStates[
				roomIndex
			];

		if (
			slotIndex < 0
			|| slotIndex >= roomState.Slots.Count
		)
		{
			return new MachineUpgradeQuote(
				0,
				0,
				0,
				false
			);
		}

		SlotData slot =
			roomState.Slots[
				slotIndex
			];

		if (!slot.Unlocked)
		{
			return new MachineUpgradeQuote(
				0,
				slot.MachineLevel,
				0,
				false
			);
		}

		MachineData machine =
			_state.Rooms[
				roomIndex
			].Machines[
				slot.MachineTier
			];

		int remainingLevels =
			Math.Max(
				0,
				machine.MaxLevel
					- slot.MachineLevel
			);

		if (remainingLevels <= 0)
		{
			return new MachineUpgradeQuote(
				0,
				slot.MachineLevel,
				0,
				false
			);
		}

		if (requestedLevels <= 0)
		{
			int affordableLevels =
				_economy
					.GetMaximumAffordableLevels(
						roomIndex,
						slot,
						slotIndex,
						_state.Tokens
					);

			double cost =
				_economy
					.GetLevelUpgradeCostForLevels(
						roomIndex,
						slot,
						slotIndex,
						affordableLevels
					);

			return new MachineUpgradeQuote(
				affordableLevels,
				slot.MachineLevel
					+ affordableLevels,
				cost,
				affordableLevels > 0
			);
		}

		int levels =
			Math.Min(
				requestedLevels,
				remainingLevels
			);

		double fixedCost =
			_economy
				.GetLevelUpgradeCostForLevels(
					roomIndex,
					slot,
					slotIndex,
					levels
				);

		return new MachineUpgradeQuote(
			levels,
			slot.MachineLevel
				+ levels,
			fixedCost,
			levels > 0
				&& _state.Tokens
					>= fixedCost
		);
	}


	public ProgressionResult UpgradeMachineLevels(
		int roomIndex,
		int slotIndex,
		int requestedLevels)
	{
		MachineUpgradeQuote quote =
			GetMachineUpgradeQuote(
				roomIndex,
				slotIndex,
				requestedLevels
			);

		if (quote.Levels <= 0)
		{
			return new ProgressionResult(
				false,
				requestedLevels <= 0
					? "Not enough Tokens for another level."
					: "No levels available."
			);
		}

		if (!quote.CanAfford)
		{
			return new ProgressionResult(
				false,
				"Not enough Tokens."
			);
		}

		SlotData slot =
			_state.RoomStates[
				roomIndex
			].Slots[
				slotIndex
			];

		MachineData machine =
			_state.Rooms[
				roomIndex
			].Machines[
				slot.MachineTier
			];

		int upgradedLevels =
			0;

		int totalResearchPoints =
			0;

		double totalSpent =
			0.0;

		for (
			int i = 0;
			i < quote.Levels;
			i++
		)
		{
			double cost =
				_economy
					.GetLevelUpgradeCost(
						roomIndex,
						slot,
						slotIndex
					);

			if (_state.Tokens < cost)
				break;

			_state.Tokens -=
				cost;

			totalSpent +=
				cost;

			slot.MachineLevel++;

			upgradedLevels++;

			int researchReward =
				GameConfig
					.GetMilestoneResearchPoints(
						slot.MachineLevel
					);

			if (
				researchReward > 0
				&& _state.Lab.Unlocked
			)
			{
				_state.Lab.ResearchPoints +=
					researchReward;

				totalResearchPoints +=
					researchReward;
			}

			/*
			 * No Data Shards are granted here anymore.
			 * Reaching total levels unlocks claimable Level Reward Road cards.
			 */
		}

		if (upgradedLevels <= 0)
		{
			return new ProgressionResult(
				false,
				"Not enough Tokens."
			);
		}

		_state.Stats.AddMachineSpending(
			machine.MachineName,
			totalSpent
		);

		string message =
			upgradedLevels == 1
				? GetUpgradeMessage(
					machine,
					slot.MachineLevel
				)
				: machine.MachineName
					+ " +"
					+ upgradedLevels
					+ " levels → Level "
					+ slot.MachineLevel;

		if (totalResearchPoints > 0)
		{
			message +=
				$" +{totalResearchPoints} RP!";
		}

		return new ProgressionResult(
			true,
			message
		);
	}


	private ProgressionResult UpgradeMachineTier(
		int roomIndex,
		SlotData slot,
		MachineData machine,
		int slotIndex)
	{
		RoomData room =
			_state.Rooms[
				roomIndex
			];

		if (
			slot.MachineTier
				>= room.Machines.Count - 1
		)
		{
			return new ProgressionResult(
				false,
				"Maximum machine reached."
			);
		}

		double cost =
			_economy.GetTierUpgradeCost(
				roomIndex,
				slot,
				slotIndex
			);

		if (_state.Tokens < cost)
		{
			return new ProgressionResult(
				false,
				"Not enough Tokens."
			);
		}

		_state.Tokens -=
			cost;

		_state.Stats.AddMachineSpending(
			machine.MachineName,
			cost
		);

		slot.MachineTier++;

		slot.MachineLevel =
			1;

		MachineData newMachine =
			room.Machines[
				slot.MachineTier
			];

		return new ProgressionResult(
			true,
			$"Upgraded to {newMachine.MachineName}!"
		);
	}


	public double GetRoomUnlockCost(
		int roomIndex)
	{
		if (
			roomIndex < 0
			|| roomIndex >= _state.Rooms.Count
		)
		{
			return 0.0;
		}

		double baseCost =
			_state.Rooms[
				roomIndex
			].UnlockCost;

		return baseCost
			* _state.Lab
				.GetUnlockCostMultiplier();
	}


	public ProgressionResult UnlockRoom(
		int roomIndex)
	{
		if (
			roomIndex < 0
			|| roomIndex >= _state.Rooms.Count
		)
		{
			return new ProgressionResult(
				false,
				"Invalid room."
			);
		}

		RoomState roomState =
			_state.RoomStates[
				roomIndex
			];

		if (roomState.Unlocked)
		{
			return new ProgressionResult(
				false,
				"Room already unlocked."
			);
		}

		double cost =
			GetRoomUnlockCost(
				roomIndex
			);

		if (_state.Tokens < cost)
		{
			return new ProgressionResult(
				false,
				$"You need {NumberFormatter.Format(cost)} Tokens."
			);
		}

		_state.Tokens -=
			cost;

		_state.Stats.AddSlotSpending(
			cost
		);

		roomState.Unlocked =
			true;

		roomState.Slots[0].Unlocked =
			true;

		roomState.Slots[0].MachineTier =
			0;

		roomState.Slots[0].MachineLevel =
			1;

		roomState.Slots[0]
			.ResetDataShardMilestones();

		/*
		 * No room-unlock Data Shards anymore.
		 * Keep the legacy flag true so old reward logic can never fire again.
		 */
		roomState.DataShardUnlockRewardClaimed =
			true;

		return new ProgressionResult(
			true,
			$"{_state.Rooms[roomIndex].Name} unlocked!"
		);
	}


	public int GetTotalLevel()
	{
		int total =
			0;

		for (
			int roomIndex = 0;
			roomIndex < _state.Rooms.Count;
			roomIndex++
		)
		{
			if (
				!_state.RoomStates[
					roomIndex
				].Unlocked
			)
			{
				continue;
			}

			RoomData room =
				_state.Rooms[
					roomIndex
				];

			foreach (
				SlotData slot
					in _state.RoomStates[
						roomIndex
					].Slots
			)
			{
				if (!slot.Unlocked)
					continue;

				for (
					int tier = 0;
					tier < slot.MachineTier;
					tier++
				)
				{
					total +=
						room.Machines[
							tier
						].MaxLevel;
				}

				total +=
					slot.MachineLevel;
			}
		}

		return total;
	}


	public int GetUnlockedSlotCount(
		int roomIndex)
	{
		return _state.RoomStates[
				roomIndex
			]
			.Slots
			.Count(
				slot =>
					slot.Unlocked
			);
	}


	private static string GetUpgradeMessage(
		MachineData machine,
		int level)
	{
		return level switch
		{
			5 =>
				$"{machine.MachineName} Level 5! Production x1.5!",

			10 =>
				$"{machine.MachineName} Level 10! Production x2!",

			15 =>
				$"{machine.MachineName} Level 15! Production x3!",

			20 =>
				$"{machine.MachineName} Level 20! Production x5!",

			25 =>
				$"{machine.MachineName} Level 25! Production x8!",

			_ =>
				$"{machine.MachineName} upgraded to Level {level}"
		};
	}
}
