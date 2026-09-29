using System.Collections.Generic;

namespace IdleAi;


public enum SingularityNodeType
{
	Empty = 0,
	Compute = 1,
	Amplifier = 2,
	Cooling = 3,
	Quantum = 4
}


public sealed class SingularityNodeData
{
	public SingularityNodeType Type { get; set; }

	public int Level { get; set; } = 1;

	/*
	 * Tracks the real Matter invested into this Node so selling can refund
	 * exactly one third of the player's investment. Old saves simply load 0
	 * here and use the safe legacy estimate in SingularityService.
	 */
	public double InvestedMatter { get; set; }
}


public sealed class SingularitySectorData
{
	public List<SingularityNodeData> Nodes { get; set; } = [];

	/*
	 * Every Sector now owns its own Singularity Core.
	 *
	 * Sector 1 starts unlocked at Level 1. New Sectors are created with a
	 * locked Core and the player buys that Core with Singularity Matter.
	 */
	public bool CoreUnlocked { get; set; }

	public int CoreLevel { get; set; } = 1;
}


public sealed class SingularitySaveData
{
	public int SaveVersion { get; set; } = 3;

	public bool Unlocked { get; set; }

	public double Matter { get; set; }

	/*
	 * Legacy field retained only so v1/v2 Singularity saves migrate without
	 * losing their original Core progression. In v3 the real Core state lives
	 * inside each SingularitySectorData.
	 */
	public int CoreLevel { get; set; } = 1;

	public int CurrentSectorIndex { get; set; }

	public long LastSaveUnix { get; set; }

	public List<SingularitySectorData> Sectors { get; set; } = [];
}
