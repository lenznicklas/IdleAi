using System.Collections.Generic;

namespace IdleAi;

public sealed class GameState
{
	public double Tokens { get; set; }


	public List<MachineData> Machines { get; }

	public List<SlotData> Slots { get; } =
		[];


	public StatsData Stats { get; } =
		new();


	public GameState(
		List<MachineData> machines)
	{
		Machines =
			machines;
	}
}
