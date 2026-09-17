namespace IdleAi;

public static class GameConfig
{
	// ==================================================
	// ROOM 1
	// GARAGE
	// ==================================================

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


	// ==================================================
	// ROOM 2
	// SERVER ROOM
	// ==================================================

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


	// ==================================================
	// ROOM 3
	// DATA CENTER
	// ==================================================

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


	// ==================================================
	// ROOM 4
	// QUANTUM LAB
	// ==================================================

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


	// ==================================================
	// LATER SLOTS ARE MORE EXPENSIVE TO UPGRADE
	// ==================================================

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
	// LEVEL SCALING
	// ==================================================

	public const double LevelCostGrowth =
		1.22;


	public const double IncomePerLevel =
		0.10;


	// ==================================================
	// ROOM SLOT COST LOOKUP
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
	
	public const double PrestigeTokensPerCore =
		1_000_000_000_000_000_000.0; // 1 Qi

	public const double ProductionBoostPerAiCore =
		0.10; // +10 %
}
