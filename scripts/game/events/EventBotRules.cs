using System;
using System.Linq;

namespace IdleAi;

/// <summary>Pure event-scoped rules shared by production, offline settlement and manager quotes.</summary>
internal static class EventBotRules
{
    internal static EventBotDefinition? Equipped(EventDefinition d, EventProgressSaveData p) =>
        p.ActiveBotId != null && p.OwnedBots.Contains(p.ActiveBotId)
            ? d.Bots.FirstOrDefault(b => b.Id == p.ActiveBotId && b.Enabled) : null;
    internal static double Multiplier(EventDefinition d, EventProgressSaveData p) => Equipped(d, p)?.ProductionMultiplier ?? 1;
    internal static double UpgradeCostMultiplier(EventDefinition d, EventProgressSaveData p) =>
        1 - (Equipped(d, p)?.SecondaryBonus?.GeneratorUpgradeDiscount ?? 0);
    internal static void Normalize(EventDefinition d, EventProgressSaveData p)
    {
        // Missing fields initialize naturally. Retired ownership stays in saves, but cannot produce/equip.
        p.UnlockedBots.UnionWith(p.OwnedBots);
        if (p.ActiveBotId != null && Equipped(d, p) == null) p.ActiveBotId = null;
    }
    internal static bool Meets(EventBotUnlockCondition c, EventProgressSaveData p) =>
        p.TotalCurrencyEarned >= c.LifetimeCurrency && p.PrestigeCount >= c.PrestigeCount
        && p.TotalPrestigeResourceEarned >= c.PrestigeCurrency
        && (c.MilestoneId.Length == 0 || p.ClaimedRewards.Contains(c.MilestoneId))
        && (c.ChallengeId.Length == 0 || ChallengeComplete(p, c.ChallengeId))
        && (c.ShopItemId.Length == 0 || p.ShopPurchases.TryGetValue(c.ShopItemId, out int count) && count > 0);
    internal static bool ChallengeComplete(EventProgressSaveData p, string id) =>
        p.Challenges.Progress.TryGetValue(id, out var challenge) && (challenge.Complete || challenge.Claimed)
        || p.Challenges.DailyHistory.Any(pair => pair.Key.EndsWith(":" + id, StringComparison.Ordinal) && (pair.Value.Complete || pair.Value.Claimed));
}

