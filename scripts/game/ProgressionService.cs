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


public readonly record struct RoomUnlockQuote(
	bool ValidTarget,
	bool AlreadyUnlocked,
	bool EligibleByProgression,
	double Cost,
	bool CanAfford,
	string Message)
{
	public bool CanUnlock => ValidTarget && !AlreadyUnlocked
		&& EligibleByProgression && CanAfford;
}


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

		return UpgradeMachine(roomIndex, slotIndex, 1);
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

		if (!_economy.TrySpendTokens(cost))
		{
			return new ProgressionResult(
				false,
				"Not enough Tokens."
			);
		}


		_economy.RecordSlotSpending(
			cost
		);

		slot.Unlocked =
			true;

		slot.MachineTier =
			0;

		slot.MachineLevel =
			1;

		slot.ResetDataShardMilestones();

		return new ProgressionResult(
			true,
			$"Slot {slotIndex + 1} unlocked!"
		);
	}


	/// <summary>Quantity applies to levels only; a capped machine advances one tier per action.</summary>
	public ProgressionResult UpgradeMachine(int roomIndex, int slotIndex, int requestedLevels)
	{
		if (roomIndex < 0 || roomIndex >= _state.RoomStates.Count
			|| roomIndex >= _state.Rooms.Count || slotIndex < 0
			|| slotIndex >= _state.RoomStates[roomIndex].Slots.Count)
			return new ProgressionResult(false, "Invalid machine.");

		SlotData slot = _state.RoomStates[roomIndex].Slots[slotIndex];
		if (!slot.Unlocked)
			return new ProgressionResult(false, "Machine is locked.");

		RoomData room =
			_state.Rooms[
				roomIndex
			];

		MachineData machine =
			room.Machines[
				slot.MachineTier
			];

		if (
			machine.IsInfiniteLevel
			|| slot.MachineLevel < machine.MaxLevel
		)
		{
			return UpgradeMachineLevels(
				roomIndex,
				slotIndex,
				requestedLevels
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

			if (!_economy.TrySpendTokens(cost))
				break;


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
		}

		if (upgradedLevels <= 0)
		{
			return new ProgressionResult(
				false,
				"Not enough Tokens."
			);
		}

		_economy.RecordMachineSpending(
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

		if (!_economy.TrySpendTokens(cost))
		{
			return new ProgressionResult(
				false,
				"Not enough Tokens."
			);
		}


		_economy.RecordMachineSpending(
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


	public RoomUnlockQuote GetRoomUnlockQuote(int roomIndex)
	{
		if (roomIndex < 0 || roomIndex >= _state.Rooms.Count
			|| roomIndex >= _state.RoomStates.Count)
			return new(false, false, false, 0, false, "Invalid room.");

		double cost = GetRoomUnlockCost(roomIndex);
		bool affordable = _economy.CanAffordTokens(cost);
		if (_state.RoomStates[roomIndex].Unlocked)
			return new(true, true, true, cost, affordable, "Room already unlocked.");

		bool eligible = RoomUnlockRules.CanUnlockRoom(_state, roomIndex, out string reason);
		return new(true, false, eligible, cost, affordable,
			!eligible ? reason : !affordable
				? $"You need {NumberFormatter.Format(cost)} Tokens."
				: "");
	}


	public bool CanUnlockRoom(int roomIndex) =>
		GetRoomUnlockQuote(roomIndex).CanUnlock;


	public ProgressionResult UnlockRoom(
		int roomIndex)
	{
		// Re-evaluate at the mutation boundary; previews never authorize a spend.
		RoomUnlockQuote quote = GetRoomUnlockQuote(roomIndex);
		if (!quote.CanUnlock)
			return new ProgressionResult(false, quote.Message);

		double cost = quote.Cost;
		if (!_economy.TrySpendTokens(cost))
			return new ProgressionResult(false,
				$"You need {NumberFormatter.Format(cost)} Tokens.");

		RoomState roomState = _state.RoomStates[roomIndex];

		_economy.RecordSlotSpending(
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

		roomState.DataShardUnlockRewardClaimed =
			true;

		return new ProgressionResult(
			true,
			$"{_state.Rooms[roomIndex].Name} unlocked!"
		);
	}


	/// <summary>
	/// Visible Total Level. It is the permanent level base from previous
	/// prestiges plus the actual levels in the current run.
	/// </summary>
	public int GetTotalLevel()
	{
		return CalculateTotalLevel(
			_state
		);
	}


	public static int CalculateTotalLevel(
		GameState state)
	{
		long total =
			(long)Math.Max(
				0,
				state.LifetimeLevelBase
			)
			+ CalculateCurrentRunLevel(
				state
			);

		return total >= int.MaxValue
			? int.MaxValue
			: (int)total;
	}


	/// <summary>
	/// Calculates only the currently installed/unlocked machine levels. This is
	/// the old GetTotalLevel calculation and is intentionally public so
	/// PrestigeService can preserve the exact visible Total Level while
	/// resetting the run.
	/// </summary>
	public static int CalculateCurrentRunLevel(
		GameState state)
	{
		long total =
			0;

		for (
			int roomIndex = 0;
			roomIndex < state.Rooms.Count
				&& roomIndex
					< state.RoomStates.Count;
			roomIndex++
		)
		{
			if (
				!state.RoomStates[
					roomIndex
				].Unlocked
			)
			{
				continue;
			}

			RoomData room =
				state.Rooms[
					roomIndex
				];

			foreach (
				SlotData slot
					in state.RoomStates[
						roomIndex
					].Slots
			)
			{
				if (!slot.Unlocked)
					continue;

				for (
					int tier = 0;
					tier < slot.MachineTier
						&& tier
							< room.Machines.Count;
					tier++
				)
				{
					total +=
						room.Machines[
							tier
						].MaxLevel;

					if (
						total >= int.MaxValue
					)
					{
						return int.MaxValue;
					}
				}

				total +=
					Math.Max(
						0,
						slot.MachineLevel
					);

				if (
					total >= int.MaxValue
				)
				{
					return int.MaxValue;
				}
			}
		}

		return (int)total;
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
