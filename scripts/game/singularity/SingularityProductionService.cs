#nullable enable

using System;

namespace IdleAi;

internal sealed class SingularityProductionService
{
	private readonly SingularitySaveData _data;
	private readonly SingularitySectorService _sectors;
	private readonly SingularityBotService _bots;

	internal SingularityProductionService(SingularitySaveData data, SingularitySectorService sectors, SingularityBotService bots)
	{
		_data = data;
		_sectors = sectors;
		_bots = bots;
	}

	internal double GetTotalOutputPerSecond()
	{
		if (!_data.Unlocked)
			return 0.0;

		double total =
			0.0;

		for (
			int sectorIndex = 0;
			sectorIndex < _data.Sectors.Count;
			sectorIndex++
		)
		{
			total +=
				GetSectorOutputPerSecond(
					sectorIndex
				);
		}

		return total;
	}

	internal double GetSectorOutputPerSecond(
		int sectorIndex)
	{
		SingularitySectorData sector =
			_sectors.GetSector(
				sectorIndex
			);

		double quantumMultiplier =
			1.0;

		foreach (
			SingularityNodeData node
				in sector.Nodes
		)
		{
			if (node.Type == SingularityNodeType.Quantum)
			{
				quantumMultiplier +=
					0.12
					* node.Level
					* _bots.GetNodeBotEffectiveMultiplier(
						node
					);
			}
		}

		double total =
			GetSectorCoreOutputPerSecond(
				sectorIndex
			);

		for (
			int i = 0;
			i < sector.Nodes.Count;
			i++
		)
		{
			SingularityNodeData node =
				sector.Nodes[i];

			if (node.Type != SingularityNodeType.Compute)
				continue;

			double baseOutput =
				0.25
				* Math.Pow(
					1.58,
					Math.Max(
						0,
						node.Level - 1
					)
				)
				* _bots.GetNodeBotEffectiveMultiplier(
					node
				);

			double adjacencyMultiplier =
				GetLocalAdjacencyMultiplier(
					sector,
					i
				)
				* GetExternalAdjacencyMultiplier(
					sectorIndex,
					i
				);

			total +=
				baseOutput
				* adjacencyMultiplier
				* quantumMultiplier
				* GetSectorComputeCoreMultiplier(
					sectorIndex
				);
		}

		return total;
	}

	internal double GetLocalAdjacencyMultiplier(
		SingularitySectorData sector,
		int nodeIndex)
	{
		int previous =
			(
				nodeIndex
				+ SingularityService.NodesPerSector
				- 1
			)
			% SingularityService.NodesPerSector;

		int next =
			(nodeIndex + 1)
			% SingularityService.NodesPerSector;

		return GetAdjacentMultiplier(
			sector.Nodes[previous]
		)
		* GetAdjacentMultiplier(
			sector.Nodes[next]
		);
	}

	internal double GetAdjacentMultiplier(
		SingularityNodeData node)
	{
		double botMultiplier =
			_bots.GetNodeBotEffectiveMultiplier(
				node
			);

		return node.Type switch
		{
			SingularityNodeType.Amplifier =>
				1.0
				+ 0.15
				* node.Level
				* botMultiplier,

			SingularityNodeType.Cooling =>
				1.0
				+ 0.10
				* node.Level
				* botMultiplier,

			_ => 1.0
		};
	}

	internal double GetExternalAdjacencyMultiplier(
		int sectorIndex,
		int nodeIndex)
	{
		if (!_sectors.TryGetCrossSectorNeighbor(
			sectorIndex,
			nodeIndex,
			out int neighborSector,
			out int neighborNode
		))
		{
			return 1.0;
		}

		return GetAdjacentMultiplier(
			_sectors.GetNode(
				neighborSector,
				neighborNode
			)
		);
	}

	internal double GetNodeDisplayedOutput(
		int sectorIndex,
		int nodeIndex)
	{
		SingularityNodeData node =
			_sectors.GetNode(
				sectorIndex,
				nodeIndex
			);

		if (node.Type != SingularityNodeType.Compute)
			return 0.0;

		SingularitySectorData sector =
			_sectors.GetSector(
				sectorIndex
			);

		double output =
			0.25
			* Math.Pow(
				1.58,
				Math.Max(
					0,
					node.Level - 1
				)
			)
			* _bots.GetNodeBotEffectiveMultiplier(
				node
			);

		output *=
			GetLocalAdjacencyMultiplier(
				sector,
				nodeIndex
			);

		output *=
			GetExternalAdjacencyMultiplier(
				sectorIndex,
				nodeIndex
			);

		double quantum =
			1.0;

		foreach (
			SingularityNodeData sectorNode
				in sector.Nodes
		)
		{
			if (sectorNode.Type == SingularityNodeType.Quantum)
			{
				quantum +=
					0.12
					* sectorNode.Level
					* _bots.GetNodeBotEffectiveMultiplier(
						sectorNode
					);
			}
		}

		output *=
			quantum;

		output *=
			GetSectorComputeCoreMultiplier(
				sectorIndex
			);

		return output;
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
