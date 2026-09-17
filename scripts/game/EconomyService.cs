using System;

namespace IdleAi;

public sealed class EconomyService
{
	private readonly GameState _state;


	public EconomyService(
		GameState state)
	{
		_state =
			state;
	}


	// ==================================================
	// LEVEL UPGRADE
	// ==================================================

	public double GetLevelUpgradeCost(
		int roomIndex,
		SlotData slot,
		int slotIndex)
	{
		MachineData machine =
			GetMachine(
				roomIndex,
				slot
			);


		double slotMultiplier =
			GameConfig.SlotUpgradeMultipliers[
				slotIndex
			];


		double levelMultiplier =
			Math.Pow(
				GameConfig.LevelCostGrowth,
				slot.MachineLevel - 1
			);


		return machine.BaseUpgradeCost
			   * slotMultiplier
			   * levelMultiplier;
	}


	// ==================================================
	// MACHINE TIER
	// ==================================================

	public double GetTierUpgradeCost(
		int roomIndex,
		SlotData slot,
		int slotIndex)
	{
		MachineData machine =
			GetMachine(
				roomIndex,
				slot
			);


		return machine.TierUpgradeCost
			   * GameConfig.SlotUpgradeMultipliers[
				   slotIndex
			   ];
	}


	// ==================================================
	// BOT COST
	// ==================================================

	public double GetBotCost(
		int roomIndex,
		SlotData slot,
		int slotIndex)
	{
		MachineData machine =
			GetMachine(
				roomIndex,
				slot
			);


		return machine.BaseUpgradeCost
			   * GameConfig.BotBaseCostMultiplier
			   * GameConfig.SlotUpgradeMultipliers[
				   slotIndex
			   ];
	}


	// ==================================================
	// PRODUCTION
	// ==================================================

	public double GetCycleReward(
		int roomIndex,
		SlotData slot)
	{
		if (!slot.Unlocked)
			return 0.0;


		MachineData machine =
			GetMachine(
				roomIndex,
				slot
			);


		double levelMultiplier =
			1.0
			+ (
				slot.MachineLevel - 1
			)
			* GameConfig.IncomePerLevel;


		double milestoneMultiplier =
			GetMilestoneMultiplier(
				slot.MachineLevel
			);


		double botMultiplier =
			BotCatalog.GetMultiplier(
				slot.BotRarity
			);


		double prestigeMultiplier =
			_state.Prestige
				.GetProductionMultiplier();


		// BaseIncome used to mean Tokens / second.
		//
		// We keep the old balancing intact by converting
		// it to one four-second production cycle.
		return machine.BaseIncome
			   * GameConfig.ProductionCycleSeconds
			   * levelMultiplier
			   * milestoneMultiplier
			   * botMultiplier
			   * prestigeMultiplier;
	}


	public double GetSlotIncome(
		int roomIndex,
		SlotData slot)
	{
		return GetCycleReward(
				   roomIndex,
				   slot
			   )
			   / GameConfig.ProductionCycleSeconds;
	}


	// ==================================================
	// AUTOMATED INCOME
	// ==================================================

	public double GetRoomIncome(
		int roomIndex)
	{
		if (
			!_state.RoomStates[
				roomIndex
			].Unlocked
		)
		{
			return 0.0;
		}


		double total =
			0.0;


		foreach (
			SlotData slot
			in _state.RoomStates[
				roomIndex
			].Slots
		)
		{
			if (
				!slot.Unlocked
				|| !slot.HasBot
			)
			{
				continue;
			}


			total +=
				GetSlotIncome(
					roomIndex,
					slot
				);
		}


		return total;
	}


	public double GetTotalIncome()
	{
		double total =
			0.0;


		for (
			int roomIndex = 0;
			roomIndex < _state.Rooms.Count;
			roomIndex++
		)
		{
			total +=
				GetRoomIncome(
					roomIndex
				);
		}


		return total;
	}


	// ==================================================
	// MILESTONE
	// ==================================================

	public static double GetMilestoneMultiplier(
		int level)
	{
		if (level >= 25)
			return 8.0;

		if (level >= 20)
			return 5.0;

		if (level >= 15)
			return 3.0;

		if (level >= 10)
			return 2.0;

		if (level >= 5)
			return 1.5;

		return 1.0;
	}


	public static string GetMilestoneText(
		int level)
	{
		if (level >= 25)
			return "x8 production";

		if (level >= 20)
			return "Level 25: x8";

		if (level >= 15)
			return "Level 20: x5";

		if (level >= 10)
			return "Level 15: x3";

		if (level >= 5)
			return "Level 10: x2";

		return "Level 5: x1.5";
	}


	private MachineData GetMachine(
		int roomIndex,
		SlotData slot)
	{
		return _state.Rooms[
			roomIndex
		].Machines[
			slot.MachineTier
		];
	}
}
