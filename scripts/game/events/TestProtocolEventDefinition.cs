using System;

namespace IdleAi;

/// <summary>Test Protocol tuning sandbox. Production and upgrade prices are per five-second cycle.</summary>
public static class TestProtocolEventDefinition
{
    public static EventDefinition Create() => new()
    {
        Id = EventCatalog.TestEventId, Name = "Test Protocol", CurrencyIconPath = "res://assets/shop/shop_production.png", Type = EventType.Weekend,
        Description = "A test environment for temporary protocols. Generate Event Data, upgrade your generators and unlock milestones.",
        StartUnix = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds(),
        EndUnix = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds(),
        IconPath = "res://assets/map/room_coming_soon.png",
        Prestige = new()
        {
            Enabled = true, Requirement = 500_000, RequirementGrowth = 1.3,
            RewardMultiplier = 1, RewardExponent = .5, ProductionBonusPerResource = 1
        },
        Generators =
        [
            Machine("data_terminal", "Data Terminal", "Collects basic protocol data.",
                "res://assets/machines/room_1/machine1.png", 5, 0),
            Machine("compute_rack", "Compute Rack", "A dedicated data processing rack.",
                "res://assets/map/room_server_room.png", 20, 350),
            Machine("neural_server", "Neural Server", "Learns from the protocol's output.",
                "res://assets/map/room_data_center.png", 80, 8_000),
            Machine("quantum_cluster", "Quantum Cluster", "Processes simultaneous protocol states.",
                "res://assets/map/room_quantum_lab.png", 320, 100_000, 1),
            Machine("prototype_core", "Prototype Core", "The next generation protocol core.",
                "res://assets/map/room_singularity.png", 1_280, 500_000, 2)
        ],
        Challenges = TestProtocolChallenges.Create(), DailyChallengeCount = 3,
        Bots =
        [
            new() { Id = "protocol_helper", EventId = EventCatalog.TestEventId, Name = "Helper Drone", Rarity = "Common",
                Description = "A reliable assistant for the first protocol experiments.",
                IconPath = "res://assets/bots/common.png", ProductionMultiplier = 1.1, AvailableFromStart = true,
                UnlockMilestoneId = "protocol_bot" },
            new() { Id = "protocol_optimizer", EventId = EventCatalog.TestEventId, Name = "Optimizer Bot", Rarity = "Rare",
                Description = "Keeps every protocol generator working efficiently.",
                IconPath = "res://assets/bots/rare.png", ProductionMultiplier = 1.25,
                UnlockPaths = [new() { LifetimeCurrency = 25_000 }] },
            new() { Id = "protocol_neural", EventId = EventCatalog.TestEventId, Name = "Neural Assistant", Rarity = "Epic",
                Description = "Retains its knowledge through every Event Prestige.",
                IconPath = "res://assets/bots/epic.png", ProductionMultiplier = 1.5,
                UnlockPaths = [new() { PrestigeCount = 1 }] },
            new() { Id = "protocol_prototype", EventId = EventCatalog.TestEventId, Name = "Prototype Intelligence", Rarity = "Legendary",
                Description = "The completed protocol's most capable intelligence.",
                IconPath = "res://assets/bots/legendary.png", ProductionMultiplier = 2,
                UnlockPaths = [new() { ChallengeId = "major_complete" }] }
        ],
        Shop = TestProtocolShop.Create(),
        // Keep stable reward IDs and amounts: existing claimed rewards must never be reissued.
        Milestones =
        [
            new("data_100", 100, new(EventRewardType.DeveloperCredits, 1)),
            new("data_500", 2_500, new(EventRewardType.DeveloperCredits, 2)),
            new("data_2500", 150_000, new(EventRewardType.DeveloperCredits, 5)),
            new("data_10000", 1_500_000, new(EventRewardType.DeveloperCredits, 10)),
            new("protocol_cache", 500, new(EventRewardType.EventCurrency, 100) { Name = "100 Event Data" }),
            new("protocol_shards", 10_000, new(EventRewardType.DataShards, 5) { IconPath = "res://assets/shop/data_shard.png" }),
            new("protocol_bot", 50_000, new(EventRewardType.EventBot, 1, "protocol_helper") { Name = "Helper Drone", IconPath = "res://assets/bots/common.png" }) { Featured = true },
            new("protocol_core", 500_000, new(EventRewardType.PrestigeResource, 1) { IconPath = "res://assets/ui/core_level.png" }),
            new("protocol_shards_2", 3_000_000, new(EventRewardType.DataShards, 10) { IconPath = "res://assets/shop/data_shard.png" }),
            new("protocol_final", 10_000_000, new(EventRewardType.PrestigeResource, 3) { Name = "3 Protocol Cores", IconPath = "res://assets/ui/core_level.png" }) { Featured = true, FinalReward = true }
        ]
    };

    private static EventGeneratorDefinition Machine(string id, string name, string description,
        string icon, double production, double unlockEarned, int prestiges = 0) => new()
    {
        Id = id, Name = name, Description = description, IconPath = icon,
        StartingLevel = 1, MaxLevel = 1000, CycleSeconds = 5,
        BaseProduction = production, Scaling = EventProductionScaling.Exponential, ProductionGrowth = 1.18,
        BaseUpgradeCost = production, UpgradeCostGrowth = 1.4,
        Unlock = new()
        {
            RequiredCurrencyEarned = unlockEarned, CurrencyScope = EventUnlockCurrencyScope.CurrentRun,
            RequiredPrestigeCount = prestiges
        }
    };
}
