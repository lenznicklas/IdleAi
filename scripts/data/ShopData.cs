using Godot;
using System;

namespace IdleAi;

public sealed class ShopData
{
	public double DataShards { get; set; } =
		GameConfig.InitialDataShards;

	/*
	 * Permanent reward-road progress.
	 *
	 * Example:
	 * 0   -> nothing claimed yet
	 * 150 -> Level 150 reward claimed
	 * 300 -> Level 150 + 300 claimed
	 *
	 * This survives Prestige so the same free Shards cannot be farmed again.
	 */
	public int HighestClaimedLevelReward { get; set; }

	public long ProductionBoostEndUnix { get; set; }

	public long BotLuckBoostEndUnix { get; set; }

	public int ProductionUpgradeLevel { get; set; }

	public int OfflineUpgradeLevel { get; set; }

	public bool ProductionBoostActive =>
		ProductionBoostEndUnix
		> GetCurrentUnixTime();

	public bool BotLuckBoostActive =>
		BotLuckBoostEndUnix
		> GetCurrentUnixTime();

	public double GetProductionBoostRemainingSeconds()
	{
		return Math.Max(
			0,
			ProductionBoostEndUnix
				- GetCurrentUnixTime()
		);
	}

	public double GetBotLuckRemainingSeconds()
	{
		return Math.Max(
			0,
			BotLuckBoostEndUnix
				- GetCurrentUnixTime()
		);
	}

	public double GetProductionMultiplier()
	{
		double permanent =
			1.0
				+ ProductionUpgradeLevel
					* GameConfig.ShopProductionUpgradeBonus;

		double temporary =
			ProductionBoostActive
				? GameConfig.ShopTemporaryProductionMultiplier
				: 1.0;

		return permanent
			* temporary;
	}

	public double GetOfflineIncomeBonus()
	{
		return OfflineUpgradeLevel
			* GameConfig.ShopOfflineUpgradeBonus;
	}

	public double GetBotLuckMultiplier()
	{
		return BotLuckBoostActive
			? GameConfig.ShopBotLuckMultiplier
			: 1.0;
	}

	private static long GetCurrentUnixTime()
	{
		return (long)
			Time.GetUnixTimeFromSystem();
	}
}
