using System.Linq;

namespace IdleAi;


public readonly record struct ProgressionResult(
	bool Changed,
	string Message
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


	// ==================================================
	// SLOT ACTION
	// ==================================================

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


	// ==================================================
	// SLOT UNLOCK COST
	// ==================================================

	public double GetSlotUnlockCost(
		int roomIndex,
		int slotIndex)
	{
		double[] costs =
			GameConfig
				.GetSlotUnlockCosts(
					roomIndex
				);


		if (
			slotIndex < 0
			|| slotIndex >= costs.Length
		)
		{
			return 0.0;
		}


		double baseCost =
			costs[
				slotIndex
			];


		return baseCost
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


		return new ProgressionResult(
			true,
			$"Slot {slotIndex + 1} unlocked!"
		);
	}


	// ==================================================
	// MACHINE UPGRADE
	// ==================================================

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
			return UpgradeMachineLevel(
				roomIndex,
				slot,
				machine,
				slotIndex
			);
		}


		return UpgradeMachineTier(
			roomIndex,
			slot,
			machine,
			slotIndex
		);
	}


	// ==================================================
	// LEVEL UPGRADE
	// ==================================================

	private ProgressionResult UpgradeMachineLevel(
		int roomIndex,
		SlotData slot,
		MachineData machine,
		int slotIndex)
	{
		double cost =
			_economy.GetLevelUpgradeCost(
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


		slot.MachineLevel++;


		// ==================================================
		// MILESTONE RESEARCH POINTS
		// ==================================================

		int researchReward =
			GetResearchPointMilestoneReward(
				slot.MachineLevel
			);


		if (
			researchReward > 0
			&& _state.Lab.Unlocked
		)
		{
			_state.Lab.ResearchPoints +=
				researchReward;
		}


		string message =
			GetUpgradeMessage(
				machine,
				slot.MachineLevel
			);


		if (
			researchReward > 0
			&& _state.Lab.Unlocked
		)
		{
			message +=
				$" +{researchReward} RP!";
		}


		return new ProgressionResult(
			true,
			message
		);
	}


	// ==================================================
	// TIER UPGRADE
	// ==================================================

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


	// ==================================================
	// RESEARCH POINT MILESTONES
	// ==================================================

	private static int GetResearchPointMilestoneReward(
		int level)
	{
		return level switch
		{
			5 =>
				1,

			10 =>
				2,

			15 =>
				3,

			20 =>
				5,

			25 =>
				8,

			_ =>
				0
		};
	}


	// ==================================================
	// ROOM UNLOCK COST
	// ==================================================

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


	// ==================================================
	// ROOM UNLOCK
	// ==================================================

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


		return new ProgressionResult(
			true,
			$"{_state.Rooms[roomIndex].Name} unlocked!"
		);
	}


	// ==================================================
	// TOTAL LEVEL
	// ==================================================

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


	// ==================================================
	// MESSAGE
	// ==================================================

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
