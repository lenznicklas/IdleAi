using System.Collections.Generic;

namespace IdleAi;

/// <summary>Reusable example pool and goals. Economy tuning stays in TestProtocolEventDefinition.</summary>
public static class TestProtocolChallenges
{
    public static IReadOnlyList<EventChallengeDefinition> Create() =>
    [
        Task("daily_upgrades", "Upgrade Event generators", "Make 10 upgrades today.", EventChallengeType.UpgradeGenerators, 10, 2, EventChallengeCategory.Daily),
        Task("daily_data", "Collect Event Data", "Earn 25K Event Data today. Spending and Prestige keep this progress.", EventChallengeType.EarnCurrency, 25_000, 2, EventChallengeCategory.Daily),
        Task("daily_production", "Accelerate the protocol", "Reach 5K Event production per second.", EventChallengeType.ReachProductionPerSecond, 5_000, 3, EventChallengeCategory.Daily)
            with { Availability = new() { RequiredPrestigeCount = 1 } },
        Task("daily_level", "Improve a generator", "Reach level 15 on any Event generator.", EventChallengeType.ReachGeneratorLevel, 15, 2, EventChallengeCategory.Daily),
        Task("daily_rewards", "Collect track rewards", "Claim 2 Event Reward Track milestones today.", EventChallengeType.ClaimRewards, 2, 2, EventChallengeCategory.Daily),
        Task("event_lifetime", "Gather protocol data", "Earn 10K lifetime Event Data.", EventChallengeType.EarnLifetimeCurrency, 10_000, 3),
        Task("event_rack", "Expand the protocol", "Unlock the Compute Rack.", EventChallengeType.UnlockGenerator, 1, 3) with { GeneratorId = "compute_rack" },
        Task("event_prestige", "Begin a new protocol", "Perform your first Event Prestige.", EventChallengeType.PerformPrestige, 1, 5),
        Task("event_cores", "Collect Protocol Cores", "Reach a balance of 5 Protocol Cores.", EventChallengeType.ReachPrestigeCurrency, 5, 5),
        Task("event_levels", "Build the network", "Reach 100 total unlocked generator levels in one run.", EventChallengeType.ReachTotalGeneratorLevels, 100, 5),
        Task("event_production", "Protocol at scale", "Reach 1M Event production per second.", EventChallengeType.ReachProductionPerSecond, 1_000_000, 10),
        Task("event_rewards", "Protocol collector", "Claim 5 Event Reward Track milestones.", EventChallengeType.ClaimRewards, 5, 5),
        Task("major_complete", "Complete Test Protocol", "Reach Prototype Core level 50.", EventChallengeType.ReachGeneratorLevel, 50, 25, EventChallengeCategory.Major)
            with { GeneratorId = "prototype_core" }
    ];
    private static EventChallengeDefinition Task(string id, string title, string description, EventChallengeType type,
        double target, double shards, EventChallengeCategory category = EventChallengeCategory.Event) => new()
    {
        Id = id, EventId = EventCatalog.TestEventId, Title = title, Description = description,
        Type = type, Target = target, Category = category,
        Reward = new(EventRewardType.DataShards, shards) { IconPath = "res://assets/shop/data_shard.png" }
    };
}
