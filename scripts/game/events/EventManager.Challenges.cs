using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace IdleAi;

[Flags]
internal enum EventChallengeSignal
{
    None = 0, Earnings = 1, Upgrades = 2, Generators = 4, Prestiges = 8, Cores = 16,
    Bots = 32, Rewards = 64, Shop = 128, Production = 256, PlayTime = 512, All = 1023
}

public sealed partial class EventManager
{
    private readonly Dictionary<string, Dictionary<EventChallengeSignal, EventChallengeDefinition[]>> _challengeGroups = [];
    private bool _challengeScheduleDirty;
#if DEBUG
    private long _debugChallengeDayOffset;
#endif
    private long ChallengeUtcDay(long timestamp) => timestamp / 86400
#if DEBUG
        + _debugChallengeDayOffset
#endif
        ;
    private long ChallengeDayEnd(long day) => (day + 1
#if DEBUG
        - _debugChallengeDayOffset
#endif
        ) * 86400;
    private static EventChallengeSignal ChallengeSignal(EventChallengeType type) => type switch
    {
        EventChallengeType.EarnCurrency or EventChallengeType.EarnLifetimeCurrency => EventChallengeSignal.Earnings,
        EventChallengeType.UpgradeGenerators => EventChallengeSignal.Upgrades,
        EventChallengeType.ReachGeneratorLevel or EventChallengeType.ReachTotalGeneratorLevels or EventChallengeType.UnlockGenerator or EventChallengeType.UnlockGenerators => EventChallengeSignal.Generators,
        EventChallengeType.PerformPrestige => EventChallengeSignal.Prestiges,
        EventChallengeType.ReachPrestigeCurrency => EventChallengeSignal.Cores,
        EventChallengeType.UnlockBot or EventChallengeType.OwnBot or EventChallengeType.EquipBot => EventChallengeSignal.Bots,
        EventChallengeType.ClaimRewards => EventChallengeSignal.Rewards,
        EventChallengeType.PurchaseShopItems => EventChallengeSignal.Shop,
        EventChallengeType.ReachProductionPerSecond => EventChallengeSignal.Production,
        EventChallengeType.PlayMinutes => EventChallengeSignal.PlayTime,
        _ => EventChallengeSignal.None
    };
    private double ChallengeMetric(EventDefinition d, EventProgressSaveData p, EventChallengeDefinition c) => c.Type switch
    {
        EventChallengeType.EarnCurrency or EventChallengeType.EarnLifetimeCurrency => p.TotalCurrencyEarned,
        EventChallengeType.UpgradeGenerators => p.Challenges.TotalGeneratorUpgrades,
        EventChallengeType.ReachGeneratorLevel => _production[d.Id].Generators.Where(g => g.Enabled && p.Generators[g.Id].Unlocked
            && (c.GeneratorId.Length == 0 || g.Id == c.GeneratorId)).Select(g => (double)p.Generators[g.Id].Level).DefaultIfEmpty().Max(),
        EventChallengeType.ReachTotalGeneratorLevels => _production[d.Id].Generators.Where(g => g.Enabled && p.Generators[g.Id].Unlocked).Sum(g => (double)p.Generators[g.Id].Level),
        EventChallengeType.UnlockGenerators => _production[d.Id].Generators.Count(g => g.Enabled && p.Generators[g.Id].Unlocked),
        EventChallengeType.UnlockGenerator => p.Generators.TryGetValue(c.GeneratorId, out var g) && g.Unlocked ? 1 : 0,
        EventChallengeType.PerformPrestige => p.PrestigeCount,
        EventChallengeType.ReachPrestigeCurrency => p.PrestigeResource,
        EventChallengeType.UnlockBot => p.UnlockedBots.Contains(c.BotId) || p.OwnedBots.Contains(c.BotId) ? 1 : 0,
        EventChallengeType.OwnBot => p.OwnedBots.Contains(c.BotId) ? 1 : 0,
        EventChallengeType.EquipBot => p.ActiveBotId == c.BotId && p.OwnedBots.Contains(c.BotId) ? 1 : 0,
        EventChallengeType.ClaimRewards => p.ClaimedRewards.Count,
        EventChallengeType.PurchaseShopItems => c.ShopItemId.Length > 0 ? PurchasedCount(d.Id, c.ShopItemId) : p.ShopPurchases.Values.Sum(v => (double)v),
        EventChallengeType.ReachProductionPerSecond => _production[d.Id].TotalPerSecond(p),
        EventChallengeType.PlayMinutes => p.Challenges.ActivePlaySeconds / 60,
        _ => 0
    };
    private EventChallengeProgress NewChallengeProgress(EventDefinition d, EventProgressSaveData p, EventChallengeDefinition c) => new()
    {
        Baseline = (c.Category == EventChallengeCategory.Daily && EventChallengeRules.IsCumulative(c.Type)) || c.Type == EventChallengeType.EarnCurrency
            ? ChallengeMetric(d, p, c) : 0
    };

    // Called only on enrollment/UTC boundaries, never by a view. Old saves keep all other progression.
    private void EnsureChallengeSchedule(EventDefinition d, EventProgressSaveData p, long timestamp, bool forceDaily = false)
    {
        if (d.Challenges.Count == 0 || p.Ended || NowUnix >= d.EndUnix) return;
        bool changed = false;
        foreach (var c in d.Challenges)
            if (c.Enabled && c.Category != EventChallengeCategory.Daily && !p.Challenges.Progress.ContainsKey(c.Id))
            {
                p.Challenges.Progress[c.Id] = NewChallengeProgress(d, p, c);
                changed = true;
            }
        long day = ChallengeUtcDay(timestamp);
        // Offline settlement reaches today's boundary before enrolling today's tasks.
        // Never generate retroactive Dailies for days missed while the app was closed.
        if (day == ChallengeUtcDay(NowUnix) && (day > p.Challenges.DailyUtcDay || forceDaily) && SelectAt(NowUnix)?.Id == d.Id)
        {
            foreach (string id in p.Challenges.DailyIds)
                if (p.Challenges.Progress.Remove(id, out var old) && (old.Complete || old.TimesClaimed > 0))
                    p.Challenges.DailyHistory[p.Challenges.DailyUtcDay.ToString(CultureInfo.InvariantCulture) + ":" + id] = old;
            p.Challenges.DailyUtcDay = day;
            p.Challenges.DailyIds.Clear();
            var candidates = d.Challenges.Where(c => c.Enabled && c.Category == EventChallengeCategory.Daily && DailyEligible(d, p, c))
                .OrderBy(c => DailyRank(d.Id, day, c.Id)).ThenBy(c => c.Id, StringComparer.Ordinal).Take(d.DailyChallengeCount);
            foreach (var c in candidates)
            {
                p.Challenges.DailyIds.Add(c.Id);
                p.Challenges.Progress[c.Id] = NewChallengeProgress(d, p, c);
            }
            changed = true;
        }
        if (!changed) return;
        p.Challenges.Revision++;
        _challengeScheduleDirty = true;
        RefreshChallenges(d, p, EventChallengeSignal.All, timestamp);
    }
    private bool DailyEligible(EventDefinition d, EventProgressSaveData p, EventChallengeDefinition c)
    {
        if (!_production[d.Id].RequirementsMet(c.Availability, p)) return false;
        return c.Type switch
        {
            EventChallengeType.ClaimRewards => d.Milestones.Count(m => !p.ClaimedRewards.Contains(m.Id)) >= c.Target,
            EventChallengeType.PurchaseShopItems => d.Shop.Where(s => s.Enabled && (c.ShopItemId.Length == 0 || s.Id == c.ShopItemId)).Sum(s => s.PurchaseLimit == 0 ? c.Target : (double)Math.Max(0, s.PurchaseLimit - PurchasedCount(d.Id, s.Id))) >= c.Target,
            EventChallengeType.PerformPrestige or EventChallengeType.ReachPrestigeCurrency => d.Prestige.Enabled,
            EventChallengeType.UnlockBot => d.Bots.Any(b => b.Id == c.BotId) && !p.OwnedBots.Contains(c.BotId),
            EventChallengeType.UnlockGenerator => _production[d.Id].Generators.Any(g => g.Id == c.GeneratorId && g.Enabled && !p.Generators[g.Id].Unlocked),
            EventChallengeType.ReachGeneratorLevel => _production[d.Id].Generators.Any(g => g.Enabled && g.MaxLevel >= c.Target && (c.GeneratorId.Length == 0 || c.GeneratorId == g.Id)),
            EventChallengeType.UpgradeGenerators => _production[d.Id].Generators.Where(g => g.Enabled).Sum(g => (double)Math.Max(0, g.MaxLevel - p.Generators[g.Id].Level)) >= c.Target,
            _ => true
        };
    }
    // Stable across platforms/builds; string.GetHashCode is intentionally randomized by .NET.
    private static uint DailyRank(string eventId, long day, string id)
    {
        uint hash = 2166136261;
        foreach (char c in eventId + ":" + day.ToString(CultureInfo.InvariantCulture) + ":" + id) hash = unchecked((hash ^ c) * 16777619);
        return hash;
    }
    private bool RefreshChallenges(EventDefinition d, EventProgressSaveData p, EventChallengeSignal signals, long timestamp, long? earnedUtcDay = null)
    {
        if (p.Ended || timestamp > d.EndUnix || !_challengeGroups.TryGetValue(d.Id, out var groups)) return false;
        bool changed = false;
        foreach (var group in groups)
        {
            if ((group.Key & signals) == 0) continue;
            foreach (var c in group.Value)
            {
                if (!p.Challenges.Progress.TryGetValue(c.Id, out var state)
                    || c.Category == EventChallengeCategory.Daily && p.Challenges.DailyUtcDay != (earnedUtcDay ?? ChallengeUtcDay(timestamp))) continue;
                if (state.Claimed && !c.Repeatable) continue;
                double metric = ChallengeMetric(d, p, c);
                double current = Math.Clamp(metric - state.Baseline, 0, c.Target);
                if (state.Claimed && c.Repeatable)
                {
                    if (current <= 0) continue;
                    state.Claimed = false; state.Complete = false; state.Progress = 0;
                    changed = true;
                }
                double progress = c.UseCurrentState ? current : Math.Max(state.Progress, current);
                bool complete = progress >= c.Target;
                if (state.Progress == progress && state.Complete == complete) continue;
                state.Progress = progress; state.Complete = complete; changed = true;
            }
        }
        if (changed) p.Challenges.Revision++;
        return changed;
    }

    public long GetChallengeRevision(string eventId) => _save.ProgressByEventId.TryGetValue(eventId, out var p) ? p.Challenges.Revision : 0;
    public long GetChallengeDailyUtcDay(string eventId) => _save.ProgressByEventId.TryGetValue(eventId, out var p) ? p.Challenges.DailyUtcDay : 0;
    public IReadOnlyList<EventChallengeSnapshot> GetChallenges(string eventId)
    {
        var result = new List<EventChallengeSnapshot>();
        if (!_byId.TryGetValue(eventId, out var d) || !_save.ProgressByEventId.TryGetValue(eventId, out var p)) return result;
        foreach (var c in d.Challenges)
            if (c.Enabled && p.Challenges.Progress.TryGetValue(c.Id, out var state))
                result.Add(new(c, state.Progress, state.Complete, state.Claimed, state.TimesClaimed, CanClaimChallenge(d, p, c, state)));
        return result;
    }
    public int ClaimableChallengeCount(string eventId)
    {
        if (!_byId.TryGetValue(eventId, out var d) || !_save.ProgressByEventId.TryGetValue(eventId, out var p)) return 0;
        int count = 0;
        foreach (var c in d.Challenges)
            if (c.Enabled && p.Challenges.Progress.TryGetValue(c.Id, out var state) && CanClaimChallenge(d, p, c, state)) count++;
        return count;
    }
    private bool CanClaimChallenge(EventDefinition d, EventProgressSaveData p, EventChallengeDefinition c, EventChallengeProgress state) =>
        !ReadOnly && state.Complete && !state.Claimed && state.TimesClaimed < int.MaxValue
        && (CanGrantAfterEnd(c.Reward) || IsActive(d));
    public EventActionResult ClaimChallenge(string eventId, string challengeId) => string.IsNullOrWhiteSpace(challengeId)
        ? new(false, "Unknown challenge.") : ClaimChallenges(eventId, challengeId);
    public EventActionResult ClaimAllChallenges(string eventId) => ClaimChallenges(eventId, null);
    private EventActionResult ClaimChallenges(string eventId, string? id)
    {
        if (_actionInProgress) return Busy();
        Tick();
        if (ReadOnly || !_byId.TryGetValue(eventId, out var d) || !_save.ProgressByEventId.TryGetValue(eventId, out var p)) return new(false, "Event unavailable.");
        var eligible = d.Challenges.Where(c => c.Enabled && (id == null || id == c.Id)
            && p.Challenges.Progress.TryGetValue(c.Id, out var state) && CanClaimChallenge(d, p, c, state)).ToArray();
        if (eligible.Length == 0) return new(false, "No claimable challenges.");
        return Transaction(d, undo =>
        {
            var signals = EventChallengeSignal.None;
            foreach (var c in eligible)
            {
                if (!GrantReward(d, p, c.Reward, undo)) return false;
                var state = p.Challenges.Progress[c.Id];
                state.Claimed = true; state.TimesClaimed++;
                signals |= RewardChallengeSignals(c.Reward);
            }
            foreach (var c in eligible)
                if (c.Repeatable) p.Challenges.Progress[c.Id].Baseline = ChallengeMetric(d, p, c);
            p.Challenges.Revision++;
            // Only active events advance other objectives; expired permanent grants cannot unlock tasks.
            if (IsActive(d)) RefreshChallenges(d, p, signals, NowUnix);
            return true;
        }, eligible.Length == 1 ? "Challenge reward claimed." : eligible.Length + " challenge rewards claimed.");
    }
    // Repeatable earning goals require new production, not mutually feeding currency rewards.
    private void ExcludeRewardCurrencyFromRepeats(EventDefinition d, EventProgressSaveData p, double amount)
    {
        if (!_challengeGroups[d.Id].TryGetValue(EventChallengeSignal.Earnings, out var goals)) return;
        foreach (var c in goals)
            if (c.Repeatable && p.Challenges.Progress.TryGetValue(c.Id, out var state)) state.Baseline += amount;
    }
    private static EventChallengeSignal RewardChallengeSignals(EventReward reward)
    {
        var signals = reward.Type switch
        {
            EventRewardType.EventCurrency => EventChallengeSignal.Earnings | EventChallengeSignal.Generators | EventChallengeSignal.Production,
            EventRewardType.PrestigeResource => EventChallengeSignal.Cores | EventChallengeSignal.Production,
            EventRewardType.EventBot => EventChallengeSignal.Bots,
            _ => EventChallengeSignal.None
        };
        foreach (var part in reward.Contents) signals |= RewardChallengeSignals(part);
        return signals;
    }
    public void RecordEventPlayTime(string eventId, double seconds)
    {
        if (_actionInProgress || !double.IsFinite(seconds) || seconds <= 0 || !_byId.TryGetValue(eventId, out var d)
            || d.Challenges.Count == 0 || !IsActive(d) || !_save.ProgressByEventId.TryGetValue(eventId, out var p)
            || !double.IsFinite(p.Challenges.ActivePlaySeconds + seconds)) return;
        p.Challenges.ActivePlaySeconds += seconds;
        if (RefreshChallenges(d, p, EventChallengeSignal.PlayTime, NowUnix))
        {
            RefreshShopUnlocks(d, p, NowUnix);
            Changed?.Invoke();
        }
    }
}
