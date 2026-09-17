using Godot;

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


		// ==================================================
		// START AUTOMATIC PRODUCTION
		// ==================================================

		if (!slot.IsRunning)
		{
			slot.IsRunning =
				true;


			slot.CycleRemaining =
				_economy.GetCycleDuration(
					slot
				);
		}


		// ==================================================
		// STATS
		// ==================================================

		_state.Stats.AddMachineSpending(
			"Bots",
			cost
		);


		BotDefinition bot =
			BotCatalog.Get(
				rarity
			);


		return new BotActionResult(
			true,
			$"You got a {bot.Name}! "
			+ $"x{bot.ProductionMultiplier:F1}"
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


		// Machine becomes manual again.
		slot.IsRunning =
			false;


		slot.CycleRemaining =
			0.0;


		return new BotActionResult(
			true,
			$"{bot.Name} sold for "
			+ $"{NumberFormatter.Format(refund)} Tokens."
		);
	}


	// ==================================================
	// BOT PRICE
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


	// ==================================================
	// SELL PRICE
	// ==================================================

	public double GetSellPrice(
		SlotData slot)
	{
		if (!slot.HasBot)
			return 0.0;


		return slot.BotPurchasePrice
			   * GameConfig.BotSellRefundFactor;
	}


	// ==================================================
	// RANDOM RARITY
	// ==================================================

	private BotRarity RollRarity()
	{
		double roll =
			_random.Randf();


		if (
			roll
			< GameConfig.CommonBotChance
		)
		{
			return BotRarity.Common;
		}


		roll -=
			GameConfig.CommonBotChance;


		if (
			roll
			< GameConfig.RareBotChance
		)
		{
			return BotRarity.Rare;
		}


		roll -=
			GameConfig.RareBotChance;


		if (
			roll
			< GameConfig.EpicBotChance
		)
		{
			return BotRarity.Epic;
		}


		return BotRarity.Legendary;
	}


	// ==================================================
	// SLOT LOOKUP
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
