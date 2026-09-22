using System;

namespace IdleAi;


public readonly record struct ManualStartResult(
	bool Started,
	string Message
);


public readonly record struct ProductionCompletionResult(
	int CompletedCycles,
	int ResearchPointsAwarded,
	double Earned
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


					ProductionCompletionResult result =
						CompleteCycle(
							roomIndex,
							slot,
							preserveOvershoot: true
						);


					earned +=
						result.Earned;


					/*
					 * Manual machines stop after one
					 * completed cycle.
					 */
					if (!slot.HasBot)
						break;
				}
			}
		}


		return earned;
	}


	// ==================================================
	// INSTANT COMPLETION
	// ==================================================

	/*
	 * Completes every production cycle that is currently
	 * running.
	 *
	 * This is used by the Shop's Instant Production item.
	 *
	 * It deliberately goes through the exact same
	 * CompleteCycle() method as natural cycle completion.
	 */
	public ProductionCompletionResult
		CompleteAllRunningCyclesInstantly()
	{
		int completedCycles =
			0;


		int researchPointsAwarded =
			0;


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
				if (
					!slot.Unlocked
					|| !slot.IsRunning
				)
				{
					continue;
				}


				ProductionCompletionResult result =
					CompleteCycle(
						roomIndex,
						slot,
						preserveOvershoot: false
					);


				completedCycles +=
					result.CompletedCycles;


				researchPointsAwarded +=
					result.ResearchPointsAwarded;


				earned +=
					result.Earned;
			}
		}


		return new ProductionCompletionResult(
			completedCycles,
			researchPointsAwarded,
			earned
		);
	}


	// ==================================================
	// COMPLETE CYCLE
	// ==================================================

	private ProductionCompletionResult CompleteCycle(
		int roomIndex,
		SlotData slot,
		bool preserveOvershoot)
	{
		if (
			!slot.Unlocked
			|| !slot.IsRunning
		)
		{
			return new ProductionCompletionResult(
				0,
				0,
				0.0
			);
		}


		// ==================================================
		// TOKENS
		// ==================================================

		double earned =
			_economy.GetCycleReward(
				roomIndex,
				slot
			);


		// ==================================================
		// RESEARCH POINT
		// ==================================================

		int researchPoints =
			TryAwardResearchPoint();


		// ==================================================
		// NEXT STATE
		// ==================================================

		if (slot.HasBot)
		{
			double duration =
				_economy.GetCycleDuration(
					slot
				);


			if (preserveOvershoot)
			{
				/*
				 * Example:
				 *
				 * Cycle was 0.05 seconds past completion.
				 *
				 * Instead of throwing that elapsed time
				 * away, the next cycle starts at:
				 *
				 * duration - 0.05
				 */
				slot.CycleRemaining +=
					duration;
			}
			else
			{
				/*
				 * Instant Production explicitly finishes
				 * the currently running cycle and begins
				 * a fresh one.
				 */
				slot.CycleRemaining =
					duration;
			}


			slot.IsRunning =
				true;
		}
		else
		{
			slot.CycleRemaining =
				0.0;


			slot.IsRunning =
				false;
		}


		return new ProductionCompletionResult(
			1,
			researchPoints,
			earned
		);
	}


	// ==================================================
	// RESEARCH POINT DROP
	// ==================================================

	private int TryAwardResearchPoint()
	{
		/*
		 * Do not accumulate Research Points before the
		 * Laboratory has been unlocked.
		 */
		if (!_state.Lab.Unlocked)
			return 0;


		double roll =
			Random.Shared.NextDouble();


		if (
			roll
			>= ResearchPointDropChance
		)
		{
			return 0;
		}


		_state.Lab.ResearchPoints +=
			ResearchPointDropAmount;


		return ResearchPointDropAmount;
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
