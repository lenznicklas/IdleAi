using System;
using System.Collections.Generic;

namespace IdleAi;

public readonly record struct ManualStartResult(
	bool Started,
	string Message
);

public readonly record struct ProductionCompletionResult(
	int CompletedCycles,
	int ResearchPointsAwarded,
	double Earned,
	double PipelineInputProduced
);

public sealed class ProductionService
{
	/*
	 * Every press while a manual machine is already running
	 * advances the cycle by this amount.
	 */
	private const double ManualCycleAdvanceSeconds =
		0.5;

	/*
	 * The 0.5 second advance is not applied in one frame.
	 * It is spread over this short interval with SmoothStep,
	 * so the progress bar visibly moves forward instead of
	 * jumping.
	 */
	private const double ManualAdvanceAnimationSeconds =
		0.18;

	private const double ManualAdvanceEpsilon =
		0.0001;

	private readonly GameState _state;
	private readonly EconomyService _economy;
	private readonly PipelineService _pipeline;

	private readonly Dictionary<
		SlotData,
		List<ManualAdvanceAnimation>
	> _manualAdvanceAnimations =
		new();

	private sealed class ManualAdvanceAnimation
	{
		public ManualAdvanceAnimation(
			double amount)
		{
			Amount =
				amount;
		}

		public double Amount { get; }

		public double Elapsed { get; set; }

		public double Applied { get; set; }
	}

	public ProductionService(
		GameState state,
		EconomyService economy,
		PipelineService pipeline)
	{
		_state =
			state;

		_economy =
			economy;

		_pipeline =
			pipeline;
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
				{
					ClearManualAdvanceAnimations(
						slot
					);

					continue;
				}

				double manualAdvance =
					ConsumeManualAdvance(
						slot,
						delta
					);

				slot.CycleRemaining -=
					delta
					+ manualAdvance;

				int safety =
					0;

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

					if (!slot.HasBot)
						break;
				}
			}
		}

		return earned;
	}

	public ProductionCompletionResult
		CompleteAllRunningCyclesInstantly()
	{
		int completedCycles =
			0;

		int researchPointsAwarded =
			0;

		double earned =
			0.0;

		double pipelineInputProduced =
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

				pipelineInputProduced +=
					result.PipelineInputProduced;
			}
		}

		return new ProductionCompletionResult(
			completedCycles,
			researchPointsAwarded,
			earned,
			pipelineInputProduced
		);
	}

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
				0.0,
				0.0
			);
		}

		ClearManualAdvanceAnimations(
			slot
		);

		double earned =
			0.0;

		double pipelineInputProduced =
			0.0;

		if (
			roomIndex
			== GameConfig.PipelineRoomIndex
		)
		{
			pipelineInputProduced =
				_economy
					.GetPipelineInputCycleReward(
						roomIndex,
						slot
					);

			_pipeline.AddMachineInput(
				pipelineInputProduced
			);
		}
		else
		{
			earned =
				_economy.GetCycleReward(
					roomIndex,
					slot
				);
		}

		int researchPoints =
			TryAwardResearchPoint();

		if (slot.HasBot)
		{
			double duration =
				_economy.GetCycleDuration(
					slot
				);

			if (preserveOvershoot)
			{
				slot.CycleRemaining +=
					duration;
			}
			else
			{
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
			earned,
			pipelineInputProduced
		);
	}

	private int TryAwardResearchPoint()
	{
		if (!_state.Lab.Unlocked)
			return 0;

		double roll =
			Random.Shared.NextDouble();

		if (
			roll
			>= GameConfig.ResearchPointDropChance
		)
		{
			return 0;
		}

		_state.Lab.ResearchPoints +=
			GameConfig.ResearchPointDropAmount;

		return GameConfig.ResearchPointDropAmount;
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
			double queuedAdvance =
				GetQueuedManualAdvance(
					slot
				);

			double availableAdvance =
				Math.Max(
					0.0,
					slot.CycleRemaining
					- queuedAdvance
				);

			double advance =
				Math.Min(
					ManualCycleAdvanceSeconds,
					availableAdvance
				);

			if (
				advance
				<= ManualAdvanceEpsilon
			)
			{
				return new ManualStartResult(
					false,
					"Cycle is almost finished."
				);
			}

			QueueManualAdvance(
				slot,
				advance
			);

			return new ManualStartResult(
				true,
				"Cycle boosted +"
				+ advance.ToString("0.0")
				+ "s"
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
	// MANUAL CYCLE ADVANCE
	// ==================================================

	private void QueueManualAdvance(
		SlotData slot,
		double amount)
	{
		if (
			amount
			<= ManualAdvanceEpsilon
		)
		{
			return;
		}

		if (
			!_manualAdvanceAnimations
				.TryGetValue(
					slot,
					out List<ManualAdvanceAnimation>? animations
				)
		)
		{
			animations =
				new List<ManualAdvanceAnimation>();

			_manualAdvanceAnimations[
				slot
			] =
				animations;
		}

		animations.Add(
			new ManualAdvanceAnimation(
				amount
			)
		);
	}

	private double ConsumeManualAdvance(
		SlotData slot,
		double delta)
	{
		if (
			delta <= 0.0
			|| !_manualAdvanceAnimations
				.TryGetValue(
					slot,
					out List<ManualAdvanceAnimation>? animations
				)
			|| animations.Count == 0
		)
		{
			return 0.0;
		}

		double advanceThisFrame =
			0.0;

		for (
			int i = animations.Count - 1;
			i >= 0;
			i--
		)
		{
			ManualAdvanceAnimation animation =
				animations[
					i
				];

			double previousElapsed =
				animation.Elapsed;

			animation.Elapsed =
				Math.Min(
					ManualAdvanceAnimationSeconds,
					animation.Elapsed
					+ delta
				);

			double previousProgress =
				Math.Clamp(
					previousElapsed
					/ ManualAdvanceAnimationSeconds,
					0.0,
					1.0
				);

			double currentProgress =
				Math.Clamp(
					animation.Elapsed
					/ ManualAdvanceAnimationSeconds,
					0.0,
					1.0
				);

			double previousEased =
				SmoothStep(
					previousProgress
				);

			double currentEased =
				SmoothStep(
					currentProgress
				);

			double targetApplied =
				animation.Amount
				* currentEased;

			double newlyApplied =
				Math.Max(
					0.0,
					targetApplied
					- animation.Applied
				);

			animation.Applied =
				targetApplied;

			advanceThisFrame +=
				newlyApplied;

			if (
				currentProgress
				>= 1.0
			)
			{
				animations.RemoveAt(
					i
				);
			}
		}

		if (animations.Count == 0)
		{
			_manualAdvanceAnimations.Remove(
				slot
			);
		}

		return advanceThisFrame;
	}

	private double GetQueuedManualAdvance(
		SlotData slot)
	{
		if (
			!_manualAdvanceAnimations
				.TryGetValue(
					slot,
					out List<ManualAdvanceAnimation>? animations
				)
		)
		{
			return 0.0;
		}

		double remaining =
			0.0;

		foreach (
			ManualAdvanceAnimation animation
				in animations
		)
		{
			remaining +=
				Math.Max(
					0.0,
					animation.Amount
					- animation.Applied
				);
		}

		return remaining;
	}

	private void ClearManualAdvanceAnimations(
		SlotData slot)
	{
		_manualAdvanceAnimations.Remove(
			slot
		);
	}

	private static double SmoothStep(
		double value)
	{
		double t =
			Math.Clamp(
				value,
				0.0,
				1.0
			);

		return t
			* t
			* (
				3.0
				- 2.0 * t
			);
	}

	// ==================================================
	// LOAD
	// ==================================================

	public void PrepareAfterLoad()
	{
		_manualAdvanceAnimations.Clear();

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

				/*
				 * GetCycleDuration also restores RuntimeCycleDuration, but unlike
				 * the old implementation we keep a valid saved CycleRemaining.
				 */
				double duration =
					_economy.GetCycleDuration(
						slot
					);

				slot.IsRunning =
					true;

				if (
					!double.IsFinite(
						slot.CycleRemaining
					)
					|| slot.CycleRemaining <= 0.0
					|| slot.CycleRemaining > duration
				)
				{
					slot.CycleRemaining =
						duration;
				}
			}
		}
	}

	private void StartCycle(
		SlotData slot)
	{
		ClearManualAdvanceAnimations(
			slot
		);

		slot.IsRunning =
			true;

		slot.CycleRemaining =
			_economy.GetCycleDuration(
				slot
			);
	}
}
