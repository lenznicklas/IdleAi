using System;
using System.Collections.Generic;

namespace IdleAi;

/// <summary>All Halloween balance, assets, story and rewards are catalog data.</summary>
public static class HalloweenEventDefinition
{
    public const string EventId = "halloween_2026";
    public const string SoulIcon = HalloweenEventAssets.Souls;
    public static readonly long StartUnix = new DateTimeOffset(2026, 10, 14, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();
    public static readonly long EndUnix = new DateTimeOffset(2026, 11, 3, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds();
    private const string ShardIcon = "res://assets/shop/data_shard.png";
    private const string CoreIcon = HalloweenEventAssets.CursedCore;
    private const string PackIcon = HalloweenEventAssets.MachineSkinPack;
    private static readonly EventReward MachinePackReward = new(EventRewardType.Bundle, 1, "halloween_machine_pack")
    {
        Name = "Halloween Machine Skin Pack", IconPath = PackIcon,
        Contents = [new(EventRewardType.MachineSkin, 1, MachineSkinCatalog.Room1HalloweenSkinId),
            new(EventRewardType.MachineSkin, 1, MachineSkinCatalog.Room2HalloweenSkinId),
            new(EventRewardType.MachineSkin, 1, MachineSkinCatalog.Room3HalloweenSkinId),
            new(EventRewardType.MachineSkin, 1, MachineSkinCatalog.Room4HalloweenSkinId)]
    };
    private static readonly EventReward HauntedNetworkBadgeReward = Reward(EventRewardType.Cosmetic, 1,
        "Haunted Network badge", HalloweenEventAssets.HauntedNetworkBadge, "haunted_network_badge");

    public static EventDefinition Create(long? startUnix = null, long? endUnix = null) => new()
    {
        Id = EventId, Name = "Halloween Protocol", Type = EventType.Major,
        StartUnix = startUnix ?? StartUnix, EndUnix = endUnix ?? EndUnix, CurrencyName = "Souls",
        Description = "An abandoned network has come back online. Harvest its lost signals, investigate the haunted datacenter and contain the intelligence waking inside.",
        IconPath = HalloweenEventAssets.Icon,
        CurrencyIconPath = SoulIcon, BackgroundPath = HalloweenEventAssets.Background,
        Theme = new() { AccentHex = "#c97a38", SecondaryHex = "#9367c8", BackgroundHex = "#0b0812", UppercaseTitle = true },
        Prestige = new() { Enabled = true, ResourceName = "Cursed Cores", IconPath = CoreIcon, Requirement = 1_000_000,
            ProductionBonusPerResource = .20, RequirementGrowth = 10 },
        Generators =
        [
            Machine("abandoned_terminal", "Abandoned Terminal", "A shutdown command still flickers on the screen.", HalloweenEventAssets.AbandonedTerminal, 1, 10),
            Machine("haunted_workstation", "Haunted Workstation", "Unknown processes are consuming the remaining memory.", HalloweenEventAssets.HauntedWorkstation, 30, 300,
                new() { RequiredCurrencyEarned = 100, CurrencyScope = EventUnlockCurrencyScope.CurrentRun }),
            Machine("possessed_server", "Possessed Server", "The rack is running programs nobody installed.", HalloweenEventAssets.PossessedServer, 800, 8_000,
                new() { RequiredCurrencyEarned = 5_000, CurrencyScope = EventUnlockCurrencyScope.CurrentRun }),
            Machine("cursed_datacenter", "Cursed Datacenter", "Every disconnected server answers the same signal.", HalloweenEventAssets.CursedDatacenter, 25_000, 250_000,
                new() { RequiredPrestigeCount = 1 }),
            Machine("soul_processor", "Soul Processor", "Lost signals converge inside a forbidden processor.", HalloweenEventAssets.SoulProcessor, 800_000, 8_000_000,
                new() { RequiredPrestigeCount = 2 }),
            Machine("nightmare_core", "Nightmare AI Core", "The corrupted intelligence has awakened.", HalloweenEventAssets.NightmareAiCore, 30_000_000, 300_000_000,
                new() { RequiredPrestigeCount = 4 })
        ],
        Bots =
        [
            new() { Id = "ghost_bot", Name = "Ghost Bot", Rarity = "Common", IconPath = HalloweenEventAssets.GhostBot, ProductionMultiplier = 1.10, UnlockMilestoneId = "souls_100" },
            new() { Id = "pumpkin_bot", Name = "Pumpkin Bot", Rarity = "Rare", IconPath = HalloweenEventAssets.PumpkinBot, ProductionMultiplier = 1.30, UnlockMilestoneId = "souls_50000" },
            new() { Id = "skeleton_bot", Name = "Skeleton Bot", Rarity = "Epic", IconPath = HalloweenEventAssets.SkeletonBot, ProductionMultiplier = 1.75, UnlockMilestoneId = "souls_2000000" },
            new() { Id = "reaper_bot", Name = "Reaper Bot", Rarity = "Legendary", IconPath = HalloweenEventAssets.ReaperBot, ProductionMultiplier = 2.50, UnlockMilestoneId = "souls_100000000" }
        ],
        Milestones =
        [
            new("souls_100", 100, Reward(EventRewardType.EventBot, 1, "Ghost Bot", HalloweenEventAssets.GhostBot, "ghost_bot")),
            new("souls_500", 500, Reward(EventRewardType.DataShards, 5, "5 Data Shards", ShardIcon)),
            new("souls_2500", 2_500, Reward(EventRewardType.EventCurrency, 500, "500 Souls", SoulIcon)),
            new("souls_10000", 10_000, Reward(EventRewardType.Cosmetic, 1, "Corrupted Signal badge", HalloweenEventAssets.CorruptedSignalBadge, "corrupted_signal_badge")),
            new("souls_50000", 50_000, Reward(EventRewardType.EventBot, 1, "Pumpkin Bot", HalloweenEventAssets.PumpkinBot, "pumpkin_bot")),
            new("souls_100000", 100_000, Reward(EventRewardType.DataShards, 10, "10 Data Shards", ShardIcon)),
            new("souls_500000", 500_000, Reward(EventRewardType.PrestigeResource, 1, "+1 Cursed Core", CoreIcon)),
            new("souls_1000000", 1_000_000, Reward(EventRewardType.EventCurrency, 100_000, "100,000 Souls", SoulIcon)),
            new("souls_2000000", 2_000_000, Reward(EventRewardType.EventBot, 1, "Skeleton Bot", HalloweenEventAssets.SkeletonBot, "skeleton_bot")),
            new("souls_10000000", 10_000_000, Reward(EventRewardType.DataShards, 20, "20 Data Shards", ShardIcon)),
            new("souls_25000000", 25_000_000, Reward(EventRewardType.PrestigeResource, 3, "+3 Cursed Cores", CoreIcon)),
            new("souls_50000000", 50_000_000, Reward(EventRewardType.Cosmetic, 1, "Containment Breach badge", HalloweenEventAssets.ContainmentBreachBadge, "containment_breach_badge")),
            new("souls_100000000", 100_000_000, Reward(EventRewardType.EventBot, 1, "Reaper Bot", HalloweenEventAssets.ReaperBot, "reaper_bot")),
            new("souls_250000000", 250_000_000, Reward(EventRewardType.DataShards, 50, "50 Data Shards", ShardIcon)),

        ],
        Shop =
        [
            new() { Id = "soul_cache", Name = "50,000 Souls", Description = "Exchange 1 Cursed Core for 50,000 Souls immediately. Limit: 3 purchases per event.",
                IconPath = HalloweenEventAssets.RewardChest, CostIconPath = CoreIcon, Cost = 1, CostCurrency = EventShopCurrency.PrestigeResource, PurchaseLimit = 3,
                Reward = Reward(EventRewardType.EventCurrency, 50_000, "50,000 Souls", SoulIcon) },
            new() { Id = "shard_exchange", Name = "Datacenter salvage", Description = "Exchange Souls for 5 permanent Data Shards.",
                IconPath = SoulIcon, Cost = 100_000, PurchaseLimit = 5, Reward = Reward(EventRewardType.DataShards, 5, "5 Data Shards", ShardIcon) },
            new() { Id = "signal_badge", Name = HauntedNetworkBadgeReward.Name, Description = "A permanent event cosmetic collectible.",
                IconPath = SoulIcon, Cost = 250_000, Reward = HauntedNetworkBadgeReward },
            new() { Id = "bot_skin", Name = "Halloween Bot Skin Pack", Description = "Unlocks the existing global Halloween bot skins. Equip in Shop Collection.",
                IconPath = HalloweenEventAssets.BotSkinPack, CostIconPath = CoreIcon, Cost = 50, CostCurrency = EventShopCurrency.PrestigeResource, Reward = Reward(EventRewardType.BotSkin, 1, "Halloween Bot Skin Pack", HalloweenEventAssets.BotSkinPack, BotSkinCatalog.HalloweenSkinId) },
            new() { Id = "machine_skin", Name = MachinePackReward.Name, Description = "Unlocks Halloween machine skins for all four rooms permanently. Equip in Shop Collection.",
                IconPath = PackIcon, CostIconPath = CoreIcon, Cost = 50, CostCurrency = EventShopCurrency.PrestigeResource,
                Reward = MachinePackReward },
            new() { Id = "garage_skin", Name = "Haunted Garage skin", Description = "Unlocks the existing Room 1 Halloween machine skin.",
                IconPath = HalloweenEventAssets.GarageSkin, Cost = 5_000_000,
                Reward = Reward(EventRewardType.MachineSkin, 1, "Garage Halloween skin", HalloweenEventAssets.GarageSkin, MachineSkinCatalog.Room1HalloweenSkinId) }
        ],
        Challenges =
        [
            Goal("harvest", "Lost signals", "Produce 2,500 Souls over the event.", EventChallengeType.EarnLifetimeCurrency, 2_500, 250),
            Goal("upgrades", "Restore the network", "Upgrade event machines 20 times.", EventChallengeType.UpgradeGenerators, 20, 1_000),
            Goal("level", "Overclocked", "Reach level 15 on any event machine.", EventChallengeType.ReachGeneratorLevel, 15, 2_500),
            Goal("machines", "Network expansion", "Unlock 3 event machines in a run.", EventChallengeType.UnlockGenerators, 3, 5_000),
            Goal("bot", "Ghost in the system", "Unlock Ghost Bot.", EventChallengeType.UnlockBot, 1, 500) with { BotId = "ghost_bot" },
            Goal("prestige", "Reboot the haunted core", "Perform 2 event Prestiges.", EventChallengeType.PerformPrestige, 2, 10_000)
        ],
        Story = [new(0, "The network is responding."), new(5_000, "Unknown processes are multiplying."),
            new(1_000_000, "The core is no longer obeying shutdown commands."), new(100_000_000, "The corrupted intelligence has awakened.")]
    };
    private static EventChallengeDefinition Goal(string id, string title, string description, EventChallengeType type, double target, double souls) => new()
    {
        Id = id, EventId = EventId, Title = title, Description = description, Type = type, Target = target,
        Reward = Reward(EventRewardType.EventCurrency, souls, souls.ToString("N0", System.Globalization.CultureInfo.InvariantCulture) + " Souls", SoulIcon)
    };
    private static EventReward Reward(EventRewardType type, double amount, string name, string icon, string id = "") =>
        new(type, amount, id) { Name = name, IconPath = icon };
    private static EventGeneratorDefinition Machine(string id, string name, string description, string icon,
        double production, double cost, EventUnlockCondition? unlock = null) => new()
        {
            Id = id, Name = name, Description = description,
            IconPath = icon,
            BaseProduction = production, BaseUpgradeCost = cost, Scaling = EventProductionScaling.Exponential,
            ProductionGrowth = 1.18, UpgradeCostGrowth = 1.6, CycleSeconds = 5, Unlock = unlock ?? new()
        };
}
