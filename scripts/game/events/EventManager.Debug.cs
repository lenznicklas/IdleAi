#if DEBUG
using System;

namespace IdleAi;

public sealed partial class EventManager
{
    internal EventActionResult DebugSetGenerator(string id, int level)
    {
        if (_actionInProgress) return Busy();
        Tick();
        if (_active == null) return new(false, "No active event.");
        var d = _active;
        foreach (var g in _production[d.Id].Generators)
            if (g.Id == id)
                return Transaction(d, _ =>
                {
                    var p = _save.ProgressByEventId[d.Id];
                    p.Generators[id].Level = Math.Clamp(level, g.StartingLevel, g.MaxLevel);
                    _production[d.Id].RefreshUnlocks(p);
                    _production[d.Id].RefreshHistory(p);
                    return true;
                }, "Generator level set.");
        return new(false, "Unknown generator ID.");
    }
    internal EventActionResult DebugGameplay(string command, long amount = 0)
    {
        Tick();
        if (_active == null) return new(false, "No active event.");
        var d = _active;
        return Transaction(d, _ =>
        {
            var p = _save.ProgressByEventId[d.Id];
            switch (command)
            {
                case "cores":
                    if (amount <= 0 || amount > long.MaxValue - p.TotalPrestigeResourceEarned) return false;
                    p.PrestigeResource += amount; p.TotalPrestigeResourceEarned += amount; break;
                case "generators": foreach (var g in _production[d.Id].Generators) if (g.Enabled) p.Generators[g.Id].Unlocked = true; break;
                case "bots": foreach (var bot in d.Bots) p.OwnedBots.Add(bot.Id); break;
                case "rewards":
                    double maximum = 0;
                    foreach (var m in d.Milestones) maximum = Math.Max(maximum, m.TargetAmount);
                    AddCurrency(d, p, Math.Max(0, maximum - p.TotalCurrencyEarned)); break;
                case "prestige":
                    var quote = GetPrestigeQuote(d.Id);
                    AddCurrency(d, p, Math.Max(0, quote.Requirement - quote.RunEarned)); break;
                default: return false;
            }
            _production[d.Id].RefreshHistory(p);
            return true;
        }, "Event debug state updated.");
    }
    internal EventActionResult DebugEndingSoon()
    {
        Tick();
        if (_active == null || ReadOnly) return new(false, "No active event.");
        var original = _active;
        var changed = original with { EndUnix = NowUnix + 23 * 3600 };
        int index = _definitions.FindIndex(d => d.Id == original.Id);
        _definitions[index] = changed;
        _byId[original.Id] = changed;
        _active = changed;
        Changed?.Invoke();
        return new(true, "Final 24-hour window simulated until restart.");
    }
    internal EventActionResult DebugResetHalloween()
    {
        Tick();
        if (ReadOnly || !_byId.ContainsKey(HalloweenEventDefinition.EventId)) return new(false, "Halloween unavailable.");
        string id = HalloweenEventDefinition.EventId;
        _save.ProgressByEventId.TryGetValue(id, out var old);
        var fresh = new EventProgressSaveData { LastUpdateUnix = NowUnix };
        _production[id].Initialize(fresh);
        _save.ProgressByEventId[id] = fresh;
        if (!TryPersist())
        {
            if (old == null) _save.ProgressByEventId.Remove(id); else _save.ProgressByEventId[id] = old;
            return SaveFailure();
        }
        Tick(); Changed?.Invoke();
        return new(true, "Halloween progress reset; global cosmetics preserved.");
    }
}
#endif
