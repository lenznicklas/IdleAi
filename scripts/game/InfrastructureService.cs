using System;

namespace IdleAi;


public readonly record struct InfrastructureUpgradeResult(
	bool Changed,
	string Message
);


public sealed class InfrastructureService
{
	private readonly GameState _state;


	public InfrastructureService(
		GameState state)
	{
		_state =
			state;
	}


	public InfrastructureData GetInfrastructure()
	{
		return _state.RoomStates[
			GameConfig.InfrastructureRoomIndex
		].Infrastructure;
	}


	public int GetLevel(
		InfrastructureSystem system)
	{
		return GetInfrastructure()
			.GetLevel(
				system
			);
	}


	public double GetCapacity(
		InfrastructureSystem system)
	{
		return GetCapacity(
			GetInfrastructure(),
			system
		);
	}


	public static double GetCapacity(
		InfrastructureData infrastructure,
		InfrastructureSystem system)
	{
		int level =
			infrastructure.GetLevel(
				system
			);


		double baseCapacity =
			system switch
			{
				InfrastructureSystem.Power =>
					GameConfig.InfrastructureBasePowerCapacity,

				InfrastructureSystem.Cooling =>
					GameConfig.InfrastructureBaseCoolingCapacity,

				InfrastructureSystem.Storage =>
					GameConfig.InfrastructureBaseStorageCapacity,

				_ =>
					1.0
			};


		return baseCapacity
			* Math.Pow(
				GameConfig.InfrastructureCapacityGrowth,
				level - 1
			);
	}


	// ==================================================
	// LOAD
	// ==================================================

	public double GetCurrentLoad()
	{
		return GetCurrentLoad(
			_state,
			includeTemporaryShopBoost:
				true
		);
	}


	public double GetCurrentLoad(
		bool includeTemporaryShopBoost)
	{
		return GetCurrentLoad(
			_state,
			includeTemporaryShopBoost
		);
	}


	public static double GetCurrentLoad(
		GameState state,
		bool includeTemporaryShopBoost)
	{
		/*
		 * Kept in the method signature so all existing
		 * callers remain compatible.
		 *
		 * Shop/Research/Prestige/Bot multipliers do not
		 * change the physical infrastructure demand
		 * anymore.
		 */
		_ =
			includeTemporaryShopBoost;


		int roomIndex =
			GameConfig.InfrastructureRoomIndex;


		if (
			roomIndex < 0
			|| roomIndex >= state.RoomStates.Count
			|| !state.RoomStates[
				roomIndex
			].Unlocked
		)
		{
			return 0.0;
		}


		double physicalTokensPerSecond =
			0.0;


		RoomData roomData =
			state.Rooms[
				roomIndex
			];


		RoomState roomState =
			state.RoomStates[
				roomIndex
			];


		foreach (
			SlotData slot
			in roomState.Slots
		)
		{
			if (!slot.Unlocked)
				continue;


			MachineData machine =
				roomData.Machines[
					slot.MachineTier
				];


			double levelMultiplier =
				1.0
				+ (
					slot.MachineLevel
					- 1
				)
				* GameConfig.IncomePerLevel;


			double milestoneMultiplier =
				EconomyService
					.GetMilestoneMultiplier(
						slot.MachineLevel
					);


			/*
			 * Infrastructure demand represents installed
			 * hardware, not temporary/global production
			 * bonuses.
			 */
			physicalTokensPerSecond +=
				machine.BaseIncome
				* levelMultiplier
				* milestoneMultiplier;
		}


		return physicalTokensPerSecond
			/ GameConfig
				.InfrastructureTokensPerLoadUnit;
	}


	// ==================================================
	// TEMPERATURE
	// ==================================================

	public double GetTemperatureCelsius()
	{
		return GetTemperatureCelsius(
			_state,
			includeTemporaryShopBoost:
				true
		);
	}


	public static double GetTemperatureCelsius(
		GameState state,
		bool includeTemporaryShopBoost)
	{
		double load =
			GetCurrentLoad(
				state,
				includeTemporaryShopBoost
			);


		if (load <= 0.0)
		{
			return GameConfig
				.InfrastructureIdleTemperature;
		}


		InfrastructureData infrastructure =
			state.RoomStates[
				GameConfig.InfrastructureRoomIndex
			].Infrastructure;


		double coolingCapacity =
			GetCapacity(
				infrastructure,
				InfrastructureSystem.Cooling
			);


		if (coolingCapacity <= 0.0)
		{
			return GameConfig
				.InfrastructureMaximumTemperature;
		}


		double idealCapacity =
			load
			* GameConfig
				.InfrastructureCoolingIdealCapacityRatio;


		if (
			coolingCapacity
			>= idealCapacity
		)
		{
			return GameConfig
				.InfrastructureIdleTemperature;
		}


		double suppliedRatio =
			Math.Clamp(
				coolingCapacity
				/ idealCapacity,
				0.0,
				1.0
			);


		double shortage =
			1.0
			- suppliedRatio;


		double temperature =
			GameConfig.InfrastructureIdleTemperature
			+ shortage
			* (
				GameConfig.InfrastructureMaximumTemperature
				- GameConfig.InfrastructureIdleTemperature
			);


		return Math.Clamp(
			temperature,
			GameConfig.InfrastructureIdleTemperature,
			GameConfig.InfrastructureMaximumTemperature
		);
	}


	// ==================================================
	// PUBLIC EFFICIENCIES
	// ==================================================

	public double GetPowerEfficiency()
	{
		return 1.0
			- GetPowerPenalty(
				_state,
				includeTemporaryShopBoost:
					true
			);
	}


	public double GetStorageEfficiency()
	{
		return 1.0
			- GetStoragePenalty(
				_state,
				includeTemporaryShopBoost:
					true
			);
	}


	public double GetHeatEfficiency()
	{
		return 1.0
			- GetHeatPenalty(
				_state,
				includeTemporaryShopBoost:
					true
			);
	}


	public double GetProductionMultiplier()
	{
		return GetProductionMultiplierForRoom(
			_state,
			GameConfig.InfrastructureRoomIndex,
			includeTemporaryShopBoost:
				true
		);
	}


	// ==================================================
	// TOTAL PRODUCTION
	// ==================================================

	public static double GetProductionMultiplierForRoom(
		GameState state,
		int roomIndex,
		bool includeTemporaryShopBoost)
	{
		if (
			roomIndex
			!= GameConfig.InfrastructureRoomIndex
			|| roomIndex < 0
			|| roomIndex >= state.RoomStates.Count
			|| !state.RoomStates[
				roomIndex
			].Unlocked
		)
		{
			return 1.0;
		}


		double powerPenalty =
			GetPowerPenalty(
				state,
				includeTemporaryShopBoost
			);


		double storagePenalty =
			GetStoragePenalty(
				state,
				includeTemporaryShopBoost
			);


		double heatPenalty =
			GetHeatPenalty(
				state,
				includeTemporaryShopBoost
			);


		/*
		 * Penalties are deliberately additive.
		 *
		 * Old model:
		 * 0.8 * 0.8 * 0.7 = 44.8%
		 *
		 * New model:
		 * 100% - individual penalties
		 *
		 * This makes infrastructure management relevant
		 * without punishing the player exponentially.
		 */
		double totalPenalty =
			powerPenalty
			+ storagePenalty
			+ heatPenalty;


		return Math.Clamp(
			1.0
			- totalPenalty,
			GameConfig
				.InfrastructureMinimumProductionMultiplier,
			1.0
		);
	}


	// ==================================================
	// POWER
	// ==================================================

	private static double GetPowerPenalty(
		GameState state,
		bool includeTemporaryShopBoost)
	{
		double load =
			GetCurrentLoad(
				state,
				includeTemporaryShopBoost
			);


		if (load <= 0.0)
			return 0.0;


		double capacity =
			GetCapacity(
				state.RoomStates[
					GameConfig.InfrastructureRoomIndex
				].Infrastructure,
				InfrastructureSystem.Power
			);


		return CalculateCapacityPenalty(
			load,
			capacity,
			GameConfig
				.InfrastructureFullEfficiencyCapacityRatio,
			GameConfig
				.InfrastructureMaximumPowerPenalty
		);
	}


	// ==================================================
	// STORAGE
	// ==================================================

	private static double GetStoragePenalty(
		GameState state,
		bool includeTemporaryShopBoost)
	{
		double load =
			GetCurrentLoad(
				state,
				includeTemporaryShopBoost
			);


		if (load <= 0.0)
			return 0.0;


		double capacity =
			GetCapacity(
				state.RoomStates[
					GameConfig.InfrastructureRoomIndex
				].Infrastructure,
				InfrastructureSystem.Storage
			);


		return CalculateCapacityPenalty(
			load,
			capacity,
			GameConfig
				.InfrastructureFullEfficiencyCapacityRatio,
			GameConfig
				.InfrastructureMaximumStoragePenalty
		);
	}


	private static double CalculateCapacityPenalty(
		double load,
		double capacity,
		double fullEfficiencyCapacityRatio,
		double maximumPenalty)
	{
		if (load <= 0.0)
			return 0.0;


		double requiredForFullEfficiency =
			load
			* fullEfficiencyCapacityRatio;


		if (
			capacity
			>= requiredForFullEfficiency
		)
		{
			return 0.0;
		}


		if (
			requiredForFullEfficiency
			<= 0.0
		)
		{
			return 0.0;
		}


		double suppliedRatio =
			Math.Clamp(
				capacity
				/ requiredForFullEfficiency,
				0.0,
				1.0
			);


		double shortage =
			1.0
			- suppliedRatio;


		return shortage
			* maximumPenalty;
	}


	// ==================================================
	// HEAT
	// ==================================================

	private static double GetHeatPenalty(
		GameState state,
		bool includeTemporaryShopBoost)
	{
		double temperature =
			GetTemperatureCelsius(
				state,
				includeTemporaryShopBoost
			);


		if (temperature <= 55.0)
			return 0.0;


		if (temperature <= 70.0)
		{
			double t =
				(
					temperature
					- 55.0
				)
				/ 15.0;


			return 0.05
				* t;
		}


		if (temperature <= 85.0)
		{
			double t =
				(
					temperature
					- 70.0
				)
				/ 15.0;


			return 0.05
				+ 0.10
				* t;
		}


		double hot =
			Math.Clamp(
				(
					temperature
					- 85.0
				)
				/ (
					GameConfig
						.InfrastructureMaximumTemperature
					- 85.0
				),
				0.0,
				1.0
			);


		return 0.15
			+ (
				GameConfig
					.InfrastructureMaximumHeatPenalty
				- 0.15
			)
			* hot;
	}


	// ==================================================
	// UPGRADE COST
	// ==================================================

	public double GetUpgradeCost(
		InfrastructureSystem system)
	{
		int level =
			GetLevel(
				system
			);


		double baseCost =
			system switch
			{
				InfrastructureSystem.Power =>
					GameConfig.InfrastructurePowerBaseUpgradeCost,

				InfrastructureSystem.Cooling =>
					GameConfig.InfrastructureCoolingBaseUpgradeCost,

				InfrastructureSystem.Storage =>
					GameConfig.InfrastructureStorageBaseUpgradeCost,

				_ =>
					GameConfig.InfrastructurePowerBaseUpgradeCost
			};


		return baseCost
			* Math.Pow(
				GameConfig.InfrastructureUpgradeCostGrowth,
				level - 1
			);
	}


	// ==================================================
	// UPGRADE
	// ==================================================

	public InfrastructureUpgradeResult Upgrade(
		InfrastructureSystem system)
	{
		int roomIndex =
			GameConfig.InfrastructureRoomIndex;


		if (
			roomIndex < 0
			|| roomIndex >= _state.RoomStates.Count
			|| !_state.RoomStates[
				roomIndex
			].Unlocked
		)
		{
			return new InfrastructureUpgradeResult(
				false,
				"Unlock the Data Center first."
			);
		}


		InfrastructureData infrastructure =
			GetInfrastructure();


		int currentLevel =
			infrastructure.GetLevel(
				system
			);


		if (
			currentLevel
			>= GameConfig.InfrastructureMaxLevel
		)
		{
			return new InfrastructureUpgradeResult(
				false,
				GetSystemName(
					system
				)
				+ " is already at maximum level."
			);
		}


		double cost =
			GetUpgradeCost(
				system
			);


		if (_state.Tokens < cost)
		{
			return new InfrastructureUpgradeResult(
				false,
				"Not enough Tokens."
			);
		}


		_state.Tokens -=
			cost;


		_state.Stats.AddMachineSpending(
			"Data Center Infrastructure - "
			+ GetSystemName(
				system
			),
			cost
		);


		infrastructure.SetLevel(
			system,
			currentLevel + 1
		);


		return new InfrastructureUpgradeResult(
			true,
			GetSystemName(
				system
			)
			+ " upgraded to Level "
			+ (
				currentLevel + 1
			)
			+ "!"
		);
	}


	public static string GetSystemName(
		InfrastructureSystem system)
	{
		return system switch
		{
			InfrastructureSystem.Power =>
				"POWER",

			InfrastructureSystem.Cooling =>
				"COOLING",

			InfrastructureSystem.Storage =>
				"STORAGE",

			_ =>
				"INFRASTRUCTURE"
		};
	}
}
