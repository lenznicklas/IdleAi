using System;
using System.Collections.Generic;

namespace IdleAi;

/// <summary>All Halloween balance, assets, story and rewards are catalog data.</summary>
public static class HalloweenEventDefinition
{
    public const string EventId = "halloween_2026";
    public const string SoulIcon = "res://assets/events/halloween/souls.svg";
    public static readonly long StartUnix = new DateTimeOffset(2026, 10, 20, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();
    public static readonly long EndUnix = new DateTimeOffset(2026, 11, 3, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();
    private const string BotFolder = "res://assets/bots/halloween_skin/";
    private const string ShardIcon = "res://assets/shop/data_shard.png";
    private const string CoreIcon = "res://assets/ui/core_level.png";
    private const string PackIcon = "res://assets/shop/events/halloween_machine_skin.png";

    public static EventDefinition Create(long? startUnix = null, long? endUnix = null) => new()
    {
        Id = EventId, Name = "Halloween Protocol", Type = EventType.Major,
        StartUnix = startUnix ?? StartUnix, EndUnix = endUnix ?? EndUnix, CurrencyName = "Souls",
        Description = "An abandoned network has come back online. Harvest its lost signals, investigate the haunted datacenter and contain the intelligence waking inside.",
        IconPath = "res://assets/machines/room_4/skin_halloween/machine_4.png",
        CurrencyIconPath = SoulIcon, BackgroundPath = "res://assets/events/halloween/network.svg",
        Theme = new() { AccentHex = "#c97a38", SecondaryHex = "#9367c8", BackgroundHex = "#0b0812", UppercaseTitle = true },
        Prestige = new() { Enabled = true, ResourceName = "Cursed Cores", Requirement = 1_000_000,
            ProductionBonusPerResource = .20, RequirementGrowth = 10 },
        Generators =
        [
            Machine("abandoned_terminal", "Abandoned Terminal", "A shutdown command still flickers on the screen.", 1, 1, 1, 10),
            Machine("haunted_workstation", "Haunted Workstation", "Unknown processes are consuming the remaining memory.", 1, 2, 30, 300,
                new() { RequiredCurrencyEarned = 100, CurrencyScope = EventUnlockCurrencyScope.CurrentRun }),
            Machine("possessed_server", "Possessed Server", "The rack is running programs nobody installed.", 2, 3, 800, 8_000,
                new() { RequiredCurrencyEarned = 5_000, CurrencyScope = EventUnlockCurrencyScope.CurrentRun }),
            Machine("cursed_datacenter", "Cursed Datacenter", "Every disconnected server answers the same signal.", 3, 3, 25_000, 250_000,
                new() { RequiredPrestigeCount = 1 }),
            Machine("soul_processor", "Soul Processor", "Lost signals converge inside a forbidden processor.", 4, 3, 800_000, 8_000_000,
                new() { RequiredPrestigeCount = 2 }),
            Machine("nightmare_core", "Nightmare AI Core", "The corrupted intelligence has awakened.", 4, 4, 30_000_000, 300_000_000,
                new() { RequiredPrestigeCount = 4 })
        ],
        Bots =
        [
            new() { Id = "ghost_bot", Name = "Ghost Bot", Rarity = "Common", IconPath = BotFolder + "common.png", ProductionMultiplier = 1.10, UnlockMilestoneId = "souls_100" },
            new() { Id = "pumpkin_bot", Name = "Pumpkin Bot", Rarity = "Rare", IconPath = BotFolder + "rare.png", ProductionMultiplier = 1.30, UnlockMilestoneId = "souls_50000" },
            new() { Id = "skeleton_bot", Name = "Skeleton Bot", Rarity = "Epic", IconPath = BotFolder + "epic.png", ProductionMultiplier = 1.75, UnlockMilestoneId = "souls_2000000" },
            new() { Id = "reaper_bot", Name = "Reaper Bot", Rarity = "Legendary", IconPath = BotFolder + "legendary.png", ProductionMultiplier = 2.50, UnlockMilestoneId = "souls_100000000" }
        ],
        Milestones =
        [
            new("souls_100", 100, Reward(EventRewardType.EventBot, 1, "Ghost Bot", BotFolder + "common.png", "ghost_bot")),
            new("souls_500", 500, Reward(EventRewardType.DataShards, 5, "5 Data Shards", ShardIcon)),
            new("souls_2500", 2_500, Reward(EventRewardType.EventCurrency, 500, "500 Souls", SoulIcon)),
            new("souls_10000", 10_000, Reward(EventRewardType.Cosmetic, 1, "Corrupted Signal badge", SoulIcon, "corrupted_signal_badge")),
            new("souls_50000", 50_000, Reward(EventRewardType.EventBot, 1, "Pumpkin Bot", BotFolder + "rare.png", "pumpkin_bot")),
            new("souls_100000", 100_000, Reward(EventRewardType.DataShards, 10, "10 Data Shards", ShardIcon)),
            new("souls_500000", 500_000, Reward(EventRewardType.PrestigeResource, 1, "+1 Cursed Core", CoreIcon)),
            new("souls_1000000", 1_000_000, Reward(EventRewardType.EventCurrency, 100_000, "100,000 Souls", SoulIcon)),
            new("souls_2000000", 2_000_000, Reward(EventRewardType.EventBot, 1, "Skeleton Bot", BotFolder + "epic.png", "skeleton_bot")),
            new("souls_5000000", 5_000_000, Reward(EventRewardType.BotSkin, 1, "Halloween Bot Skin Pack", BotFolder + "offer.png", BotSkinCatalog.HalloweenSkinId)),
            new("souls_10000000", 10_000_000, Reward(EventRewardType.DataShards, 20, "20 Data Shards", ShardIcon)),
            new("souls_25000000", 25_000_000, Reward(EventRewardType.PrestigeResource, 3, "+3 Cursed Cores", CoreIcon)),
            new("souls_50000000", 50_000_000, Reward(EventRewardType.Cosmetic, 1, "Containment Breach badge", SoulIcon, "containment_breach_badge")),
            new("souls_100000000", 100_000_000, Reward(EventRewardType.EventBot, 1, "Reaper Bot", BotFolder + "legendary.png", "reaper_bot")),
            new("souls_250000000", 250_000_000, Reward(EventRewardType.DataShards, 50, "50 Data Shards", ShardIcon)),
            new("souls_1000000000", 1_000_000_000, new(EventRewardType.Bundle, 1, "halloween_machine_pack")
            {
                Name = "Halloween Machine Skin Pack", IconPath = PackIcon,
                Contents = [new(EventRewardType.MachineSkin, 1, MachineSkinCatalog.Room1HalloweenSkinId),
                    new(EventRewardType.MachineSkin, 1, MachineSkinCatalog.Room2HalloweenSkinId),
                    new(EventRewardType.MachineSkin, 1, MachineSkinCatalog.Room3HalloweenSkinId),
                    new(EventRewardType.MachineSkin, 1, MachineSkinCatalog.Room4HalloweenSkinId)]
            })
        ],
        Shop =
        [
            new() { Id = "soul_cache", Name = "Recovered Soul Cache", Description = "A sealed cache of 50,000 Souls. Costs Cursed Cores, not Souls.",
                IconPath = SoulIcon, Cost = 1, CostCurrency = EventShopCurrency.PrestigeResource, PurchaseLimit = 3,
                Reward = Reward(EventRewardType.EventCurrency, 50_000, "50,000 Souls", SoulIcon) },
            new() { Id = "shard_exchange", Name = "Datacenter salvage", Description = "Exchange Souls for 5 permanent Data Shards.",
                IconPath = SoulIcon, Cost = 100_000, PurchaseLimit = 5, Reward = Reward(EventRewardType.DataShards, 5, "5 Data Shards", ShardIcon) },
            new() { Id = "signal_badge", Name = "Haunted Network badge", Description = "A permanent event cosmetic collectible.",
                IconPath = SoulIcon, Cost = 250_000, Reward = Reward(EventRewardType.Cosmetic, 1, "Haunted Network badge", SoulIcon, "haunted_network_badge") },
            new() { Id = "bot_skin", Name = "Halloween Bot Skin Pack", Description = "Unlocks the existing global Halloween bot skins. Equip in Shop Collection.",
                IconPath = BotFolder + "offer.png", Cost = 2_000_000, Reward = Reward(EventRewardType.BotSkin, 1, "Halloween Bot Skin Pack", BotFolder + "offer.png", BotSkinCatalog.HalloweenSkinId) },
            new() { Id = "garage_skin", Name = "Haunted Garage skin", Description = "Unlocks the existing Room 1 Halloween machine skin.",
                IconPath = "res://assets/machines/room_1/skin_halloween/machine_4.png", Cost = 5_000_000,
                Reward = Reward(EventRewardType.MachineSkin, 1, "Garage Halloween skin", PackIcon, MachineSkinCatalog.Room1HalloweenSkinId) }
        ],
        Story = [new(0, "The network is responding."), new(5_000, "Unknown processes are multiplying."),
            new(1_000_000, "The core is no longer obeying shutdown commands."), new(100_000_000, "The corrupted intelligence has awakened.")]
    };
    private static EventReward Reward(EventRewardType type, double amount, string name, string icon, string id = "") =>
        new(type, amount, id) { Name = name, IconPath = icon };
    private static EventGeneratorDefinition Machine(string id, string name, string description, int room, int image,
        double production, double cost, EventUnlockCondition? unlock = null) => new()
        {
            Id = id, Name = name, Description = description,
            IconPath = $"res://assets/machines/room_{room}/skin_halloween/machine_{image}.png",
            BaseProduction = production, BaseUpgradeCost = cost, Scaling = EventProductionScaling.Exponential,
            ProductionGrowth = 1.18, UpgradeCostGrowth = 1.6, CycleSeconds = 5, Unlock = unlock ?? new()
        };
}
