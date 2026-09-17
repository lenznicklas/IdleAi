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
						slot.CycleRemaining +=
							_economy.GetCycleDuration(
								slot
							);


						slot.IsRunning =
							true;
					}
					else
					{
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


				slot.IsRunning =
					true;


				slot.CycleRemaining =
					_economy.GetCycleDuration(
						slot
					);
			}
		}
	}


	private void StartCycle(
		SlotData slot)
	{
		slot.IsRunning =
			true;


		slot.CycleRemaining =
			_economy.GetCycleDuration(
				slot
			);
	}
}
