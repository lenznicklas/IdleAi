using System;

namespace IdleAi;

public sealed class EconomyService
{
	private readonly GameState _state;

	public EconomyService(GameState state)
	{
		_state = state;
	}

	public double GetLevelUpgradeCost(
		int roomIndex,
		SlotData slot,
		int slotIndex)
	{
		return GetLevelUpgradeCostAtLevel(
			roomIndex,
			slot,
			slotIndex,
			slot.MachineLevel
		);
	}

	public double GetLevelUpgradeCostAtLevel(
		int roomIndex,
		SlotData slot,
		int slotIndex,
		int currentLevel)
	{
		MachineData machine = GetMachine(roomIndex, slot);

		double slotMultiplier =
			GameConfig.SlotUpgradeMultipliers[slotIndex];

		double levelMultiplier =
			Math.Pow(
				GameConfig.LevelCostGrowth,
				currentLevel - 1
			);

		double researchMultiplier =
			_state.Lab.GetMachineUpgradeCostMultiplier();

		return machine.BaseUpgradeCost
			* slotMultiplier
			* levelMultiplier
			* researchMultiplier;
	}

	public double GetLevelUpgradeCostForLevels(
		int roomIndex,
		SlotData slot,
		int slotIndex,
		int levelCount)
	{
		if (levelCount <= 0)
			return 0.0;

		MachineData machine =
			GetMachine(
				roomIndex,
				slot
			);

		int remaining =
			Math.Max(
				0,
				machine.MaxLevel
				- slot.MachineLevel
			);

		int actualLevels =
			Math.Min(
				levelCount,
				remaining
			);

		double total =
			0.0;

		for (
			int i = 0;
			i < actualLevels;
			i++
		)
		{
			int level =
				slot.MachineLevel
				+ i;

			total +=
				GetLevelUpgradeCostAtLevel(
					roomIndex,
					slot,
					slotIndex,
					level
				);
		}

		return total;
	}

	public int GetMaximumAffordableLevels(
		int roomIndex,
		SlotData slot,
		int slotIndex,
		double availableTokens)
	{
		MachineData machine =
			GetMachine(
				roomIndex,
				slot
			);

		int remaining =
			Math.Max(
				0,
				machine.MaxLevel
				- slot.MachineLevel
			);

		int levels =
			0;

		double total =
			0.0;

		for (
			int i = 0;
			i < remaining;
			i++
		)
		{
			int level =
				slot.MachineLevel
				+ i;

			double nextCost =
				GetLevelUpgradeCostAtLevel(
					roomIndex,
					slot,
					slotIndex,
					level
				);

			if (
				total + nextCost
				> availableTokens
			)
			{
				break;
			}

			total +=
				nextCost;

			levels++;
		}

		return levels;
	}

	public double GetTierUpgradeCost(
		int roomIndex,
		SlotData slot,
		int slotIndex)
	{
		MachineData machine =
			GetMachine(
				roomIndex,
				slot
			);

		double researchMultiplier =
			_state.Lab.GetMachineUpgradeCostMultiplier();

		return machine.TierUpgradeCost
			* GameConfig.SlotUpgradeMultipliers[
				slotIndex
			]
			* researchMultiplier;
	}

	public double GetBotCost(
		int roomIndex,
		SlotData slot,
		int slotIndex)
	{
		MachineData machine =
			GetMachine(
				roomIndex,
				slot
			);

		return machine.BaseUpgradeCost
			* GameConfig.BotBaseCostMultiplier
			* GameConfig.SlotUpgradeMultipliers[
				slotIndex
			];
	}

	public double GetBaseCycleDuration(
		SlotData slot)
	{
		return GameConfig
			.GetProductionCycleSeconds(
				slot.MachineTier
			);
	}

	public double GetCycleDuration(
		SlotData slot)
	{
		double baseDuration =
			GetBaseCycleDuration(
				slot
			);

		double duration =
			Math.Max(
				0.25,

				baseDuration
				* _state.Lab
					.GetCycleTimeMultiplier()
			);

		slot.RuntimeCycleDuration =
			duration;

		return duration;
	}

	private double GetMachineRewardBeforePipeline(
		int roomIndex,
		SlotData slot,
		bool includeTemporaryShopBoost)
	{
		if (!slot.Unlocked)
			return 0.0;

		MachineData machine =
			GetMachine(
				roomIndex,
				slot
			);

		double levelMultiplier =
			1.0
			+ (
				slot.MachineLevel
				- 1
			)
			* GameConfig.IncomePerLevel;

		double milestoneMultiplier =
			GetMilestoneMultiplier(
				slot.MachineLevel
			);

		double botMultiplier =
			BotCatalog.GetMultiplier(
				slot.BotRarity
			);

		if (slot.HasBot)
		{
			botMultiplier *=
				_state.Lab
					.GetBotPowerMultiplier();
		}

		double prestigeMultiplier =
			_state.Prestige
				.GetProductionMultiplier();

		double researchProductionMultiplier =
			_state.Lab
				.GetProductionMultiplier();

		double shopProductionMultiplier =
			includeTemporaryShopBoost
				? _state.Shop
					.GetProductionMultiplier()
				: 1.0
					+ _state.Shop
						.ProductionUpgradeLevel
					* GameConfig
						.ShopProductionUpgradeBonus;

		double infrastructureMultiplier =
			InfrastructureService
				.GetProductionMultiplierForRoom(
					_state,
					roomIndex,
					includeTemporaryShopBoost
				);


		double quantumMultiplier =
			QuantumService
				.GetProductionMultiplierForRoom(
					_state,
					roomIndex
				);

		double baseCycleDuration =
			GetBaseCycleDuration(
				slot
			);

		return machine.BaseIncome
			* baseCycleDuration
			* levelMultiplier
			* milestoneMultiplier
			* botMultiplier
			* prestigeMultiplier
			* researchProductionMultiplier
			* shopProductionMultiplier
			* infrastructureMultiplier
			* quantumMultiplier;
	}

	public double GetCycleReward(
		int roomIndex,
		SlotData slot)
	{
		if (
			roomIndex
			== GameConfig.PipelineRoomIndex
		)
		{
			return 0.0;
		}

		return GetMachineRewardBeforePipeline(
			roomIndex,
			slot,
			true
		);
	}

	private double GetCycleRewardWithoutTemporaryShopBoost(
		int roomIndex,
		SlotData slot)
	{
		if (
			roomIndex
			== GameConfig.PipelineRoomIndex
		)
		{
			return 0.0;
		}

		return GetMachineRewardBeforePipeline(
			roomIndex,
			slot,
			false
		);
	}

	public double GetPipelineInputCycleReward(
		int roomIndex,
		SlotData slot)
	{
		if (
			roomIndex
			!= GameConfig.PipelineRoomIndex
		)
		{
			return 0.0;
		}

		return GetMachineRewardBeforePipeline(
			roomIndex,
			slot,
			true
		)
		/ GameConfig.PipelineTokensPerOutputUnit;
	}

	public double GetPipelineInputCycleRewardWithoutTemporaryShopBoost(
		int roomIndex,
		SlotData slot)
	{
		if (
			roomIndex
			!= GameConfig.PipelineRoomIndex
		)
		{
			return 0.0;
		}

		return GetMachineRewardBeforePipeline(
			roomIndex,
			slot,
			false
		)
		/ GameConfig.PipelineTokensPerOutputUnit;
	}

	public double GetPipelineInputPerSecond(
		int roomIndex,
		SlotData slot)
	{
		double duration =
			GetCycleDuration(
				slot
			);

		if (duration <= 0.0)
			return 0.0;

		return GetPipelineInputCycleReward(
			roomIndex,
			slot
		)
		/ duration;
	}

	public double GetPipelineInputPerSecondWithoutTemporaryShopBoost(
		int roomIndex,
		SlotData slot)
	{
		double duration =
			GetCycleDuration(
				slot
			);

		if (duration <= 0.0)
			return 0.0;

		return GetPipelineInputCycleRewardWithoutTemporaryShopBoost(
			roomIndex,
			slot
		)
		/ duration;
	}

	public double GetSlotIncome(
		int roomIndex,
		SlotData slot)
	{
		double duration =
			GetCycleDuration(
				slot
			);

		if (duration <= 0.0)
			return 0.0;

		if (
			roomIndex
			== GameConfig.PipelineRoomIndex
		)
		{
			return GetPipelineInputPerSecond(
				roomIndex,
				slot
			)
			* GameConfig
				.PipelineTokensPerOutputUnit;
		}

		return GetCycleReward(
			roomIndex,
			slot
		)
		/ duration;
	}

	private double GetSlotIncomeWithoutTemporaryShopBoost(
		int roomIndex,
		SlotData slot)
	{
		double duration =
			GetCycleDuration(
				slot
			);

		if (duration <= 0.0)
			return 0.0;

		return GetCycleRewardWithoutTemporaryShopBoost(
			roomIndex,
			slot
		)
		/ duration;
	}

	public double GetRoomIncome(
		int roomIndex)
	{
		if (
			!_state.RoomStates[
				roomIndex
			].Unlocked
		)
		{
			return 0.0;
		}

		if (
			roomIndex
			== GameConfig.PipelineRoomIndex
		)
		{
			return GetPipelineSteadyTokenOutputPerSecond(
				includeTemporaryShopBoost:
					true
			);
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
				GetSlotIncome(
					roomIndex,
					slot
				);
		}

		return total;
	}

	private double GetRoomIncomeWithoutTemporaryShopBoost(
		int roomIndex)
	{
		if (
			!_state.RoomStates[
				roomIndex
			].Unlocked
		)
		{
			return 0.0;
		}

		if (
			roomIndex
			== GameConfig.PipelineRoomIndex
		)
		{
			return GetPipelineSteadyTokenOutputPerSecond(
				includeTemporaryShopBoost:
					false
			);
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
				GetSlotIncomeWithoutTemporaryShopBoost(
					roomIndex,
					slot
				);
		}

		return total;
	}

	private double GetPipelineSteadyTokenOutputPerSecond(
		bool includeTemporaryShopBoost)
	{
		int roomIndex =
			GameConfig.PipelineRoomIndex;

		double input =
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

			input +=
				includeTemporaryShopBoost
					? GetPipelineInputPerSecond(
						roomIndex,
						slot
					)
					: GetPipelineInputPerSecondWithoutTemporaryShopBoost(
						roomIndex,
						slot
					);
		}

		PipelineData pipeline =
			_state.RoomStates[
				roomIndex
			].Pipeline;

		double throughput =
			Math.Min(
				input,
				PipelineService.GetCapacity(
					pipeline,
					PipelineStage.Compute
				)
			);

		throughput =
			Math.Min(
				throughput,
				PipelineService.GetCapacity(
					pipeline,
					PipelineStage.Data
				)
			);

		throughput =
			Math.Min(
				throughput,
				PipelineService.GetCapacity(
					pipeline,
					PipelineStage.Model
				)
			);

		throughput =
			Math.Min(
				throughput,
				PipelineService.GetCapacity(
					pipeline,
					PipelineStage.Output
				)
			);

		return throughput
			* GameConfig
				.PipelineTokensPerOutputUnit;
	}

	public double GetTotalIncome()
	{
		double total =
			0.0;

		for (
			int roomIndex = 0;
			roomIndex < _state.Rooms.Count;
			roomIndex++
		)
		{
			total +=
				GetRoomIncome(
					roomIndex
				);
		}

		return total;
	}

	public double GetTotalIncomeWithoutTemporaryShopBoost()
	{
		double total =
			0.0;

		for (
			int roomIndex = 0;
			roomIndex < _state.Rooms.Count;
			roomIndex++
		)
		{
			total +=
				GetRoomIncomeWithoutTemporaryShopBoost(
					roomIndex
				);
		}

		return total;
	}

	public static double GetMilestoneMultiplier(
		int level)
	{
		double baseMultiplier;

		if (level >= 25)
			baseMultiplier = 8.0;
		else if (level >= 20)
			baseMultiplier = 5.0;
		else if (level >= 15)
			baseMultiplier = 3.0;
		else if (level >= 10)
			baseMultiplier = 2.0;
		else if (level >= 5)
			baseMultiplier = 1.5;
		else
			baseMultiplier = 1.0;


		/*
		 * Final-tier machines can level forever.
		 * After Level 25 they receive another x1.5
		 * milestone every 25 levels.
		 */
		if (level <= 25)
			return baseMultiplier;


		int extraMilestones =
			(level - 25)
			/ 25;


		return baseMultiplier
			* Math.Pow(
				1.5,
				extraMilestones
			);
	}


	public static string GetMilestoneText(
		int level)
	{
		if (level >= 25)
		{
			int nextMilestone =
				(
					level / 25
					+ 1
				)
				* 25;


			double nextMultiplier =
				GetMilestoneMultiplier(
					nextMilestone
				);


			return "Level "
				+ nextMilestone
				+ ": x"
				+ nextMultiplier.ToString(
					"0.##"
				);
		}


		if (level >= 20)
			return "Level 25: x8";

		if (level >= 15)
			return "Level 20: x5";

		if (level >= 10)
			return "Level 15: x3";

		if (level >= 5)
			return "Level 10: x2";

		return "Level 5: x1.5";
	}


	private MachineData GetMachine(
		int roomIndex,
		SlotData slot)
	{
		return _state.Rooms[
			roomIndex
		].Machines[
			slot.MachineTier
		];
	}
}
