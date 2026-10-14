#if DEBUG
using System;
using System.Globalization;

namespace IdleAi;

// Enabled only with --event-tools in a Debug C# build, through the existing Shop code box.
internal sealed class EventDeveloperTools(EventManager manager)
{
    internal EventActionResult? Execute(string code)
    {
        if (code.Equals("event-shop-unlock", StringComparison.OrdinalIgnoreCase)) return manager.DebugShop("unlock");
        if (code.Equals("event-shop-reset", StringComparison.OrdinalIgnoreCase)) return manager.DebugShop("reset");
        if (code.Equals("event-shop-print", StringComparison.OrdinalIgnoreCase)) return manager.DebugShop("print");
        if (code.Equals("event-shop-expire", StringComparison.OrdinalIgnoreCase)) return manager.DebugExpire();
        if (code.StartsWith("event-shop-fund:", StringComparison.OrdinalIgnoreCase)) return manager.DebugShop("fund", code[16..]);
        if (code.StartsWith("event-shop-count:", StringComparison.OrdinalIgnoreCase))
        {
            string[] values = code.Split(':');
            return values.Length == 3 && int.TryParse(values[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int count)
                ? manager.DebugShop("count", values[1], count) : new(false, "Use event-shop-count:item_id:2.");
        }
        if (code.Equals("event-challenges-complete", StringComparison.OrdinalIgnoreCase)
            || code.Equals("event-challenges-ready", StringComparison.OrdinalIgnoreCase)) return manager.DebugChallenges("complete");
        if (code.Equals("event-dailies-reset", StringComparison.OrdinalIgnoreCase)) return manager.DebugChallenges("daily");
        if (code.Equals("event-day-next", StringComparison.OrdinalIgnoreCase)) return manager.DebugChallenges("day");
        if (code.Equals("event-challenges-claim-all", StringComparison.OrdinalIgnoreCase))
            return manager.ActiveEvent is { } active ? manager.ClaimAllChallenges(active.Id) : new(false, "No active event.");
        if (code.StartsWith("event-challenge-progress:", StringComparison.OrdinalIgnoreCase))
        {
            string[] parts = code.Split(':');
            return parts.Length == 3 && double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double progress)
                ? manager.DebugChallenges("set", parts[1], progress) : new(false, "Use event-challenge-progress:challenge_id:10.");
        }
        if (code.Equals("halloween-force", StringComparison.OrdinalIgnoreCase)) return manager.DebugForce(HalloweenEventDefinition.EventId);
        if (code.Equals("halloween-reset", StringComparison.OrdinalIgnoreCase)) return manager.DebugResetHalloween();
        if (code.Equals("event-unlock-generators", StringComparison.OrdinalIgnoreCase)) return manager.DebugGameplay("generators");
        if (code.Equals("event-unlock-bots", StringComparison.OrdinalIgnoreCase)) return manager.DebugBots("all");
        if (code.Equals("event-bots-reset", StringComparison.OrdinalIgnoreCase)) return manager.DebugBots("reset");
        if (code.StartsWith("event-bot-grant:", StringComparison.OrdinalIgnoreCase)) return manager.DebugBots("grant", code[16..]);
        if (code.StartsWith("event-bot-equip:", StringComparison.OrdinalIgnoreCase)) return manager.DebugBots("equip", code[16..]);
        if (code.Equals("event-ready-rewards", StringComparison.OrdinalIgnoreCase)) return manager.DebugGameplay("rewards");
        if (code.Equals("event-ready-prestige", StringComparison.OrdinalIgnoreCase)) return manager.DebugGameplay("prestige");
        if (code.Equals("event-ending-soon", StringComparison.OrdinalIgnoreCase)) return manager.DebugEndingSoon();
        if (code.StartsWith("event-cores:", StringComparison.OrdinalIgnoreCase)) return long.TryParse(code[12..], out long cores)
            ? manager.DebugGameplay("cores", cores) : new(false, "Use event-cores:2.");
        if (code.StartsWith("event-level:", StringComparison.OrdinalIgnoreCase))
        {
            string[] parts = code.Split(':');
            return parts.Length == 3 && int.TryParse(parts[2], out int level) ? manager.DebugSetGenerator(parts[1], level)
                : new(false, "Use event-level:generator_id:10.");
        }
        if (code.Equals("event-end", StringComparison.OrdinalIgnoreCase)) return manager.DebugExpire();
        if (code.Equals("event-reset", StringComparison.OrdinalIgnoreCase)) return manager.DebugResetTest();
        if (code.StartsWith("event-force:", StringComparison.OrdinalIgnoreCase)) return manager.DebugForce(code[12..]);
        if (code.StartsWith("event-add:", StringComparison.OrdinalIgnoreCase))
            return double.TryParse(code[10..], NumberStyles.Float, CultureInfo.InvariantCulture, out double amount)
                ? manager.DebugAddCurrency(amount) : new(false, "Use event-add:1000.");
        return null;
    }
}
#endif
