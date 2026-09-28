using Godot;
using System;
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

	private const long MaximumOfflineSeconds =
		7 * 24 * 60 * 60;

	private readonly JsonSerializerOptions _jsonOptions =
		new()
		{
			WriteIndented = true,
			PropertyNameCaseInsensitive = true
		};

	private SingularitySaveData _data =
		new();


	public bool Unlocked =>
		_data.Unlocked;

	public double Matter =>
		_data.Matter;

	/*
	 * "CoreLevel" is kept for save compatibility.
	 *
	 * Gameplay meaning:
	 * Core 1 is installed for free when Singularity starts.
	 * Core 2, Core 3, ... have to be purchased with Matter.
	 */
	public int CoreLevel =>
		Math.Max(
			1,
			_data.CoreLevel
		);

	public int CoreCount =>
		CoreLevel;

	public int CurrentSectorIndex =>
		_data.CurrentSectorIndex;

	public int SectorNumber =>
		_data.CurrentSectorIndex + 1;

	public int SectorCount =>
		_data.Sectors.Count;

	public event Action? Changed;


	public SingularityService()
	{
		Load();

		_data.CoreLevel =
			Math.Max(
				1,
				_data.CoreLevel
			);

		EnsureSector(
			0
		);

		ApplyOfflineIncome();
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

		if (output <= 0.0)
			return;

		_data.Matter +=
			output
			* delta;
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

		/*
		 * The very first Core is free. This also makes old/migrated saves safe
		 * if a zero somehow reaches the JSON.
		 */
		_data.CoreLevel =
			Math.Max(
				1,
				_data.CoreLevel
			);

		EnsureSector(
			0
		);

		SaveAndNotify();

		return new SingularityActionResult(
			true,
			"SINGULARITY ONLINE • CORE 1 INSTALLED"
		);
	}


	public SingularitySectorData GetCurrentSector()
	{
		EnsureSector(
			_data.CurrentSectorIndex
		);

		return _data.Sectors[
			_data.CurrentSectorIndex
		];
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
	// CORES
	// ==================================================

	public double GetNextCoreCost()
	{
		/*
		 * Core 1 costs nothing because it already exists.
		 * This is the purchase price for CoreCount + 1.
		 */
		return 100.0
			* Math.Pow(
				2.15,
				Math.Max(
					0,
					CoreCount - 1
				)
			);
	}


	/*
	 * Compatibility with the first draft.
	 */
	public double GetCoreUpgradeCost()
	{
		return GetNextCoreCost();
	}


	public SingularityActionResult BuyNextCore()
	{
		double cost =
			GetNextCoreCost();

		if (_data.Matter < cost)
		{
			return NotEnoughMatter(
				cost
			);
		}

		_data.Matter -=
			cost;

		_data.CoreLevel =
			Math.Min(
				int.MaxValue,
				CoreCount + 1
			);

		SaveAndNotify();

		return new SingularityActionResult(
			true,
			"Core "
				+ CoreCount
				+ " installed."
		);
	}


	/*
	 * Compatibility with the first draft.
	 */
	public SingularityActionResult UpgradeCore()
	{
		return BuyNextCore();
	}


	public double GetCoreOutputPerSecond()
	{
		return GetCoreOutputPerSecond(
			CoreCount
		);
	}


	public static double GetCoreOutputPerSecond(
		int coreCount)
	{
		return 0.05
			* Math.Pow(
				1.42,
				Math.Max(
					0,
					coreCount - 1
				)
			);
	}


	public double GetComputeCoreMultiplier()
	{
		return 1.0
			+ 0.10
			* Math.Max(
				0,
				CoreCount - 1
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
			SingularityNodeType.Compute =>
				true,

			SingularityNodeType.Amplifier =>
				true,

			SingularityNodeType.Cooling =>
				CoreCount >= 3,

			SingularityNodeType.Quantum =>
				CoreCount >= 5,

			_ =>
				false
		};
	}


	public string GetNodeUnlockText(
		SingularityNodeType type)
	{
		return type switch
		{
			SingularityNodeType.Compute =>
				"UNLOCKED",

			SingularityNodeType.Amplifier =>
				"UNLOCKED",

			SingularityNodeType.Cooling =>
				"CORE 3",

			SingularityNodeType.Quantum =>
				"CORE 5",

			_ =>
				""
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

		double sectorMultiplier =
			Math.Pow(
				3.0,
				Math.Max(
					0,
					sectorIndex
				)
			);

		double networkGrowth =
			Math.Pow(
				1.22,
				builtNodes
			);

		return baseCost
			* sectorMultiplier
			* networkGrowth;
	}


	public SingularityActionResult BuildNode(
		int sectorIndex,
		int nodeIndex,
		SingularityNodeType type)
	{
		if (
			type == SingularityNodeType.Empty
			|| !IsNodeTypeUnlocked(
				type
			)
		)
		{
			return new SingularityActionResult(
				false,
				type
					+ " is not unlocked yet."
			);
		}

		SingularityNodeData node =
			GetNode(
				sectorIndex,
				nodeIndex
			);

		if (
			node.Type
			!= SingularityNodeType.Empty
		)
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
		{
			return NotEnoughMatter(
				cost
			);
		}

		_data.Matter -=
			cost;

		node.Type =
			type;

		node.Level =
			1;

		node.InvestedMatter =
			cost;

		SaveAndNotify();

		return new SingularityActionResult(
			true,
			type
				+ " Node constructed."
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

		if (
			node.Type
			== SingularityNodeType.Empty
		)
		{
			return 0.0;
		}

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
				Math.Max(
					0,
					sectorIndex
				)
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

		if (
			node.Type
			== SingularityNodeType.Empty
		)
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
		{
			return NotEnoughMatter(
				cost
			);
		}

		_data.Matter -=
			cost;

		/*
		 * Saves created before sell support did not store InvestedMatter.
		 * Seed those Nodes with a conservative reconstruction before adding
		 * the new upgrade cost, so they can still be sold sensibly.
		 */
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

		if (
			node.Type
			== SingularityNodeType.Empty
		)
		{
			return 0.0;
		}

		double invested =
			node.InvestedMatter > 0.0
				? node.InvestedMatter
				: EstimateLegacyNodeInvestment(
					sectorIndex,
					node
				);

		return Math.Max(
			0.0,
			invested
				* NodeSellRefundFraction
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

		if (
			node.Type
			== SingularityNodeType.Empty
		)
		{
			return new SingularityActionResult(
				false,
				"This Node is already empty."
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

		SaveAndNotify();

		return new SingularityActionResult(
			true,
			soldType
				+ " Node sold for "
				+ NumberFormatter.Format(
					refund
				)
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
				Math.Max(
					0,
					sectorIndex
				)
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
					Math.Max(
						0,
						sectorIndex
					)
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
		/*
		 * The 2D Singularity map expands along a deterministic spiral. Expansion
		 * always continues from the newest/frontier Sector, independent of which
		 * Sector the player is currently looking at on the map.
		 */
		if (_data.Sectors.Count == 0)
			return false;

		SingularitySectorData frontier =
			_data.Sectors[
				_data.Sectors.Count - 1
			];

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

		/* Navigation focus is persisted without emitting a progression change. */
		Save();
	}


	public SingularityActionResult UnlockNextSector()
	{
		if (!CanUnlockNextSector())
		{
			return new SingularityActionResult(
				false,
				"Fill all 8 nodes in Sector "
					+ (GetFrontierSectorIndex() + 1)
					+ " first."
			);
		}

		int nextIndex =
			_data.Sectors.Count;

		double cost =
			GetNextSectorUnlockCost();

		if (_data.Matter < cost)
		{
			return NotEnoughMatter(
				cost
			);
		}

		_data.Matter -=
			cost;

		EnsureSector(
			nextIndex
		);

		_data.CurrentSectorIndex =
			nextIndex;

		SaveAndNotify();

		return new SingularityActionResult(
			true,
			"Sector "
				+ SectorNumber
				+ " opened."
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
	// OUTPUT
	// ==================================================

	public double GetTotalOutputPerSecond()
	{
		if (!Unlocked)
			return 0.0;

		double output =
			GetCoreOutputPerSecond();

		for (
			int sectorIndex = 0;
			sectorIndex < _data.Sectors.Count;
			sectorIndex++
		)
		{
			output +=
				GetSectorOutputPerSecond(
					sectorIndex
				);
		}

		return output;
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
			if (
				node.Type
				== SingularityNodeType.Quantum
			)
			{
				quantumMultiplier +=
					0.12
					* node.Level;
			}
		}

		double total =
			0.0;

		for (
			int i = 0;
			i < sector.Nodes.Count;
			i++
		)
		{
			SingularityNodeData node =
				sector.Nodes[
					i
				];

			if (
				node.Type
				!= SingularityNodeType.Compute
			)
			{
				continue;
			}

			double baseOutput =
				0.25
				* Math.Pow(
					1.58,
					Math.Max(
						0,
						node.Level - 1
					)
				);

			int previous =
				(
					i
					+ NodesPerSector
					- 1
				)
				% NodesPerSector;

			int next =
				(
					i + 1
				)
				% NodesPerSector;

			double adjacencyMultiplier =
				GetAdjacentMultiplier(
					sector.Nodes[
						previous
					]
				)
				* GetAdjacentMultiplier(
					sector.Nodes[
						next
					]
				);

			total +=
				baseOutput
				* adjacencyMultiplier
				* quantumMultiplier;
		}

		return total
			* GetComputeCoreMultiplier();
	}


	private static double GetAdjacentMultiplier(
		SingularityNodeData node)
	{
		return node.Type switch
		{
			SingularityNodeType.Amplifier =>
				1.0
				+ 0.15
				* node.Level,

			SingularityNodeType.Cooling =>
				1.0
				+ 0.10
				* node.Level,

			_ =>
				1.0
		};
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

		if (
			node.Type
			!= SingularityNodeType.Compute
		)
		{
			return 0.0;
		}

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
			);

		int previous =
			(
				nodeIndex
				+ NodesPerSector
				- 1
			)
			% NodesPerSector;

		int next =
			(
				nodeIndex + 1
			)
			% NodesPerSector;

		output *=
			GetAdjacentMultiplier(
				sector.Nodes[
					previous
				]
			);

		output *=
			GetAdjacentMultiplier(
				sector.Nodes[
					next
				]
			);

		double quantum =
			1.0;

		foreach (
			SingularityNodeData sectorNode
				in sector.Nodes
		)
		{
			if (
				sectorNode.Type
				== SingularityNodeType.Quantum
			)
			{
				quantum +=
					0.12
					* sectorNode.Level;
			}
		}

		output *=
			quantum;

		output *=
			GetComputeCoreMultiplier();

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
				if (
					node.Type
					!= SingularityNodeType.Empty
				)
				{
					count++;
				}
			}
		}

		return count;
	}


	// ==================================================
	// SAVE
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

		if (
			!Godot.FileAccess.FileExists(
				SavePath
			)
		)
		{
			EnsureSector(
				0
			);

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
			{
				_data =
					loaded;
			}
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

		_data.SaveVersion =
			Math.Max(
				2,
				_data.SaveVersion
			);

		_data.CoreLevel =
			Math.Max(
				1,
				_data.CoreLevel
			);

		_data.CurrentSectorIndex =
			Math.Max(
				0,
				_data.CurrentSectorIndex
			);

		EnsureSector(
			_data.CurrentSectorIndex
		);
	}


	private void ApplyOfflineIncome()
	{
		if (
			!Unlocked
			|| _data.LastSaveUnix <= 0
		)
		{
			return;
		}

		long now =
			GetUnixNow();

		long seconds =
			Math.Clamp(
				now - _data.LastSaveUnix,
				0,
				MaximumOfflineSeconds
			);

		if (seconds <= 0)
			return;

		double earned =
			GetTotalOutputPerSecond()
			* seconds
			* OfflineEfficiency;

		if (earned <= 0.0)
			return;

		_data.Matter +=
			earned;

		GD.Print(
			"Singularity offline matter: ",
			earned
		);
	}


	private void EnsureSector(
		int index)
	{
		index =
			Math.Max(
				0,
				index
			);

		while (
			_data.Sectors.Count
			<= index
		)
		{
			SingularitySectorData sector =
				new();

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
			while (
				sector.Nodes.Count
				< NodesPerSector
			)
			{
				sector.Nodes.Add(
					new SingularityNodeData()
				);
			}
		}
	}


	private SingularityActionResult NotEnoughMatter(
		double cost)
	{
		return new SingularityActionResult(
			false,
			"Need "
				+ NumberFormatter.Format(
					cost
				)
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
