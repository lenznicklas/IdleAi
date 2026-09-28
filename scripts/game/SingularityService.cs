using Godot;
using System;
using System.IO;
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

	public int CoreLevel =>
		_data.CoreLevel;

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

		EnsureSector(
			0
		);

		Save();
		Changed?.Invoke();

		return new SingularityActionResult(
			true,
			"SINGULARITY ONLINE"
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


	public double GetCoreUpgradeCost()
	{
		return 100.0
			* Math.Pow(
				2.15,
				Math.Max(
					0,
					CoreLevel - 1
				)
			);
	}


	public SingularityActionResult UpgradeCore()
	{
		double cost =
			GetCoreUpgradeCost();

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
				_data.CoreLevel + 1
			);

		SaveAndNotify();

		return new SingularityActionResult(
			true,
			"Singularity Core upgraded to Level "
				+ CoreLevel
				+ "."
		);
	}


	public bool IsNodeTypeUnlocked(
		SingularityNodeType type)
	{
		return type switch
		{
			SingularityNodeType.Compute =>
				true,

			SingularityNodeType.Amplifier =>
				CoreLevel >= 3,

			SingularityNodeType.Cooling =>
				CoreLevel >= 5,

			SingularityNodeType.Quantum =>
				CoreLevel >= 8,

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
				"CORE 3",

			SingularityNodeType.Cooling =>
				"CORE 5",

			SingularityNodeType.Quantum =>
				"CORE 8",

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
		SingularitySectorData current =
			GetCurrentSector();

		return current.Nodes.TrueForAll(
			node =>
				node.Type
				!= SingularityNodeType.Empty
		);
	}


	public SingularityActionResult UnlockNextSector()
	{
		if (!CanUnlockNextSector())
		{
			return new SingularityActionResult(
				false,
				"Fill all 8 nodes in this sector first."
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


	public double GetCoreOutputPerSecond()
	{
		return 0.05
			* Math.Pow(
				1.42,
				Math.Max(
					0,
					CoreLevel - 1
				)
			);
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

			double adjacencyMultiplier =
				1.0;

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

			adjacencyMultiplier *=
				GetAdjacentMultiplier(
					sector.Nodes[
						previous
					]
				);

			adjacencyMultiplier *=
				GetAdjacentMultiplier(
					sector.Nodes[
						next
					]
				);

			total +=
				baseOutput
				* adjacencyMultiplier
				* quantumMultiplier;
		}

		double coreMultiplier =
			1.0
			+ 0.10
			* Math.Max(
				0,
				CoreLevel - 1
			);

		return total
			* coreMultiplier;
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
			1.0
			+ 0.10
			* Math.Max(
				0,
				CoreLevel - 1
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
