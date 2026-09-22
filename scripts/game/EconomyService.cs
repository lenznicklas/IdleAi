using System;

namespace IdleAi;

public sealed class EconomyService
{
	private readonly GameState _state;


	public EconomyService(
		GameState state)
	{
		_state =
			state;
	}


	// ==================================================
	// LEVEL UPGRADE COST
	// ==================================================

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
		MachineData machine =
			GetMachine(
				roomIndex,
				slot
			);


		double slotMultiplier =
			GameConfig.SlotUpgradeMultipliers[
				slotIndex
			];


		double levelMultiplier =
			Math.Pow(
				GameConfig.LevelCostGrowth,
				currentLevel - 1
			);


		double researchMultiplier =
			_state.Lab
				.GetMachineUpgradeCostMultiplier();


		return machine.BaseUpgradeCost
			* slotMultiplier
			* levelMultiplier
			* researchMultiplier;
	}


	// ==================================================
	// MULTI LEVEL UPGRADE COST
	// ==================================================

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


	// ==================================================
	// TIER UPGRADE COST
	// ==================================================

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
			_state.Lab
				.GetMachineUpgradeCostMultiplier();


		return machine.TierUpgradeCost
			* GameConfig.SlotUpgradeMultipliers[
				slotIndex
			]
			* researchMultiplier;
	}


	// ==================================================
	// BOT COST
	// ==================================================

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


	// ==================================================
	// CYCLE
	// ==================================================

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


	// ==================================================
	// REWARD
	// ==================================================

	public double GetCycleReward(
		int roomIndex,
		SlotData slot)
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
			_state.Shop
				.GetProductionMultiplier();


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
			* shopProductionMultiplier;
	}


	// ==================================================
	// REWARD WITHOUT TEMPORARY SHOP BOOST
	// ==================================================

	/*
	 * Used for offline-income snapshots.
	 *
	 * The normal GetCycleReward() contains both:
	 *
	 * - permanent Shop production upgrades
	 * - temporary 2x production boost
	 *
	 * For offline income we must save the permanent/base
	 * production separately and apply the temporary boost
	 * only to the time during which it was actually active.
	 */
	private double GetCycleRewardWithoutTemporaryShopBoost(
		int roomIndex,
		SlotData slot)
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


		/*
		 * Keep the permanent Shop production upgrade.
		 *
		 * Only the temporary 2x multiplier is excluded.
		 */
		double permanentShopProductionMultiplier =
			1.0
			+ _state.Shop.ProductionUpgradeLevel
			* GameConfig.ShopProductionUpgradeBonus;


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
			* permanentShopProductionMultiplier;
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


	// ==================================================
	// AUTOMATED INCOME
	// ==================================================

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


	/*
	 * Offline income must be based on this value.
	 *
	 * Temporary boosts are calculated separately using
	 * their real expiration timestamp.
	 */
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


	// ==================================================
	// MILESTONES
	// ==================================================

	public static double GetMilestoneMultiplier(
		int level)
	{
		if (level >= 25)
			return 8.0;


		if (level >= 20)
			return 5.0;


		if (level >= 15)
			return 3.0;


		if (level >= 10)
			return 2.0;


		if (level >= 5)
			return 1.5;


		return 1.0;
	}


	public static string GetMilestoneText(
		int level)
	{
		if (level >= 25)
			return "x8 production";


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


	// ==================================================
	// HELPERS
	// ==================================================

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
