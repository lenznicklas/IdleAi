namespace IdleAi;

public static class GameConfig
{
	public static readonly double[] SlotUnlockCosts =
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
}
