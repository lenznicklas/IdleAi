using System;

namespace IdleAi;

public readonly record struct PipelineUpgradeResult(
	bool Changed,
	string Message
);

public sealed class PipelineService
{
	private readonly GameState _state;

	public PipelineService(GameState state)
	{
		_state = state;
	}

	public PipelineData GetPipeline()
	{
		return _state.RoomStates[GameConfig.PipelineRoomIndex].Pipeline;
	}

	public int GetLevel(PipelineStage stage)
	{
		return GetPipeline().GetLevel(stage);
	}

	public double GetCapacity(PipelineStage stage)
	{
		return GetCapacity(GetPipeline(), stage);
	}

	public static double GetCapacity(PipelineData pipeline, PipelineStage stage)
	{
		int level = pipeline.GetLevel(stage);

		return GameConfig.PipelineBaseCapacity
			* Math.Pow(
				GameConfig.PipelineCapacityGrowth,
				level - 1
			);
	}

	public PipelineStage GetBottleneck()
	{
		return GetBottleneck(GetPipeline());
	}

	public static PipelineStage GetBottleneck(PipelineData pipeline)
	{
		PipelineStage bottleneck = PipelineStage.Compute;
		double minimum = GetCapacity(pipeline, bottleneck);

		foreach (PipelineStage stage in Enum.GetValues<PipelineStage>())
		{
			double capacity = GetCapacity(pipeline, stage);

			if (capacity < minimum)
			{
				minimum = capacity;
				bottleneck = stage;
			}
		}

		return bottleneck;
	}

	public double GetEfficiency()
	{
		return GetEfficiency(GetPipeline());
	}

	public static double GetEfficiency(PipelineData pipeline)
	{
		double minimum = double.MaxValue;
		double maximum = 0.0;

		foreach (PipelineStage stage in Enum.GetValues<PipelineStage>())
		{
			double capacity = GetCapacity(pipeline, stage);
			minimum = Math.Min(minimum, capacity);
			maximum = Math.Max(maximum, capacity);
		}

		if (maximum <= 0.0)
			return 1.0;

		return Math.Clamp(minimum / maximum, 0.0, 1.0);
	}

	public double GetProductionMultiplier()
	{
		return CalculateProductionMultiplier(GetPipeline());
	}

	public static double GetProductionMultiplierForRoom(
		GameState state,
		int roomIndex)
	{
		if (
			roomIndex != GameConfig.PipelineRoomIndex
			|| roomIndex < 0
			|| roomIndex >= state.RoomStates.Count
			|| !state.RoomStates[roomIndex].Unlocked
		)
		{
			return 1.0;
		}

		return CalculateProductionMultiplier(
			state.RoomStates[roomIndex].Pipeline
		);
	}

	public static double CalculateProductionMultiplier(PipelineData pipeline)
	{
		double total = 0.0;

		foreach (PipelineStage stage in Enum.GetValues<PipelineStage>())
		{
			total += GetCapacity(pipeline, stage);
		}

		double averageCapacity = total / 4.0;
		double averageMultiplier =
			averageCapacity / GameConfig.PipelineBaseCapacity;

		double efficiency = GetEfficiency(pipeline);
		double weight = GameConfig.PipelineBalanceWeight;

		double balanceMultiplier =
			1.0 - weight + efficiency * weight;

		return Math.Max(
			1.0,
			averageMultiplier * balanceMultiplier
		);
	}

	public double GetUpgradeCost(PipelineStage stage)
	{
		int level = GetLevel(stage);

		return GameConfig.PipelineBaseUpgradeCost
			* GameConfig.GetPipelineStageCostMultiplier(stage)
			* Math.Pow(
				GameConfig.PipelineUpgradeCostGrowth,
				level - 1
			);
	}

	public PipelineUpgradeResult Upgrade(PipelineStage stage)
	{
		int roomIndex = GameConfig.PipelineRoomIndex;

		if (
			roomIndex < 0
			|| roomIndex >= _state.RoomStates.Count
			|| !_state.RoomStates[roomIndex].Unlocked
		)
		{
			return new PipelineUpgradeResult(
				false,
				"Unlock the Server Room first."
			);
		}

		PipelineData pipeline = GetPipeline();
		int currentLevel = pipeline.GetLevel(stage);

		if (currentLevel >= GameConfig.PipelineMaxLevel)
		{
			return new PipelineUpgradeResult(
				false,
				GetStageName(stage) + " is already at maximum level."
			);
		}

		double cost = GetUpgradeCost(stage);

		if (_state.Tokens < cost)
		{
			return new PipelineUpgradeResult(
				false,
				"Not enough Tokens."
			);
		}

		_state.Tokens -= cost;

		_state.Stats.AddMachineSpending(
			"Server Pipeline - " + GetStageName(stage),
			cost
		);

		pipeline.SetLevel(stage, currentLevel + 1);

		return new PipelineUpgradeResult(
			true,
			GetStageName(stage)
			+ " upgraded to Level "
			+ (currentLevel + 1)
			+ "! Pipeline x"
			+ GetProductionMultiplier().ToString("0.00")
		);
	}

	public static string GetStageName(PipelineStage stage)
	{
		return stage switch
		{
			PipelineStage.Compute => "COMPUTE",
			PipelineStage.Data => "DATA PROCESSING",
			PipelineStage.Model => "MODEL TRAINING",
			PipelineStage.Output => "AI OUTPUT",
			_ => "PIPELINE"
		};
	}

	public static string GetStageShortName(PipelineStage stage)
	{
		return stage switch
		{
			PipelineStage.Compute => "COMPUTE",
			PipelineStage.Data => "DATA",
			PipelineStage.Model => "MODEL",
			PipelineStage.Output => "OUTPUT",
			_ => "PIPELINE"
		};
	}
}
