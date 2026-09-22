using System.Collections.Generic;

namespace IdleAi;


public sealed class SaveGameData
{
	public int SaveVersion { get; set; }


	public double Tokens { get; set; }


	public double RunEarnedTokens { get; set; }


	public long LastSaveUnix { get; set; }


	public double IncomePerSecond { get; set; }


	public int CurrentRoomIndex { get; set; }


	public long AiCores { get; set; }


	public int PrestigeCount { get; set; }


	// ==================================================
	// LAB
	// ==================================================

	public bool LabUnlocked { get; set; }


	public double ResearchPoints { get; set; }


	public List<string> CompletedResearch { get; set; } =
		[];


	public string? ActiveResearchId { get; set; }


	public long ActiveResearchEndUnix { get; set; }


	// ==================================================
	// SHOP
	// ==================================================

	public double DataShards { get; set; }


	public long ShopProductionBoostEndUnix { get; set; }


	public long ShopBotLuckBoostEndUnix { get; set; }


	public int ShopProductionUpgradeLevel { get; set; }


	public int ShopOfflineUpgradeLevel { get; set; }


	// ==================================================
	// ROOMS
	// ==================================================

	public List<RoomSaveData> Rooms { get; set; } =
		[];


	public StatsSaveData Stats { get; set; } =
		new();
}


public sealed class RoomSaveData
{
	public bool Unlocked { get; set; }


	public bool DataShardUnlockRewardClaimed { get; set; }


	public PipelineSaveData? Pipeline { get; set; } =
		new();


	public List<SlotSaveData> Slots { get; set; } =
		[];
}


public sealed class PipelineSaveData
{
	public int ComputeLevel { get; set; } =
		1;


	public int DataLevel { get; set; } =
		1;


	public int ModelLevel { get; set; } =
		1;


	public int OutputLevel { get; set; } =
		1;


	public double RawInputBuffer { get; set; }


	public double ComputeBuffer { get; set; }


	public double DataBuffer { get; set; }


	public double ModelBuffer { get; set; }
}


public sealed class SlotSaveData
{
	public bool Unlocked { get; set; }


	public int MachineTier { get; set; }


	public int MachineLevel { get; set; } =
		1;


	public List<int> ClaimedDataShardMilestones { get; set; } =
		[];


	public BotRarity? BotRarity { get; set; }


	public double BotPurchasePrice { get; set; }


	public bool IsRunning { get; set; }


	public double CycleRemaining { get; set; }
}


public sealed class StatsSaveData
{
	public double TotalEarned { get; set; }


	public double OfflineEarned { get; set; }


	public double TotalSpent { get; set; }


	public double SlotUnlockSpent { get; set; }


	public Dictionary<string, double> MachineSpending
		{ get; set; } =
			[];
}
