using Godot;
using System;

namespace IdleAi;


public readonly record struct ShopResult(
	bool Changed,
	string Message
);


public sealed class ShopService
{
	private readonly GameState _state;

	private readonly EconomyService _economy;


	public ShopService(
		GameState state,
		EconomyService economy)
	{
		_state =
			state;

		_economy =
			economy;
	}


	// ==================================================
	// COSTS
	// ==================================================

	public double GetProductionUpgradeCost()
	{
		return GameConfig
			.ShopProductionUpgradeBaseCost
			* Math.Pow(
				GameConfig
					.ShopProductionUpgradeCostGrowth,

				_state.Shop
					.ProductionUpgradeLevel
			);
	}


	public double GetOfflineUpgradeCost()
	{
		return GameConfig
			.ShopOfflineUpgradeBaseCost
			* Math.Pow(
				GameConfig
					.ShopOfflineUpgradeCostGrowth,

				_state.Shop
					.OfflineUpgradeLevel
			);
	}


	// ==================================================
	// PRODUCTION BOOST
	// ==================================================

	public ShopResult BuyProductionBoost()
	{
		if (
			!TrySpend(
				GameConfig.ShopProductionBoostCost
			)
		)
		{
			return NotEnough();
		}


		long now =
			GetCurrentUnixTime();


		long start =
			Math.Max(
				now,
				_state.Shop
					.ProductionBoostEndUnix
			);


		_state.Shop.ProductionBoostEndUnix =
			start
			+ GameConfig
				.ShopProductionBoostMinutes
			* 60L;


		return new ShopResult(
			true,
			$"2x Production active for {GameConfig.ShopProductionBoostMinutes} minutes."
		);
	}


	// ==================================================
	// BOT LUCK
	// ==================================================

	public ShopResult BuyBotLuck()
	{
		if (
			!TrySpend(
				GameConfig.ShopBotLuckCost
			)
		)
		{
			return NotEnough();
		}


		long now =
			GetCurrentUnixTime();


		long start =
			Math.Max(
				now,
				_state.Shop
					.BotLuckBoostEndUnix
			);


		_state.Shop.BotLuckBoostEndUnix =
			start
			+ GameConfig
				.ShopBotLuckMinutes
			* 60L;


		return new ShopResult(
			true,
			$"Bot Luck active for {GameConfig.ShopBotLuckMinutes} minutes."
		);
	}


	// ==================================================
	// INSTANT PRODUCTION
	// ==================================================

	public ShopResult BuyInstantProduction()
	{
		bool hasRunningMachine =
			false;


		foreach (
			RoomState room
			in _state.RoomStates
		)
		{
			if (!room.Unlocked)
				continue;


			foreach (
				SlotData slot
				in room.Slots
			)
			{
				if (
					slot.Unlocked
					&& slot.IsRunning
				)
				{
					hasRunningMachine =
						true;

					break;
				}
			}


			if (hasRunningMachine)
				break;
		}


		if (!hasRunningMachine)
		{
			return new ShopResult(
				false,
				"No production cycles are currently running."
			);
		}


		if (
			!TrySpend(
				GameConfig.ShopInstantProductionCost
			)
		)
		{
			return NotEnough();
		}


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


				earned +=
					_economy.GetCycleReward(
						roomIndex,
						slot
					);


				if (slot.HasBot)
				{
					slot.IsRunning =
						true;


					slot.CycleRemaining =
						_economy.GetCycleDuration(
							slot
						);
				}
				else
				{
					slot.IsRunning =
						false;


					slot.CycleRemaining =
						0.0;
				}
			}
		}


		if (earned > 0.0)
		{
			_state.Tokens +=
				earned;


			_state.RunEarnedTokens +=
				earned;


			_state.Stats.AddEarned(
				earned
			);
		}


		return new ShopResult(
			true,
			"Production completed instantly. +"
			+ NumberFormatter.Format(
				earned
			)
			+ " Tokens"
		);
	}


	// ==================================================
	// PERMANENT PRODUCTION
	// ==================================================

	public ShopResult BuyProductionUpgrade()
	{
		if (
			_state.Shop.ProductionUpgradeLevel
			>= GameConfig.ShopProductionUpgradeMaxLevel
		)
		{
			return new ShopResult(
				false,
				"Production upgrade already at maximum level."
			);
		}


		double cost =
			GetProductionUpgradeCost();


		if (!TrySpend(cost))
			return NotEnough();


		_state.Shop.ProductionUpgradeLevel++;


		return new ShopResult(
			true,
			"Permanent production increased to +"
			+ (
				_state.Shop.ProductionUpgradeLevel
				* GameConfig.ShopProductionUpgradeBonus
				* 100.0
			).ToString(
				"0"
			)
			+ "%."
		);
	}


	// ==================================================
	// PERMANENT OFFLINE
	// ==================================================

	public ShopResult BuyOfflineUpgrade()
	{
		if (
			_state.Shop.OfflineUpgradeLevel
			>= GameConfig.ShopOfflineUpgradeMaxLevel
		)
		{
			return new ShopResult(
				false,
				"Offline upgrade already at maximum level."
			);
		}


		double cost =
			GetOfflineUpgradeCost();


		if (!TrySpend(cost))
			return NotEnough();


		_state.Shop.OfflineUpgradeLevel++;


		return new ShopResult(
			true,
			"Offline income increased by +5%."
		);
	}


	// ==================================================
	// SPEND
	// ==================================================

	private bool TrySpend(
		double amount)
	{
		if (
			amount <= 0.0
			|| _state.Shop.DataShards
			< amount
		)
		{
			return false;
		}


		_state.Shop.DataShards -=
			amount;


		return true;
	}


	private static ShopResult NotEnough()
	{
		return new ShopResult(
			false,
			"Not enough Data Shards."
		);
	}


	// ==================================================
	// TIME
	// ==================================================

	private static long GetCurrentUnixTime()
	{
		return (long)
			Time.GetUnixTimeFromSystem();
	}
}
