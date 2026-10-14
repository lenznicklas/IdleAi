using System;
using System.Collections.Generic;
using System.Linq;

namespace IdleAi;

public readonly record struct EventActionResult(bool Changed, string Message);
public readonly record struct EventProgressSnapshot(double Currency, double TotalEarned, int Level,
    int HighestLevel, int PrestigeCount, double DeveloperCredits, bool Ended)
{
    public long PrestigeResource { get; init; }
    public double HighestProduction { get; init; }
    public double RunEarned { get; init; }
}

/// <summary>Only the manager selects schedules, settles timestamps and changes event economy.
/// Online ticks and offline recovery use the same cursor; opening views never grants income.
/// Overlaps select Major before Seasonal before Weekend, then latest start, then stable ID.</summary>
public sealed partial class EventManager
{
    private readonly EventSaveData _save;
    private readonly List<EventDefinition> _definitions;
    private readonly Dictionary<string, EventDefinition> _byId = new(StringComparer.Ordinal);
    private readonly Func<long> _clock;
    private readonly Func<bool> _persist;
    private readonly Func<bool> _canWrite;
    private readonly Func<EventReward, Action?>? _grantExternalReward;
    private EventDefinition? _active;
    private readonly Dictionary<string, EventProductionService> _production = new(StringComparer.Ordinal);
    private bool _actionInProgress;
    private bool _persistInProgress;
    private readonly Func<EventReward, bool>? _validateCosmetic;
#if DEBUG
    private string? _debugForcedEventId;
#endif

    public event Action? Changed;
    public IReadOnlyList<EventDefinition> Definitions => _definitions;
    public EventDefinition? ActiveEvent => _active;
    public EventDefinition? DisplayEvent => _active ??
        (_save.LastEventId != null && _byId.TryGetValue(_save.LastEventId, out var last) && last.Enabled ? last : null);
    public long NowUnix => Math.Max(_save.LastEvaluationUnix, _clock());
    public bool ReadOnly => !_canWrite();

    // External rewards must return an undo action; event claim and main currency
    // are committed in one normal-game atomic save, or both rolled back on failure.
    public EventManager(EventSaveData save, IReadOnlyList<EventDefinition> definitions,
        Func<bool> persist, Func<long>? clock = null, Func<bool>? canWrite = null,
        Func<EventReward, Action?>? grantExternalReward = null, Func<EventReward, bool>? validateCosmetic = null,
        Func<double>? dataShardBalance = null, Func<double, Action?>? spendDataShards = null,
        Func<EventReward, bool>? cosmeticOwned = null)
    {
        if (!save.IsValid()) throw new ArgumentException("Invalid event save data.", nameof(save));
        _save = save;
        _definitions = new(definitions);
        _persist = persist;
        _clock = clock ?? (() => DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        _canWrite = canWrite ?? (() => true);
        _grantExternalReward = grantExternalReward;
        _validateCosmetic = validateCosmetic;
        _dataShardBalance = dataShardBalance; _spendDataShards = spendDataShards; _cosmeticOwned = cosmeticOwned;
        _save.Version = 2;
        foreach (var definition in definitions)
        {
            Validate(definition);
            if (!_byId.TryAdd(definition.Id, definition)) throw new ArgumentException("Duplicate event ID.");
            var production = new EventProductionService(definition);
            EventDefinitionValidator.Validate(definition, production.Generators);
            _production.Add(definition.Id, production);
            _challengeGroups.Add(definition.Id, definition.Challenges.Where(c => c.Enabled)
                .GroupBy(c => ChallengeSignal(c.Type)).ToDictionary(g => g.Key, g => g.ToArray()));
            if (_save.ProgressByEventId.TryGetValue(definition.Id, out var progress))
            {
                EventBotRules.Normalize(definition, progress);
                production.Initialize(progress);
            }
        }
    }

    public long RemainingSeconds(EventDefinition definition) => Math.Max(0, definition.EndUnix - NowUnix);
    public bool IsActive(EventDefinition definition) => _active?.Id == definition.Id
        && definition.Enabled && NowUnix >= definition.StartUnix && NowUnix < definition.EndUnix
        && !HasEnded(definition.Id) && !ReadOnly;
    public bool HasEnded(string id) => _save.ProgressByEventId.TryGetValue(id, out var p) && p.Ended;
    public EventProgressSnapshot GetProgress(string id) => _save.ProgressByEventId.TryGetValue(id, out var p)
        ? new(p.Currency, p.TotalCurrencyEarned, _production.TryGetValue(id, out var production) ? production.EventLevel(p) : p.ProgressionLevel, p.HighestProgressionReached,
            p.PrestigeCount, p.DeveloperCredits, p.Ended) { PrestigeResource = p.PrestigeResource,
                HighestProduction = p.HighestProductionReached, RunEarned = Math.Max(0, p.TotalCurrencyEarned - p.PrestigeEarnedCheckpoint) }
        : new(0, 0, 1, 1, 0, 0, false);
    public bool IsMilestoneUnlocked(string eventId, string milestoneId) =>
        _save.ProgressByEventId.TryGetValue(eventId, out var p) && p.UnlockedMilestones.Contains(milestoneId);
    public bool IsRewardClaimed(string eventId, string milestoneId) =>
        _save.ProgressByEventId.TryGetValue(eventId, out var p) && p.ClaimedRewards.Contains(milestoneId);

    public void Tick()
    {
        if (_actionInProgress || _persistInProgress) return;
        long now = NowUnix;
        if (ReadOnly) { _active = SelectAt(now); return; }
        bool changed = false;
        bool initialized = _save.LastEvaluationUnix == 0;
        if (initialized) _save.LastEvaluationUnix = now; // Never award time before the first enrollment.
        long cursor = _save.LastEvaluationUnix;
        while (cursor < now)
        {
            var selected = SelectAt(cursor);
            long next = now;
            // Split at schedule boundaries, including successor events while the app is closed.
            foreach (var d in _definitions)
            {
                if (!d.Enabled) continue;
                if (d.StartUnix > cursor && d.StartUnix < next) next = d.StartUnix;
                if (d.EndUnix > cursor && d.EndUnix < next) next = d.EndUnix;
            }
            if (selected != null)
            {
                _save.LastEventId = selected.Id;
                var progress = EnsureProgress(selected, cursor);
                if (selected.Challenges.Count > 0)
                {
                    long todayStart = now - now % 86400;
                    if (cursor < todayStart && todayStart < next) next = todayStart;
                    long dailyEnd = ChallengeDayEnd(progress.Challenges.DailyUtcDay);
                    if (cursor < dailyEnd && dailyEnd < next) next = dailyEnd;
                }
                int unlockedBefore = selected.Challenges.Count == 0 ? 0 : progress.Generators.Values.Count(g => g.Unlocked);
                double earned = _production[selected.Id].Settle(progress, next - cursor);
                if (earned > 0) { UnlockMilestones(selected, progress); changed = true; }
                var signals = earned > 0 ? EventChallengeSignal.Earnings : EventChallengeSignal.None;
                if (selected.Challenges.Count > 0 && unlockedBefore != progress.Generators.Values.Count(g => g.Unlocked))
                    signals |= EventChallengeSignal.Generators | EventChallengeSignal.Production;
                changed |= RefreshChallenges(selected, progress, signals, next, ChallengeUtcDay(cursor));
                if (earned > 0 || signals != EventChallengeSignal.None) changed |= RefreshBotProgress(selected, progress, next);
                if (earned > 0 || signals != EventChallengeSignal.None) changed |= RefreshShopUnlocks(selected, progress, next);
                progress.LastUpdateUnix = next;
            }
            cursor = next;
        }
        _save.LastEvaluationUnix = now;
        foreach (var definition in _definitions)
            if (now >= definition.EndUnix && _save.ProgressByEventId.TryGetValue(definition.Id, out var progress)
                && !progress.Ended)
            {
                progress.Ended = true;
                progress.LastUpdateUnix = Math.Min(now, definition.EndUnix);
                UpdateArchive(definition, progress);
                changed = true;
            }
        var active = SelectAt(now);
        bool activeChanged = active?.Id != _active?.Id;
        if (activeChanged) changed = true;
        _active = active;
        if (active != null)
        {
            changed |= !_save.ProgressByEventId.ContainsKey(active.Id);
            var progress = EnsureProgress(active, now);
            if (activeChanged) changed |= RefreshChallenges(active, progress, EventChallengeSignal.All, now);
            _save.LastEventId = active.Id;
        }
        // Old claims and unknown/retired event IDs remain untouched in the dictionary.
        if (initialized || _challengeScheduleDirty)
        {
            changed |= _challengeScheduleDirty;
            if (TryPersist()) _challengeScheduleDirty = false;
        }
        if (changed) Changed?.Invoke();
    }

    public EventActionResult Upgrade(string eventId) => Upgrade(eventId,
        _production.TryGetValue(eventId, out var production) ? production.Generators[0].Id : "");

    public EventActionResult ClaimReward(string eventId, string milestoneId) =>
        string.IsNullOrWhiteSpace(milestoneId) ? new(false, "Unknown milestone.") : ClaimRewards(eventId, milestoneId);

    public EventActionResult ClaimAllRewards(string eventId) => ClaimRewards(eventId, null);

    // Snapshot eligible rewards before granting: currency rewards cannot chain-unlock
    // additional claims in the same batch. One transaction, save and notification.
    private EventActionResult ClaimRewards(string eventId, string? milestoneId)
    {
        if (_actionInProgress) return Busy();
        Tick();
        if (ReadOnly || !_byId.TryGetValue(eventId, out var definition)
            || !_save.ProgressByEventId.TryGetValue(eventId, out var p)) return new(false, "Event unavailable.");
        var eligible = new List<EventMilestone>();
        foreach (var m in definition.Milestones)
            if ((milestoneId == null || m.Id == milestoneId) && CanClaimReward(eventId, m.Id)) eligible.Add(m);
        if (eligible.Count == 0) return new(false, "No claimable rewards.");
        return Transaction(definition, undo =>
        {
            var signals = EventChallengeSignal.Rewards;
            foreach (var m in eligible)
            {
                if (!GrantReward(definition, p, m.Reward, undo)) return false;
                p.ClaimedRewards.Add(m.Id);
                signals |= RewardChallengeSignals(m.Reward);
            }
            if (IsActive(definition)) RefreshChallenges(definition, p, signals, NowUnix);
            UpdateArchive(definition, p);
            return true;
        }, eligible.Count == 1 ? "Milestone reward claimed." : eligible.Count + " rewards claimed.");
    }

    private EventProgressSaveData EnsureProgress(EventDefinition definition, long timestamp)
    {
        if (!_save.ProgressByEventId.TryGetValue(definition.Id, out var p))
        {
            p = new() { LastUpdateUnix = timestamp };
            _save.ProgressByEventId.Add(definition.Id, p);
        }
        EventBotRules.Normalize(definition, p);
        _production[definition.Id].Initialize(p);
        UnlockMilestones(definition, p);
        EnsureChallengeSchedule(definition, p, timestamp);
        if (!_botEnrolled.TryGetValue(definition.Id, out var enrolled) || !ReferenceEquals(enrolled, p))
        {
            _botEnrolled[definition.Id] = p;
            if (!p.Ended && timestamp < definition.EndUnix) RefreshBotProgress(definition, p, timestamp);
            RefreshShopUnlocks(definition, p, timestamp);
        }
        return p;
    }

    private EventDefinition? SelectAt(long timestamp)
    {
#if DEBUG
        // A force command selects a whole sandbox session, including its ended state.
        // Do not silently fall back to a different live event when that sandbox expires.
        if (_debugForcedEventId != null)
            return _byId.TryGetValue(_debugForcedEventId, out var forced)
                && timestamp >= forced.StartUnix && timestamp < forced.EndUnix && !HasEnded(forced.Id) ? forced : null;
#endif
        EventDefinition? selected = null;
        foreach (var d in _definitions)
        {
            if (!d.Enabled || timestamp < d.StartUnix || timestamp >= d.EndUnix || HasEnded(d.Id)) continue;
            if (selected == null || d.Type > selected.Type || (d.Type == selected.Type
                && (d.StartUnix > selected.StartUnix || (d.StartUnix == selected.StartUnix
                    && string.CompareOrdinal(d.Id, selected.Id) < 0)))) selected = d;
        }
        return selected;
    }

    private void AddCurrency(EventDefinition d, EventProgressSaveData p, double amount)
    {
        if (!double.IsFinite(amount) || amount < 0 || !double.IsFinite(p.Currency + amount)
            || !double.IsFinite(p.TotalCurrencyEarned + amount)) throw new InvalidOperationException("Invalid event currency amount.");
        p.Currency += amount;
        p.TotalCurrencyEarned += amount;
        ExcludeRewardCurrencyFromRepeats(d, p, amount);
        UnlockMilestones(d, p);
        _production[d.Id].RefreshUnlocks(p);
        _production[d.Id].RefreshHistory(p);
    }
    private static void UnlockMilestones(EventDefinition d, EventProgressSaveData p)
    {
        if (p.Ended) return;
        foreach (var milestone in d.Milestones)
            if (p.TotalCurrencyEarned >= milestone.TargetAmount) p.UnlockedMilestones.Add(milestone.Id);
    }

    private bool TryPersist()
    {
        if (_persistInProgress) return false;
        _persistInProgress = true;
        try { return !ReadOnly && _persist(); }
        catch { return false; }
        finally { _persistInProgress = false; }
    }
    private static EventActionResult SaveFailure() => new(false, "Could not save event progress. Action was reverted.");

    private static void Validate(EventDefinition d)
    {
        var p = d.Progression;
        if (string.IsNullOrWhiteSpace(d.Id) || string.IsNullOrWhiteSpace(d.Name) || d.StartUnix <= 0
            || d.EndUnix <= d.StartUnix || !Enum.IsDefined(d.Type) || p.MaxLevel < 1
            || !double.IsFinite(p.CycleSeconds) || p.CycleSeconds <= 0
            || !double.IsFinite(p.BaseProduction) || p.BaseProduction < 0
            || !double.IsFinite(p.ProductionPerLevel) || p.ProductionPerLevel < 0
            || !double.IsFinite(p.BaseUpgradeCost) || p.BaseUpgradeCost <= 0
            || !double.IsFinite(p.UpgradeCostGrowth) || p.UpgradeCostGrowth < 1
            || !double.IsFinite(p.Production(p.MaxLevel)))
            throw new ArgumentException("Invalid event definition: " + d.Id);
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var m in d.Milestones)
            if (string.IsNullOrWhiteSpace(m.Id) || !ids.Add(m.Id) || !double.IsFinite(m.TargetAmount)
                || m.TargetAmount < 0 || !Enum.IsDefined(m.Reward.Type)
                || !double.IsFinite(m.Reward.Amount) || m.Reward.Amount < 0)
                throw new ArgumentException("Invalid event milestone: " + m.Id);
    }

#if DEBUG
    internal EventActionResult DebugForce(string eventId)
    {
        Tick();
        if (ReadOnly || !_byId.TryGetValue(eventId, out var original)) return new(false, "Unknown event ID.");
        long now = NowUnix;
        var forced = original with { Enabled = true, StartUnix = now - 1, EndUnix = now + 7 * 86400 };
        int index = _definitions.FindIndex(d => d.Id == eventId);
        var progress = EnsureProgress(original, now);
        bool ended = progress.Ended;
        progress.Ended = false;
        string? previousForcedId = _debugForcedEventId;
        _debugForcedEventId = eventId;
        _definitions[index] = forced;
        _byId[eventId] = forced;
        if (!TryPersist())
        {
            progress.Ended = ended;
            _debugForcedEventId = previousForcedId;
            _definitions[index] = original;
            _byId[eventId] = original;
            return SaveFailure();
        }
        Tick();
        Changed?.Invoke();
        return new(true, "Event schedule overridden for seven days, until restart.");
    }

    internal EventActionResult DebugAddCurrency(double amount)
    {
        Tick();
        if (_active == null || !double.IsFinite(amount) || amount <= 0 || ReadOnly) return new(false, "No active event.");
        var before = _save.ProgressByEventId[_active.Id].Copy();
        AddCurrency(_active, _save.ProgressByEventId[_active.Id], amount);
        RefreshChallenges(_active, _save.ProgressByEventId[_active.Id], EventChallengeSignal.Earnings | EventChallengeSignal.Generators | EventChallengeSignal.Production, NowUnix);
        if (!TryPersist()) { _save.ProgressByEventId[_active.Id] = before; return SaveFailure(); }
        Changed?.Invoke();
        return new(true, "Added test event currency.");
    }

    internal EventActionResult DebugExpire()
    {
        Tick();
        if (_active == null || ReadOnly) return new(false, "No active event.");
        var d = _active;
        var result = Transaction(d, _ =>
        {
            var p = _save.ProgressByEventId[d.Id];
            p.Ended = true;
            UpdateArchive(d, p);
            return true;
        }, "Event expiration simulated and saved.");
        if (result.Changed) Tick();
        return result;
    }

    internal EventActionResult DebugResetTest()
    {
        Tick();
        if (ReadOnly || !_byId.ContainsKey(EventCatalog.TestEventId)) return new(false, "Test event unavailable.");
        _save.ProgressByEventId.TryGetValue(EventCatalog.TestEventId, out var previous);
        _save.ProgressByEventId[EventCatalog.TestEventId] = new() { LastUpdateUnix = NowUnix };
        if (!TryPersist())
        {
            if (previous == null) _save.ProgressByEventId.Remove(EventCatalog.TestEventId);
            else _save.ProgressByEventId[EventCatalog.TestEventId] = previous;
            return SaveFailure();
        }
        Tick();
        Changed?.Invoke();
        return new(true, "Test Protocol progress reset.");
    }
#endif
}
