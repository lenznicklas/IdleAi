using System;
using System.Collections.Generic;

namespace IdleAi;

/// <summary>Shared production and unlock rules for live ticks, offline settlement and UI quotes.</summary>
internal sealed class EventProductionService
{
    internal IReadOnlyList<EventGeneratorDefinition> Generators { get; }
    private readonly EventDefinition _definition;
    internal EventProductionService(EventDefinition definition)
    {
        _definition = definition;
        // Preserve old definitions/API without keeping a second production implementation.
        var legacy = definition.Progression;
        Generators = definition.Generators.Count > 0 ? definition.Generators :
        [new EventGeneratorDefinition { Id = "generator", Name = legacy.GeneratorName,
            BaseProduction = legacy.BaseProduction, ProductionPerLevel = legacy.ProductionPerLevel,
            BaseUpgradeCost = legacy.BaseUpgradeCost, UpgradeCostGrowth = legacy.UpgradeCostGrowth,
            MaxLevel = legacy.MaxLevel, CycleSeconds = legacy.CycleSeconds }];
    }
    internal void Initialize(EventProgressSaveData p)
    {
        bool legacy = p.GeneratorDataVersion == 0;
        for (int i = 0; i < Generators.Count; i++)
        {
            var g = Generators[i];
            if (p.Generators.TryGetValue(g.Id, out var existing))
            {
                existing.Level = Math.Clamp(existing.Level, g.StartingLevel, g.MaxLevel);
                existing.CycleElapsedSeconds %= g.CycleSeconds;
                continue;
            }
            p.Generators.Add(g.Id, new()
            {
                Level = legacy && i == 0 ? Math.Clamp(p.ProgressionLevel, g.StartingLevel, g.MaxLevel) : g.StartingLevel,
                CycleElapsedSeconds = legacy && i == 0 ? p.CycleElapsedSeconds % g.CycleSeconds : 0
            });
        }
        p.GeneratorDataVersion = 2;
        RefreshUnlocks(p);
        RefreshHistory(p);
    }
    internal double RunEarned(EventProgressSaveData p) => Math.Max(0, p.TotalCurrencyEarned - p.PrestigeEarnedCheckpoint);
    internal bool NonCurrencyRequirements(EventUnlockCondition c, EventProgressSaveData p) =>
        p.PrestigeCount >= c.RequiredPrestigeCount && (string.IsNullOrEmpty(c.RequiredGeneratorId)
        || p.Generators.TryGetValue(c.RequiredGeneratorId, out var required) && required.Unlocked && required.Level >= c.RequiredGeneratorLevel);
    internal bool RequirementsMet(EventUnlockCondition c, EventProgressSaveData p) => NonCurrencyRequirements(c, p)
        && (c.CurrencyScope == EventUnlockCurrencyScope.Lifetime ? p.TotalCurrencyEarned : RunEarned(p)) >= c.RequiredCurrencyEarned;
    internal void RefreshUnlocks(EventProgressSaveData p)
    {
        // Dependency chains can be declared in any catalog order. Bound work to generator count.
        for (int pass = 0; pass < Generators.Count; pass++)
        {
            bool changed = false;
            foreach (var g in Generators)
                if (g.Enabled && !p.Generators[g.Id].Unlocked && RequirementsMet(g.Unlock, p))
                { p.Generators[g.Id].Unlocked = true; changed = true; }
            if (!changed) break;
        }
    }
    internal double PrestigeMultiplier(EventProgressSaveData p) => _definition.Prestige.Enabled
        ? 1 + p.TotalPrestigeResourceEarned * _definition.Prestige.ProductionBonusPerResource : 1;
    internal double GlobalMultiplier(EventProgressSaveData p)
    {
        return _definition.ProductionMultiplier * PrestigeMultiplier(p) * EventBotRules.Multiplier(_definition, p);
    }
    internal double CycleProduction(EventGeneratorDefinition g, EventProgressSaveData p)
    {
        var state = p.Generators[g.Id];
        double levels = Math.Max(0, state.Level - g.StartingLevel);
        double raw = g.Scaling == EventProductionScaling.Exponential ? g.BaseProduction * Math.Pow(g.ProductionGrowth, levels)
            : g.BaseProduction + levels * g.ProductionPerLevel;
        return raw * g.Multiplier * state.Multiplier * GlobalMultiplier(p);
    }
    internal double UpgradeCost(EventGeneratorDefinition g, int level) =>
        g.BaseUpgradeCost * Math.Pow(g.UpgradeCostGrowth, Math.Max(0, level - g.StartingLevel));
    internal int EventLevel(EventProgressSaveData p)
    {
        long levels = 0;
        foreach (var g in Generators) if (g.Enabled && p.Generators[g.Id].Unlocked) levels += p.Generators[g.Id].Level;
        return (int)Math.Min(int.MaxValue, Math.Max(1, levels));
    }
    internal double TotalPerSecond(EventProgressSaveData p)
    {
        double total = 0;
        foreach (var g in Generators)
            if (g.Enabled && p.Generators[g.Id].Unlocked) total += CycleProduction(g, p) / g.CycleSeconds;
        return total;
    }
    internal void RefreshHistory(EventProgressSaveData p)
    {
        // Legacy progression fields remain a mirror of the first generator for compatibility.
        p.ProgressionLevel = p.Generators[Generators[0].Id].Level;
        p.CycleElapsedSeconds = p.Generators[Generators[0].Id].CycleElapsedSeconds;
        p.HighestProgressionReached = Math.Max(p.HighestProgressionReached, Math.Max(p.ProgressionLevel, EventLevel(p)));
        p.HighestProductionReached = Math.Max(p.HighestProductionReached, TotalPerSecond(p));
    }
    private static double Cycles(double seconds, double duration)
    {
        double ratio = seconds / duration;
        double rounded = Math.Round(ratio);
        // Normalize division roundoff at exact cycle boundaries, rather than adding gameplay time.
        if (Math.Abs(ratio - rounded) <= 2 * Math.Abs(Math.BitIncrement(ratio) - ratio)) ratio = rounded;
        return Math.Floor(ratio);
    }
    private double ProductionDuring(EventProgressSaveData p, double seconds)
    {
        double earned = 0;
        foreach (var g in Generators)
            if (g.Enabled && p.Generators[g.Id].Unlocked)
                earned += Cycles(p.Generators[g.Id].CycleElapsedSeconds + seconds, g.CycleSeconds) * CycleProduction(g, p);
        return earned;
    }
    internal double Settle(EventProgressSaveData p, double seconds)
    {
        double total = 0;
        while (seconds > 0)
        {
            RefreshUnlocks(p);
            double needed = double.PositiveInfinity;
            foreach (var g in Generators)
            {
                if (!g.Enabled || p.Generators[g.Id].Unlocked || !NonCurrencyRequirements(g.Unlock, p)) continue;
                double earned = g.Unlock.CurrencyScope == EventUnlockCurrencyScope.Lifetime ? p.TotalCurrencyEarned : RunEarned(p);
                needed = Math.Min(needed, g.Unlock.RequiredCurrencyEarned - earned);
            }
            double step = seconds;
            if (needed > 0 && double.IsFinite(needed) && ProductionDuring(p, seconds) >= needed)
            {
                // Find the first producing cycle that crosses an unlock. Binary-search
                // cycle counts, not individual cycles: years offline remain bounded work.
                foreach (var g in Generators)
                {
                    var state = p.Generators[g.Id];
                    if (!g.Enabled || !state.Unlocked) continue;
                    double count = Cycles(state.CycleElapsedSeconds + seconds, g.CycleSeconds);
                    long high = (long)Math.Min(count, long.MaxValue - 1.0);
                    if (high < 1) continue;
                    double last = high * g.CycleSeconds - state.CycleElapsedSeconds;
                    if (ProductionDuring(p, last) < needed) continue;
                    long low = 1;
                    while (low < high)
                    {
                        long middle = low + (high - low) / 2;
                        double time = middle * g.CycleSeconds - state.CycleElapsedSeconds;
                        if (ProductionDuring(p, time) >= needed) high = middle; else low = middle + 1;
                    }
                    step = Math.Min(step, low * g.CycleSeconds - state.CycleElapsedSeconds);
                }
            }
            if (step <= 0) break; // Invalid/overflowing external content cannot spin the game loop.
            double gain = ProductionDuring(p, step);
            foreach (var g in Generators)
            {
                var state = p.Generators[g.Id];
                if (!g.Enabled || !state.Unlocked) continue;
                double elapsed = state.CycleElapsedSeconds + step;
                state.CycleElapsedSeconds = Math.Clamp(elapsed - Cycles(elapsed, g.CycleSeconds) * g.CycleSeconds, 0, g.CycleSeconds);
            }
            if (double.IsFinite(gain) && double.IsFinite(p.TotalCurrencyEarned + gain) && double.IsFinite(p.Currency + gain))
            { p.Currency += gain; p.TotalCurrencyEarned += gain; total += gain; }
            seconds = Math.Max(0, seconds - step);
        }
        RefreshUnlocks(p);
        RefreshHistory(p);
        return total;
    }
}
