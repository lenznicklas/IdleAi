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

	public List<SlotData> Slots { get; } = [];
}
