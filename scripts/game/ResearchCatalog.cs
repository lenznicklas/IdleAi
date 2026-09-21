using System;
using System.Collections.Generic;

namespace IdleAi;

public static class ResearchCatalog
{
	// ==================================================
	// HARDWARE
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
	// ROBOTICS
	// ==================================================

	public const string BotEngineering1Id =
		"bot_engineering_1";

	public const string BotEngineering2Id =
		"bot_engineering_2";

	public const string RareComponentsId =
		"rare_components";

	public const string AdvancedRoboticsId =
		"advanced_robotics";

	public const string QuantumRoboticsId =
		"quantum_robotics";


	// ==================================================
	// INFRASTRUCTURE
	// ==================================================

	public const string EfficientPurchasing1Id =
		"efficient_purchasing_1";

	public const string EfficientPurchasing2Id =
		"efficient_purchasing_2";

	public const string ModularExpansionId =
		"modular_expansion";

	public const string OfflineServersId =
		"offline_servers";

	public const string AutonomousInfrastructureId =
		"autonomous_infrastructure";


	// ==================================================
	// CATALOG
	// ==================================================

	private static readonly List<ResearchDefinition> Research =
	[
		// ==================================================
		// HARDWARE
		// ==================================================

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
		),


		// ==================================================
		// ROBOTICS
		// ==================================================

		new ResearchDefinition(
			BotEngineering1Id,
			"Bot Engineering I",
			"+5% power for all bots.",
			ResearchBranch.Robotics,
			25.0,
			BotPowerBonus: 0.05
		),

		new ResearchDefinition(
			BotEngineering2Id,
			"Bot Engineering II",
			"+10% additional power for all bots.",
			ResearchBranch.Robotics,
			60.0,
			BotPowerBonus: 0.10,
			PrerequisiteId: BotEngineering1Id
		),

		new ResearchDefinition(
			RareComponentsId,
			"Rare Components",
			"+5 percentage points Rare Bot chance.",
			ResearchBranch.Robotics,
			120.0,
			RareBotChanceBonus: 0.05,
			PrerequisiteId: BotEngineering2Id
		),

		new ResearchDefinition(
			AdvancedRoboticsId,
			"Advanced Robotics",
			"+5 percentage points Epic Bot chance.",
			ResearchBranch.Robotics,
			250.0,
			EpicBotChanceBonus: 0.05,
			PrerequisiteId: RareComponentsId
		),

		new ResearchDefinition(
			QuantumRoboticsId,
			"Quantum Robotics",
			"+3 percentage points Legendary Bot chance.",
			ResearchBranch.Robotics,
			500.0,
			LegendaryBotChanceBonus: 0.03,
			PrerequisiteId: AdvancedRoboticsId
		),


		// ==================================================
		// INFRASTRUCTURE
		// ==================================================

		new ResearchDefinition(
			EfficientPurchasing1Id,
			"Efficient Purchasing I",
			"-5% machine upgrade costs.",
			ResearchBranch.Infrastructure,
			25.0,
			MachineUpgradeCostReduction: 0.05
		),

		new ResearchDefinition(
			EfficientPurchasing2Id,
			"Efficient Purchasing II",
			"-10% additional machine upgrade costs.",
			ResearchBranch.Infrastructure,
			60.0,
			MachineUpgradeCostReduction: 0.10,
			PrerequisiteId: EfficientPurchasing1Id
		),

		new ResearchDefinition(
			ModularExpansionId,
			"Modular Expansion",
			"-10% slot and room unlock costs.",
			ResearchBranch.Infrastructure,
			120.0,
			UnlockCostReduction: 0.10,
			PrerequisiteId: EfficientPurchasing2Id
		),

		new ResearchDefinition(
			OfflineServersId,
			"Offline Servers",
			"+10 percentage points offline income.",
			ResearchBranch.Infrastructure,
			250.0,
			OfflineIncomeBonus: 0.10,
			PrerequisiteId: ModularExpansionId
		),

		new ResearchDefinition(
			AutonomousInfrastructureId,
			"Autonomous Infrastructure",
			"+15 percentage points additional offline income.",
			ResearchBranch.Infrastructure,
			500.0,
			OfflineIncomeBonus: 0.15,
			PrerequisiteId: OfflineServersId
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
		double bonus = 0.0;


		foreach (
			ResearchDefinition research
			in Research
		)
		{
			if (
				lab.IsResearchCompleted(
					research.Id
				)
			)
			{
				bonus +=
					research.ProductionBonus;
			}
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
	// CYCLE
	// ==================================================

	public static double GetCycleTimeReduction(
		LabData lab)
	{
		double reduction = 0.0;


		foreach (
			ResearchDefinition research
			in Research
		)
		{
			if (
				lab.IsResearchCompleted(
					research.Id
				)
			)
			{
				reduction +=
					research.CycleTimeReduction;
			}
		}


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
		double bonus = 0.0;


		foreach (
			ResearchDefinition research
			in Research
		)
		{
			if (
				lab.IsResearchCompleted(
					research.Id
				)
			)
			{
				bonus +=
					research.BotPowerBonus;
			}
		}


		return bonus;
	}


	public static double GetBotPowerMultiplier(
		LabData lab)
	{
		return 1.0
			+ GetBotPowerBonus(
				lab
			);
	}


	// ==================================================
	// BOT CHANCES
	// ==================================================

	public static double GetRareBotChanceBonus(
		LabData lab)
	{
		double bonus = 0.0;


		foreach (
			ResearchDefinition research
			in Research
		)
		{
			if (
				lab.IsResearchCompleted(
					research.Id
				)
			)
			{
				bonus +=
					research.RareBotChanceBonus;
			}
		}


		return bonus;
	}


	public static double GetEpicBotChanceBonus(
		LabData lab)
	{
		double bonus = 0.0;


		foreach (
			ResearchDefinition research
			in Research
		)
		{
			if (
				lab.IsResearchCompleted(
					research.Id
				)
			)
			{
				bonus +=
					research.EpicBotChanceBonus;
			}
		}


		return bonus;
	}


	public static double GetLegendaryBotChanceBonus(
		LabData lab)
	{
		double bonus = 0.0;


		foreach (
			ResearchDefinition research
			in Research
		)
		{
			if (
				lab.IsResearchCompleted(
					research.Id
				)
			)
			{
				bonus +=
					research.LegendaryBotChanceBonus;
			}
		}


		return bonus;
	}


	// ==================================================
	// MACHINE COST
	// ==================================================

	public static double GetMachineUpgradeCostReduction(
		LabData lab)
	{
		double reduction = 0.0;


		foreach (
			ResearchDefinition research
			in Research
		)
		{
			if (
				lab.IsResearchCompleted(
					research.Id
				)
			)
			{
				reduction +=
					research.MachineUpgradeCostReduction;
			}
		}


		return Math.Clamp(
			reduction,
			0.0,
			0.75
		);
	}


	public static double GetMachineUpgradeCostMultiplier(
		LabData lab)
	{
		return 1.0
			- GetMachineUpgradeCostReduction(
				lab
			);
	}


	// ==================================================
	// UNLOCK COST
	// ==================================================

	public static double GetUnlockCostReduction(
		LabData lab)
	{
		double reduction = 0.0;


		foreach (
			ResearchDefinition research
			in Research
		)
		{
			if (
				lab.IsResearchCompleted(
					research.Id
				)
			)
			{
				reduction +=
					research.UnlockCostReduction;
			}
		}


		return Math.Clamp(
			reduction,
			0.0,
			0.75
		);
	}


	public static double GetUnlockCostMultiplier(
		LabData lab)
	{
		return 1.0
			- GetUnlockCostReduction(
				lab
			);
	}


	// ==================================================
	// OFFLINE
	// ==================================================

	public static double GetOfflineIncomeBonus(
		LabData lab)
	{
		double bonus = 0.0;


		foreach (
			ResearchDefinition research
			in Research
		)
		{
			if (
				lab.IsResearchCompleted(
					research.Id
				)
			)
			{
				bonus +=
					research.OfflineIncomeBonus;
			}
		}


		return Math.Clamp(
			bonus,
			0.0,
			0.75
		);
	}
	
	// ==================================================
// RESEARCH DURATION
// ==================================================

public static double GetDurationSeconds(
	ResearchDefinition research)
{
	if (research.Cost <= 25.0)
		return 30.0;

	if (research.Cost <= 60.0)
		return 60.0;

	if (research.Cost <= 120.0)
		return 120.0;

	if (research.Cost <= 250.0)
		return 240.0;


	return 480.0;
}
}
