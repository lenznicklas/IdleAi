using System;
using System.Collections.Generic;
using System.Linq;

namespace IdleAi;

public sealed record SingularityNodeBonuses(double AmplifierBonus, double CoolingBonus, double QuantumBonus,
    double FormationBonus, double QualityMultiplier, double SectorMultiplier, string[] Formations)
{
    public double SynergyMultiplier => (1 + AmplifierBonus) * (1 + CoolingBonus) * (1 + QuantumBonus);
    public double TotalMultiplier => SynergyMultiplier * (1 + FormationBonus) * QualityMultiplier * SectorMultiplier;
    public string Summary => $"Synergies: Amplifiers +{AmplifierBonus:P0}, Cooling +{CoolingBonus:P0}, Quantum +{QuantumBonus:P0}\n"
        + (Formations.Length > 0 ? $"Formations: {string.Join(", ", Formations)} (+{FormationBonus:P0})" : "Formations: none");
}

internal sealed class SingularitySynergyService(SingularitySaveData data, SingularitySectorService sectors, SingularityBotService bots)
{
    internal SingularityNodeBonuses Calculate(int sectorIndex, int index, IReadOnlyList<SingularityFormation>? formations = null)
    {
        var sector = data.Sectors[sectorIndex];
        var node = sector.Nodes[index];
        formations ??= SingularityFormationService.Detect(sector);
        double amplifier = 1, cooling = 1, quantum = 0;
        var neighbors = new List<(int Sector, int Node)> { (sectorIndex, (index + 7) % 8), (sectorIndex, (index + 1) % 8) };
        if (sectors.TryGetCrossSectorNeighbor(sectorIndex, index, out int otherSector, out int otherNode))
            neighbors.Add((otherSector, otherNode));
        if (node.Type is SingularityNodeType.Compute or SingularityNodeType.Quantum)
        foreach (var neighbor in neighbors)
        {
            var supportSector = data.Sectors[neighbor.Sector];
            var support = supportSector.Nodes[neighbor.Node];
            if (support.Type is not (SingularityNodeType.Amplifier or SingularityNodeType.Cooling or SingularityNodeType.Quantum)) continue;
            // Support never feeds back into its own neighbors: no recursive amplification loops.
            double strength = support.Level * bots.GetNodeBotEffectiveMultiplier(support)
                * SingularityBuildRules.QualityMultiplier(support.Quality)
                * SingularityBuildRules.TypeMultiplier(supportSector.Modifier, support.Type)
                * (1 + SingularityFormationService.Bonus(neighbor.Sector == sectorIndex ? formations : SingularityFormationService.Detect(supportSector), supportSector, neighbor.Node));
            if (support.Type == SingularityNodeType.Amplifier)
                amplifier *= 1 + (node.Type == SingularityNodeType.Quantum ? .10 : .15) * strength;
            if (support.Type == SingularityNodeType.Cooling)
                cooling *= 1 + (node.Type == SingularityNodeType.Quantum ? .20 : .10) * strength;
            if (support.Type == SingularityNodeType.Quantum && node.Type == SingularityNodeType.Compute)
                quantum += .05 * strength;
        }
        return new(Math.Min(2, amplifier - 1), Math.Min(2, cooling - 1), Math.Min(.30, quantum),
            SingularityFormationService.Bonus(formations, sector, index), SingularityBuildRules.QualityMultiplier(node.Quality),
            SingularityBuildRules.TypeMultiplier(sector.Modifier, node.Type),
            formations.Where(f => Array.IndexOf(f.Nodes, index) >= 0).Select(f => f.Name).Distinct().ToArray());
    }
}
