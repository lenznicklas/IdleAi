namespace IdleAi;

public readonly record struct ManualStartResult(
	bool Started,
	string Message
);


public sealed class ProductionService
{
	private readonly GameState _state;

	private readonly EconomyService _economy;


	public ProductionService(
		GameState state,
		EconomyService economy)
	{
		_state =
			state;

		_economy =
			economy;
	}


	// ==================================================
	// PROCESS ALL MACHINES
	// ==================================================

	public double Update(
		double delta)
	{
		double earned =
			0.0;


		for (
			int roomIndex = 0;
			roomIndex < _state.RoomStates.Count;
			roomIndex++
		)
		{
			RoomState room =
				_state.RoomStates[
					roomIndex
				];


			if (!room.Unlocked)
				continue;


			foreach (
				SlotData slot
				in room.Slots
			)
			{
				if (!slot.Unlocked)
					continue;


				// Bots always make sure that
				// the machine is running.
				if (
					slot.HasBot
					&& !slot.IsRunning
				)
				{
					StartCycle(
						slot
					);
				}


				if (!slot.IsRunning)
					continue;


				slot.CycleRemaining -=
					delta;


				int safety =
					0;


				while (
					slot.CycleRemaining <= 0.0
					&& safety < 100
				)
				{
					safety++;


					earned +=
						_economy.GetCycleReward(
							roomIndex,
							slot
						);


					if (slot.HasBot)
					{
						// Start next automatic cycle.
						slot.CycleRemaining +=
							GameConfig.ProductionCycleSeconds;

						slot.IsRunning =
							true;
					}
					else
					{
						// Manual machine waits for
						// the user to press START again.
						slot.CycleRemaining =
							0.0;

						slot.IsRunning =
							false;

						break;
					}
				}
			}
		}


		return earned;
	}


	// ==================================================
	// MANUAL START
	// ==================================================

	public ManualStartResult TryStartManual(
		int roomIndex,
		int slotIndex)
	{
		if (
			roomIndex < 0
			|| roomIndex >= _state.RoomStates.Count
		)
		{
			return new ManualStartResult(
				false,
                "Invalid room."
			);
		}


		RoomState room =
			_state.RoomStates[
				roomIndex
			];


		if (
			slotIndex < 0
			|| slotIndex >= room.Slots.Count
		)
		{
			return new ManualStartResult(
				false,
                "Invalid machine."
			);
		}


		SlotData slot =
			room.Slots[
				slotIndex
			];


		if (!slot.Unlocked)
		{
			return new ManualStartResult(
				false,
                "Machine is locked."
			);
		}


		if (slot.HasBot)
		{
			return new ManualStartResult(
				false,
                "This machine is automated."
			);
		}


		if (slot.IsRunning)
		{
			return new ManualStartResult(
				false,
                "Machine is still running."
			);
		}


		StartCycle(
			slot
		);


		return new ManualStartResult(
			true,
            "Machine started."
		);
	}


	// ==================================================
	// AFTER LOADING
	// ==================================================

	public void PrepareAfterLoad()
	{
		foreach (
			RoomState room
			in _state.RoomStates
		)
		{
			foreach (
				SlotData slot
				in room.Slots
			)
			{
				if (
					!slot.Unlocked
					|| !slot.HasBot
				)
				{
					continue;
				}


				// Offline income was already calculated
				// separately, so start a fresh bot cycle.
				slot.IsRunning =
					true;


				slot.CycleRemaining =
					GameConfig.ProductionCycleSeconds;
			}
		}
	}


	private static void StartCycle(
		SlotData slot)
	{
		slot.IsRunning =
			true;


		slot.CycleRemaining =
			GameConfig.ProductionCycleSeconds;
	}
}
