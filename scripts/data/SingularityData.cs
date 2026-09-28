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
}


public sealed class SingularitySectorData
{
	public List<SingularityNodeData> Nodes { get; set; } = [];
}


public sealed class SingularitySaveData
{
	public int SaveVersion { get; set; } = 1;

	public bool Unlocked { get; set; }

	public double Matter { get; set; }

	public int CoreLevel { get; set; } = 1;

	public int CurrentSectorIndex { get; set; }

	public long LastSaveUnix { get; set; }

	public List<SingularitySectorData> Sectors { get; set; } = [];
}
