using System;
using System.Collections.Generic;

namespace IdleAi;

public sealed class EconomyService
{
	private readonly IReadOnlyList<MachineData>
		_machines;


	private readonly double[]
		_slotUpgradeMultipliers;


	public EconomyService(
		IReadOnlyList<MachineData> machines,
		double[] slotUpgradeMultipliers)
	{
		_machines =
			machines;


		_slotUpgradeMultipliers =
			slotUpgradeMultipliers;
	}


	// ==================================================
	// UPGRADE COST
	// ==================================================

	public double GetLevelUpgradeCost(
		SlotData slot,
		int slotIndex)
	{
		MachineData machine =
			GetMachine(
				slot
			);


		double slotMultiplier =
			_slotUpgradeMultipliers[
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


	public double GetTierUpgradeCost(
		SlotData slot,
		int slotIndex)
	{
		MachineData machine =
			GetMachine(
				slot
			);


		return machine.TierUpgradeCost
			   * _slotUpgradeMultipliers[
				   slotIndex
			   ];
	}


	// ==================================================
	// INCOME
	// ==================================================

	public double GetSlotIncome(
		SlotData slot)
	{
		if (!slot.Unlocked)
			return 0.0;


		MachineData machine =
			GetMachine(
				slot
			);


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


		return machine.BaseIncome
			   * levelMultiplier
			   * milestoneMultiplier;
	}


	public double GetTotalIncome(
		IEnumerable<SlotData> slots)
	{
		double total =
			0.0;


		foreach (
			SlotData slot
			in slots
		)
		{
			total +=
				GetSlotIncome(
					slot
				);
		}


		return total;
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


	private MachineData GetMachine(
		SlotData slot)
	{
		return _machines[
			slot.MachineTier
		];
	}
}
