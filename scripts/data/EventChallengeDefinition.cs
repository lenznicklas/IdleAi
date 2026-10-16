using System.Collections.Generic;

namespace IdleAi;

public enum EventChallengeType
{
    EarnCurrency, EarnLifetimeCurrency, UpgradeGenerators, ReachGeneratorLevel,
    ReachTotalGeneratorLevels, UnlockGenerator, PerformPrestige, ReachPrestigeCurrency,
    UnlockBot, ClaimRewards, PurchaseShopItems, ReachProductionPerSecond, PlayMinutes, OwnBot, EquipBot, UnlockGenerators
}
public enum EventChallengeCategory { Daily, Event, Major }

public sealed record EventChallengeDefinition
{
    public required string Id { get; init; }
    public required string EventId { get; init; }
    public required string Title { get; init; }
    public string Description { get; init; } = "";
    public EventChallengeType Type { get; init; }
    public double Target { get; init; } = 1;
    public string GeneratorId { get; init; } = ""; // Empty ReachGeneratorLevel means any generator.
    public string BotId { get; init; } = "";
    public string ShopItemId { get; init; } = ""; // optional specific-item purchase metric
    public required EventReward Reward { get; init; }
    public EventChallengeCategory Category { get; init; } = EventChallengeCategory.Event;
    public bool Repeatable { get; init; }
    public bool Enabled { get; init; } = true;
    // Reach goals remember their high-water mark by default. Opt in to live state.
    public bool UseCurrentState { get; init; }
    public EventUnlockCondition Availability { get; init; } = new();
}

public readonly record struct EventChallengeSnapshot(EventChallengeDefinition Definition,
    double Progress, bool Complete, bool Claimed, int TimesClaimed, bool CanClaim);

internal static class EventChallengeRules
{
    internal static bool IsCumulative(EventChallengeType type) => type is
        EventChallengeType.EarnCurrency or EventChallengeType.EarnLifetimeCurrency or EventChallengeType.UpgradeGenerators
        or EventChallengeType.PerformPrestige or EventChallengeType.ClaimRewards or EventChallengeType.PurchaseShopItems or EventChallengeType.PlayMinutes;
}
