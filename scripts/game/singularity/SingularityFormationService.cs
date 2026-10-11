using System;
using System.Collections.Generic;
using System.Linq;

namespace IdleAi;

public sealed record SingularityFormation(string Name, int[] Nodes, double Bonus);

// The actual topology is an eight-slot ring, not a square grid. No cross-sector patterns.
internal static class SingularityFormationService
{
    internal static List<SingularityFormation> Detect(SingularitySectorData sector)
    {
        var result = new List<SingularityFormation>();
        bool Is(int i, SingularityNodeType type) => sector.Nodes[(i + 16) % 8].Type == type;
        void Add(string name, double bonus, params int[] nodes)
        {
            nodes = nodes.Select(n => (n + 16) % 8).OrderBy(n => n).ToArray();
            if (!result.Any(f => f.Name == name && f.Nodes.SequenceEqual(nodes)))
                result.Add(new(name, nodes, bonus));
        }
        for (int i = 0; i < 8; i++)
        {
            if (Enumerable.Range(0, 4).All(n => Is(i + n, SingularityNodeType.Compute)))
                Add("Compute Arc", .20, i, i + 1, i + 2, i + 3);
            if (Is(i, SingularityNodeType.Compute) && Is(i - 1, SingularityNodeType.Amplifier) && Is(i + 1, SingularityNodeType.Amplifier))
                Add("Neural Bridge", .35, i);
            if (Is(i, SingularityNodeType.Quantum))
            {
                if ((Is(i - 1, SingularityNodeType.Amplifier) && Is(i + 1, SingularityNodeType.Cooling)) ||
                    (Is(i + 1, SingularityNodeType.Amplifier) && Is(i - 1, SingularityNodeType.Cooling)))
                    Add("Quantum Matrix", .40, i);
                if (Is(i - 1, SingularityNodeType.Cooling) && Is(i + 1, SingularityNodeType.Cooling))
                    Add("Cooling Cradle", .30, i);
            }
            foreach (int direction in new[] { -1, 1 })
                if (Is(i, SingularityNodeType.Compute) && Is(i + direction, SingularityNodeType.Amplifier) && Is(i + 2 * direction, SingularityNodeType.Quantum))
                    Add("Stabilized Chain", .15, i, i + direction, i + 2 * direction);
        }
        return result;
    }
    internal static double Bonus(IReadOnlyList<SingularityFormation> formations, SingularitySectorData sector, int node) =>
        Math.Min(.75, formations.Where(f => Array.IndexOf(f.Nodes, node) >= 0)
            .GroupBy(f => f.Name).Sum(group => group.Max(f => f.Bonus))
            * (sector.Modifier == SingularitySectorModifier.ResonantFormations ? 1.35 : 1.0));
}
