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
	// ENDLESS RESEARCH
	// ==================================================

	private const string EndlessHardwarePrefix =
		"endless_hardware_";

	private const string EndlessRoboticsPrefix =
		"endless_robotics_";

	private const string EndlessInfrastructurePrefix =
		"endless_infrastructure_";


	private const double EndlessBaseCost =
		750.0;


	private const double EndlessCostGrowth =
		1.35;


	private const double EndlessBaseDurationSeconds =
		172_800.0;


	private const double EndlessDurationStepSeconds =
		21_600.0;


	private const double EndlessMaximumDurationSeconds =
		604_800.0;


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


		if (
			TryParseEndlessResearchId(
				id,
				out ResearchBranch branch,
				out int level
			)
		)
		{
			return CreateEndlessResearch(
				branch,
				level
			);
		}


		return null;
	}


	public static bool IsEndlessResearch(
		string id)
	{
		return TryParseEndlessResearchId(
			id,
			out _,
			out _
		);
	}


	public static ResearchDefinition GetNextEndlessResearch(
		ResearchBranch branch,
		LabData lab)
	{
		int completed =
			GetCompletedEndlessCount(
				lab,
				branch
			);


		return CreateEndlessResearch(
			branch,
			completed + 1
		);
	}


	private static ResearchDefinition CreateEndlessResearch(
		ResearchBranch branch,
		int level)
	{
		level =
			Math.Max(
				1,
				level
			);


		string id =
			GetEndlessPrefix(
				branch
			)
			+ level;


		string prerequisiteId =
			level == 1
				? GetFinalBaseResearchId(
					branch
				)
				: GetEndlessPrefix(
					branch
				)
				+ (
					level - 1
				);


		double cost =
			EndlessBaseCost
			* Math.Pow(
				EndlessCostGrowth,
				level - 1
			);


		return branch switch
		{
			ResearchBranch.Hardware =>
				new ResearchDefinition(
					id,
					"Hardware Optimization "
						+ ToRomanOrNumber(
							level
						),
					"+1.5% permanent production.",
					branch,
					cost,
					ProductionBonus: 0.015,
					PrerequisiteId: prerequisiteId
				),

			ResearchBranch.Robotics =>
				new ResearchDefinition(
					id,
					"Bot Intelligence "
						+ ToRomanOrNumber(
							level
						),
					"+1% permanent Bot Power.",
					branch,
					cost,
					BotPowerBonus: 0.01,
					PrerequisiteId: prerequisiteId
				),

			ResearchBranch.Infrastructure =>
				new ResearchDefinition(
					id,
					"Autonomous Systems "
						+ ToRomanOrNumber(
							level
						),
					"+0.5% permanent production and +0.5 percentage points offline income.",
					branch,
					cost,
					ProductionBonus: 0.005,
					OfflineIncomeBonus: 0.005,
					PrerequisiteId: prerequisiteId
				),

			_ =>
				new ResearchDefinition(
					id,
					"Endless Research "
						+ level,
					"+1% permanent production.",
					branch,
					cost,
					ProductionBonus: 0.01,
					PrerequisiteId: prerequisiteId
				)
		};
	}


	private static string GetEndlessPrefix(
		ResearchBranch branch)
	{
		return branch switch
		{
			ResearchBranch.Hardware =>
				EndlessHardwarePrefix,

			ResearchBranch.Robotics =>
				EndlessRoboticsPrefix,

			ResearchBranch.Infrastructure =>
				EndlessInfrastructurePrefix,

			_ =>
				EndlessHardwarePrefix
		};
	}


	private static string GetFinalBaseResearchId(
		ResearchBranch branch)
	{
		return branch switch
		{
			ResearchBranch.Hardware =>
				QuantumComponentsId,

			ResearchBranch.Robotics =>
				QuantumRoboticsId,

			ResearchBranch.Infrastructure =>
				AutonomousInfrastructureId,

			_ =>
				QuantumComponentsId
		};
	}


	private static bool TryParseEndlessResearchId(
		string id,
		out ResearchBranch branch,
		out int level)
	{
		foreach (
			ResearchBranch candidate
			in Enum.GetValues<ResearchBranch>()
		)
		{
			string prefix =
				GetEndlessPrefix(
					candidate
				);


			if (
				!id.StartsWith(
					prefix,
					StringComparison.Ordinal
				)
			)
			{
				continue;
			}


			string number =
				id[
					prefix.Length..
				];


			if (
				int.TryParse(
					number,
					out int parsed
				)
				&& parsed > 0
			)
			{
				branch =
					candidate;


				level =
					parsed;


				return true;
			}
		}


		branch =
			ResearchBranch.Hardware;


		level =
			0;


		return false;
	}


	private static int GetCompletedEndlessCount(
		LabData lab,
		ResearchBranch branch)
	{
		int count =
			0;


		string prefix =
			GetEndlessPrefix(
				branch
			);


		foreach (
			string id
			in lab.CompletedResearch
		)
		{
			if (
				id.StartsWith(
					prefix,
					StringComparison.Ordinal
				)
			)
			{
				count++;
			}
		}


		return count;
	}


	private static string ToRomanOrNumber(
		int level)
	{
		return level switch
		{
			1 => "I",
			2 => "II",
			3 => "III",
			4 => "IV",
			5 => "V",
			6 => "VI",
			7 => "VII",
			8 => "VIII",
			9 => "IX",
			10 => "X",
			_ => level.ToString()
		};
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
				lab.IsResearchCompleted(
					research.Id
				)
			)
			{
				bonus +=
					research.ProductionBonus;
			}
		}


		bonus +=
			GetCompletedEndlessCount(
				lab,
				ResearchBranch.Hardware
			)
			* 0.015;


		bonus +=
			GetCompletedEndlessCount(
				lab,
				ResearchBranch.Infrastructure
			)
			* 0.005;


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
		double reduction =
			0.0;


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
	// BOT
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
				lab.IsResearchCompleted(
					research.Id
				)
			)
			{
				bonus +=
					research.BotPowerBonus;
			}
		}


		bonus +=
			GetCompletedEndlessCount(
				lab,
				ResearchBranch.Robotics
			)
			* 0.01;


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
		double bonus =
			0.0;


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
		double bonus =
			0.0;


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
		double bonus =
			0.0;


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
	// UPGRADE COST
	// ==================================================

	public static double GetMachineUpgradeCostReduction(
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
		double reduction =
			0.0;


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
		double bonus =
			0.0;


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


		bonus +=
			GetCompletedEndlessCount(
				lab,
				ResearchBranch.Infrastructure
			)
			* 0.005;


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
		if (
			TryParseEndlessResearchId(
				research.Id,
				out _,
				out int level
			)
		)
		{
			return Math.Min(
				EndlessMaximumDurationSeconds,
				EndlessBaseDurationSeconds
				+ (
					level - 1
				)
				* EndlessDurationStepSeconds
			);
		}


		if (research.Cost <= 25.0)
			return 3_600.0;


		if (research.Cost <= 60.0)
			return 10_800.0;


		if (research.Cost <= 120.0)
			return 28_800.0;


		if (research.Cost <= 250.0)
			return 64_800.0;


		return 129_600.0;
	}
}
