using System.Collections.Generic;

namespace IdleAi;


public sealed class SaveGameData
{
	public int SaveVersion { get; set; }

	public double Tokens { get; set; }

	public long LastSaveUnix { get; set; }

	public double IncomePerSecond { get; set; }


	public List<SlotSaveData> Slots { get; set; } =
		[];


	public StatsSaveData Stats { get; set; } =
		new();
}


public sealed class SlotSaveData
{
	public bool Unlocked { get; set; }

	public int MachineTier { get; set; }

	public int MachineLevel { get; set; } =
		1;
}


public sealed class StatsSaveData
{
	public double TotalEarned { get; set; }

	public double OfflineEarned { get; set; }

	public double TotalSpent { get; set; }

	public double SlotUnlockSpent { get; set; }


	public Dictionary<string, double>
		MachineSpending
		{ get; set; } =
			[];
}
