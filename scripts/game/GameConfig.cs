namespace IdleAi;

public static class GameConfig
{
	public static readonly double[] GarageSlotUnlockCosts =
	[
		0.0,
		50.0,
		500.0,
		5_000.0,
		50_000.0,
		500_000.0,
		5_000_000.0,
		50_000_000.0
	];


	public static readonly double[] ServerRoomSlotUnlockCosts =
	[
		0.0,
		25_000_000.0,
		100_000_000.0,
		500_000_000.0,
		2_500_000_000.0,
		10_000_000_000.0,
		50_000_000_000.0,
		250_000_000_000.0
	];


	public static readonly double[] DataCenterSlotUnlockCosts =
	[
		0.0,
		500_000_000_000.0,
		2_000_000_000_000.0,
		10_000_000_000_000.0,
		50_000_000_000_000.0,
		250_000_000_000_000.0,
		1_000_000_000_000_000.0,
		5_000_000_000_000_000.0
	];


	public static readonly double[] QuantumLabSlotUnlockCosts =
	[
		0.0,
		25_000_000_000_000_000.0,
		100_000_000_000_000_000.0,
		500_000_000_000_000_000.0,
		2_500_000_000_000_000_000.0,
		10_000_000_000_000_000_000.0,
		50_000_000_000_000_000_000.0,
		250_000_000_000_000_000_000.0
	];


	public static readonly double[] SlotUpgradeMultipliers =
	[
		1.0,
		4.0,
		10.0,
		25.0,
		60.0,
		150.0,
		400.0,
		1_000.0
	];


	// ==================================================
	// LAB
	// ==================================================

	public const double LabUnlockCost =
		100_000_000.0;


	public const double TokensPerResearchPoint =
		1_000_000.0;


	public const int ResearchPointPurchaseAmount =
		10;


	public const double ResearchPointDropChance =
		0.001;


	public const int ResearchPointDropAmount =
		1;


	// ==================================================
	// SHOP
	// ==================================================

	public const double InitialDataShards =
		0.0;


	public const double ShopProductionBoostCost =
		25.0;


	public const int ShopProductionBoostMinutes =
		15;


	public const double ShopTemporaryProductionMultiplier =
		2.0;


	public const double ShopBotLuckCost =
		20.0;


	public const int ShopBotLuckMinutes =
		10;


	public const double ShopBotLuckMultiplier =
		1.35;


	public const double ShopInstantProductionCost =
		10.0;


	public const double ShopProductionUpgradeBaseCost =
		40.0;


	public const double ShopProductionUpgradeCostGrowth =
		1.55;


	public const double ShopProductionUpgradeBonus =
		0.02;


	public const int ShopProductionUpgradeMaxLevel =
		20;


	public const double ShopOfflineUpgradeBaseCost =
		35.0;


	public const double ShopOfflineUpgradeCostGrowth =
		1.60;


	public const double ShopOfflineUpgradeBonus =
		0.05;


	public const int ShopOfflineUpgradeMaxLevel =
		15;


	// ==================================================
	// DATA SHARD REWARDS
	// ==================================================

	public const double RoomUnlockDataShardReward =
		15.0;


	public const double PrestigeDataShardReward =
		25.0;


	public static int GetMilestoneDataShards(
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
				10,

			_ =>
				0
		};
	}


	// ==================================================
	// MACHINE ECONOMY
	// ==================================================

	public const double LevelCostGrowth =
		1.22;


	public const double IncomePerLevel =
		0.10;


	// ==================================================
	// SERVER ROOM PIPELINE
	// ==================================================

	public const int PipelineRoomIndex =
		1;


	public const int PipelineMaxLevel =
		25;


	public const double PipelineTokensPerOutputUnit =
		10_000.0;


	/*
	 * Capacity is expressed as material units per second.
	 *
	 * In the actual simulation it is converted into a
	 * batch:
	 *
	 * batch = capacity * cycle duration
	 */
	public const double PipelineBaseCapacity =
		100.0;


	public const double PipelineCapacityGrowth =
		1.65;


	public const double PipelineBaseUpgradeCost =
		20_000_000.0;


	public const double PipelineUpgradeCostGrowth =
		1.65;


	public const double PipelineComputeCycleSeconds =
		3.0;


	public const double PipelineDataCycleSeconds =
		3.5;


	public const double PipelineModelCycleSeconds =
		4.0;


	public const double PipelineOutputCycleSeconds =
		4.5;


	public static double GetPipelineCycleSeconds(
		PipelineStage stage)
	{
		return stage switch
		{
			PipelineStage.Compute =>
				PipelineComputeCycleSeconds,

			PipelineStage.Data =>
				PipelineDataCycleSeconds,

			PipelineStage.Model =>
				PipelineModelCycleSeconds,

			PipelineStage.Output =>
				PipelineOutputCycleSeconds,

			_ =>
				PipelineComputeCycleSeconds
		};
	}


	public static double GetPipelineStageCostMultiplier(
		PipelineStage stage)
	{
		return stage switch
		{
			PipelineStage.Compute =>
				1.00,

			PipelineStage.Data =>
				1.15,

			PipelineStage.Model =>
				1.35,

			PipelineStage.Output =>
				1.60,

			_ =>
				1.0
		};
	}


	// ==================================================
	// OFFLINE
	// ==================================================

	public const double BaseOfflineIncomeFactor =
		0.25;


	// ==================================================
	// BOTS
	// ==================================================

	public const double BotBaseCostMultiplier =
		20.0;


	public const double BotSellRefundFactor =
		1.0 / 3.0;


	public const double CommonBotChance =
		0.60;


	public const double RareBotChance =
		0.25;


	public const double EpicBotChance =
		0.10;


	// ==================================================
	// PRESTIGE
	// ==================================================

	public const double PrestigeTokensPerCore =
		1_000_000_000_000_000_000.0;


	public const double ProductionBoostPerAiCore =
		0.02;


	// ==================================================
	// MACHINE CYCLE
	// ==================================================

	public static double GetProductionCycleSeconds(
		int machineTier)
	{
		return machineTier switch
		{
			0 =>
				4.0,

			1 =>
				4.5,

			2 =>
				5.0,

			3 =>
				5.5,

			_ =>
				5.5
		};
	}


	// ==================================================
	// RESEARCH POINT MILESTONES
	// ==================================================

	public static int GetMilestoneResearchPoints(
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
				10,

			_ =>
				0
		};
	}


	// ==================================================
	// SLOT COSTS
	// ==================================================

	public static double[] GetSlotUnlockCosts(
		int roomIndex)
	{
		return roomIndex switch
		{
			0 =>
				GarageSlotUnlockCosts,

			1 =>
				ServerRoomSlotUnlockCosts,

			2 =>
				DataCenterSlotUnlockCosts,

			3 =>
				QuantumLabSlotUnlockCosts,

			_ =>
				GarageSlotUnlockCosts
		};
	}
}
