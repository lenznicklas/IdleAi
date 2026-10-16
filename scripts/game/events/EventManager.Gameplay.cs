using System;
using System.Collections.Generic;
using System.Linq;

namespace IdleAi;

public readonly record struct EventGeneratorSnapshot(int Level, bool Unlocked, double ProductionPerCycle,
    double ProductionPerSecond, double UpgradeCost, bool CanUpgrade);

public sealed partial class EventManager
{
    public EventDefinition? GetDefinition(string id) => _byId.TryGetValue(id, out var d) ? d : null;
    public IReadOnlyList<EventGeneratorDefinition> GetGenerators(string eventId) => _production[eventId].Generators;
    public EventGeneratorSnapshot GetGenerator(string eventId, string generatorId)
    {
        if (!_byId.TryGetValue(eventId, out var d) || !_save.ProgressByEventId.TryGetValue(eventId, out var p)) return default;
        var production = _production[eventId];
        foreach (var g in production.Generators)
        {
            if (g.Id != generatorId) continue;
            var state = p.Generators[g.Id];
            double output = production.CycleProduction(g, p);
            double cost = production.UpgradeCost(g, state.Level) * EventBotRules.UpgradeCostMultiplier(d, p);
            bool unlocked = g.Enabled && state.Unlocked;
            return new(state.Level, unlocked, output, IsActive(d) && unlocked ? output / g.CycleSeconds : 0, cost,
                IsActive(d) && unlocked && state.Level < g.MaxLevel && double.IsFinite(cost) && p.Currency >= cost);
        }
        return default;
    }
    // Presentation only: interpolate between the existing whole-second economy ticks.
    public double GetGeneratorCycleProgress(string eventId, string generatorId, double fractionalSecond = 0)
    {
        if (!_byId.TryGetValue(eventId, out var definition)
            || !_save.ProgressByEventId.TryGetValue(eventId, out var progress)
            || !progress.Generators.TryGetValue(generatorId, out var state) || !state.Unlocked) return 0;
        var generator = GetGenerators(eventId).FirstOrDefault(g => g.Id == generatorId && g.Enabled);
        if (generator == null || generator.CycleSeconds <= 0) return 0;
        double elapsed = state.CycleElapsedSeconds;
        if (IsActive(definition))
            elapsed += Math.Max(0, NowUnix - progress.LastUpdateUnix) + Math.Clamp(fractionalSecond, 0, 1);
        return Math.Clamp((elapsed % generator.CycleSeconds) / generator.CycleSeconds, 0, 1);
    }

    public double GetTotalProduction(string eventId) => _byId.TryGetValue(eventId, out var definition) && IsActive(definition)
        && _save.ProgressByEventId.TryGetValue(eventId, out var p) && _production.TryGetValue(eventId, out var production)
        ? production.TotalPerSecond(p) : 0;
    public string? GetNextUnlock(string eventId)
    {
        foreach (var g in GetGenerators(eventId)) if (g.Enabled && !GetGenerator(eventId, g.Id).Unlocked) return g.Name;
        return null;
    }
    public EventPrestigeQuote GetPrestigeQuote(string eventId) => _byId.TryGetValue(eventId, out var d)
        && _save.ProgressByEventId.TryGetValue(eventId, out var p) ? EventPrestigeService.Quote(d, p, IsActive(d)) : default;
    public EventActionResult Upgrade(string eventId, string generatorId)
    {
        if (_actionInProgress) return Busy();
        Tick();
        if (!_byId.TryGetValue(eventId, out var d) || !IsActive(d)) return new(false, "Event production is unavailable.");
        var snapshot = GetGenerator(eventId, generatorId);
        if (!snapshot.Unlocked) return new(false, "Generator is locked or disabled.");
        if (!snapshot.CanUpgrade) return new(false, "Cannot afford upgrade or maximum level reached.");
        var p = _save.ProgressByEventId[eventId];
        return Transaction(d, _ =>
        {
            p.Currency -= snapshot.UpgradeCost;
            p.Generators[generatorId].Level++;
            p.Challenges.TotalGeneratorUpgrades = checked(p.Challenges.TotalGeneratorUpgrades + 1);
            _production[eventId].RefreshUnlocks(p);
            _production[eventId].RefreshHistory(p);
            RefreshChallenges(d, p, EventChallengeSignal.Upgrades | EventChallengeSignal.Generators | EventChallengeSignal.Production, NowUnix);
            return true;
        }, "Generator upgraded.");
    }
    public EventActionResult Prestige(string eventId, int expectedPrestigeCount, long? expectedGain = null)
    {
        if (_actionInProgress) return Busy();
        Tick();
        var quote = GetPrestigeQuote(eventId);
        if (!quote.Available) return new(false, "Event prestige is unavailable.");
        if (quote.PrestigeCount != expectedPrestigeCount || expectedGain.HasValue && expectedGain.Value != quote.ResourceGained)
            return new(false, "Prestige preview changed. Review it again.");
        var d = _byId[eventId];
        return Transaction(d, _ =>
        {
            var p = _save.ProgressByEventId[eventId];
            EventPrestigeService.Reset(p, _production[eventId], quote.ResourceGained);
            RefreshChallenges(d, p, EventChallengeSignal.Prestiges | EventChallengeSignal.Cores | EventChallengeSignal.Generators | EventChallengeSignal.Production, NowUnix);
            return true;
        }, "Event prestige completed.");
    }
    private static EventActionResult Busy() => new(false, "An event action is already being saved.");

    // All multi-field event actions use this same atomic commit/rollback path.
    private EventActionResult Transaction(EventDefinition d, Func<List<Action>, bool> mutate, string success)
    {
        if (_actionInProgress || ReadOnly) return Busy();
        var before = _save.ProgressByEventId[d.Id].Copy();
        var cosmetics = new HashSet<string>(_save.CosmeticEntitlements);
        var undo = new List<Action>();
        bool committed = false;
        _actionInProgress = true;
        try
        {
            if (mutate(undo))
            {
                var progress = _save.ProgressByEventId[d.Id];
                RefreshBotProgress(d, progress, NowUnix);
                RefreshShopUnlocks(d, progress, NowUnix);

                committed = _save.IsValid() && TryPersist();
            }
        }
        catch { committed = false; }
        finally
        {
            if (!committed)
            {
                for (int i = undo.Count - 1; i >= 0; i--) undo[i]();
                _save.ProgressByEventId[d.Id] = before;
                _save.CosmeticEntitlements = cosmetics;
            }
            _actionInProgress = false;
        }
        if (!committed) return SaveFailure();
        Changed?.Invoke();
        return new(true, success);
    }
    public long GetTotalPrestigeResourceEarned(string id) => _save.ProgressByEventId.TryGetValue(id, out var p) ? p.TotalPrestigeResourceEarned : 0;
    public string GetMilestoneTargetText(string id, string milestoneId)
    {
        foreach (var m in _byId[id].Milestones) if (m.Id == milestoneId) return NumberFormatter.Format(m.TargetAmount) + " " + _byId[id].CurrencyName;
        return milestoneId;
    }
    public int ClaimedRewardCount(string id) => _byId.TryGetValue(id, out var d) && _save.ProgressByEventId.TryGetValue(id, out var p)
        ? d.Milestones.Count(m => p.ClaimedRewards.Contains(m.Id)) : 0;
}
