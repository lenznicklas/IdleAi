using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using IdleAi;

// Only the catalog ID is needed; no Godot, assets, game saves, or copied economy formulas.
namespace IdleAi { public static class EventCatalog { public const string TestEventId = "event_test_001"; } }

internal static class Program
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    private static void Main(string[] args)
    {
        string? Option(string key) { int i = Array.IndexOf(args, key); return i >= 0 ? args[i + 1] : null; }
        double Number(string key, double fallback) => Option(key) is { } value
            ? double.Parse(value, System.Globalization.CultureInfo.InvariantCulture) : fallback;
        var definition = Option("--config") is { } path
            ? JsonSerializer.Deserialize<EventDefinition>(File.ReadAllText(path))! : TestProtocolEventDefinition.Create();
        if (Option("--export") is { } export) { File.WriteAllText(export, JsonSerializer.Serialize(definition, Json)); return; }
        var service = new EventProductionService(definition);
        EventDefinitionValidator.Validate(definition, service.Generators);
        var p = new EventProgressSaveData();
        service.Initialize(p);
        double minutes = Number("--minutes", 60), interval = Number("--visit-seconds", 30);
        double offline = Number("--offline-hours", 6);
        if (!double.IsFinite(minutes + interval + offline) || minutes < 0 || minutes > 525600 || interval < 1 || offline < 0 || offline > 8760)
            throw new ArgumentException("Use 0–525600 minutes, visit interval >= 1 second, and 0–8760 offline hours.");
        bool prestige = !args.Contains("--no-prestige");
        double elapsed = 0, lastPrestige = 0;
        double firstPrestige = 0, secondRun = 0, earlyShare = 0;
        var unlockTimes = new System.Collections.Generic.Dictionary<string, double>();
        var seen = new System.Collections.Generic.HashSet<string>();
        void Report(string message) => Console.WriteLine($"{elapsed / 60,7:F2}m {message}");
        void Unlocks()
        {
            foreach (var g in service.Generators)
                if (p.Generators[g.Id].Unlocked && seen.Add(g.Id))
                { unlockTimes[g.Id] = elapsed; Report($"unlock {g.Name}"); }
            foreach (var m in definition.Milestones)
                if (p.TotalCurrencyEarned >= m.TargetAmount && seen.Add(m.Id)) Report($"milestone {m.Id}: {m.TargetAmount:N0}");
        }
        void Snapshot(string label) => Report($"{label}: earned={p.TotalCurrencyEarned:N0}, wallet={p.Currency:N0}, /s={service.TotalPerSecond(p):N1}, prestiges={p.PrestigeCount}, multiplier={service.PrestigeMultiplier(p):F2}; "
            + string.Join(", ", service.Generators.Where(g => p.Generators[g.Id].Unlocked).Select(g => $"{g.Id} L{p.Generators[g.Id].Level} ({service.CycleProduction(g, p) / g.CycleSeconds:N1}/s)")));
        Unlocks();
        while (elapsed < minutes * 60)
        {
            double step = Math.Min(interval, minutes * 60 - elapsed);
            service.Settle(p, step); elapsed += step; Unlocks();
            var quote = EventPrestigeService.Quote(definition, p, true);
            if (prestige && quote.Available)
            {
                Report($"prestige {p.PrestigeCount + 1}, run={(elapsed - lastPrestige) / 60:F2}m, +{quote.ResourceGained} cores, x{quote.NewMultiplier:F2}");
                if (p.PrestigeCount == 0) firstPrestige = elapsed;
                if (p.PrestigeCount == 1) secondRun = elapsed - lastPrestige;
                EventPrestigeService.Reset(p, service, quote.ResourceGained);
                if (EventPrestigeService.Quote(definition, p, true).Available) throw new Exception("Prestige immediately repeatable.");
                lastPrestige = elapsed; Unlocks();
            }
            // Buy the best marginal production per currency among affordable upgrades.
            for (int purchases = 0; purchases < 10000; purchases++)
            {
                EventGeneratorDefinition? best = null; double score = 0, cost = 0;
                foreach (var g in service.Generators)
                {
                    var state = p.Generators[g.Id];
                    double price = service.UpgradeCost(g, state.Level);
                    if (!g.Enabled || !state.Unlocked || state.Level >= g.MaxLevel || price > p.Currency) continue;
                    double before = service.CycleProduction(g, p);
                    state.Level++; double gain = (service.CycleProduction(g, p) - before) / g.CycleSeconds; state.Level--;
                    if (gain / price > score) { best = g; score = gain / price; cost = price; }
                }
                if (best == null) break;
                p.Currency -= cost; p.Generators[best.Id].Level++;
                if (seen.Add("first-upgrade")) Report($"first upgrade: {best.Name}");
                service.RefreshUnlocks(p); Unlocks();
            }
            service.RefreshHistory(p);
            if (elapsed >= 1200 && elapsed - step < 1200)
                earlyShare = service.Generators.Take(2).Sum(g => service.CycleProduction(g, p) / g.CycleSeconds) / service.TotalPerSecond(p);
            if (elapsed % 600 < interval || elapsed == minutes * 60) Snapshot("checkpoint");
        }
        if (args.Contains("--check"))
        {
            void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
            Check(minutes == 60 && interval <= 30 && prestige && Option("--config") == null, "--check requires default balance, 60 minutes, visits <= 30 seconds, prestige enabled.");
            Check(firstPrestige >= 1200 && firstPrestige <= 2400, "First Prestige outside 20–40 minutes.");
            Check(secondRun >= 180 && secondRun < firstPrestige, "Second run should be faster without instant repetition.");
            Check(unlockTimes["compute_rack"] >= 60 && unlockTimes["compute_rack"] <= 180, "Second generator outside 1–3 minutes.");
            Check(unlockTimes.Count == 5 && unlockTimes["prototype_core"] > unlockTimes["quantum_cluster"], "Late generator progression missing.");
            Check(earlyShare >= .25, "Early generators lost relevance by minute 20.");
            var fresh = new EventProgressSaveData(); service.Initialize(fresh); service.Settle(fresh, 5);
            Check(fresh.Currency >= service.UpgradeCost(service.Generators[0], 1), "First upgrade unavailable after five seconds.");
            Check(double.IsFinite(p.TotalCurrencyEarned) && p.TotalCurrencyEarned < 100_000_000, "First-hour numeric runaway.");
            Report("pacing checks PASS");
        }
        var online = p.Copy();
        service.Settle(p, offline * 3600); elapsed += offline * 3600; Snapshot($"after {offline}h offline (no purchases or resets)");
        var sliced = online.Copy();
        double remaining = offline * 3600;
        while (remaining > 0) { double step = Math.Min(60, remaining); service.Settle(sliced, step); remaining -= step; }
        if (Math.Abs(sliced.TotalCurrencyEarned - p.TotalCurrencyEarned) > Math.Max(1e-6, p.TotalCurrencyEarned * 1e-10)
            || Math.Abs(sliced.Currency - p.Currency) > Math.Max(1e-6, p.Currency * 1e-10)
            || service.Generators.Any(g => sliced.Generators[g.Id].Unlocked != p.Generators[g.Id].Unlocked
                || Math.Abs(sliced.Generators[g.Id].CycleElapsedSeconds - p.Generators[g.Id].CycleElapsedSeconds) > 1e-6))
            throw new Exception("Offline settlement differs from minute-by-minute production.");
        var ready = EventPrestigeService.Quote(definition, p, true);
        Report($"return prestige available={ready.Available}, gain={ready.ResourceGained}; offline equivalence PASS");
        if (!new EventSaveData { ProgressByEventId = { [definition.Id] = p } }.IsValid())
            throw new Exception("Simulation produced invalid save state.");
    }
}
