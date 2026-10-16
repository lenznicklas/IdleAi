using System;
using System.Collections.Generic;
using System.Linq;

namespace IdleAi;

public sealed class EventGeneratorSaveData
{
    public int Level { get; set; } = 1;
    public bool Unlocked { get; set; }
    public double CycleElapsedSeconds { get; set; }
    public double Multiplier { get; set; } = 1;
    public EventGeneratorSaveData Copy() => (EventGeneratorSaveData)MemberwiseClone();
}
public sealed class EventArchiveResult
{
    public string EventId { get; set; } = "";
    public string Name { get; set; } = "";
    public int Year { get; set; }
    public string CurrencyName { get; set; } = "";
    public double TotalEarned { get; set; }
    public int HighestLevel { get; set; }
    public int PrestigeCount { get; set; }
    public long PrestigeResource { get; set; }
    public long TotalPrestigeResourceEarned { get; set; }
    public double HighestProduction { get; set; }
    public int RewardsClaimed { get; set; }
    public int RewardsTotal { get; set; }
    public int BotsUnlocked { get; set; }
    public bool LegendaryBotOwned { get; set; }
    public int CosmeticsOwned { get; set; }
    public EventArchiveResult Copy() => (EventArchiveResult)MemberwiseClone();
}

public sealed class EventProgressSaveData
{
    public EventChallengeSaveData Challenges { get; set; } = new();
    public int GeneratorDataVersion { get; set; }
    public Dictionary<string, EventGeneratorSaveData> Generators { get; set; } = [];
    public double PrestigeEarnedCheckpoint { get; set; }
    public long PrestigeResource { get; set; }
    public long TotalPrestigeResourceEarned { get; set; }
    public double HighestProductionReached { get; set; }
    public HashSet<string> UnlockedBots { get; set; } = [];
    public HashSet<string> OwnedBots { get; set; } = [];
    public string? ActiveBotId { get; set; }
    public HashSet<string> Cosmetics { get; set; } = [];
    public HashSet<string> UnlockedShopItems { get; set; } = [];
    public Dictionary<string, int> ShopPurchases { get; set; } = [];
    // Legacy payload retained only for save round-tripping; Events has no history feature.
    public EventArchiveResult? Archive { get; set; }
    public double Currency { get; set; }
    public double TotalCurrencyEarned { get; set; }
    public int ProgressionLevel { get; set; } = 1;
    public HashSet<string> UnlockedMilestones { get; set; } = [];
    public HashSet<string> ClaimedRewards { get; set; } = [];
    public int PrestigeCount { get; set; }
    public int HighestProgressionReached { get; set; } = 1;
    public long LastUpdateUnix { get; set; }
    public double CycleElapsedSeconds { get; set; }
    public double DeveloperCredits { get; set; }
    public bool Ended { get; set; }
    public Dictionary<string, string> ContentState { get; set; } = [];

    public EventProgressSaveData Copy() => new()
    {
        Challenges = Challenges.Copy(), GeneratorDataVersion = GeneratorDataVersion,
        Generators = Generators.ToDictionary(pair => pair.Key, pair => pair.Value.Copy()),
        PrestigeEarnedCheckpoint = PrestigeEarnedCheckpoint, PrestigeResource = PrestigeResource,
        TotalPrestigeResourceEarned = TotalPrestigeResourceEarned,
        HighestProductionReached = HighestProductionReached, UnlockedBots = new(UnlockedBots), OwnedBots = new(OwnedBots), ActiveBotId = ActiveBotId,
        Cosmetics = new(Cosmetics), ShopPurchases = new(ShopPurchases), UnlockedShopItems = new(UnlockedShopItems), Archive = Archive?.Copy(),
        Currency = Currency, TotalCurrencyEarned = TotalCurrencyEarned,
        ProgressionLevel = ProgressionLevel, PrestigeCount = PrestigeCount,
        HighestProgressionReached = HighestProgressionReached, LastUpdateUnix = LastUpdateUnix,
        CycleElapsedSeconds = CycleElapsedSeconds, DeveloperCredits = DeveloperCredits, Ended = Ended,
        UnlockedMilestones = new(UnlockedMilestones), ClaimedRewards = new(ClaimedRewards),
        ContentState = new(ContentState)
    };
}

public sealed class EventSaveData
{
    public int Version { get; set; } = 2;
    public HashSet<string> CosmeticEntitlements { get; set; } = [];
    public long LastEvaluationUnix { get; set; }
    public string? LastEventId { get; set; }
    public Dictionary<string, EventProgressSaveData> ProgressByEventId { get; set; } = [];
    public EventSaveData Copy() => new()
    {
        CosmeticEntitlements = new(CosmeticEntitlements), Version = Version, LastEvaluationUnix = LastEvaluationUnix, LastEventId = LastEventId,
        ProgressByEventId = ProgressByEventId.ToDictionary(pair => pair.Key, pair => pair.Value.Copy())
    };

    // Missing Events in legacy saves uses the property initializer; explicit corruption
    // fails normal save recovery instead of silently resetting reward claims.
    public bool IsValid() => Version is 1 or 2 && CosmeticEntitlements != null &&
        CosmeticEntitlements.All(id => !string.IsNullOrWhiteSpace(id)) && LastEvaluationUnix >= 0 && ProgressByEventId != null
        && ProgressByEventId.All(pair => !string.IsNullOrWhiteSpace(pair.Key) && pair.Value != null
            && ValidProgress(pair.Value));

    private static bool ValidProgress(EventProgressSaveData p) =>
        p.Challenges != null && p.Challenges.IsValid() && p.GeneratorDataVersion is 0 or 2 && p.Generators != null && p.Generators.All(pair =>
            !string.IsNullOrWhiteSpace(pair.Key) && pair.Value != null && pair.Value.Level >= 1
            && double.IsFinite(pair.Value.CycleElapsedSeconds) && pair.Value.CycleElapsedSeconds >= 0
            && double.IsFinite(pair.Value.Multiplier) && pair.Value.Multiplier > 0)
        && double.IsFinite(p.PrestigeEarnedCheckpoint) && p.PrestigeEarnedCheckpoint >= 0
        && p.PrestigeEarnedCheckpoint <= p.TotalCurrencyEarned && p.PrestigeResource >= 0
        && p.TotalPrestigeResourceEarned >= p.PrestigeResource
        && double.IsFinite(p.HighestProductionReached) && p.HighestProductionReached >= 0
        && p.UnlockedBots != null && p.UnlockedBots.All(id => !string.IsNullOrWhiteSpace(id))
        && p.OwnedBots != null && p.OwnedBots.All(id => !string.IsNullOrWhiteSpace(id))
        && p.Cosmetics != null && p.Cosmetics.All(id => !string.IsNullOrWhiteSpace(id))
        && p.UnlockedShopItems != null && p.UnlockedShopItems.All(id => !string.IsNullOrWhiteSpace(id))
        && p.ShopPurchases != null && p.ShopPurchases.All(pair => !string.IsNullOrWhiteSpace(pair.Key) && pair.Value >= 0)
        && double.IsFinite(p.Currency) && p.Currency >= 0 && double.IsFinite(p.TotalCurrencyEarned)
        && p.TotalCurrencyEarned >= p.Currency && p.ProgressionLevel >= 1 && p.PrestigeCount >= 0
        && p.HighestProgressionReached >= p.ProgressionLevel && p.LastUpdateUnix >= 0
        && double.IsFinite(p.CycleElapsedSeconds) && p.CycleElapsedSeconds >= 0
        && double.IsFinite(p.DeveloperCredits) && p.DeveloperCredits >= 0
        && p.UnlockedMilestones != null && p.ClaimedRewards != null && p.ContentState != null
        && p.UnlockedMilestones.All(id => !string.IsNullOrWhiteSpace(id))
        && p.ClaimedRewards.All(id => !string.IsNullOrWhiteSpace(id) && p.UnlockedMilestones.Contains(id));
}
