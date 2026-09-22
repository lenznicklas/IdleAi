using System;

namespace IdleAi;


public readonly record struct PipelineUpgradeResult(
	bool Changed,
	string Message
);


public sealed class PipelineService
{
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
	// LIVE PROCESSING
	// ==================================================

	/*
	 * The pipeline is intentionally processed from the
	 * end backwards.
	 *
	 * That prevents newly processed material from
	 * instantly crossing all four stages in one frame.
	 *
	 * Machines
	 * -> raw input
	 * -> compute
	 * -> data
	 * -> model
	 * -> output
	 * -> Tokens
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


		double outputProcessed =
			ProcessOutput(
				pipeline,
				delta
			);


		ProcessModel(
			pipeline,
			delta
		);


		ProcessData(
			pipeline,
			delta
		);


		ProcessCompute(
			pipeline,
			delta
		);


		double tokens =
			outputProcessed
			* GameConfig
				.PipelineTokensPerOutputUnit;


		pipeline.LastMachineInputPerSecond =
			GetEstimatedMachineInputPerSecond();


		pipeline.LastTokenOutputPerSecond =
			delta > 0.0
				? tokens / delta
				: 0.0;


		return tokens;
	}


	private static void ProcessCompute(
		PipelineData pipeline,
		double delta)
	{
		double amount =
			Math.Min(
				pipeline.RawInputBuffer,
				GetCapacity(
					pipeline,
					PipelineStage.Compute
				)
				* delta
			);


		if (amount <= 0.0)
			return;


		pipeline.RawInputBuffer -=
			amount;


		pipeline.ComputeBuffer +=
			amount;
	}


	private static void ProcessData(
		PipelineData pipeline,
		double delta)
	{
		double amount =
			Math.Min(
				pipeline.ComputeBuffer,
				GetCapacity(
					pipeline,
					PipelineStage.Data
				)
				* delta
			);


		if (amount <= 0.0)
			return;


		pipeline.ComputeBuffer -=
			amount;


		pipeline.DataBuffer +=
			amount;
	}


	private static void ProcessModel(
		PipelineData pipeline,
		double delta)
	{
		double amount =
			Math.Min(
				pipeline.DataBuffer,
				GetCapacity(
					pipeline,
					PipelineStage.Model
				)
				* delta
			);


		if (amount <= 0.0)
			return;


		pipeline.DataBuffer -=
			amount;


		pipeline.ModelBuffer +=
			amount;
	}


	private static double ProcessOutput(
		PipelineData pipeline,
		double delta)
	{
		double amount =
			Math.Min(
				pipeline.ModelBuffer,
				GetCapacity(
					pipeline,
					PipelineStage.Output
				)
				* delta
			);


		if (amount <= 0.0)
			return 0.0;


		pipeline.ModelBuffer -=
			amount;


		return amount;
	}


	// ==================================================
	// STEADY-STATE OUTPUT
	// ==================================================

	/*
	 * Used for income display and offline income.
	 *
	 * Only automated machines are included because
	 * manual machines do not continuously produce while
	 * the player is away.
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
			Math.Min(
				input,
				GetCapacity(
					pipeline,
					PipelineStage.Compute
				)
			);


		throughput =
			Math.Min(
				throughput,
				GetCapacity(
					pipeline,
					PipelineStage.Data
				)
			);


		throughput =
			Math.Min(
				throughput,
				GetCapacity(
					pipeline,
					PipelineStage.Model
				)
			);


		throughput =
			Math.Min(
				throughput,
				GetCapacity(
					pipeline,
					PipelineStage.Output
				)
			);


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


		if (
			currentLevel
			>= GameConfig.PipelineMaxLevel
		)
		{
			return new PipelineUpgradeResult(
				false,
				GetStageName(
					stage
				)
				+ " is already at maximum level."
			);
		}


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
