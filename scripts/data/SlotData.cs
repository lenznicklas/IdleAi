namespace IdleAi;

public sealed class SlotData
{
	public bool Unlocked { get; set; }


	public int MachineTier { get; set; }


	public int MachineLevel { get; set; } =
		1;


	// ==================================================
	// BOT
	// ==================================================

	public BotRarity? BotRarity { get; set; }


	public double BotPurchasePrice { get; set; }


	public bool HasBot =>
		BotRarity.HasValue;


	// ==================================================
	// PRODUCTION CYCLE
	// ==================================================

	public bool IsRunning { get; set; }


	public double CycleRemaining { get; set; }


	// ==================================================
	// SAVE
	// ==================================================

	public SlotSaveData ToSaveData()
	{
		return new SlotSaveData
		{
			Unlocked =
				Unlocked,

			MachineTier =
				MachineTier,

			MachineLevel =
				MachineLevel,

			BotRarity =
				BotRarity,

			BotPurchasePrice =
				BotPurchasePrice,

			IsRunning =
				IsRunning,

			CycleRemaining =
				CycleRemaining
		};
	}


	public void LoadFromSaveData(
		SlotSaveData data)
	{
		Unlocked =
			data.Unlocked;


		MachineTier =
			data.MachineTier;


		MachineLevel =
			data.MachineLevel;


		BotRarity =
			data.BotRarity;


		BotPurchasePrice =
			data.BotPurchasePrice;


		IsRunning =
			data.IsRunning;


		CycleRemaining =
			data.CycleRemaining;
	}
}
