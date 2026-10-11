#nullable enable

using Godot;
using System;
using System.Collections.Generic;

namespace IdleAi;

internal sealed class SingularitySectorService
{
	private readonly List<Vector2I> _sectorGridCache =
		[
			Vector2I.Zero
		];

	private readonly Dictionary<Vector2I, int> _sectorIndexByGrid =
		[];

	private Vector2I _spiralCursor =
		Vector2I.Zero;

	private Vector2I _spiralDirection =
		new(
			1,
			0
		);

	private int _spiralStepLength =
		1;

	private int _spiralStepProgress;

	private int _spiralLegsAtCurrentLength;
	private readonly SingularitySaveData _data;
	private readonly SingularityEconomy _economy;
	private readonly Action _saveAndNotify;
	private readonly Action _save;

	internal SingularitySectorService(SingularitySaveData data, SingularityEconomy economy, Action saveAndNotify, Action save)
	{
		_data = data;
		_economy = economy;
		_saveAndNotify = saveAndNotify;
		_save = save;
	}

	internal SingularitySectorData GetCurrentSector()
	{
		return GetSector(
			_data.CurrentSectorIndex
		);
	}

	internal SingularitySectorData GetSector(
		int index)
	{
		EnsureSector(
			index
		);

		return _data.Sectors[
			index
		];
	}

	internal SingularityNodeData GetNode(
		int sectorIndex,
		int nodeIndex)
	{
		SingularitySectorData sector =
			GetSector(
				sectorIndex
			);

		return sector.Nodes[
			Math.Clamp(
				nodeIndex,
				0,
				SingularityService.NodesPerSector - 1
			)
		];
	}

	internal bool IsSectorCoreUnlocked(
		int sectorIndex)
	{
		return GetSector(
			sectorIndex
		).CoreUnlocked;
	}

	internal int GetSectorCoreLevel(
		int sectorIndex)
	{
		SingularitySectorData sector =
			GetSector(
				sectorIndex
			);

		return Math.Max(
			1,
			sector.CoreLevel
		);
	}

	internal double GetSectorCoreUnlockCost(
		int sectorIndex)
	{
		if (sectorIndex <= 0)
			return 0.0;

		return 250.0
			* Math.Pow(
				4.0,
				sectorIndex - 1
			);
	}

	internal SingularityActionResult UnlockSectorCore(
		int sectorIndex)
	{
		if (
			sectorIndex < 0
			|| sectorIndex >= _data.Sectors.Count
		)
		{
			return new SingularityActionResult(
				false,
				"Invalid Sector Core."
			);
		}

		SingularitySectorData sector =
			GetSector(
				sectorIndex
			);

		if (sector.CoreUnlocked)
		{
			return new SingularityActionResult(
				false,
				"This Sector Core is already online."
			);
		}

		double cost =
			GetSectorCoreUnlockCost(
				sectorIndex
			);

		if (!_economy.TrySpend(cost))
			return SingularityEconomy.NotEnoughMatter(cost);

		sector.CoreUnlocked =
			true;

		sector.CoreLevel =
			1;

		_saveAndNotify();

		return new SingularityActionResult(
			true,
			"Sector "
				+ (sectorIndex + 1)
				+ " Core online."
		);
	}

	internal double GetSectorCoreUpgradeCost(
		int sectorIndex)
	{
		if (!IsSectorCoreUnlocked(sectorIndex))
			return GetSectorCoreUnlockCost(sectorIndex);

		int level =
			GetSectorCoreLevel(
				sectorIndex
			);

		return 100.0
			* Math.Pow(
				2.15,
				Math.Max(
					0,
					level - 1
				)
			)
			* Math.Pow(
				3.0,
				Math.Max(
					0,
					sectorIndex
				)
			);
	}

	internal SingularityActionResult UpgradeSectorCore(
		int sectorIndex)
	{
		if (!IsSectorCoreUnlocked(sectorIndex))
			return UnlockSectorCore(sectorIndex);

		double cost =
			GetSectorCoreUpgradeCost(
				sectorIndex
			);

		if (!_economy.TrySpend(cost))
			return SingularityEconomy.NotEnoughMatter(cost);

		SingularitySectorData sector =
			GetSector(
				sectorIndex
			);

		sector.CoreLevel =
			Math.Min(
				int.MaxValue,
				GetSectorCoreLevel(sectorIndex) + 1
			);

		if (sectorIndex == 0)
		{
			_data.CoreLevel =
				sector.CoreLevel;
		}

		_saveAndNotify();

		return new SingularityActionResult(
			true,
			"Sector "
				+ (sectorIndex + 1)
				+ " Core upgraded to Level "
				+ sector.CoreLevel
				+ "."
		);
	}

	internal double GetNextCoreCost()
	{
		return GetCoreUpgradeCost();
	}

	internal double GetCoreUpgradeCost()
	{
		return IsSectorCoreUnlocked(_data.CurrentSectorIndex)
			? GetSectorCoreUpgradeCost(_data.CurrentSectorIndex)
			: GetSectorCoreUnlockCost(_data.CurrentSectorIndex);
	}

	internal SingularityActionResult BuyNextCore()
	{
		return UpgradeSectorCore(
			_data.CurrentSectorIndex
		);
	}

	internal SingularityActionResult UpgradeCore()
	{
		return UpgradeSectorCore(
			_data.CurrentSectorIndex
		);
	}

	internal double GetNextSectorUnlockCost()
	{
		int nextSector =
			_data.Sectors.Count;

		return 750.0
			* Math.Pow(
				5.0,
				Math.Max(
					0,
					nextSector - 1
				)
			);
	}

	internal bool CanUnlockNextSector()
	{
		if (_data.Sectors.Count == 0)
			return false;

		SingularitySectorData frontier =
			_data.Sectors[
				_data.Sectors.Count - 1
			];

		if (!frontier.CoreUnlocked)
			return false;

		return frontier.Nodes.TrueForAll(
			node =>
				node.Type
				!= SingularityNodeType.Empty
		);
	}

	internal int GetFrontierSectorIndex()
	{
		return Math.Max(
			0,
			_data.Sectors.Count - 1
		);
	}

	internal void SelectSector(
		int sectorIndex)
	{
		if (
			sectorIndex < 0
			|| sectorIndex >= _data.Sectors.Count
			|| sectorIndex == _data.CurrentSectorIndex
		)
		{
			return;
		}

		_data.CurrentSectorIndex =
			sectorIndex;

		_save();
	}

	internal SingularityActionResult UnlockNextSector()
	{
		if (!CanUnlockNextSector())
		{
			int frontierIndex =
				GetFrontierSectorIndex();

			if (!IsSectorCoreUnlocked(frontierIndex))
			{
				return new SingularityActionResult(
					false,
					"Unlock the Sector "
						+ (frontierIndex + 1)
						+ " Core first."
				);
			}

			return new SingularityActionResult(
				false,
				"Fill all 8 nodes in Sector "
					+ (frontierIndex + 1)
					+ " first."
			);
		}

		int nextIndex =
			_data.Sectors.Count;

		double cost =
			GetNextSectorUnlockCost();

		if (!_economy.TrySpend(cost))
			return SingularityEconomy.NotEnoughMatter(cost);

		EnsureSector(
			nextIndex
		);

		SingularitySectorData newSector =
			_data.Sectors[nextIndex];

		newSector.CoreUnlocked =
			false;

		newSector.CoreLevel =
			1;

		_data.CurrentSectorIndex =
			nextIndex;

		_saveAndNotify();

		return new SingularityActionResult(
			true,
			"Sector "
				+ (_data.CurrentSectorIndex + 1)
				+ " opened • Core locked."
		);
	}

	internal bool GoToPreviousSector()
	{
		if (_data.CurrentSectorIndex <= 0)
			return false;

		_data.CurrentSectorIndex--;

		_saveAndNotify();

		return true;
	}

	internal bool GoToNextExistingSector()
	{
		if (
			_data.CurrentSectorIndex
			>= _data.Sectors.Count - 1
		)
		{
			return false;
		}

		_data.CurrentSectorIndex++;

		_saveAndNotify();

		return true;
	}

	internal void EnsureSector(
		int index)
	{
		index =
			Math.Max(
				0,
				index
			);

		while (_data.Sectors.Count <= index)
		{
			int newIndex =
				_data.Sectors.Count;

			SingularitySectorData sector =
				new()
				{
					CoreUnlocked =
						newIndex == 0,

					CoreLevel = 1
				};

			for (
				int i = 0;
				i < SingularityService.NodesPerSector;
				i++
			)
			{
				sector.Nodes.Add(
					new SingularityNodeData()
				);
			}

			_data.Sectors.Add(
				sector
			);
		}

		foreach (
			SingularitySectorData sector
				in _data.Sectors
		)
		{
			while (sector.Nodes.Count < SingularityService.NodesPerSector)
			{
				sector.Nodes.Add(
					new SingularityNodeData()
				);
			}

			sector.CoreLevel =
				Math.Max(
					1,
					sector.CoreLevel
				);
		}

		EnsureGridCache();
	}

	internal void RebuildGridIndex()
	{
		_sectorIndexByGrid.Clear();
		EnsureGridCache();
	}

	internal void EnsureGridCache()
	{
		while (_sectorGridCache.Count < _data.Sectors.Count)
		{
			_spiralCursor +=
				_spiralDirection;

			_sectorGridCache.Add(
				_spiralCursor
			);

			_spiralStepProgress++;

			if (_spiralStepProgress < _spiralStepLength)
				continue;

			_spiralStepProgress =
				0;

			_spiralDirection =
				new Vector2I(
					-_spiralDirection.Y,
					_spiralDirection.X
				);

			_spiralLegsAtCurrentLength++;

			if (_spiralLegsAtCurrentLength < 2)
				continue;

			_spiralLegsAtCurrentLength =
				0;

			_spiralStepLength++;
		}

		for (
			int i = _sectorIndexByGrid.Count;
			i < _data.Sectors.Count;
			i++
		)
		{
			_sectorIndexByGrid[
				GetSectorGridPosition(i)
			] = i;
		}
	}

	internal Vector2I GetSectorGridPosition(
		int sectorIndex)
	{
		sectorIndex =
			Math.Clamp(
				sectorIndex,
				0,
				Math.Max(
					0,
					_data.Sectors.Count - 1
				)
			);

		while (_sectorGridCache.Count <= sectorIndex)
		{
			_spiralCursor +=
				_spiralDirection;

			_sectorGridCache.Add(
				_spiralCursor
			);

			_spiralStepProgress++;

			if (_spiralStepProgress < _spiralStepLength)
				continue;

			_spiralStepProgress = 0;

			_spiralDirection =
				new Vector2I(
					-_spiralDirection.Y,
					_spiralDirection.X
				);

			_spiralLegsAtCurrentLength++;

			if (_spiralLegsAtCurrentLength >= 2)
			{
				_spiralLegsAtCurrentLength = 0;
				_spiralStepLength++;
			}
		}

		return _sectorGridCache[
			sectorIndex
		];
	}

	internal bool TryGetCrossSectorNeighbor(
		int sectorIndex,
		int nodeIndex,
		out int neighborSector,
		out int neighborNode)
	{
		neighborSector =
			-1;

		neighborNode =
			-1;

		Vector2I offset;

		switch (nodeIndex)
		{
			case 0:
				offset = new Vector2I(0, -1);
				neighborNode = 4;
				break;

			case 2:
				offset = new Vector2I(1, 0);
				neighborNode = 6;
				break;

			case 4:
				offset = new Vector2I(0, 1);
				neighborNode = 0;
				break;

			case 6:
				offset = new Vector2I(-1, 0);
				neighborNode = 2;
				break;

			default:
				return false;
		}

		EnsureGridCache();

		Vector2I target =
			GetSectorGridPosition(sectorIndex)
			+ offset;

		return _sectorIndexByGrid.TryGetValue(
			target,
			out neighborSector
		);
	}

	internal int CoreCount
	{
		get
		{
			int count = 0;
			foreach (SingularitySectorData sector in _data.Sectors)
			{
				if (sector.CoreUnlocked)
					count++;
			}
			return count;
		}
	}

	internal void InstallFirstCore()
	{
		EnsureSector(0);
		SingularitySectorData firstSector = _data.Sectors[0];
		firstSector.CoreUnlocked = true;
		firstSector.CoreLevel = Math.Max(1, firstSector.CoreLevel);
	}

}
