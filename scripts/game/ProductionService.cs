using System;

namespace IdleAi;


public readonly record struct ManualStartResult(
	bool Started,
	string Message
);


public sealed class ProductionService
{
	// ==================================================
	// RESEARCH POINT DROPS
	// ==================================================

	/*
	 * 0.0025 = 0.25 %
	 *
	 * Every completed production cycle has a
	 * small chance to generate one Research Point.
	 *
	 * Research Points only drop after the
	 * Laboratory has been unlocked.
	 */

	private const double ResearchPointDropChance =
		0.0025;


	private const int ResearchPointDropAmount =
		1;


	// ==================================================
	// SERVICES
	// ==================================================

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
	// UPDATE
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


				// ==================================================
				// AUTO START BOT MACHINES
				// ==================================================

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


				/*
				 * More than one cycle may complete in one
				 * frame, for example after a frame spike.
				 */

				while (
					slot.CycleRemaining <= 0.0
					&& safety < 100
				)
				{
					safety++;


					// ==================================================
					// TOKEN REWARD
					// ==================================================

					earned +=
						_economy.GetCycleReward(
							roomIndex,
							slot
						);


					// ==================================================
					// RESEARCH POINT DROP
					// ==================================================

					TryAwardResearchPoint();


					// ==================================================
					// NEXT CYCLE
					// ==================================================

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


	// ==================================================
	// RESEARCH POINT DROP
	// ==================================================

	private void TryAwardResearchPoint()
	{
		/*
		 * Do not secretly accumulate Research Points
		 * before the player has unlocked the lab.
		 */

		if (!_state.Lab.Unlocked)
			return;


		double roll =
			Random.Shared.NextDouble();


		if (
			roll
			>= ResearchPointDropChance
		)
		{
			return;
		}


		_state.Lab.ResearchPoints +=
			ResearchPointDropAmount;
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
	// LOAD
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


				slot.IsRunning =
					true;


				slot.CycleRemaining =
					_economy.GetCycleDuration(
						slot
					);
			}
		}
	}


	// ==================================================
	// START CYCLE
	// ==================================================

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
