using System;

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
	private const double ResearchPointDropChance =
		0.0025;


	private const int ResearchPointDropAmount =
		1;


	private readonly GameState _state;

	private readonly EconomyService _economy;

	private readonly PipelineService _pipeline;


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
			>= ResearchPointDropChance
		)
		{
			return 0;
		}


		_state.Lab.ResearchPoints +=
			ResearchPointDropAmount;


		return ResearchPointDropAmount;
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
