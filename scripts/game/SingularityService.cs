#nullable enable

using Godot;
using System;

namespace IdleAi;

public readonly record struct SingularityActionResult(
	bool Changed,
	string Message
);

public sealed class SingularityService
{
	public const double TestUnlockTokenCost =
		50_000_000_000_000_000.0; // 50aa

	public const int NodesPerSector =
		8;

	private readonly SingularitySaveData _data = new();
	private readonly SingularityEconomy _economy;
	private readonly SingularitySectorService _sectors;
	private readonly SingularityNodeService _nodes;
	private readonly SingularityBotService _bots;
	private readonly SingularityProductionService _production;
	private readonly SingularityPersistenceService _persistence;
	private readonly SingularityOfflineService _offline;

	public bool Unlocked =>
		_data.Unlocked;

	public double Matter =>
		_data.Matter;

	public int CoreLevel =>
		GetSectorCoreLevel(
			CurrentSectorIndex
		);

	public int CoreCount => _sectors.CoreCount;

	public int CurrentSectorIndex =>
		_data.CurrentSectorIndex;

	public int SectorNumber =>
		_data.CurrentSectorIndex + 1;

	public int SectorCount =>
		_data.Sectors.Count;

	public event Action? Changed;

	public SingularityService(GameState? state = null) : this(state, GetUnixNow, null)
	{
	}

	// Clock and roll seams are internal; production uses Godot time and randomized RNG.
	internal SingularityService(GameState? state, Func<long> now, Func<double>? roll)
	{
		_economy = new SingularityEconomy(_data);
		// The sector callback first runs during Load, after the bot RNG has been initialized.
		_sectors = new SingularitySectorService(_data, _economy, SaveAndNotify, Save, () => _bots!.RollContent());
		_bots = new SingularityBotService(_data, _sectors, _economy, SaveAndNotify, state?.Lab, roll);
		_nodes = new SingularityNodeService(_data, _sectors, _economy, SaveAndNotify, _bots.RollContent);
		_production = new SingularityProductionService(_data, _sectors, _bots);
		_persistence = new SingularityPersistenceService(_data, _sectors, _bots, now);
		_offline = new SingularityOfflineService(_data, _bots, _production, _persistence, _economy, () => Changed?.Invoke(), now);
		_persistence.Load();
		_sectors.EnsureSector(0);
		_sectors.RebuildGridIndex();
		_offline.PendingOfflineEarnings = _offline.ApplyOfflineIncomeFromTimestamp(_data.LastSaveUnix, now());
		if (_offline.PendingOfflineEarnings > 0.0)
			Save();
	}

	public void Update(
		double delta)
	{
		if (
			!Unlocked
			|| delta <= 0.0
		)
		{
			return;
		}

		double output =
			GetTotalOutputPerSecond();

		if (output > 0.0)
		{
			_economy.GrantProduction(output * delta);
		}

		/*
		 * Durability is consumed only while a Node exists, its Sector Core is
		 * online, and the Bot is still functional.
		 */
		_bots.ConsumeSingularityBotWork(
			delta
		);
	}

	public SingularityActionResult Unlock(GameState gameState) =>
		Unlock(gameState, new EconomyService(gameState));

	public SingularityActionResult Unlock(
		GameState gameState,
		EconomyService economy)
	{
		if (_persistence.LastLoadFailed)
			return new SingularityActionResult(false, "Singularity save could not be recovered. Existing data is preserved.");

		if (Unlocked)
		{
			return new SingularityActionResult(
				false,
				"The Singularity is already online."
			);
		}

		if (!RoomUnlockRules.CanUnlockSingularity(gameState, out string reason))
			return new SingularityActionResult(false, reason);

		if (
			!economy.TrySpendTokens(TestUnlockTokenCost)
		)
		{
			return new SingularityActionResult(
				false,
				"Need "
				+ NumberFormatter.Format(
					TestUnlockTokenCost
				)
				+ " Tokens to initialize The Singularity."
			);
		}

		economy.RecordSlotSpending(
			TestUnlockTokenCost
		);

		_data.Unlocked =
			true;

		_sectors.InstallFirstCore();

		SaveAndNotify();

		return new SingularityActionResult(
			true,
			"SINGULARITY ONLINE • SECTOR 1 CORE INSTALLED"
		);
	}

	public SingularitySectorData GetCurrentSector() =>
		_sectors.GetCurrentSector();

	public SingularitySectorData GetSector(
		int index) =>
		_sectors.GetSector(index);

	public SingularityNodeData GetNode(
		int sectorIndex,
		int nodeIndex) =>
		_sectors.GetNode(sectorIndex, nodeIndex);

	public bool IsSectorCoreUnlocked(
		int sectorIndex) =>
		_sectors.IsSectorCoreUnlocked(sectorIndex);

	public int GetSectorCoreLevel(
		int sectorIndex) =>
		_sectors.GetSectorCoreLevel(sectorIndex);

	public double GetSectorCoreUnlockCost(
		int sectorIndex) =>
		_sectors.GetSectorCoreUnlockCost(sectorIndex);

	public SingularityActionResult UnlockSectorCore(
		int sectorIndex) =>
		_sectors.UnlockSectorCore(sectorIndex);

	public double GetSectorCoreUpgradeCost(
		int sectorIndex) =>
		_sectors.GetSectorCoreUpgradeCost(sectorIndex);

	public SingularityActionResult UpgradeSectorCore(
		int sectorIndex) =>
		_sectors.UpgradeSectorCore(sectorIndex);

	public double GetSectorCoreOutputPerSecond(
		int sectorIndex) =>
		_production.GetSectorCoreOutputPerSecond(sectorIndex);

	public double GetSectorComputeCoreMultiplier(
		int sectorIndex) =>
		_production.GetSectorComputeCoreMultiplier(sectorIndex);

	public double GetNextCoreCost() =>
		_sectors.GetNextCoreCost();

	public double GetCoreUpgradeCost() =>
		_sectors.GetCoreUpgradeCost();

	public SingularityActionResult BuyNextCore() =>
		_sectors.BuyNextCore();

	public SingularityActionResult UpgradeCore() =>
		_sectors.UpgradeCore();

	public double GetCoreOutputPerSecond() =>
		_production.GetCoreOutputPerSecond();

	public static double GetCoreOutputPerSecond(
		int coreLevel) =>
		SingularityProductionService.GetCoreOutputPerSecond(coreLevel);

	public double GetComputeCoreMultiplier() =>
		_production.GetComputeCoreMultiplier();

	public bool IsNodeTypeUnlocked(
		SingularityNodeType type) =>
		_nodes.IsNodeTypeUnlocked(type);

	public string GetNodeUnlockText(
		SingularityNodeType type) =>
		_nodes.GetNodeUnlockText(type);

	public double GetBuildCost(
		int sectorIndex,
		SingularityNodeType type) =>
		_nodes.GetBuildCost(sectorIndex, type);

	public SingularityActionResult BuildNode(
		int sectorIndex,
		int nodeIndex,
		SingularityNodeType type) =>
		_nodes.BuildNode(sectorIndex, nodeIndex, type);

	public double GetNodeUpgradeCost(
		int sectorIndex,
		int nodeIndex) =>
		_nodes.GetNodeUpgradeCost(sectorIndex, nodeIndex);

	public SingularityActionResult UpgradeNode(
		int sectorIndex,
		int nodeIndex) =>
		_nodes.UpgradeNode(sectorIndex, nodeIndex);

	public double GetNodeBotPurchaseCost(
		int sectorIndex,
		int nodeIndex) =>
		_bots.GetNodeBotPurchaseCost(sectorIndex, nodeIndex);

	public SingularityActionResult BuyNodeBot(
		int sectorIndex,
		int nodeIndex) =>
		_bots.BuyNodeBot(sectorIndex, nodeIndex);

	public double GetNodeBotSellRefund(
		int sectorIndex,
		int nodeIndex) =>
		_bots.GetNodeBotSellRefund(sectorIndex, nodeIndex);

	public SingularityActionResult SellNodeBot(
		int sectorIndex,
		int nodeIndex) =>
		_bots.SellNodeBot(sectorIndex, nodeIndex);

	public double GetNodeBotRepairCost(
		int sectorIndex,
		int nodeIndex) =>
		_bots.GetNodeBotRepairCost(sectorIndex, nodeIndex);

	public SingularityActionResult RepairNodeBot(
		int sectorIndex,
		int nodeIndex) =>
		_bots.RepairNodeBot(sectorIndex, nodeIndex);

	public double GetNodeBotDurabilityRatio(
		int sectorIndex,
		int nodeIndex) =>
		_bots.GetNodeBotDurabilityRatio(sectorIndex, nodeIndex);

	public double GetNodeBotEffectiveMultiplier(
		int sectorIndex,
		int nodeIndex) =>
		_bots.GetNodeBotEffectiveMultiplier(sectorIndex, nodeIndex);

	public (
		double Common,
		double Rare,
		double Epic,
		double Legendary
	) GetNodeBotRarityChances() =>
		_bots.GetNodeBotRarityChances();

	public double GetNodeSellRefund(
		int sectorIndex,
		int nodeIndex) =>
		_nodes.GetNodeSellRefund(sectorIndex, nodeIndex);

	public SingularityActionResult SellNode(
		int sectorIndex,
		int nodeIndex) =>
		_nodes.SellNode(sectorIndex, nodeIndex);

	public double GetNextSectorUnlockCost() =>
		_sectors.GetNextSectorUnlockCost();

	public bool CanUnlockNextSector() =>
		_sectors.CanUnlockNextSector();

	public int GetFrontierSectorIndex() =>
		_sectors.GetFrontierSectorIndex();

	public void SelectSector(
		int sectorIndex) =>
		_sectors.SelectSector(sectorIndex);

	public SingularityActionResult UnlockNextSector() =>
		_sectors.UnlockNextSector();

	public bool GoToPreviousSector() =>
		_sectors.GoToPreviousSector();

	public bool GoToNextExistingSector() =>
		_sectors.GoToNextExistingSector();

	public double GetTotalOutputPerSecond() =>
		_production.GetTotalOutputPerSecond();

	public double GetSectorOutputPerSecond(
		int sectorIndex) =>
		_production.GetSectorOutputPerSecond(sectorIndex);

	public SingularityNodeBonuses GetNodeBonuses(int sectorIndex, int nodeIndex) =>
		_production.GetNodeBonuses(sectorIndex, nodeIndex);

	public System.Collections.Generic.IReadOnlyList<SingularityFormation> GetSectorFormations(int sectorIndex) =>
		SingularityFormationService.Detect(GetSector(sectorIndex));

	public string GetNodeEffectText(int sectorIndex, int nodeIndex)
	{
		double strength = GetNodeEffectStrength(sectorIndex, nodeIndex);
		return GetNode(sectorIndex, nodeIndex).Type switch
		{
			SingularityNodeType.Amplifier => $"Adjacent boost: Compute +{.15 * strength:P0}; Quantum +{.10 * strength:P0}",
			SingularityNodeType.Cooling => $"Adjacent speed: Compute +{.10 * strength:P0}; Quantum +{.20 * strength:P0}",
			SingularityNodeType.Quantum => $"Sector Compute boost: +{.12 * strength:P0}",
			_ => ""
		};
	}

	public double GetNodeEffectStrength(int sectorIndex, int nodeIndex) =>
		_production.GetNodeEffectStrength(sectorIndex, nodeIndex);

	public double GetNodeDisplayedOutput(
		int sectorIndex,
		int nodeIndex) =>
		_production.GetNodeDisplayedOutput(sectorIndex, nodeIndex);

	public int GetBuiltNodeCount() =>
		_nodes.GetBuiltNodeCount();

	public double ConsumePendingOfflineEarnings() =>
		_offline.ConsumePendingOfflineEarnings();

	public void HandleApplicationPaused() =>
		_offline.HandleApplicationPaused();

	public double HandleApplicationResumed() =>
		_offline.HandleApplicationResumed();

	public void Save() =>
		_persistence.Save();

	private void SaveAndNotify()
	{
		Save();
		Changed?.Invoke();
	}

	private static long GetUnixNow()
	{
		return (long)
			Time.GetUnixTimeFromSystem();
	}
}
