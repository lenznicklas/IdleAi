using Godot;
using System;

namespace IdleAi;

public readonly record struct BotActionResult(
	bool Changed,
	string Message
);


public sealed class BotService
{
	private const double FullRepairCostFraction =
		0.20;

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

		slot.InitializeBotDurability();

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
			+ $"x{effectiveMultiplier:F2} • "
			+ FormatWorkingTime(
				bot.WorkingLifetimeSeconds
			)
			+ " durability"
		);
	}


	// ==================================================
	// REPAIR
	// ==================================================

	public double GetRepairPrice(
		SlotData slot)
	{
		if (!slot.HasBot)
			return 0.0;

		double missing =
			1.0
			- slot.BotDurabilityRatio;

		if (missing <= 0.0001)
			return 0.0;

		return Math.Max(
			1.0,
			slot.BotPurchasePrice
				* FullRepairCostFraction
				* missing
		);
	}


	public BotActionResult RepairBot(
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
				"No bot to repair."
			);
		}

		double cost =
			GetRepairPrice(
				slot
			);

		if (cost <= 0.0)
		{
			return new BotActionResult(
				false,
				"Bot durability is already full."
			);
		}

		if (_state.Tokens < cost)
		{
			return new BotActionResult(
				false,
				"Not enough Tokens to repair this bot."
			);
		}

		_state.Tokens -=
			cost;

		slot.RepairBot();

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
			"Bot Repairs",
			cost
		);

		return new BotActionResult(
			true,
			"Bot repaired to 100% durability."
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

		slot.ClearBot();

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
		if (!slot.HasWorkingBot)
			return 1.0;

		double baseMultiplier =
			BotCatalog.GetMultiplier(
				slot.BotRarity
			);

		return baseMultiplier
			* _state.Lab
				.GetBotPowerMultiplier();
	}


	public static string FormatWorkingTime(
		double seconds)
	{
		seconds =
			Math.Max(
				0.0,
				seconds
			);

		TimeSpan time =
			TimeSpan.FromSeconds(
				seconds
			);

		if (time.TotalDays >= 1.0)
		{
			return ((int)time.TotalDays)
				+ "d "
				+ time.Hours
				+ "h";
		}

		if (time.TotalHours >= 1.0)
		{
			return ((int)time.TotalHours)
				+ "h "
				+ time.Minutes
				+ "m";
		}

		if (time.TotalMinutes >= 1.0)
		{
			return ((int)time.TotalMinutes)
				+ "m";
		}

		return Math.Max(
			0,
			(int)Math.Ceiling(
				time.TotalSeconds
			)
		)
		+ "s";
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


	public BotRarity RollRarityPublic()
	{
		return RollRarity();
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
