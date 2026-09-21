using Godot;
using System;

namespace IdleAi;

public readonly record struct BotActionResult(
	bool Changed,
	string Message
);


public sealed class BotService
{
	private readonly GameState _state;

	private readonly EconomyService _economy;


	private readonly RandomNumberGenerator _random =
		new();


	public BotService(
		GameState state,
		EconomyService economy)
	{
		_state =
			state;


		_economy =
			economy;


		_random.Randomize();
	}


	// ==================================================
	// BUY
	// ==================================================

	public BotActionResult BuyBot(
		int roomIndex,
		int slotIndex)
	{
		SlotData? slot =
			GetSlot(
				roomIndex,
				slotIndex
			);


		if (slot == null)
		{
			return new BotActionResult(
				false,
				"Invalid machine."
			);
		}


		if (!slot.Unlocked)
		{
			return new BotActionResult(
				false,
				"Machine is locked."
			);
		}


		if (slot.HasBot)
		{
			return new BotActionResult(
				false,
				"This machine already has a bot."
			);
		}


		double cost =
			_economy.GetBotCost(
				roomIndex,
				slot,
				slotIndex
			);


		if (_state.Tokens < cost)
		{
			return new BotActionResult(
				false,
				"Not enough Tokens."
			);
		}


		_state.Tokens -=
			cost;


		BotRarity rarity =
			RollRarity();


		slot.BotRarity =
			rarity;


		slot.BotPurchasePrice =
			cost;


		if (!slot.IsRunning)
		{
			slot.IsRunning =
				true;


			slot.CycleRemaining =
				_economy.GetCycleDuration(
					slot
				);
		}


		_state.Stats.AddMachineSpending(
			"Bots",
			cost
		);


		BotDefinition bot =
			BotCatalog.Get(
				rarity
			);


		double effectiveMultiplier =
			bot.ProductionMultiplier
			* _state.Lab
				.GetBotPowerMultiplier();


		return new BotActionResult(
			true,
			$"You got a {bot.Name}! "
			+ $"x{effectiveMultiplier:F2}"
		);
	}


	// ==================================================
	// SELL
	// ==================================================

	public BotActionResult SellBot(
		int roomIndex,
		int slotIndex)
	{
		SlotData? slot =
			GetSlot(
				roomIndex,
				slotIndex
			);


		if (
			slot == null
			|| !slot.HasBot
		)
		{
			return new BotActionResult(
				false,
				"No bot to sell."
			);
		}


		double refund =
			slot.BotPurchasePrice
			* GameConfig.BotSellRefundFactor;


		BotDefinition bot =
			BotCatalog.Get(
				slot.BotRarity!.Value
			);


		_state.Tokens +=
			refund;


		slot.BotRarity =
			null;


		slot.BotPurchasePrice =
			0.0;


		slot.IsRunning =
			false;


		slot.CycleRemaining =
			0.0;


		slot.RuntimeCycleDuration =
			0.0;


		return new BotActionResult(
			true,
			$"{bot.Name} sold for "
			+ $"{NumberFormatter.Format(refund)} Tokens."
		);
	}


	// ==================================================
	// PRICE
	// ==================================================

	public double GetBotPrice(
		int roomIndex,
		int slotIndex)
	{
		SlotData? slot =
			GetSlot(
				roomIndex,
				slotIndex
			);


		if (slot == null)
			return 0.0;


		return _economy.GetBotCost(
			roomIndex,
			slot,
			slotIndex
		);
	}


	public double GetSellPrice(
		SlotData slot)
	{
		if (!slot.HasBot)
			return 0.0;


		return slot.BotPurchasePrice
			* GameConfig.BotSellRefundFactor;
	}


	// ==================================================
	// EFFECTIVE BOT POWER
	// ==================================================

	public double GetEffectiveMultiplier(
		SlotData slot)
	{
		if (!slot.HasBot)
			return 1.0;


		double baseMultiplier =
			BotCatalog.GetMultiplier(
				slot.BotRarity
			);


		return baseMultiplier
			* _state.Lab
				.GetBotPowerMultiplier();
	}


	// ==================================================
	// RARITY CHANCES
	// ==================================================

	public (
		double Common,
		double Rare,
		double Epic,
		double Legendary
	) GetRarityChances()
	{
		double rare =
			GameConfig.RareBotChance
			+ _state.Lab
				.GetRareBotChanceBonus();


		double epic =
			GameConfig.EpicBotChance
			+ _state.Lab
				.GetEpicBotChanceBonus();


		double baseLegendary =
			1.0
			- GameConfig.CommonBotChance
			- GameConfig.RareBotChance
			- GameConfig.EpicBotChance;


		double legendary =
			baseLegendary
			+ _state.Lab
				.GetLegendaryBotChanceBonus();


		double luck =
			_state.Shop
				.GetBotLuckMultiplier();


		rare *=
			luck;


		epic *=
			luck;


		legendary *=
			luck;


		rare =
			Math.Max(
				0.0,
				rare
			);


		epic =
			Math.Max(
				0.0,
				epic
			);


		legendary =
			Math.Max(
				0.0,
				legendary
			);


		double specialTotal =
			rare
			+ epic
			+ legendary;


		if (specialTotal > 1.0)
		{
			rare /=
				specialTotal;


			epic /=
				specialTotal;


			legendary /=
				specialTotal;


			return (
				0.0,
				rare,
				epic,
				legendary
			);
		}


		double common =
			1.0
			- specialTotal;


		return (
			common,
			rare,
			epic,
			legendary
		);
	}


	private BotRarity RollRarity()
	{
		(
			double common,
			double rare,
			double epic,
			double legendary
		) =
			GetRarityChances();


		double roll =
			_random.Randf();


		if (roll < common)
			return BotRarity.Common;


		roll -=
			common;


		if (roll < rare)
			return BotRarity.Rare;


		roll -=
			rare;


		if (roll < epic)
			return BotRarity.Epic;


		return BotRarity.Legendary;
	}


	// ==================================================
	// SLOT
	// ==================================================

	private SlotData? GetSlot(
		int roomIndex,
		int slotIndex)
	{
		if (
			roomIndex < 0
			|| roomIndex >= _state.RoomStates.Count
		)
		{
			return null;
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
			return null;
		}


		return room.Slots[
			slotIndex
		];
	}
}
