using System;

namespace IdleAi;


public readonly record struct PipelineUpgradeResult(
	bool Changed,
	string Message
);


public sealed class PipelineService
{
	private static readonly PipelineStage[] ReverseStageOrder =
	[
		PipelineStage.Output,
		PipelineStage.Model,
		PipelineStage.Data,
		PipelineStage.Compute
	];


	private readonly GameState _state;

	private readonly EconomyService _economy;


	public PipelineService(
		GameState state,
		EconomyService economy)
	{
		_state =
			state;


		_economy =
			economy;
	}


	// ==================================================
	// ACCESS
	// ==================================================

	public PipelineData GetPipeline()
	{
		return _state.RoomStates[
			GameConfig.PipelineRoomIndex
		].Pipeline;
	}


	public int GetLevel(
		PipelineStage stage)
	{
		return GetPipeline()
			.GetLevel(
				stage
			);
	}


	public double GetCapacity(
		PipelineStage stage)
	{
		return GetCapacity(
			GetPipeline(),
			stage
		);
	}


	public static double GetCapacity(
		PipelineData pipeline,
		PipelineStage stage)
	{
		int level =
			pipeline.GetLevel(
				stage
			);


		return GameConfig.PipelineBaseCapacity
			* Math.Pow(
				GameConfig.PipelineCapacityGrowth,
				level - 1
			);
	}


	public double GetCycleDuration(
		PipelineStage stage)
	{
		return GameConfig
			.GetPipelineCycleSeconds(
				stage
			);
	}


	public double GetCycleRemaining(
		PipelineStage stage)
	{
		return GetPipeline()
			.GetCycleRemaining(
				stage
			);
	}


	public bool IsStageRunning(
		PipelineStage stage)
	{
		return GetCycleRemaining(
			stage
		)
		> 0.0;
	}


	public double GetBatchCapacity(
		PipelineStage stage)
	{
		return GetCapacity(
			stage
		)
		* GetCycleDuration(
			stage
		);
	}


	public double GetBufferBeforeStage(
		PipelineStage stage)
	{
		return GetPipeline()
			.GetBufferBeforeStage(
				stage
			);
	}


	// ==================================================
	// MACHINE INPUT
	// ==================================================

	public void AddMachineInput(
		double amount)
	{
		if (amount <= 0.0)
			return;


		GetPipeline()
			.AddRawInput(
				amount
			);
	}


	public double GetEstimatedMachineInputPerSecond(
		bool includeTemporaryShopBoost = true)
	{
		int roomIndex =
			GameConfig.PipelineRoomIndex;


		if (
			roomIndex < 0
			|| roomIndex >= _state.RoomStates.Count
			|| !_state.RoomStates[
				roomIndex
			].Unlocked
		)
		{
			return 0.0;
		}


		double total =
			0.0;


		foreach (
			SlotData slot
			in _state.RoomStates[
				roomIndex
			].Slots
		)
		{
			if (
				!slot.Unlocked
				|| !slot.HasBot
			)
			{
				continue;
			}


			total +=
				includeTemporaryShopBoost
					? _economy
						.GetPipelineInputPerSecond(
							roomIndex,
							slot
						)
					: _economy
						.GetPipelineInputPerSecondWithoutTemporaryShopBoost(
							roomIndex,
							slot
						);
		}


		return total;
	}


	// ==================================================
	// LIVE CYCLE PROCESSING
	// ==================================================

	/*
	 * Each stage now behaves like a real machine:
	 *
	 * - material arrives in front of the stage
	 * - a cycle starts
	 * - the progress timer counts down
	 * - only when the cycle finishes is a batch moved
	 *   to the next stage
	 *
	 * Stages are updated from Output backwards so one
	 * freshly completed batch cannot travel through
	 * several stages in the same frame.
	 */
	public double Update(
		double delta)
	{
		if (delta <= 0.0)
			return 0.0;


		int roomIndex =
			GameConfig.PipelineRoomIndex;


		if (
			roomIndex < 0
			|| roomIndex >= _state.RoomStates.Count
			|| !_state.RoomStates[
				roomIndex
			].Unlocked
		)
		{
			return 0.0;
		}


		PipelineData pipeline =
			GetPipeline();


		double tokens =
			0.0;


		foreach (
			PipelineStage stage
			in ReverseStageOrder
		)
		{
			tokens +=
				UpdateStage(
					pipeline,
					stage,
					delta
				);
		}


		pipeline.LastMachineInputPerSecond =
			GetEstimatedMachineInputPerSecond();


		pipeline.LastTokenOutputPerSecond =
			GetSteadyTokenOutputPerSecond(
				includeTemporaryShopBoost: true
			);


		return tokens;
	}


	private double UpdateStage(
		PipelineData pipeline,
		PipelineStage stage,
		double delta)
	{
		double remaining =
			pipeline.GetCycleRemaining(
				stage
			);


		double waiting =
			pipeline.GetBufferBeforeStage(
				stage
			);


		if (remaining <= 0.0)
		{
			if (waiting <= 0.0)
			{
				pipeline.SetCycleRemaining(
					stage,
					0.0
				);


				return 0.0;
			}


			remaining =
				GetCycleDuration(
					stage
				);
		}


		remaining -=
			delta;


		double tokens =
			0.0;


		int safety =
			0;


		while (
			remaining <= 0.0
			&& safety < 100
		)
		{
			safety++;


			double processed =
				CompleteStageCycle(
					pipeline,
					stage
				);


			if (
				stage
					== PipelineStage.Output
				&& processed > 0.0
			)
			{
				tokens +=
					processed
					* GameConfig
						.PipelineTokensPerOutputUnit;
			}


			double waitingAfter =
				pipeline.GetBufferBeforeStage(
					stage
				);


			if (waitingAfter <= 0.0)
			{
				remaining =
					0.0;


				break;
			}


			remaining +=
				GetCycleDuration(
					stage
				);
		}


		pipeline.SetCycleRemaining(
			stage,
			Math.Max(
				0.0,
				remaining
			)
		);


		return tokens;
	}

	private double CompleteStageCycle(
		PipelineData pipeline,
		PipelineStage stage)
	{
		double waiting =
			pipeline.GetBufferBeforeStage(
				stage
			);


		double processed =
			Math.Min(
				waiting,
				GetBatchCapacity(
					stage
				)
			);


		if (processed <= 0.0)
			return 0.0;


		switch (stage)
		{
			case PipelineStage.Compute:
				pipeline.RawInputBuffer -=
					processed;


				pipeline.ComputeBuffer +=
					processed;
				break;


			case PipelineStage.Data:
				pipeline.ComputeBuffer -=
					processed;


				pipeline.DataBuffer +=
					processed;
				break;


			case PipelineStage.Model:
				pipeline.DataBuffer -=
					processed;


				pipeline.ModelBuffer +=
					processed;
				break;


			case PipelineStage.Output:
				pipeline.ModelBuffer -=
					processed;
				break;
		}


		return processed;
	}


	// ==================================================
	// STEADY-STATE OUTPUT
	// ==================================================

	/*
	 * Used for the top bar and offline income.
	 *
	 * The real live pipeline runs in visible batches,
	 * but over a longer period the average throughput is
	 * still limited by:
	 *
	 * machine input
	 * and
	 * every stage's capacity per second.
	 */
	public double GetSteadyTokenOutputPerSecond(
		bool includeTemporaryShopBoost)
	{
		double input =
			GetEstimatedMachineInputPerSecond(
				includeTemporaryShopBoost
			);


		if (input <= 0.0)
			return 0.0;


		PipelineData pipeline =
			GetPipeline();


		double throughput =
			input;


		foreach (
			PipelineStage stage
			in Enum.GetValues<PipelineStage>()
		)
		{
			throughput =
				Math.Min(
					throughput,
					GetCapacity(
						pipeline,
						stage
					)
				);
		}


		return throughput
			* GameConfig
				.PipelineTokensPerOutputUnit;
	}


	// ==================================================
	// COST
	// ==================================================

	public double GetUpgradeCost(
		PipelineStage stage)
	{
		int level =
			GetLevel(
				stage
			);


		return GameConfig.PipelineBaseUpgradeCost
			* GameConfig
				.GetPipelineStageCostMultiplier(
					stage
				)
			* Math.Pow(
				GameConfig.PipelineUpgradeCostGrowth,
				level - 1
			);
	}


	// ==================================================
	// UPGRADE
	// ==================================================

	public PipelineUpgradeResult Upgrade(
		PipelineStage stage)
	{
		int roomIndex =
			GameConfig.PipelineRoomIndex;


		if (
			roomIndex < 0
			|| roomIndex >= _state.RoomStates.Count
			|| !_state.RoomStates[
				roomIndex
			].Unlocked
		)
		{
			return new PipelineUpgradeResult(
				false,
				"Unlock the Server Room first."
			);
		}


		PipelineData pipeline =
			GetPipeline();


		int currentLevel =
			pipeline.GetLevel(
				stage
			);



		double cost =
			GetUpgradeCost(
				stage
			);


		if (_state.Tokens < cost)
		{
			return new PipelineUpgradeResult(
				false,
				"Not enough Tokens."
			);
		}


		_state.Tokens -=
			cost;


		_state.Stats.AddMachineSpending(
			"Server Pipeline - "
			+ GetStageName(
				stage
			),
			cost
		);


		pipeline.SetLevel(
			stage,
			currentLevel + 1
		);


		return new PipelineUpgradeResult(
			true,
			GetStageName(
				stage
			)
			+ " upgraded to Level "
			+ (
				currentLevel + 1
			)
			+ "!"
		);
	}


	// ==================================================
	// TEXT
	// ==================================================

	public static string GetStageName(
		PipelineStage stage)
	{
		return stage switch
		{
			PipelineStage.Compute =>
				"COMPUTE",

			PipelineStage.Data =>
				"DATA PROCESSING",

			PipelineStage.Model =>
				"MODEL TRAINING",

			PipelineStage.Output =>
				"AI OUTPUT",

			_ =>
				"PIPELINE"
		};
	}
}
