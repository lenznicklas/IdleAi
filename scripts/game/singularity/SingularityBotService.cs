#nullable enable

using Godot;
using System;

namespace IdleAi;

internal sealed class SingularityBotService
{
	private const double BotSellRefundFraction = 1.0 / 3.0;
	private const double BotFullRepairCostFraction = 0.20;
	private const double SingularityBotBaseBonus = 0.10;
	private readonly LabData? _lab;
	private readonly Func<double> _roll;
	private readonly SingularitySaveData _data;
	private readonly SingularitySectorService _sectors;
	private readonly SingularityEconomy _economy;
	private readonly Action _saveAndNotify;
	private readonly Action<BotRarity>? _discover;

	internal SingularityBotService(SingularitySaveData data, SingularitySectorService sectors, SingularityEconomy economy, Action saveAndNotify, LabData? lab, Func<double>? roll, Action<BotRarity>? discover = null)
	{
		_data = data;
		_sectors = sectors;
		_economy = economy;
		_saveAndNotify = saveAndNotify;
		_lab = lab;
		_discover = discover;
		if (roll == null)
		{
			var random = new RandomNumberGenerator();
			random.Randomize();
			_roll = () => random.Randf();
		}
		else
			_roll = roll;
	}

	internal double GetNodeBotPurchaseCost(
		int sectorIndex,
		int nodeIndex)
	{
		SingularityNodeData node =
			_sectors.GetNode(
				sectorIndex,
				nodeIndex
			);

		if (node.Type == SingularityNodeType.Empty)
			return 0.0;

		return 30.0
			* Math.Pow(
				2.4,
				Math.Max(
					0,
					sectorIndex
				)
			)
			* Math.Pow(
				1.35,
				Math.Max(
					0,
					node.Level - 1
				)
			);
	}

	internal SingularityActionResult BuyNodeBot(
		int sectorIndex,
		int nodeIndex)
	{
		if (!_sectors.IsSectorCoreUnlocked(sectorIndex))
		{
			return new SingularityActionResult(
				false,
				"Unlock this Sector Core first."
			);
		}

		SingularityNodeData node =
			_sectors.GetNode(
				sectorIndex,
				nodeIndex
			);

		if (node.Type == SingularityNodeType.Empty)
		{
			return new SingularityActionResult(
				false,
				"Build a Node first."
			);
		}

		if (node.HasBot)
		{
			return new SingularityActionResult(
				false,
				"This Node already has a Bot."
			);
		}

		double cost =
			GetNodeBotPurchaseCost(
				sectorIndex,
				nodeIndex
			);

		if (!_economy.TrySpend(cost))
			return SingularityEconomy.NotEnoughMatter(cost);

		BotRarity rarity =
			RollBotRarity();

		node.BotRarity =
			rarity;

		_discover?.Invoke(rarity);
		node.BotPurchasePriceMatter =
			cost;

		InitializeNodeBotDurability(
			node
		);

		_saveAndNotify();

		BotDefinition bot =
			BotCatalog.Get(
				rarity
			);

		return new SingularityActionResult(
			true,
			bot.Name
				+ " installed on "
				+ node.Type
				+ " Node • "
				+ BotService.FormatWorkingTime(
					bot.WorkingLifetimeSeconds
				)
				+ " durability."
		);
	}

	internal double GetNodeBotSellRefund(
		int sectorIndex,
		int nodeIndex)
	{
		SingularityNodeData node =
			_sectors.GetNode(
				sectorIndex,
				nodeIndex
			);

		if (!node.HasBot)
			return 0.0;

		return Math.Max(
			0.0,
			node.BotPurchasePriceMatter
				* BotSellRefundFraction
		);
	}

	internal SingularityActionResult SellNodeBot(
		int sectorIndex,
		int nodeIndex)
	{
		SingularityNodeData node =
			_sectors.GetNode(
				sectorIndex,
				nodeIndex
			);

		if (!node.HasBot)
		{
			return new SingularityActionResult(
				false,
				"No Bot installed on this Node."
			);
		}

		BotDefinition bot =
			BotCatalog.Get(
				node.BotRarity!.Value
			);

		double refund =
			GetNodeBotSellRefund(
				sectorIndex,
				nodeIndex
			);

		_economy.Refund(refund);

		ClearNodeBot(
			node
		);

		_saveAndNotify();

		return new SingularityActionResult(
			true,
			bot.Name
				+ " sold for "
				+ NumberFormatter.Format(
					refund
				)
				+ " Matter."
		);
	}

	internal double GetNodeBotRepairCost(
		int sectorIndex,
		int nodeIndex)
	{
		SingularityNodeData node =
			_sectors.GetNode(
				sectorIndex,
				nodeIndex
			);

		if (!node.HasBot)
			return 0.0;

		double missing =
			1.0
			- GetNodeBotDurabilityRatio(
				sectorIndex,
				nodeIndex
			);

		if (missing <= 0.0001)
			return 0.0;

		return Math.Max(
			1.0,
			node.BotPurchasePriceMatter
				* BotFullRepairCostFraction
				* missing
		);
	}

	internal SingularityActionResult RepairNodeBot(
		int sectorIndex,
		int nodeIndex)
	{
		SingularityNodeData node =
			_sectors.GetNode(
				sectorIndex,
				nodeIndex
			);

		if (!node.HasBot)
		{
			return new SingularityActionResult(
				false,
				"No Bot installed on this Node."
			);
		}

		double cost =
			GetNodeBotRepairCost(
				sectorIndex,
				nodeIndex
			);

		if (cost <= 0.0)
		{
			return new SingularityActionResult(
				false,
				"Bot durability is already full."
			);
		}

		if (!_economy.TrySpend(cost))
			return SingularityEconomy.NotEnoughMatter(cost);

		InitializeNodeBotDurability(
			node
		);

		_saveAndNotify();

		return new SingularityActionResult(
			true,
			"Node Bot repaired to 100% durability."
		);
	}

	internal double GetNodeBotDurabilityRatio(
		int sectorIndex,
		int nodeIndex)
	{
		SingularityNodeData node =
			_sectors.GetNode(
				sectorIndex,
				nodeIndex
			);

		if (!node.HasBot)
			return 0.0;

		double maximum =
			BotCatalog.GetWorkingLifetimeSeconds(
				node.BotRarity!.Value
			);

		if (maximum <= 0.0)
			return 0.0;

		if (!node.BotDurabilityInitialized)
			return 1.0;

		return Math.Clamp(
			node.BotDurabilitySecondsRemaining
				/ maximum,
			0.0,
			1.0
		);
	}

	internal double GetNodeBotEffectiveMultiplier(
		int sectorIndex,
		int nodeIndex)
	{
		return GetNodeBotEffectiveMultiplier(
			_sectors.GetNode(
				sectorIndex,
				nodeIndex
			)
		);
	}

	internal double GetNodeBotEffectiveMultiplier(
		SingularityNodeData node)
	{
		if (!node.HasWorkingBot)
			return 1.0;

		double multiplier =
			BotCatalog.GetMultiplier(
				node.BotRarity
			)
			+ SingularityBotBaseBonus;

		if (_lab != null)
		{
			multiplier *=
				_lab
					.GetBotPowerMultiplier();
		}

		return multiplier;
	}

	internal void InitializeNodeBotDurability(
		SingularityNodeData node)
	{
		if (!node.BotRarity.HasValue)
		{
			node.BotDurabilityInitialized =
				false;

			node.BotDurabilitySecondsRemaining =
				0.0;

			return;
		}

		node.BotDurabilityInitialized =
			true;

		node.BotDurabilitySecondsRemaining =
			BotCatalog.GetWorkingLifetimeSeconds(
				node.BotRarity.Value
			);
	}

	internal static void ClearNodeBot(
		SingularityNodeData node)
	{
		node.BotRarity =
			null;

		node.BotPurchasePriceMatter =
			0.0;

		node.BotDurabilityInitialized =
			false;

		node.BotDurabilitySecondsRemaining =
			0.0;
	}

	internal void ConsumeSingularityBotWork(
		double seconds)
	{
		if (seconds <= 0.0)
			return;

		for (
			int sectorIndex = 0;
			sectorIndex < _data.Sectors.Count;
			sectorIndex++
		)
		{
			SingularitySectorData sector =
				_data.Sectors[
					sectorIndex
				];

			if (!sector.CoreUnlocked)
				continue;

			foreach (
				SingularityNodeData node
					in sector.Nodes
			)
			{
				if (
					node.Type == SingularityNodeType.Empty
					|| !node.HasWorkingBot
				)
				{
					continue;
				}

				if (!node.BotDurabilityInitialized)
				{
					InitializeNodeBotDurability(
						node
					);
				}

				node.BotDurabilitySecondsRemaining =
					Math.Max(
						0.0,
						node.BotDurabilitySecondsRemaining
							- seconds
					);
			}
		}
	}

	internal double GetNextNodeBotBreakSeconds()
	{
		double next =
			double.PositiveInfinity;

		foreach (
			SingularitySectorData sector
				in _data.Sectors
		)
		{
			if (!sector.CoreUnlocked)
				continue;

			foreach (
				SingularityNodeData node
					in sector.Nodes
			)
			{
				if (
					node.Type == SingularityNodeType.Empty
					|| !node.HasWorkingBot
				)
				{
					continue;
				}

				if (!node.BotDurabilityInitialized)
				{
					InitializeNodeBotDurability(
						node
					);
				}

				next =
					Math.Min(
						next,
						node.BotDurabilitySecondsRemaining
					);
			}
		}

		return next;
	}

	internal (
		double Common,
		double Rare,
		double Epic,
		double Legendary
	) GetNodeBotRarityChances()
	{
		double rare =
			GameConfig.RareBotChance;

		double epic =
			GameConfig.EpicBotChance;

		double legendary =
			1.0
			- GameConfig.CommonBotChance
			- GameConfig.RareBotChance
			- GameConfig.EpicBotChance;

		if (_lab != null)
		{
			rare +=
				_lab
					.GetRareBotChanceBonus();

			epic +=
				_lab
					.GetEpicBotChanceBonus();

			legendary +=
				_lab
					.GetLegendaryBotChanceBonus();
		}

		rare =
			Math.Max(
				0.0,
				rare
			);

		epic =
			Math.Max(
				0.0,
				epic
			);

		legendary =
			Math.Max(
				0.0,
				legendary
			);

		double special =
			rare + epic + legendary;

		if (special > 1.0)
		{
			rare /= special;
			epic /= special;
			legendary /= special;

			return (
				0.0,
				rare,
				epic,
				legendary
			);
		}

		return (
			1.0 - special,
			rare,
			epic,
			legendary
		);
	}

	// Content rolls share the existing randomized Godot RNG and injectable deterministic seam.
	internal double RollContent() => _roll();

	internal BotRarity RollBotRarity()
	{
		(
			double common,
			double rare,
			double epic,
			double legendary
		) =
			GetNodeBotRarityChances();

		double roll =
			_roll();

		if (roll < common)
			return BotRarity.Common;

		roll -=
			common;

		if (roll < rare)
			return BotRarity.Rare;

		roll -=
			rare;

		if (roll < epic)
			return BotRarity.Epic;

		return BotRarity.Legendary;
	}
}
