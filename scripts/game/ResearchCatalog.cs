using System;
using System.Collections.Generic;

namespace IdleAi;

public static class ResearchCatalog
{
	// ==================================================
	// HARDWARE IDS
	// ==================================================

	public const string EfficientHardware1Id =
		"efficient_hardware_1";


	public const string EfficientHardware2Id =
		"efficient_hardware_2";


	public const string Overclocking1Id =
		"overclocking_1";


	public const string AdvancedCoolingId =
		"advanced_cooling";


	public const string QuantumComponentsId =
		"quantum_components";


	// ==================================================
	// CATALOG
	// ==================================================

	private static readonly List<ResearchDefinition> Research =
	[
		new ResearchDefinition(
			EfficientHardware1Id,
			"Efficient Hardware I",
			"+5% production for all machines.",
			ResearchBranch.Hardware,
			25.0,
			ProductionBonus: 0.05
		),

		new ResearchDefinition(
			EfficientHardware2Id,
			"Efficient Hardware II",
			"+10% production for all machines.",
			ResearchBranch.Hardware,
			60.0,
			ProductionBonus: 0.10,
			PrerequisiteId: EfficientHardware1Id
		),

		new ResearchDefinition(
			Overclocking1Id,
			"Overclocking I",
			"-5% production cycle time.",
			ResearchBranch.Hardware,
			120.0,
			CycleTimeReduction: 0.05,
			PrerequisiteId: EfficientHardware2Id
		),

		new ResearchDefinition(
			AdvancedCoolingId,
			"Advanced Cooling",
			"-5% additional production cycle time.",
			ResearchBranch.Hardware,
			250.0,
			CycleTimeReduction: 0.05,
			PrerequisiteId: Overclocking1Id
		),

		new ResearchDefinition(
			QuantumComponentsId,
			"Quantum Components",
			"+25% production for all machines.",
			ResearchBranch.Hardware,
			500.0,
			ProductionBonus: 0.25,
			PrerequisiteId: AdvancedCoolingId
		)
	];


	public static IReadOnlyList<ResearchDefinition> All =>
		Research;


	// ==================================================
	// GET
	// ==================================================

	public static ResearchDefinition? Get(
		string id)
	{
		foreach (
			ResearchDefinition research
			in Research
		)
		{
			if (research.Id == id)
				return research;
		}


		return null;
	}


	// ==================================================
	// PRODUCTION
	// ==================================================

	public static double GetProductionBonus(
		LabData lab)
	{
		double bonus =
			0.0;


		foreach (
			ResearchDefinition research
			in Research
		)
		{
			if (
				!lab.IsResearchCompleted(
					research.Id
				)
			)
			{
				continue;
			}


			bonus +=
				research.ProductionBonus;
		}


		return bonus;
	}


	public static double GetProductionMultiplier(
		LabData lab)
	{
		return 1.0
			+ GetProductionBonus(
				lab
			);
	}


	// ==================================================
	// CYCLE TIME
	// ==================================================

	public static double GetCycleTimeReduction(
		LabData lab)
	{
		double reduction =
			0.0;


		foreach (
			ResearchDefinition research
			in Research
		)
		{
			if (
				!lab.IsResearchCompleted(
					research.Id
				)
			)
			{
				continue;
			}


			reduction +=
				research.CycleTimeReduction;
		}


		/*
		 * Safety cap:
		 *
		 * Research should never reduce cycle time
		 * to zero or a negative value.
		 */

		return Math.Clamp(
			reduction,
			0.0,
			0.75
		);
	}


	public static double GetCycleTimeMultiplier(
		LabData lab)
	{
		return 1.0
			- GetCycleTimeReduction(
				lab
			);
	}


	// ==================================================
	// BOT POWER
	// ==================================================

	public static double GetBotPowerBonus(
		LabData lab)
	{
		double bonus =
			0.0;


		foreach (
			ResearchDefinition research
			in Research
		)
		{
			if (
				!lab.IsResearchCompleted(
					research.Id
				)
			)
			{
				continue;
			}


			bonus +=
				research.BotPowerBonus;
		}


		return bonus;
	}


	// ==================================================
	// OFFLINE INCOME
	// ==================================================

	public static double GetOfflineIncomeBonus(
		LabData lab)
	{
		double bonus =
			0.0;


		foreach (
			ResearchDefinition research
			in Research
		)
		{
			if (
				!lab.IsResearchCompleted(
					research.Id
				)
			)
			{
				continue;
			}


			bonus +=
				research.OfflineIncomeBonus;
		}


		return bonus;
	}
}
