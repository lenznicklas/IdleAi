using Godot;
using System;

namespace IdleAi;

public sealed class ShopData
{
	public double DataShards { get; set; } =
		GameConfig.InitialDataShards;


	public long ProductionBoostEndUnix { get; set; }

	public long BotLuckBoostEndUnix { get; set; }


	public int ProductionUpgradeLevel { get; set; }

	public int OfflineUpgradeLevel { get; set; }


	// ==================================================
	// TEMPORARY BOOSTS
	// ==================================================

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


	// ==================================================
	// PRODUCTION
	// ==================================================

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


	// ==================================================
	// OFFLINE
	// ==================================================

	public double GetOfflineIncomeBonus()
	{
		return OfflineUpgradeLevel
			* GameConfig.ShopOfflineUpgradeBonus;
	}


	// ==================================================
	// BOT LUCK
	// ==================================================

	public double GetBotLuckMultiplier()
	{
		return BotLuckBoostActive
			? GameConfig.ShopBotLuckMultiplier
			: 1.0;
	}


	// ==================================================
	// TIME
	// ==================================================

	private static long GetCurrentUnixTime()
	{
		return (long)
			Time.GetUnixTimeFromSystem();
	}
}
