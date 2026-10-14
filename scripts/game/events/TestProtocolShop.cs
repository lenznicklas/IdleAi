using System.Collections.Generic;

namespace IdleAi;

/// <summary>Sandbox-only offers; IDs remain stable when future tuning changes prices or ordering.</summary>
public static class TestProtocolShop
{
    private const string DataIcon = "res://assets/shop/shop_production.png";
    private const string CoreIcon = "res://assets/ui/core_level.png";
    private const string ShardIcon = "res://assets/shop/data_shard.png";
    public static IReadOnlyList<EventShopItem> Create() =>
    [
        new() { Id = "optimizer_license", EventId = EventCatalog.TestEventId, Name = "Optimizer Bot", Category = "Bots", Featured = true,
            Description = "+25% Event production when equipped. An alternative to its lifetime-data unlock.",
            IconPath = "res://assets/bots/rare.png", Cost = 5_000, CostIconPath = DataIcon,
            Reward = new(EventRewardType.EventBot, 1, "protocol_optimizer") { Name = "Optimizer Bot" } },
        new() { Id = "helper_license", EventId = EventCatalog.TestEventId, Name = "Helper Drone", Category = "Bots",
            Description = "+10% Event production when equipped. Already included at the start of Test Protocol.",
            IconPath = "res://assets/bots/common.png", Cost = 200, CostIconPath = DataIcon,
            Reward = new(EventRewardType.EventBot, 1, "protocol_helper") { Name = "Helper Drone" } },
        new() { Id = "small_data", EventId = EventCatalog.TestEventId, Name = "Small Event Data cache", Category = "Resources", SortOrder = 10,
            Description = "A limited cache of 10K Event Data for the active protocol.", IconPath = DataIcon,
            Cost = 1, CostCurrency = EventShopCurrency.PrestigeResource, CostIconPath = CoreIcon, PurchaseLimit = 3,
            Reward = new(EventRewardType.EventCurrency, 10_000) { Name = "10K Event Data" } },
        new() { Id = "large_data", EventId = EventCatalog.TestEventId, Name = "Large Event Data cache", Category = "Resources", SortOrder = 20,
            Description = "40K Event Data. Available after your first Event Prestige.", IconPath = DataIcon,
            Cost = 2, CostCurrency = EventShopCurrency.PrestigeResource, CostIconPath = CoreIcon, PurchaseLimit = 2,
            Availability = new() { RequiredPrestigeCount = 1 }, Reward = new(EventRewardType.EventCurrency, 40_000) { Name = "40K Event Data" } },
        new() { Id = "protocol_core_offer", EventId = EventCatalog.TestEventId, Name = "Protocol Core", Category = "Resources", SortOrder = 30,
            Description = "One permanent Protocol Core, including its configured production bonus.", IconPath = CoreIcon,
            Cost = 50_000, CostIconPath = DataIcon, PurchaseLimit = 2, Availability = new() { RequiredCurrencyEarned = 25_000 },
            Reward = new(EventRewardType.PrestigeResource, 1) { Name = "1 Protocol Core" } },
        new() { Id = "shard_pack", EventId = EventCatalog.TestEventId, Name = "Data Shard pack", Category = "Resources", SortOrder = 40,
            Description = "Three permanent Data Shards. Requires reaching the 10K track milestone.", IconPath = ShardIcon,
            Cost = 10_000, CostIconPath = DataIcon, PurchaseLimit = 3, Unlock = new() { RequiredMilestoneId = "protocol_shards" },
            Reward = new(EventRewardType.DataShards, 3) { Name = "3 Data Shards" } },
        new() { Id = "training_data", EventId = EventCatalog.TestEventId, Name = "Training data", Category = "Resources", SortOrder = 50,
            Description = "Exchange one Data Shard for 2K Event Data. Optional; no purchase limit.", IconPath = DataIcon,
            Cost = 1, CostCurrency = EventShopCurrency.DataShards, CostIconPath = ShardIcon, PurchaseLimit = 0,
            Reward = new(EventRewardType.EventCurrency, 2_000) { Name = "2K Event Data" } },
        new() { Id = "nature_bot_skin", EventId = EventCatalog.TestEventId, Name = "Nature Overgrown Bot Skin", Category = "Cosmetics", SortOrder = 10,
            Description = "The existing global Nature Overgrown skin pack. Remains owned after the Event.",
            IconPath = "res://assets/bots/nature_overgrown_skin/legendary.png", Cost = 50, CostCurrency = EventShopCurrency.DataShards,
            CostIconPath = ShardIcon, Unlock = new() { RequiredChallengeId = "event_prestige" },
            Reward = new(EventRewardType.BotSkin, 1, BotSkinCatalog.NatureOvergrownSkinId) { Name = "Nature Overgrown Bot Skin" } }
    ];
}
