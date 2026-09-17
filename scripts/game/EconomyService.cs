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
	// UPGRADE COST
	// ==================================================

	public double GetLevelUpgradeCost(
		int roomIndex,
		SlotData slot,
		int slotIndex)
	{
		MachineData machine =
			_state.Rooms[
				roomIndex
			].Machines[
				slot.MachineTier
			];


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
	// TIER COST
	// ==================================================

	public double GetTierUpgradeCost(
		int roomIndex,
		SlotData slot,
		int slotIndex)
	{
		MachineData machine =
			_state.Rooms[
				roomIndex
			].Machines[
				slot.MachineTier
			];


		return machine.TierUpgradeCost
			   * GameConfig.SlotUpgradeMultipliers[
				   slotIndex
			   ];
	}


	// ==================================================
	// SLOT INCOME
	// ==================================================

	public double GetSlotIncome(
		int roomIndex,
		SlotData slot)
	{
		if (!slot.Unlocked)
			return 0.0;


		MachineData machine =
			_state.Rooms[
				roomIndex
			].Machines[
				slot.MachineTier
			];


		double levelMultiplier =
			1.0
			+ (
				slot.MachineLevel
				- 1
			)
			* GameConfig.IncomePerLevel;


		double milestoneMultiplier =
			GetMilestoneMultiplier(
				slot.MachineLevel
			);


		double prestigeMultiplier =
			_state.Prestige
				.GetProductionMultiplier();


		return machine.BaseIncome
			   * levelMultiplier
			   * milestoneMultiplier
			   * prestigeMultiplier;
	}


	// ==================================================
	// ROOM INCOME
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
			total +=
				GetSlotIncome(
					roomIndex,
					slot
				);
		}


		return total;
	}


	// ==================================================
	// TOTAL INCOME
	// ==================================================

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
	// PRESTIGE
	// ==================================================

	public double GetPrestigeMultiplier()
	{
		return _state.Prestige
			.GetProductionMultiplier();
	}


	// ==================================================
	// MILESTONES
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
}
