using System.Collections.Generic;

namespace IdleAi;

public sealed class RoomState
{
	public bool Unlocked { get; set; }

	/*
	 * Unlike normal room progression this value survives Prestige.
	 * The player should receive the room Data-Shard reward only once.
	 */
	public bool DataShardUnlockRewardClaimed { get; set; }

	public PipelineData Pipeline { get; } = new();

	public InfrastructureData Infrastructure { get; } = new();

	public QuantumData Quantum { get; } = new();

	/// <summary>Resets normal progression; the permanent room reward claim survives.</summary>
	public void ResetForNewRun(bool isStartingRoom)
	{
		Unlocked = isStartingRoom;
		Pipeline.Reset();
		Infrastructure.Reset();
		Quantum.Reset();
		for (int slotIndex = 0; slotIndex < Slots.Count; slotIndex++)
			Slots[slotIndex].ResetForNewRun(isStartingRoom && slotIndex == 0);
	}

	public List<SlotData> Slots { get; } = [];
}
