using System.Collections.Generic;

namespace IdleAi;

public sealed class RoomState
{
	public bool Unlocked { get; set; }

	public List<SlotData> Slots { get; } =
		[];
}
