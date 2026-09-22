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


		double rawTokensPerSecond =
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


			double botMultiplier =
				BotCatalog.GetMultiplier(
					slot.BotRarity
				);


			if (slot.HasBot)
			{
				botMultiplier *=
					state.Lab
						.GetBotPowerMultiplier();
			}


			double prestigeMultiplier =
				state.Prestige
					.GetProductionMultiplier();


			double researchProductionMultiplier =
				state.Lab
					.GetProductionMultiplier();


			double shopProductionMultiplier =
				includeTemporaryShopBoost
					? state.Shop
						.GetProductionMultiplier()
					: 1.0
						+ state.Shop
							.ProductionUpgradeLevel
						* GameConfig
							.ShopProductionUpgradeBonus;


			double cycleTimeMultiplier =
				state.Lab
					.GetCycleTimeMultiplier();


			double speedMultiplier =
				cycleTimeMultiplier > 0.0
					? 1.0
						/ cycleTimeMultiplier
					: 1.0;


			rawTokensPerSecond +=
				machine.BaseIncome
				* levelMultiplier
				* milestoneMultiplier
				* botMultiplier
				* prestigeMultiplier
				* researchProductionMultiplier
				* shopProductionMultiplier
				* speedMultiplier;
		}


		return rawTokensPerSecond
			/ GameConfig
				.InfrastructureTokensPerLoadUnit;
	}


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


		double ratio =
			load
			/ coolingCapacity;


		double temperature;


		if (ratio <= 0.70)
		{
			temperature =
				GameConfig
					.InfrastructureIdleTemperature;
		}
		else
		{
			temperature =
				GameConfig
					.InfrastructureIdleTemperature
				+ (
					ratio - 0.70
				)
				* 55.0;
		}


		return Math.Clamp(
			temperature,
			GameConfig.InfrastructureIdleTemperature,
			GameConfig.InfrastructureMaximumTemperature
		);
	}


	public double GetPowerEfficiency()
	{
		return GetPowerEfficiency(
			_state,
			includeTemporaryShopBoost:
				true
		);
	}


	public double GetStorageEfficiency()
	{
		return GetStorageEfficiency(
			_state,
			includeTemporaryShopBoost:
				true
		);
	}


	public double GetHeatEfficiency()
	{
		return GetHeatEfficiency(
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


		double power =
			GetPowerEfficiency(
				state,
				includeTemporaryShopBoost
			);


		double storage =
			GetStorageEfficiency(
				state,
				includeTemporaryShopBoost
			);


		double heat =
			GetHeatEfficiency(
				state,
				includeTemporaryShopBoost
			);


		return Math.Clamp(
			power
			* storage
			* heat,
			GameConfig
				.InfrastructureMinimumProductionMultiplier,
			1.0
		);
	}


	private static double GetPowerEfficiency(
		GameState state,
		bool includeTemporaryShopBoost)
	{
		double load =
			GetCurrentLoad(
				state,
				includeTemporaryShopBoost
			);


		if (load <= 0.0)
			return 1.0;


		double capacity =
			GetCapacity(
				state.RoomStates[
					GameConfig.InfrastructureRoomIndex
				].Infrastructure,
				InfrastructureSystem.Power
			);


		double ratio =
			Math.Clamp(
				capacity / load,
				0.0,
				1.0
			);


		return GameConfig
			.InfrastructurePowerMinimumEfficiency
			+ (
				1.0
				- GameConfig
					.InfrastructurePowerMinimumEfficiency
			)
			* ratio;
	}


	private static double GetStorageEfficiency(
		GameState state,
		bool includeTemporaryShopBoost)
	{
		double load =
			GetCurrentLoad(
				state,
				includeTemporaryShopBoost
			);


		if (load <= 0.0)
			return 1.0;


		double capacity =
			GetCapacity(
				state.RoomStates[
					GameConfig.InfrastructureRoomIndex
				].Infrastructure,
				InfrastructureSystem.Storage
			);


		double ratio =
			Math.Clamp(
				capacity / load,
				0.0,
				1.0
			);


		return GameConfig
			.InfrastructureStorageMinimumEfficiency
			+ (
				1.0
				- GameConfig
					.InfrastructureStorageMinimumEfficiency
			)
			* ratio;
	}


	private static double GetHeatEfficiency(
		GameState state,
		bool includeTemporaryShopBoost)
	{
		double temperature =
			GetTemperatureCelsius(
				state,
				includeTemporaryShopBoost
			);


		if (temperature <= 55.0)
			return 1.0;


		if (temperature <= 70.0)
		{
			double t =
				(
					temperature
					- 55.0
				)
				/ 15.0;


			return 1.0
				- 0.10
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


			return 0.90
				- 0.20
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


		return 0.70
			- 0.20
			* hot;
	}


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
