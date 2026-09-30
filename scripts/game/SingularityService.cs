using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

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

	private const string SavePath =
		"user://singularity_save.json";

	private const double OfflineEfficiency =
		0.50;

	private const double NodeSellRefundFraction =
		1.0 / 3.0;

	private const double BotSellRefundFraction =
		1.0 / 3.0;

	private const double BotFullRepairCostFraction =
		0.20;

	/*
	 * Singularity nodes run continuously, so even a Common Bot must have a
	 * useful effect. The normal Bot multiplier is therefore given a small
	 * +10% Singularity resonance bonus.
	 */
	private const double SingularityBotBaseBonus =
		0.10;

	private const long MaximumOfflineSeconds =
		7 * 24 * 60 * 60;

	private readonly JsonSerializerOptions _jsonOptions =
		new()
		{
			WriteIndented = true,
			PropertyNameCaseInsensitive = true
		};

	private readonly GameState? _state;

	private readonly RandomNumberGenerator _random =
		new();

	private SingularitySaveData _data =
		new();

	private long _pauseUnix;

	private double _pendingOfflineEarnings;

	private readonly List<Vector2I> _sectorGridCache =
		[
			Vector2I.Zero
		];

	private readonly Dictionary<Vector2I, int> _sectorIndexByGrid =
		[];

	private Vector2I _spiralCursor =
		Vector2I.Zero;

	private Vector2I _spiralDirection =
		new(
			1,
			0
		);

	private int _spiralStepLength =
		1;

	private int _spiralStepProgress;

	private int _spiralLegsAtCurrentLength;


	public bool Unlocked =>
		_data.Unlocked;

	public double Matter =>
		_data.Matter;

	public int CoreLevel =>
		GetSectorCoreLevel(
			CurrentSectorIndex
		);

	public int CoreCount
	{
		get
		{
			int count =
				0;

			foreach (
				SingularitySectorData sector
					in _data.Sectors
			)
			{
				if (sector.CoreUnlocked)
					count++;
			}

			return count;
		}
	}

	public int CurrentSectorIndex =>
		_data.CurrentSectorIndex;

	public int SectorNumber =>
		_data.CurrentSectorIndex + 1;

	public int SectorCount =>
		_data.Sectors.Count;

	public event Action? Changed;


	public SingularityService(
		GameState? state = null)
	{
		_state =
			state;

		_random.Randomize();

		Load();

		EnsureSector(
			0
		);

		RebuildGridIndex();

		_pendingOfflineEarnings =
			ApplyOfflineIncomeFromTimestamp(
				_data.LastSaveUnix,
				GetUnixNow()
			);

		if (_pendingOfflineEarnings > 0.0)
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
			_data.Matter +=
				output
				* delta;
		}

		/*
		 * Durability is consumed only while a Node exists, its Sector Core is
		 * online, and the Bot is still functional.
		 */
		ConsumeSingularityBotWork(
			delta
		);
	}


	public SingularityActionResult Unlock(
		GameState gameState)
	{
		if (Unlocked)
		{
			return new SingularityActionResult(
				false,
				"The Singularity is already online."
			);
		}

		if (
			gameState.Tokens
			< TestUnlockTokenCost
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

		gameState.Tokens -=
			TestUnlockTokenCost;

		gameState.Stats.AddSlotSpending(
			TestUnlockTokenCost
		);

		_data.Unlocked =
			true;

		EnsureSector(
			0
		);

		SingularitySectorData firstSector =
			_data.Sectors[0];

		firstSector.CoreUnlocked =
			true;

		firstSector.CoreLevel =
			Math.Max(
				1,
				firstSector.CoreLevel
			);

		SaveAndNotify();

		return new SingularityActionResult(
			true,
			"SINGULARITY ONLINE • SECTOR 1 CORE INSTALLED"
		);
	}


	public SingularitySectorData GetCurrentSector()
	{
		return GetSector(
			_data.CurrentSectorIndex
		);
	}


	public SingularitySectorData GetSector(
		int index)
	{
		EnsureSector(
			index
		);

		return _data.Sectors[
			index
		];
	}


	public SingularityNodeData GetNode(
		int sectorIndex,
		int nodeIndex)
	{
		SingularitySectorData sector =
			GetSector(
				sectorIndex
			);

		return sector.Nodes[
			Math.Clamp(
				nodeIndex,
				0,
				NodesPerSector - 1
			)
		];
	}


	// ==================================================
	// SECTOR CORES
	// ==================================================

	public bool IsSectorCoreUnlocked(
		int sectorIndex)
	{
		return GetSector(
			sectorIndex
		).CoreUnlocked;
	}


	public int GetSectorCoreLevel(
		int sectorIndex)
	{
		SingularitySectorData sector =
			GetSector(
				sectorIndex
			);

		return Math.Max(
			1,
			sector.CoreLevel
		);
	}


	public double GetSectorCoreUnlockCost(
		int sectorIndex)
	{
		if (sectorIndex <= 0)
			return 0.0;

		return 250.0
			* Math.Pow(
				4.0,
				sectorIndex - 1
			);
	}


	public SingularityActionResult UnlockSectorCore(
		int sectorIndex)
	{
		if (
			sectorIndex < 0
			|| sectorIndex >= _data.Sectors.Count
		)
		{
			return new SingularityActionResult(
				false,
				"Invalid Sector Core."
			);
		}

		SingularitySectorData sector =
			GetSector(
				sectorIndex
			);

		if (sector.CoreUnlocked)
		{
			return new SingularityActionResult(
				false,
				"This Sector Core is already online."
			);
		}

		double cost =
			GetSectorCoreUnlockCost(
				sectorIndex
			);

		if (_data.Matter < cost)
			return NotEnoughMatter(cost);

		_data.Matter -=
			cost;

		sector.CoreUnlocked =
			true;

		sector.CoreLevel =
			1;

		SaveAndNotify();

		return new SingularityActionResult(
			true,
			"Sector "
				+ (sectorIndex + 1)
				+ " Core online."
		);
	}


	public double GetSectorCoreUpgradeCost(
		int sectorIndex)
	{
		if (!IsSectorCoreUnlocked(sectorIndex))
			return GetSectorCoreUnlockCost(sectorIndex);

		int level =
			GetSectorCoreLevel(
				sectorIndex
			);

		return 100.0
			* Math.Pow(
				2.15,
				Math.Max(
					0,
					level - 1
				)
			)
			* Math.Pow(
				3.0,
				Math.Max(
					0,
					sectorIndex
				)
			);
	}


	public SingularityActionResult UpgradeSectorCore(
		int sectorIndex)
	{
		if (!IsSectorCoreUnlocked(sectorIndex))
			return UnlockSectorCore(sectorIndex);

		double cost =
			GetSectorCoreUpgradeCost(
				sectorIndex
			);

		if (_data.Matter < cost)
			return NotEnoughMatter(cost);

		_data.Matter -=
			cost;

		SingularitySectorData sector =
			GetSector(
				sectorIndex
			);

		sector.CoreLevel =
			Math.Min(
				int.MaxValue,
				GetSectorCoreLevel(sectorIndex) + 1
			);

		if (sectorIndex == 0)
		{
			_data.CoreLevel =
				sector.CoreLevel;
		}

		SaveAndNotify();

		return new SingularityActionResult(
			true,
			"Sector "
				+ (sectorIndex + 1)
				+ " Core upgraded to Level "
				+ sector.CoreLevel
				+ "."
		);
	}


	public double GetSectorCoreOutputPerSecond(
		int sectorIndex)
	{
		if (!IsSectorCoreUnlocked(sectorIndex))
			return 0.0;

		return GetCoreOutputPerSecond(
			GetSectorCoreLevel(
				sectorIndex
			)
		)
		* (
			1.0
			+ 0.35
			* Math.Max(
				0,
				sectorIndex
			)
		);
	}


	public double GetSectorComputeCoreMultiplier(
		int sectorIndex)
	{
		if (!IsSectorCoreUnlocked(sectorIndex))
			return 1.0;

		return 1.0
			+ 0.10
			* Math.Max(
				0,
				GetSectorCoreLevel(sectorIndex) - 1
			);
	}


	public double GetNextCoreCost()
	{
		return GetCoreUpgradeCost();
	}


	public double GetCoreUpgradeCost()
	{
		return IsSectorCoreUnlocked(CurrentSectorIndex)
			? GetSectorCoreUpgradeCost(CurrentSectorIndex)
			: GetSectorCoreUnlockCost(CurrentSectorIndex);
	}


	public SingularityActionResult BuyNextCore()
	{
		return UpgradeSectorCore(
			CurrentSectorIndex
		);
	}


	public SingularityActionResult UpgradeCore()
	{
		return UpgradeSectorCore(
			CurrentSectorIndex
		);
	}


	public double GetCoreOutputPerSecond()
	{
		return GetSectorCoreOutputPerSecond(
			CurrentSectorIndex
		);
	}


	public static double GetCoreOutputPerSecond(
		int coreLevel)
	{
		return 0.05
			* Math.Pow(
				1.42,
				Math.Max(
					0,
					coreLevel - 1
				)
			);
	}


	public double GetComputeCoreMultiplier()
	{
		return GetSectorComputeCoreMultiplier(
			CurrentSectorIndex
		);
	}


	// ==================================================
	// NODE UNLOCKS / COSTS
	// ==================================================

	public bool IsNodeTypeUnlocked(
		SingularityNodeType type)
	{
		return type switch
		{
			SingularityNodeType.Compute => true,
			SingularityNodeType.Amplifier => true,
			SingularityNodeType.Cooling => CoreCount >= 3,
			SingularityNodeType.Quantum => CoreCount >= 5,
			_ => false
		};
	}


	public string GetNodeUnlockText(
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


	public double GetBuildCost(
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


	public SingularityActionResult BuildNode(
		int sectorIndex,
		int nodeIndex,
		SingularityNodeType type)
	{
		if (!IsSectorCoreUnlocked(sectorIndex))
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
			GetNode(
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

		if (_data.Matter < cost)
			return NotEnoughMatter(cost);

		_data.Matter -=
			cost;

		node.Type =
			type;

		node.Level =
			1;

		node.InvestedMatter =
			cost;

		ClearNodeBot(
			node
		);

		SaveAndNotify();

		return new SingularityActionResult(
			true,
			type + " Node constructed."
		);
	}


	public double GetNodeUpgradeCost(
		int sectorIndex,
		int nodeIndex)
	{
		SingularityNodeData node =
			GetNode(
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


	public SingularityActionResult UpgradeNode(
		int sectorIndex,
		int nodeIndex)
	{
		SingularityNodeData node =
			GetNode(
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

		if (_data.Matter < cost)
			return NotEnoughMatter(cost);

		_data.Matter -=
			cost;

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

		SaveAndNotify();

		return new SingularityActionResult(
			true,
			node.Type
				+ " Node upgraded to Level "
				+ node.Level
				+ "."
		);
	}


	// ==================================================
	// SINGULARITY NODE BOTS
	// ==================================================

	public double GetNodeBotPurchaseCost(
		int sectorIndex,
		int nodeIndex)
	{
		SingularityNodeData node =
			GetNode(
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


	public SingularityActionResult BuyNodeBot(
		int sectorIndex,
		int nodeIndex)
	{
		if (!IsSectorCoreUnlocked(sectorIndex))
		{
			return new SingularityActionResult(
				false,
				"Unlock this Sector Core first."
			);
		}

		SingularityNodeData node =
			GetNode(
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

		if (_data.Matter < cost)
			return NotEnoughMatter(cost);

		_data.Matter -=
			cost;

		BotRarity rarity =
			RollBotRarity();

		node.BotRarity =
			rarity;

		node.BotPurchasePriceMatter =
			cost;

		InitializeNodeBotDurability(
			node
		);

		SaveAndNotify();

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


	public double GetNodeBotSellRefund(
		int sectorIndex,
		int nodeIndex)
	{
		SingularityNodeData node =
			GetNode(
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


	public SingularityActionResult SellNodeBot(
		int sectorIndex,
		int nodeIndex)
	{
		SingularityNodeData node =
			GetNode(
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

		_data.Matter +=
			refund;

		ClearNodeBot(
			node
		);

		SaveAndNotify();

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


	public double GetNodeBotRepairCost(
		int sectorIndex,
		int nodeIndex)
	{
		SingularityNodeData node =
			GetNode(
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


	public SingularityActionResult RepairNodeBot(
		int sectorIndex,
		int nodeIndex)
	{
		SingularityNodeData node =
			GetNode(
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

		if (_data.Matter < cost)
			return NotEnoughMatter(cost);

		_data.Matter -=
			cost;

		InitializeNodeBotDurability(
			node
		);

		SaveAndNotify();

		return new SingularityActionResult(
			true,
			"Node Bot repaired to 100% durability."
		);
	}


	public double GetNodeBotDurabilityRatio(
		int sectorIndex,
		int nodeIndex)
	{
		SingularityNodeData node =
			GetNode(
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


	public double GetNodeBotEffectiveMultiplier(
		int sectorIndex,
		int nodeIndex)
	{
		return GetNodeBotEffectiveMultiplier(
			GetNode(
				sectorIndex,
				nodeIndex
			)
		);
	}


	private double GetNodeBotEffectiveMultiplier(
		SingularityNodeData node)
	{
		if (!node.HasWorkingBot)
			return 1.0;

		double multiplier =
			BotCatalog.GetMultiplier(
				node.BotRarity
			)
			+ SingularityBotBaseBonus;

		if (_state != null)
		{
			multiplier *=
				_state.Lab
					.GetBotPowerMultiplier();
		}

		return multiplier;
	}


	private void InitializeNodeBotDurability(
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


	private static void ClearNodeBot(
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


	private void ConsumeSingularityBotWork(
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


	private double GetNextNodeBotBreakSeconds()
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


	public (
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

		if (_state != null)
		{
			rare +=
				_state.Lab
					.GetRareBotChanceBonus();

			epic +=
				_state.Lab
					.GetEpicBotChanceBonus();

			legendary +=
				_state.Lab
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


	private BotRarity RollBotRarity()
	{
		(
			double common,
			double rare,
			double epic,
			double legendary
		) =
			GetNodeBotRarityChances();

		double roll =
			_random.Randf();

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


	// ==================================================
	// NODE SELLING
	// ==================================================

	public double GetNodeSellRefund(
		int sectorIndex,
		int nodeIndex)
	{
		SingularityNodeData node =
			GetNode(
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


	public SingularityActionResult SellNode(
		int sectorIndex,
		int nodeIndex)
	{
		SingularityNodeData node =
			GetNode(
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

		_data.Matter +=
			refund;

		node.Type =
			SingularityNodeType.Empty;

		node.Level =
			1;

		node.InvestedMatter =
			0.0;

		ClearNodeBot(
			node
		);

		SaveAndNotify();

		return new SingularityActionResult(
			true,
			soldType
				+ " Node sold for "
				+ NumberFormatter.Format(refund)
				+ " Matter."
		);
	}


	private static double EstimateLegacyNodeInvestment(
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


	// ==================================================
	// SECTORS
	// ==================================================

	public double GetNextSectorUnlockCost()
	{
		int nextSector =
			_data.Sectors.Count;

		return 750.0
			* Math.Pow(
				5.0,
				Math.Max(
					0,
					nextSector - 1
				)
			);
	}


	public bool CanUnlockNextSector()
	{
		if (_data.Sectors.Count == 0)
			return false;

		SingularitySectorData frontier =
			_data.Sectors[
				_data.Sectors.Count - 1
			];

		if (!frontier.CoreUnlocked)
			return false;

		return frontier.Nodes.TrueForAll(
			node =>
				node.Type
				!= SingularityNodeType.Empty
		);
	}


	public int GetFrontierSectorIndex()
	{
		return Math.Max(
			0,
			_data.Sectors.Count - 1
		);
	}


	public void SelectSector(
		int sectorIndex)
	{
		if (
			sectorIndex < 0
			|| sectorIndex >= _data.Sectors.Count
			|| sectorIndex == _data.CurrentSectorIndex
		)
		{
			return;
		}

		_data.CurrentSectorIndex =
			sectorIndex;

		Save();
	}


	public SingularityActionResult UnlockNextSector()
	{
		if (!CanUnlockNextSector())
		{
			int frontierIndex =
				GetFrontierSectorIndex();

			if (!IsSectorCoreUnlocked(frontierIndex))
			{
				return new SingularityActionResult(
					false,
					"Unlock the Sector "
						+ (frontierIndex + 1)
						+ " Core first."
				);
			}

			return new SingularityActionResult(
				false,
				"Fill all 8 nodes in Sector "
					+ (frontierIndex + 1)
					+ " first."
			);
		}

		int nextIndex =
			_data.Sectors.Count;

		double cost =
			GetNextSectorUnlockCost();

		if (_data.Matter < cost)
			return NotEnoughMatter(cost);

		_data.Matter -=
			cost;

		EnsureSector(
			nextIndex
		);

		SingularitySectorData newSector =
			_data.Sectors[nextIndex];

		newSector.CoreUnlocked =
			false;

		newSector.CoreLevel =
			1;

		_data.CurrentSectorIndex =
			nextIndex;

		SaveAndNotify();

		return new SingularityActionResult(
			true,
			"Sector "
				+ SectorNumber
				+ " opened • Core locked."
		);
	}


	public bool GoToPreviousSector()
	{
		if (_data.CurrentSectorIndex <= 0)
			return false;

		_data.CurrentSectorIndex--;

		SaveAndNotify();

		return true;
	}


	public bool GoToNextExistingSector()
	{
		if (
			_data.CurrentSectorIndex
			>= _data.Sectors.Count - 1
		)
		{
			return false;
		}

		_data.CurrentSectorIndex++;

		SaveAndNotify();

		return true;
	}


	// ==================================================
	// OUTPUT / CROSS-SECTOR ADJACENCY
	// ==================================================

	public double GetTotalOutputPerSecond()
	{
		if (!Unlocked)
			return 0.0;

		double total =
			0.0;

		for (
			int sectorIndex = 0;
			sectorIndex < _data.Sectors.Count;
			sectorIndex++
		)
		{
			total +=
				GetSectorOutputPerSecond(
					sectorIndex
				);
		}

		return total;
	}


	public double GetSectorOutputPerSecond(
		int sectorIndex)
	{
		SingularitySectorData sector =
			GetSector(
				sectorIndex
			);

		double quantumMultiplier =
			1.0;

		foreach (
			SingularityNodeData node
				in sector.Nodes
		)
		{
			if (node.Type == SingularityNodeType.Quantum)
			{
				quantumMultiplier +=
					0.12
					* node.Level
					* GetNodeBotEffectiveMultiplier(
						node
					);
			}
		}

		double total =
			GetSectorCoreOutputPerSecond(
				sectorIndex
			);

		for (
			int i = 0;
			i < sector.Nodes.Count;
			i++
		)
		{
			SingularityNodeData node =
				sector.Nodes[i];

			if (node.Type != SingularityNodeType.Compute)
				continue;

			double baseOutput =
				0.25
				* Math.Pow(
					1.58,
					Math.Max(
						0,
						node.Level - 1
					)
				)
				* GetNodeBotEffectiveMultiplier(
					node
				);

			double adjacencyMultiplier =
				GetLocalAdjacencyMultiplier(
					sector,
					i
				)
				* GetExternalAdjacencyMultiplier(
					sectorIndex,
					i
				);

			total +=
				baseOutput
				* adjacencyMultiplier
				* quantumMultiplier
				* GetSectorComputeCoreMultiplier(
					sectorIndex
				);
		}

		return total;
	}


	private double GetLocalAdjacencyMultiplier(
		SingularitySectorData sector,
		int nodeIndex)
	{
		int previous =
			(
				nodeIndex
				+ NodesPerSector
				- 1
			)
			% NodesPerSector;

		int next =
			(nodeIndex + 1)
			% NodesPerSector;

		return GetAdjacentMultiplier(
			sector.Nodes[previous]
		)
		* GetAdjacentMultiplier(
			sector.Nodes[next]
		);
	}


	private double GetAdjacentMultiplier(
		SingularityNodeData node)
	{
		double botMultiplier =
			GetNodeBotEffectiveMultiplier(
				node
			);

		return node.Type switch
		{
			SingularityNodeType.Amplifier =>
				1.0
				+ 0.15
				* node.Level
				* botMultiplier,

			SingularityNodeType.Cooling =>
				1.0
				+ 0.10
				* node.Level
				* botMultiplier,

			_ => 1.0
		};
	}


	private double GetExternalAdjacencyMultiplier(
		int sectorIndex,
		int nodeIndex)
	{
		if (!TryGetCrossSectorNeighbor(
			sectorIndex,
			nodeIndex,
			out int neighborSector,
			out int neighborNode
		))
		{
			return 1.0;
		}

		return GetAdjacentMultiplier(
			GetNode(
				neighborSector,
				neighborNode
			)
		);
	}


	private bool TryGetCrossSectorNeighbor(
		int sectorIndex,
		int nodeIndex,
		out int neighborSector,
		out int neighborNode)
	{
		neighborSector =
			-1;

		neighborNode =
			-1;

		Vector2I offset;

		switch (nodeIndex)
		{
			case 0:
				offset = new Vector2I(0, -1);
				neighborNode = 4;
				break;

			case 2:
				offset = new Vector2I(1, 0);
				neighborNode = 6;
				break;

			case 4:
				offset = new Vector2I(0, 1);
				neighborNode = 0;
				break;

			case 6:
				offset = new Vector2I(-1, 0);
				neighborNode = 2;
				break;

			default:
				return false;
		}

		EnsureGridCache();

		Vector2I target =
			GetSectorGridPosition(sectorIndex)
			+ offset;

		return _sectorIndexByGrid.TryGetValue(
			target,
			out neighborSector
		);
	}


	public double GetNodeDisplayedOutput(
		int sectorIndex,
		int nodeIndex)
	{
		SingularityNodeData node =
			GetNode(
				sectorIndex,
				nodeIndex
			);

		if (node.Type != SingularityNodeType.Compute)
			return 0.0;

		SingularitySectorData sector =
			GetSector(
				sectorIndex
			);

		double output =
			0.25
			* Math.Pow(
				1.58,
				Math.Max(
					0,
					node.Level - 1
				)
			)
			* GetNodeBotEffectiveMultiplier(
				node
			);

		output *=
			GetLocalAdjacencyMultiplier(
				sector,
				nodeIndex
			);

		output *=
			GetExternalAdjacencyMultiplier(
				sectorIndex,
				nodeIndex
			);

		double quantum =
			1.0;

		foreach (
			SingularityNodeData sectorNode
				in sector.Nodes
		)
		{
			if (sectorNode.Type == SingularityNodeType.Quantum)
			{
				quantum +=
					0.12
					* sectorNode.Level
					* GetNodeBotEffectiveMultiplier(
						sectorNode
					);
			}
		}

		output *=
			quantum;

		output *=
			GetSectorComputeCoreMultiplier(
				sectorIndex
			);

		return output;
	}


	public int GetBuiltNodeCount()
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


	// ==================================================
	// OFFLINE / LIFECYCLE
	// ==================================================

	public double ConsumePendingOfflineEarnings()
	{
		double value =
			_pendingOfflineEarnings;

		_pendingOfflineEarnings =
			0.0;

		return value;
	}


	public void HandleApplicationPaused()
	{
		if (!Unlocked)
			return;

		_pauseUnix =
			GetUnixNow();

		Save();
	}


	public double HandleApplicationResumed()
	{
		if (
			!Unlocked
			|| _pauseUnix <= 0
		)
		{
			return 0.0;
		}

		long now =
			GetUnixNow();

		double earned =
			ApplyOfflineIncomeFromTimestamp(
				_pauseUnix,
				now
			);

		_pauseUnix =
			0;

		Save();

		if (earned > 0.0)
			Changed?.Invoke();

		return earned;
	}


	private double ApplyOfflineIncomeFromTimestamp(
		long fromUnix,
		long toUnix)
	{
		if (
			!Unlocked
			|| fromUnix <= 0
			|| toUnix <= fromUnix
		)
		{
			return 0.0;
		}

		long seconds =
			Math.Clamp(
				toUnix - fromUnix,
				0,
				MaximumOfflineSeconds
			);

		if (seconds <= 0)
			return 0.0;

		double remaining =
			seconds;

		double earned =
			0.0;

		int safety =
			0;

		/*
		 * Exact durability-aware offline simulation:
		 * each chunk ends at the next Bot break. Output is recalculated after
		 * every break, so a Bot cannot keep boosting the full offline period
		 * after its remaining work time has reached zero.
		 */
		while (
			remaining > 0.0001
			&& safety < 100_000
		)
		{
			safety++;

			double output =
				GetTotalOutputPerSecond();

			double nextBreak =
				GetNextNodeBotBreakSeconds();

			double chunk =
				double.IsFinite(
					nextBreak
				)
				? Math.Min(
					remaining,
					Math.Max(
						0.001,
						nextBreak
					)
				)
				: remaining;

			if (output > 0.0)
			{
				earned +=
					output
					* chunk
					* OfflineEfficiency;
			}

			ConsumeSingularityBotWork(
				chunk
			);

			remaining -=
				chunk;
		}

		if (earned <= 0.0)
			return 0.0;

		_data.Matter +=
			earned;

		GD.Print(
			"Singularity offline matter: ",
			earned
		);

		return earned;
	}


	// ==================================================
	// SAVE / MIGRATION
	// ==================================================

	public void Save()
	{
		try
		{
			_data.LastSaveUnix =
				GetUnixNow();

			string json =
				JsonSerializer.Serialize(
					_data,
					_jsonOptions
				);

			using Godot.FileAccess file =
				Godot.FileAccess.Open(
					SavePath,
					Godot.FileAccess.ModeFlags.Write
				);

			file.StoreString(
				json
			);
		}
		catch (Exception exception)
		{
			GD.PushWarning(
				"Singularity save failed: "
					+ exception.Message
			);
		}
	}


	private void Load()
	{
		_data =
			new SingularitySaveData();

		if (!Godot.FileAccess.FileExists(SavePath))
		{
			EnsureSector(0);
			_data.Sectors[0].CoreUnlocked = true;
			_data.Sectors[0].CoreLevel = 1;
			_data.SaveVersion = 4;
			return;
		}

		try
		{
			using Godot.FileAccess file =
				Godot.FileAccess.Open(
					SavePath,
					Godot.FileAccess.ModeFlags.Read
				);

			string json =
				file.GetAsText();

			SingularitySaveData? loaded =
				JsonSerializer.Deserialize<SingularitySaveData>(
					json,
					_jsonOptions
				);

			if (loaded != null)
				_data = loaded;
		}
		catch (Exception exception)
		{
			GD.PushWarning(
				"Singularity save load failed: "
					+ exception.Message
			);

			_data =
				new SingularitySaveData();
		}

		int loadedVersion =
			_data.SaveVersion;

		_data.CurrentSectorIndex =
			Math.Max(
				0,
				_data.CurrentSectorIndex
			);

		EnsureSector(
			_data.CurrentSectorIndex
		);

		if (loadedVersion < 3)
		{
			SingularitySectorData first =
				_data.Sectors[0];

			first.CoreUnlocked =
				true;

			first.CoreLevel =
				Math.Max(
					1,
					_data.CoreLevel
				);

			for (
				int i = 1;
				i < _data.Sectors.Count;
				i++
			)
			{
				_data.Sectors[i].CoreUnlocked = false;
				_data.Sectors[i].CoreLevel = 1;
			}
		}

		_data.Sectors[0].CoreUnlocked =
			true;

		_data.Sectors[0].CoreLevel =
			Math.Max(
				1,
				_data.Sectors[0].CoreLevel
			);

		foreach (
			SingularitySectorData sector
				in _data.Sectors
		)
		{
			sector.CoreLevel =
				Math.Max(
					1,
					sector.CoreLevel
				);

			foreach (
				SingularityNodeData node
					in sector.Nodes
			)
			{
				if (!node.BotRarity.HasValue)
				{
					ClearNodeBot(
						node
					);

					continue;
				}

				if (!node.BotDurabilityInitialized)
				{
					/*
					 * Future-safe migration: if a save somehow contains a Node Bot
					 * but predates durability, start that Bot at 100%.
					 */
					InitializeNodeBotDurability(
						node
					);
				}
				else
				{
					node.BotDurabilitySecondsRemaining =
						Math.Clamp(
							node.BotDurabilitySecondsRemaining,
							0.0,
							BotCatalog.GetWorkingLifetimeSeconds(
								node.BotRarity.Value
							)
						);
				}
			}
		}

		_data.SaveVersion =
			4;
	}


	private void EnsureSector(
		int index)
	{
		index =
			Math.Max(
				0,
				index
			);

		while (_data.Sectors.Count <= index)
		{
			int newIndex =
				_data.Sectors.Count;

			SingularitySectorData sector =
				new()
				{
					CoreUnlocked =
						newIndex == 0,

					CoreLevel = 1
				};

			for (
				int i = 0;
				i < NodesPerSector;
				i++
			)
			{
				sector.Nodes.Add(
					new SingularityNodeData()
				);
			}

			_data.Sectors.Add(
				sector
			);
		}

		foreach (
			SingularitySectorData sector
				in _data.Sectors
		)
		{
			while (sector.Nodes.Count < NodesPerSector)
			{
				sector.Nodes.Add(
					new SingularityNodeData()
				);
			}

			sector.CoreLevel =
				Math.Max(
					1,
					sector.CoreLevel
				);
		}

		EnsureGridCache();
	}


	private void RebuildGridIndex()
	{
		_sectorIndexByGrid.Clear();
		EnsureGridCache();
	}


	private void EnsureGridCache()
	{
		while (_sectorGridCache.Count < _data.Sectors.Count)
		{
			_spiralCursor +=
				_spiralDirection;

			_sectorGridCache.Add(
				_spiralCursor
			);

			_spiralStepProgress++;

			if (_spiralStepProgress < _spiralStepLength)
				continue;

			_spiralStepProgress =
				0;

			_spiralDirection =
				new Vector2I(
					-_spiralDirection.Y,
					_spiralDirection.X
				);

			_spiralLegsAtCurrentLength++;

			if (_spiralLegsAtCurrentLength < 2)
				continue;

			_spiralLegsAtCurrentLength =
				0;

			_spiralStepLength++;
		}

		for (
			int i = _sectorIndexByGrid.Count;
			i < _data.Sectors.Count;
			i++
		)
		{
			_sectorIndexByGrid[
				GetSectorGridPosition(i)
			] = i;
		}
	}


	private Vector2I GetSectorGridPosition(
		int sectorIndex)
	{
		sectorIndex =
			Math.Clamp(
				sectorIndex,
				0,
				Math.Max(
					0,
					_data.Sectors.Count - 1
				)
			);

		while (_sectorGridCache.Count <= sectorIndex)
		{
			_spiralCursor +=
				_spiralDirection;

			_sectorGridCache.Add(
				_spiralCursor
			);

			_spiralStepProgress++;

			if (_spiralStepProgress < _spiralStepLength)
				continue;

			_spiralStepProgress = 0;

			_spiralDirection =
				new Vector2I(
					-_spiralDirection.Y,
					_spiralDirection.X
				);

			_spiralLegsAtCurrentLength++;

			if (_spiralLegsAtCurrentLength >= 2)
			{
				_spiralLegsAtCurrentLength = 0;
				_spiralStepLength++;
			}
		}

		return _sectorGridCache[
			sectorIndex
		];
	}


	private SingularityActionResult NotEnoughMatter(
		double cost)
	{
		return new SingularityActionResult(
			false,
			"Need "
				+ NumberFormatter.Format(cost)
				+ " Singularity Matter."
		);
	}


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
