#nullable enable

using System;
using System.Collections.Generic;

namespace IdleAi;

internal sealed class SingularityProductionService
{
    private readonly SingularitySaveData _data;
    private readonly SingularitySectorService _sectors;
    private readonly SingularityBotService _bots;
    private readonly SingularitySynergyService _synergies;

    internal SingularityProductionService(SingularitySaveData data, SingularitySectorService sectors, SingularityBotService bots)
    {
        _data = data;
        _sectors = sectors;
        _bots = bots;
        _synergies = new(data, sectors, bots);
    }

    internal SingularityNodeBonuses GetNodeBonuses(int sectorIndex, int nodeIndex)
    {
        _sectors.GetSector(sectorIndex);
        return _synergies.Calculate(sectorIndex, Math.Clamp(nodeIndex, 0, SingularityService.NodesPerSector - 1));
    }

    internal double GetNodeEffectStrength(int sectorIndex, int nodeIndex) =>
        EffectStrength(sectorIndex, Math.Clamp(nodeIndex, 0, SingularityService.NodesPerSector - 1), SingularityFormationService.Detect(_sectors.GetSector(sectorIndex)));

    private double EffectStrength(int sectorIndex, int index, IReadOnlyList<SingularityFormation> formations)
    {
        var node = _data.Sectors[sectorIndex].Nodes[index];
        return node.Level * _bots.GetNodeBotEffectiveMultiplier(node)
            * _synergies.Calculate(sectorIndex, index, formations).TotalMultiplier;
    }

    internal double GetTotalOutputPerSecond()
    {
        if (!_data.Unlocked) return 0;
        double total = 0;
        for (int i = 0; i < _data.Sectors.Count; i++) total += GetSectorOutputPerSecond(i);
        return total;
    }

    private double QuantumMultiplier(int sectorIndex, IReadOnlyList<SingularityFormation> formations)
    {
        var sector = _sectors.GetSector(sectorIndex);
        double bonus = 0;
        for (int i = 0; i < SingularityService.NodesPerSector; i++)
            if (sector.Nodes[i].Type == SingularityNodeType.Quantum)
                bonus += .12 * EffectStrength(sectorIndex, i, formations);
        return 1 + bonus; // Contributions add, never multiply each other.
    }

    private double ComputeOutput(int sectorIndex, int index, double quantum, IReadOnlyList<SingularityFormation> formations)
    {
        var node = _data.Sectors[sectorIndex].Nodes[index];
        if (node.Type != SingularityNodeType.Compute) return 0;
        return .25 * Math.Pow(1.58, Math.Max(0, node.Level - 1))
            * _bots.GetNodeBotEffectiveMultiplier(node)
            * _synergies.Calculate(sectorIndex, index, formations).TotalMultiplier
            * quantum * GetSectorComputeCoreMultiplier(sectorIndex);
    }

    internal double GetSectorOutputPerSecond(int sectorIndex)
    {
        var sector = _sectors.GetSector(sectorIndex);
        var formations = SingularityFormationService.Detect(sector);
        double quantum = QuantumMultiplier(sectorIndex, formations);
        double total = GetSectorCoreOutputPerSecond(sectorIndex);
        for (int i = 0; i < SingularityService.NodesPerSector; i++)
            total += ComputeOutput(sectorIndex, i, quantum, formations);
        return total;
    }

    internal double GetNodeDisplayedOutput(int sectorIndex, int nodeIndex)
    {
        nodeIndex = Math.Clamp(nodeIndex, 0, SingularityService.NodesPerSector - 1);
        if (_sectors.GetNode(sectorIndex, nodeIndex).Type != SingularityNodeType.Compute) return 0;
        var formations = SingularityFormationService.Detect(_sectors.GetSector(sectorIndex));
        return ComputeOutput(sectorIndex, nodeIndex, QuantumMultiplier(sectorIndex, formations), formations);
    }

	internal double GetSectorCoreOutputPerSecond(
		int sectorIndex)
	{
		if (!_sectors.IsSectorCoreUnlocked(sectorIndex))
			return 0.0;

		return GetCoreOutputPerSecond(
			_sectors.GetSectorCoreLevel(
				sectorIndex
			)
		)
		* (
			1.0
			+ 0.35
			* Math.Max(
				0,
				sectorIndex
			)
		);
	}

	internal double GetSectorComputeCoreMultiplier(
		int sectorIndex)
	{
		if (!_sectors.IsSectorCoreUnlocked(sectorIndex))
			return 1.0;

		return 1.0
			+ 0.10
			* Math.Max(
				0,
				_sectors.GetSectorCoreLevel(sectorIndex) - 1
			);
	}

	internal double GetCoreOutputPerSecond()
	{
		return GetSectorCoreOutputPerSecond(
			_data.CurrentSectorIndex
		);
	}

	internal static double GetCoreOutputPerSecond(
		int coreLevel)
	{
		return 0.05
			* Math.Pow(
				1.42,
				Math.Max(
					0,
					coreLevel - 1
				)
			);
	}

	internal double GetComputeCoreMultiplier()
	{
		return GetSectorComputeCoreMultiplier(
			_data.CurrentSectorIndex
		);
	}
}
