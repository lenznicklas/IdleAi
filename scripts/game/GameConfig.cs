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


	public const double LevelCostGrowth =
		1.22;


	public const double IncomePerLevel =
		0.10;


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


	public const double PrestigeTokensPerCore =
		1_000_000_000_000_000_000.0;


	public const double ProductionBoostPerAiCore =
		0.02;


	// ==================================================
	// CYCLE DURATION
	// ==================================================

	public static double GetProductionCycleSeconds(
		int machineTier)
	{
		return machineTier switch
		{
			0 => 4.0,
			1 => 4.5,
			2 => 5.0,
			3 => 5.5,
			_ => 5.5
		};
	}


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
