#nullable enable

using System;

namespace IdleAi;

internal sealed class SingularityNodeService
{
	private const double NodeSellRefundFraction = 1.0 / 3.0;
	private readonly SingularitySaveData _data;
	private readonly SingularitySectorService _sectors;
	private readonly SingularityEconomy _economy;
	private readonly Action _saveAndNotify;

	internal SingularityNodeService(SingularitySaveData data, SingularitySectorService sectors, SingularityEconomy economy, Action saveAndNotify)
	{
		_data = data;
		_sectors = sectors;
		_economy = economy;
		_saveAndNotify = saveAndNotify;
	}

	internal bool IsNodeTypeUnlocked(
		SingularityNodeType type)
	{
		return type switch
		{
			SingularityNodeType.Compute => true,
			SingularityNodeType.Amplifier => true,
			SingularityNodeType.Cooling => _sectors.CoreCount >= 3,
			SingularityNodeType.Quantum => _sectors.CoreCount >= 5,
			_ => false
		};
	}

	internal string GetNodeUnlockText(
		SingularityNodeType type)
	{
		return type switch
		{
			SingularityNodeType.Compute => "UNLOCKED",
			SingularityNodeType.Amplifier => "UNLOCKED",
			SingularityNodeType.Cooling => "3 CORES",
			SingularityNodeType.Quantum => "5 CORES",
			_ => ""
		};
	}

	internal double GetBuildCost(
		int sectorIndex,
		SingularityNodeType type)
	{
		int builtNodes =
			GetBuiltNodeCount();

		double baseCost =
			type switch
			{
				SingularityNodeType.Compute => 10.0,
				SingularityNodeType.Amplifier => 45.0,
				SingularityNodeType.Cooling => 80.0,
				SingularityNodeType.Quantum => 260.0,
				_ => 0.0
			};

		return baseCost
			* Math.Pow(
				3.0,
				Math.Max(0, sectorIndex)
			)
			* Math.Pow(
				1.22,
				builtNodes
			);
	}

	internal SingularityActionResult BuildNode(
		int sectorIndex,
		int nodeIndex,
		SingularityNodeType type)
	{
		if (!_sectors.IsSectorCoreUnlocked(sectorIndex))
		{
			return new SingularityActionResult(
				false,
				"Unlock this Sector Core first."
			);
		}

		if (
			type == SingularityNodeType.Empty
			|| !IsNodeTypeUnlocked(type)
		)
		{
			return new SingularityActionResult(
				false,
				type + " is not unlocked yet."
			);
		}

		SingularityNodeData node =
			_sectors.GetNode(
				sectorIndex,
				nodeIndex
			);

		if (node.Type != SingularityNodeType.Empty)
		{
			return new SingularityActionResult(
				false,
				"This node is already occupied."
			);
		}

		double cost =
			GetBuildCost(
				sectorIndex,
				type
			);

		if (!_economy.TrySpend(cost))
			return SingularityEconomy.NotEnoughMatter(cost);

		node.Type =
			type;

		node.Level =
			1;

		node.InvestedMatter =
			cost;

		SingularityBotService.ClearNodeBot(
			node
		);

		_saveAndNotify();

		return new SingularityActionResult(
			true,
			type + " Node constructed."
		);
	}

	internal double GetNodeUpgradeCost(
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

		double typeBase =
			node.Type switch
			{
				SingularityNodeType.Compute => 18.0,
				SingularityNodeType.Amplifier => 60.0,
				SingularityNodeType.Cooling => 95.0,
				SingularityNodeType.Quantum => 320.0,
				_ => 18.0
			};

		return typeBase
			* Math.Pow(
				1.85,
				Math.Max(
					0,
					node.Level - 1
				)
			)
			* Math.Pow(
				2.6,
				Math.Max(0, sectorIndex)
			);
	}

	internal SingularityActionResult UpgradeNode(
		int sectorIndex,
		int nodeIndex)
	{
		SingularityNodeData node =
			_sectors.GetNode(
				sectorIndex,
				nodeIndex
			);

		if (node.Type == SingularityNodeType.Empty)
		{
			return new SingularityActionResult(
				false,
				"Build a node first."
			);
		}

		double cost =
			GetNodeUpgradeCost(
				sectorIndex,
				nodeIndex
			);

		if (!_economy.TrySpend(cost))
			return SingularityEconomy.NotEnoughMatter(cost);

		if (node.InvestedMatter <= 0.0)
		{
			node.InvestedMatter =
				EstimateLegacyNodeInvestment(
					sectorIndex,
					node
				);
		}

		node.InvestedMatter +=
			cost;

		node.Level =
			Math.Min(
				int.MaxValue,
				node.Level + 1
			);

		_saveAndNotify();

		return new SingularityActionResult(
			true,
			node.Type
				+ " Node upgraded to Level "
				+ node.Level
				+ "."
		);
	}

	internal double GetNodeSellRefund(
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

		double invested =
			node.InvestedMatter > 0.0
				? node.InvestedMatter
				: EstimateLegacyNodeInvestment(
					sectorIndex,
					node
				);

		return Math.Max(
			0.0,
			invested * NodeSellRefundFraction
		);
	}

	internal SingularityActionResult SellNode(
		int sectorIndex,
		int nodeIndex)
	{
		SingularityNodeData node =
			_sectors.GetNode(
				sectorIndex,
				nodeIndex
			);

		if (node.Type == SingularityNodeType.Empty)
		{
			return new SingularityActionResult(
				false,
				"This Node is already empty."
			);
		}

		if (node.HasBot)
		{
			return new SingularityActionResult(
				false,
				"Sell the Node's Bot first."
			);
		}

		SingularityNodeType soldType =
			node.Type;

		double refund =
			GetNodeSellRefund(
				sectorIndex,
				nodeIndex
			);

		_economy.Refund(refund);

		node.Type =
			SingularityNodeType.Empty;

		node.Level =
			1;

		node.InvestedMatter =
			0.0;

		SingularityBotService.ClearNodeBot(
			node
		);

		_saveAndNotify();

		return new SingularityActionResult(
			true,
			soldType
				+ " Node sold for "
				+ NumberFormatter.Format(refund)
				+ " Matter."
		);
	}

	internal static double EstimateLegacyNodeInvestment(
		int sectorIndex,
		SingularityNodeData node)
	{
		double buildBase =
			node.Type switch
			{
				SingularityNodeType.Compute => 10.0,
				SingularityNodeType.Amplifier => 45.0,
				SingularityNodeType.Cooling => 80.0,
				SingularityNodeType.Quantum => 260.0,
				_ => 0.0
			};

		double investment =
			buildBase
			* Math.Pow(
				3.0,
				Math.Max(0, sectorIndex)
			);

		double upgradeBase =
			node.Type switch
			{
				SingularityNodeType.Compute => 18.0,
				SingularityNodeType.Amplifier => 60.0,
				SingularityNodeType.Cooling => 95.0,
				SingularityNodeType.Quantum => 320.0,
				_ => 0.0
			};

		for (
			int level = 1;
			level < node.Level;
			level++
		)
		{
			investment +=
				upgradeBase
				* Math.Pow(
					1.85,
					level - 1
				)
				* Math.Pow(
					2.6,
					Math.Max(0, sectorIndex)
				);
		}

		return Math.Max(
			0.0,
			investment
		);
	}

	internal int GetBuiltNodeCount()
	{
		int count =
			0;

		foreach (
			SingularitySectorData sector
				in _data.Sectors
		)
		{
			foreach (
				SingularityNodeData node
					in sector.Nodes
			)
			{
				if (node.Type != SingularityNodeType.Empty)
					count++;
			}
		}

		return count;
	}
}
