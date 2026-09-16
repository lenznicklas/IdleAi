namespace IdleAi;

public sealed class SlotData
{
	public bool Unlocked { get; set; }

	public int MachineTier { get; set; }

	public int MachineLevel { get; set; } =
		1;


	public SlotSaveData ToSaveData()
	{
		return new SlotSaveData
		{
			Unlocked =
				Unlocked,

			MachineTier =
				MachineTier,

			MachineLevel =
				MachineLevel
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
	}
}
