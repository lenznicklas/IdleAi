#nullable enable

using Godot;
using System;
using System.Text.Json;
using System.IO;
using System.Linq;
using System.Text;

namespace IdleAi;

internal sealed class SingularityPersistenceService
{
	private readonly JsonSerializerOptions _jsonOptions =
		new()
		{
			WriteIndented = true,
			PropertyNameCaseInsensitive = true
		};

	private const string SavePath = "user://singularity_save.json";
	internal bool LastLoadFailed { get; private set; }
	private readonly SingularitySaveData _data;
	private readonly SingularitySectorService _sectors;
	private readonly SingularityBotService _bots;
	private readonly Func<long> _now;

	internal SingularityPersistenceService(SingularitySaveData data, SingularitySectorService sectors, SingularityBotService bots, Func<long> now)
	{
		_data = data;
		_sectors = sectors;
		_bots = bots;
		_now = now;
	}

	internal void Save()
	{
		if (LastLoadFailed) return; // Preserve the original file for recovery.
		try
		{
			_data.LastSaveUnix =
				_now();

			string json =
				JsonSerializer.Serialize(
					_data,
					_jsonOptions
				);

			string path = ProjectSettings.GlobalizePath(SavePath);
			string temporaryPath = ProjectSettings.GlobalizePath("user://singularity_save.tmp.json");
			using (var stream = new FileStream(temporaryPath, FileMode.Create, System.IO.FileAccess.Write, FileShare.None))
			{
				byte[] bytes = Encoding.UTF8.GetBytes(json);
				stream.Write(bytes);
				stream.Flush(true);
			}
			File.Move(temporaryPath, path, overwrite: true);
		}
		catch (Exception exception)
		{
			GD.PushWarning(
				"Singularity save failed: "
					+ exception.Message
			);
		}
	}

	internal void Load()
	{
		LastLoadFailed = false;
		Reset(new SingularitySaveData());

		if (!Godot.FileAccess.FileExists(SavePath))
		{
			_sectors.EnsureSector(0);
			_data.Sectors[0].CoreUnlocked = true;
			_data.Sectors[0].CoreLevel = 1;
			_data.SaveVersion = 4;
			return;
		}

		try
		{
			using Godot.FileAccess file =
				Godot.FileAccess.Open(
					SavePath,
					Godot.FileAccess.ModeFlags.Read
				);

			string json =
				file.GetAsText();

			SingularitySaveData? loaded =
				JsonSerializer.Deserialize<SingularitySaveData>(
					json,
					_jsonOptions
				);

			if (loaded?.Sectors == null || loaded.Sectors.Any(sector =>
				sector?.Nodes == null || sector.Nodes.Any(node => node == null)))
				throw new InvalidDataException("Invalid Singularity save structure.");
			Reset(loaded);
		}
		catch (Exception exception)
		{
			GD.PushWarning(
				"Singularity save load failed; saving is blocked: "
					+ exception.Message
			);

			LastLoadFailed = true;
			Reset(new SingularitySaveData());
		}

		int loadedVersion =
			_data.SaveVersion;

		_data.CurrentSectorIndex =
			Math.Max(
				0,
				_data.CurrentSectorIndex
			);

		_sectors.EnsureSector(
			_data.CurrentSectorIndex
		);

		if (loadedVersion < 3)
		{
			SingularitySectorData first =
				_data.Sectors[0];

			first.CoreUnlocked =
				true;

			first.CoreLevel =
				Math.Max(
					1,
					_data.CoreLevel
				);

			for (
				int i = 1;
				i < _data.Sectors.Count;
				i++
			)
			{
				_data.Sectors[i].CoreUnlocked = false;
				_data.Sectors[i].CoreLevel = 1;
			}
		}

		_data.Sectors[0].CoreUnlocked =
			true;

		_data.Sectors[0].CoreLevel =
			Math.Max(
				1,
				_data.Sectors[0].CoreLevel
			);

		foreach (
			SingularitySectorData sector
				in _data.Sectors
		)
		{
			sector.CoreLevel =
				Math.Max(
					1,
					sector.CoreLevel
				);

			foreach (
				SingularityNodeData node
					in sector.Nodes
			)
			{
				if (!node.BotRarity.HasValue)
				{
					SingularityBotService.ClearNodeBot(
						node
					);

					continue;
				}

				if (!node.BotDurabilityInitialized)
				{
					/*
					 * Future-safe migration: if a save somehow contains a Node Bot
					 * but predates durability, start that Bot at 100%.
					 */
					_bots.InitializeNodeBotDurability(
						node
					);
				}
				else
				{
					node.BotDurabilitySecondsRemaining =
						Math.Clamp(
							node.BotDurabilitySecondsRemaining,
							0.0,
							BotCatalog.GetWorkingLifetimeSeconds(
								node.BotRarity.Value
							)
						);
				}
			}
		}

		_data.SaveVersion =
			4;
	}

	private void Reset(SingularitySaveData loaded)
	{
		_data.SaveVersion = loaded.SaveVersion;
		_data.Unlocked = loaded.Unlocked;
		_data.Matter = loaded.Matter;
		_data.CoreLevel = loaded.CoreLevel;
		_data.CurrentSectorIndex = loaded.CurrentSectorIndex;
		_data.LastSaveUnix = loaded.LastSaveUnix;
		_data.Sectors = loaded.Sectors;
	}

}
