#if DEBUG
using System;
using System.Globalization;

namespace IdleAi;

// Enabled only with --event-tools in a Debug C# build, through the existing Shop code box.
internal sealed class EventDeveloperTools(EventManager manager)
{
    internal EventActionResult? Execute(string code)
    {
        if (code.Equals("halloween-force", StringComparison.OrdinalIgnoreCase)) return manager.DebugForce(HalloweenEventDefinition.EventId);
        if (code.Equals("halloween-reset", StringComparison.OrdinalIgnoreCase)) return manager.DebugResetHalloween();
        if (code.Equals("event-unlock-generators", StringComparison.OrdinalIgnoreCase)) return manager.DebugGameplay("generators");
        if (code.Equals("event-unlock-bots", StringComparison.OrdinalIgnoreCase)) return manager.DebugGameplay("bots");
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
