using System;
using System.Collections.Generic;

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
            double cost = production.UpgradeCost(g, state.Level);
            bool unlocked = g.Enabled && state.Unlocked;
            return new(state.Level, unlocked, output, IsActive(d) && unlocked ? output / g.CycleSeconds : 0, cost,
                IsActive(d) && unlocked && state.Level < g.MaxLevel && double.IsFinite(cost) && p.Currency >= cost);
        }
        return default;
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
            _production[eventId].RefreshUnlocks(p);
            _production[eventId].RefreshHistory(p);
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
            EventPrestigeService.Reset(_save.ProgressByEventId[eventId], _production[eventId], quote.ResourceGained);
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
        try { committed = mutate(undo) && _save.IsValid() && TryPersist(); }
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
    private static void UpdateArchive(EventDefinition d, EventProgressSaveData p)
    {
        if (!p.Ended || d.Type != EventType.Major) return;
        p.Archive = new()
        {
            EventId = d.Id, Name = d.Name, Year = DateTimeOffset.FromUnixTimeSeconds(d.StartUnix).Year,
            CurrencyName = d.CurrencyName, TotalEarned = p.TotalCurrencyEarned,
            HighestLevel = p.HighestProgressionReached, PrestigeCount = p.PrestigeCount,
            PrestigeResource = p.PrestigeResource, TotalPrestigeResourceEarned = p.TotalPrestigeResourceEarned,
            HighestProduction = p.HighestProductionReached, RewardsClaimed = p.ClaimedRewards.Count,
            RewardsTotal = d.Milestones.Count, CosmeticsOwned = p.Cosmetics.Count
        };
    }
    public IEnumerable<EventArchiveResult> GetArchives()
    {
        foreach (var p in _save.ProgressByEventId.Values) if (p.Archive != null) yield return p.Archive.Copy();
    }
    public long GetTotalPrestigeResourceEarned(string id) => _save.ProgressByEventId.TryGetValue(id, out var p) ? p.TotalPrestigeResourceEarned : 0;
    public string GetMilestoneTargetText(string id, string milestoneId)
    {
        foreach (var m in _byId[id].Milestones) if (m.Id == milestoneId) return NumberFormatter.Format(m.TargetAmount) + " " + _byId[id].CurrencyName;
        return milestoneId;
    }
    public int ClaimedRewardCount(string id) => _save.ProgressByEventId.TryGetValue(id, out var p) ? p.ClaimedRewards.Count : 0;
}
