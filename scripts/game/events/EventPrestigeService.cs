using System;

namespace IdleAi;

public readonly record struct EventPrestigeQuote(bool Available, int PrestigeCount, double RunEarned,
    double Requirement, long ResourceGained, long CurrentResource, double CurrentMultiplier, double NewMultiplier);

internal static class EventPrestigeService
{
    internal static EventPrestigeQuote Quote(EventDefinition d, EventProgressSaveData p, bool active)
    {
        var config = d.Prestige;
        double earned = Math.Max(0, p.TotalCurrencyEarned - p.PrestigeEarnedCheckpoint);
        double requirement = config.Requirement * Math.Pow(config.RequirementGrowth, p.PrestigeCount);
        double raw = earned >= requirement ? Math.Floor(config.RewardMultiplier * Math.Pow(earned / requirement, config.RewardExponent)) : 0;
        long gain = double.IsFinite(raw) && raw >= 1 && raw < long.MaxValue - (double)p.PrestigeResource
            && raw < long.MaxValue - (double)p.TotalPrestigeResourceEarned ? (long)raw : 0;
        double multiplier = config.Enabled ? 1 + p.TotalPrestigeResourceEarned * config.ProductionBonusPerResource : 1;
        double next = config.Enabled ? 1 + (p.TotalPrestigeResourceEarned + (double)gain) * config.ProductionBonusPerResource : 1;
        return new(config.Enabled && active && gain > 0 && p.PrestigeCount < int.MaxValue, p.PrestigeCount,
            earned, requirement, gain, p.PrestigeResource, multiplier, next);
    }
    internal static void Reset(EventProgressSaveData p, EventProductionService production, long gain)
    {
        p.Currency = 0;
        p.PrestigeEarnedCheckpoint = p.TotalCurrencyEarned;
        p.PrestigeCount++;
        p.PrestigeResource += gain;
        p.TotalPrestigeResourceEarned += gain;
        foreach (var g in production.Generators)
        {
            var state = p.Generators[g.Id];
            state.Level = g.StartingLevel;
            state.Unlocked = false;
            state.CycleElapsedSeconds = 0;
            state.Multiplier = 1;
        }
        production.RefreshUnlocks(p);
        production.RefreshHistory(p);
        // Lifetime earnings, rewards, bots, cosmetics, shop limits and history deliberately survive.
    }
}
