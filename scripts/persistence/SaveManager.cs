using Godot;

using System;
using System.Collections.Generic;
using System.Text.Json;

using GArray =
	Godot.Collections.Array;

using GDictionary =
	Godot.Collections.Dictionary;


namespace IdleAi;


public partial class SaveManager
	: Node
{
	/*
	 * Native Godot save files.
	 *
	 * They live inside user://, which is persistent on
	 * desktop and Android and does not depend on the
	 * current working directory.
	 */
	private const string SavePath =
		"user://idle_ai_save.dat";


	private const string BackupSavePath =
		"user://idle_ai_save_backup.dat";


	private const string TempSavePath =
		"user://idle_ai_save_tmp.dat";


	/*
	 * Old JSON paths are kept only for migration from
	 * previous versions of the project.
	 */
	private const string LegacyJsonSavePath =
		"user://idle_ai_save.json";


	private const string LegacyJsonBackupPath =
		"user://idle_ai_save_backup.json";


	private Timer? _autosaveTimer;


	public event Action? AutosaveRequested;


	// ==================================================
	// AUTOSAVE
	// ==================================================

	public void StartAutosave(
		double intervalSeconds)
	{
		if (_autosaveTimer != null)
		{
			_autosaveTimer.Stop();

			_autosaveTimer.QueueFree();
		}


		_autosaveTimer =
			new Timer
			{
				WaitTime =
					Math.Max(
						1.0,
						intervalSeconds
					),

				OneShot =
					false,

				Autostart =
					true,

				ProcessMode =
					ProcessModeEnum.Always
			};


		_autosaveTimer.Timeout +=
			OnAutosaveTimeout;


		AddChild(
			_autosaveTimer
		);
	}


	private void OnAutosaveTimeout()
	{
		AutosaveRequested?.Invoke();
	}


	// ==================================================
	// SAVE
	// ==================================================

	public bool SaveData(
		SaveGameData data)
	{
		try
		{
			/*
			 * 1) Write the candidate to a temporary file.
			 */
			if (
				!WriteBinary(
					TempSavePath,
					data
				)
			)
			{
				GD.PushError(
					"Could not write temporary save."
				);


				return false;
			}


			/*
			 * 2) Read it back immediately.
			 *
			 * If this fails, the current primary save is
			 * left completely untouched.
			 */
			SaveGameData? verified =
				TryReadBinary(
					TempSavePath,
					logErrors:
						true
				);


			if (verified == null)
			{
				GD.PushError(
					"Temporary save verification failed."
				);


				return false;
			}


			/*
			 * 3) Preserve the previous valid primary.
			 */
			SaveGameData? previousPrimary =
				TryReadBinary(
					SavePath,
					logErrors:
						false
				);


			if (previousPrimary != null)
			{
				if (
					!WriteBinary(
						BackupSavePath,
						previousPrimary
					)
				)
				{
					GD.PushWarning(
						"Could not refresh save backup."
					);
				}
			}


			/*
			 * 4) Replace the primary with the verified data.
			 */
			if (
				!WriteBinary(
					SavePath,
					verified
				)
			)
			{
				GD.PushError(
					"Could not write primary save."
				);


				return false;
			}


			/*
			 * 5) Verify the final primary too.
			 */
			SaveGameData? finalVerification =
				TryReadBinary(
					SavePath,
					logErrors:
						true
				);


			if (finalVerification == null)
			{
				GD.PushError(
					"Primary save verification failed."
				);


				return false;
			}


			DeleteFileIfExists(
				TempSavePath
			);


			GD.Print(
				"Idle AI save OK: ",
				ProjectSettings.GlobalizePath(
					SavePath
				)
			);


			return true;
		}
		catch (Exception exception)
		{
			GD.PushError(
				"Could not save game: "
				+ exception
			);


			return false;
		}
	}


	private static bool WriteBinary(
		string path,
		SaveGameData data)
	{
		using Godot.FileAccess? file =
			Godot.FileAccess.Open(
				path,
				Godot.FileAccess.ModeFlags.Write
			);


		if (file == null)
		{
			GD.PushError(
				"Could not open save for writing: "
				+ path
				+ " | "
				+ Godot.FileAccess.GetOpenError()
			);


			return false;
		}


		GDictionary dictionary =
			ToDictionary(
				data
			);


		bool stored =
			file.StoreVar(
				dictionary,
				fullObjects:
					false
			);


		file.Flush();


		if (!stored)
		{
			GD.PushError(
				"StoreVar failed for: "
				+ path
			);
		}


		return stored;
	}


	// ==================================================
	// LOAD
	// ==================================================

	public SaveGameData? LoadData()
	{
		/*
		 * Prefer the new native Godot binary save.
		 */
		SaveGameData? primary =
			TryReadBinary(
				SavePath,
				logErrors:
					true
			);


		if (primary != null)
		{
			GD.Print(
				"Idle AI save loaded from primary binary."
			);


			return primary;
		}


		/*
		 * Fall back to the binary backup.
		 */
		SaveGameData? backup =
			TryReadBinary(
				BackupSavePath,
				logErrors:
					true
			);


		if (backup != null)
		{
			GD.PushWarning(
				"Primary save unavailable. "
				+ "Loaded binary backup."
			);


			return backup;
		}


		/*
		 * Migration path from the old JSON save format.
		 *
		 * Game.cs saves once at the end of startup, so a
		 * successfully migrated JSON save is immediately
		 * converted to the new binary format.
		 */
		SaveGameData? legacy =
			TryReadLegacyJson(
				LegacyJsonSavePath
			);


		if (legacy != null)
		{
			GD.Print(
				"Idle AI legacy JSON save loaded. "
				+ "It will be migrated to binary."
			);


			return legacy;
		}


		SaveGameData? legacyBackup =
			TryReadLegacyJson(
				LegacyJsonBackupPath
			);


		if (legacyBackup != null)
		{
			GD.PushWarning(
				"Idle AI legacy JSON backup loaded. "
				+ "It will be migrated to binary."
			);


			return legacyBackup;
		}


		GD.Print(
			"No readable Idle AI save found. "
				+ "Starting a new game."
		);


		return null;
	}


	private static SaveGameData? TryReadBinary(
		string path,
		bool logErrors)
	{
		if (
			!Godot.FileAccess.FileExists(
				path
			)
		)
		{
			return null;
		}


		try
		{
			using Godot.FileAccess? file =
				Godot.FileAccess.Open(
					path,
					Godot.FileAccess.ModeFlags.Read
				);


			if (file == null)
			{
				if (logErrors)
				{
					GD.PushError(
						"Could not open save for reading: "
							+ path
							+ " | "
							+ Godot.FileAccess.GetOpenError()
					);
				}


				return null;
			}


			Variant raw =
				file.GetVar(
					allowObjects:
						false
				);


			GDictionary dictionary =
				raw.AsGodotDictionary();


			if (dictionary.Count == 0)
			{
				if (logErrors)
				{
					GD.PushError(
						"Save dictionary is empty: "
							+ path
					);
				}


				return null;
			}


			SaveGameData data =
				FromDictionary(
					dictionary
				);


			return data;
		}
		catch (Exception exception)
		{
			if (logErrors)
			{
				GD.PushError(
					"Could not read binary save "
						+ path
						+ ": "
						+ exception
				);
			}


			return null;
		}
	}


	// ==================================================
	// LEGACY JSON MIGRATION
	// ==================================================

	private static SaveGameData? TryReadLegacyJson(
		string path)
	{
		if (
			!Godot.FileAccess.FileExists(
				path
			)
		)
		{
			return null;
		}


		try
		{
			using Godot.FileAccess? file =
				Godot.FileAccess.Open(
					path,
					Godot.FileAccess.ModeFlags.Read
				);


			if (file == null)
				return null;


			string json =
				file.GetAsText();


			if (
				string.IsNullOrWhiteSpace(
					json
				)
			)
			{
				return null;
			}


			return JsonSerializer.Deserialize(
				json,
				SaveJsonContext
					.Default
					.SaveGameData
			);
		}
		catch (Exception exception)
		{
			GD.PushWarning(
				"Legacy JSON save could not be loaded: "
					+ exception.Message
			);


			return null;
		}
	}


	// ==================================================
	// EXISTS / DELETE
	// ==================================================

	public bool HasSave()
	{
		return Godot.FileAccess.FileExists(
				SavePath
			)
			|| Godot.FileAccess.FileExists(
				BackupSavePath
			)
			|| Godot.FileAccess.FileExists(
				LegacyJsonSavePath
			)
			|| Godot.FileAccess.FileExists(
				LegacyJsonBackupPath
			);
	}


	public void DeleteSave()
	{
		DeleteFileIfExists(
			SavePath
		);


		DeleteFileIfExists(
			BackupSavePath
		);


		DeleteFileIfExists(
			TempSavePath
		);


		DeleteFileIfExists(
			LegacyJsonSavePath
		);


		DeleteFileIfExists(
			LegacyJsonBackupPath
		);
	}


	// ==================================================
	// SERIALIZATION
	// ==================================================

	private static GDictionary ToDictionary(
		SaveGameData data)
	{
		GDictionary result =
			new();


		result[
			"save_version"
		] =
			data.SaveVersion;


		result[
			"tokens"
		] =
			data.Tokens;


		result[
			"run_earned_tokens"
		] =
			data.RunEarnedTokens;


		result[
			"last_save_unix"
		] =
			data.LastSaveUnix;


		result[
			"income_per_second"
		] =
			data.IncomePerSecond;


		result[
			"current_room_index"
		] =
			data.CurrentRoomIndex;


		result[
			"ai_cores"
		] =
			data.AiCores;


		result[
			"prestige_count"
		] =
			data.PrestigeCount;


		result[
			"lab_unlocked"
		] =
			data.LabUnlocked;


		result[
			"research_points"
		] =
			data.ResearchPoints;


		result[
			"completed_research"
		] =
			StringListToArray(
				data.CompletedResearch
			);


		result[
			"active_research_id"
		] =
			data.ActiveResearchId
			?? "";


		result[
			"active_research_end_unix"
		] =
			data.ActiveResearchEndUnix;


		result[
			"data_shards"
		] =
			data.DataShards;


		result[
			"shop_production_boost_end_unix"
		] =
			data.ShopProductionBoostEndUnix;


		result[
			"shop_bot_luck_boost_end_unix"
		] =
			data.ShopBotLuckBoostEndUnix;


		result[
			"shop_production_upgrade_level"
		] =
			data.ShopProductionUpgradeLevel;


		result[
			"shop_offline_upgrade_level"
		] =
			data.ShopOfflineUpgradeLevel;


		GArray rooms =
			new();


		if (data.Rooms != null)
		{
			foreach (
				RoomSaveData room
				in data.Rooms
			)
			{
				rooms.Add(
					RoomToDictionary(
						room
					)
				);
			}
		}


		result[
			"rooms"
		] =
			rooms;


		result[
			"stats"
		] =
			StatsToDictionary(
				data.Stats
				?? new StatsSaveData()
			);


		return result;
	}


	private static SaveGameData FromDictionary(
		GDictionary data)
	{
		SaveGameData result =
			new()
			{
				SaveVersion =
					GetInt(
						data,
						"save_version",
						0
					),

				Tokens =
					GetDouble(
						data,
						"tokens",
						0.0
					),

				RunEarnedTokens =
					GetDouble(
						data,
						"run_earned_tokens",
						0.0
					),

				LastSaveUnix =
					GetLong(
						data,
						"last_save_unix",
						0
					),

				IncomePerSecond =
					GetDouble(
						data,
						"income_per_second",
						0.0
					),

				CurrentRoomIndex =
					GetInt(
						data,
						"current_room_index",
						0
					),

				AiCores =
					GetLong(
						data,
						"ai_cores",
						0
					),

				PrestigeCount =
					GetInt(
						data,
						"prestige_count",
						0
					),

				LabUnlocked =
					GetBool(
						data,
						"lab_unlocked",
						false
					),

				ResearchPoints =
					GetDouble(
						data,
						"research_points",
						0.0
					),

				CompletedResearch =
					ArrayToStringList(
						GetArray(
							data,
							"completed_research"
						)
					),

				ActiveResearchEndUnix =
					GetLong(
						data,
						"active_research_end_unix",
						0
					),

				DataShards =
					GetDouble(
						data,
						"data_shards",
						0.0
					),

				ShopProductionBoostEndUnix =
					GetLong(
						data,
						"shop_production_boost_end_unix",
						0
					),

				ShopBotLuckBoostEndUnix =
					GetLong(
						data,
						"shop_bot_luck_boost_end_unix",
						0
					),

				ShopProductionUpgradeLevel =
					GetInt(
						data,
						"shop_production_upgrade_level",
						0
					),

				ShopOfflineUpgradeLevel =
					GetInt(
						data,
						"shop_offline_upgrade_level",
						0
					)
			};


		string activeResearch =
			GetString(
				data,
				"active_research_id",
				""
			);


		result.ActiveResearchId =
			string.IsNullOrWhiteSpace(
				activeResearch
			)
				? null
				: activeResearch;


		GArray rooms =
			GetArray(
				data,
				"rooms"
			);


		foreach (
			Variant roomVariant
			in rooms
		)
		{
			result.Rooms.Add(
				RoomFromDictionary(
					roomVariant
						.AsGodotDictionary()
				)
			);
		}


		GDictionary stats =
			GetDictionary(
				data,
				"stats"
			);


		result.Stats =
			StatsFromDictionary(
				stats
			);


		return result;
	}


	// ==================================================
	// ROOM
	// ==================================================

	private static GDictionary RoomToDictionary(
		RoomSaveData room)
	{
		GDictionary result =
			new();


		result[
			"unlocked"
		] =
			room.Unlocked;


		result[
			"data_shard_unlock_reward_claimed"
		] =
			room.DataShardUnlockRewardClaimed;


		result[
			"pipeline"
		] =
			PipelineToDictionary(
				room.Pipeline
				?? new PipelineSaveData()
			);


		result[
			"infrastructure"
		] =
			InfrastructureToDictionary(
				room.Infrastructure
				?? new InfrastructureSaveData()
			);


		result[
			"quantum"
		] =
			QuantumToDictionary(
				room.Quantum
				?? new QuantumSaveData()
			);


		GArray slots =
			new();


		if (room.Slots != null)
		{
			foreach (
				SlotSaveData slot
				in room.Slots
			)
			{
				slots.Add(
					SlotToDictionary(
						slot
					)
				);
			}
		}


		result[
			"slots"
		] =
			slots;


		return result;
	}


	private static RoomSaveData RoomFromDictionary(
		GDictionary data)
	{
		RoomSaveData result =
			new()
			{
				Unlocked =
					GetBool(
						data,
						"unlocked",
						false
					),

				DataShardUnlockRewardClaimed =
					GetBool(
						data,
						"data_shard_unlock_reward_claimed",
						false
					),

				Pipeline =
					PipelineFromDictionary(
						GetDictionary(
							data,
							"pipeline"
						)
					),

				Infrastructure =
					InfrastructureFromDictionary(
						GetDictionary(
							data,
							"infrastructure"
						)
					),

				Quantum =
					QuantumFromDictionary(
						GetDictionary(
							data,
							"quantum"
						)
					)
			};


		GArray slots =
			GetArray(
				data,
				"slots"
			);


		foreach (
			Variant slotVariant
			in slots
		)
		{
			result.Slots.Add(
				SlotFromDictionary(
					slotVariant
						.AsGodotDictionary()
				)
			);
		}


		return result;
	}


	// ==================================================
	// PIPELINE
	// ==================================================

	private static GDictionary PipelineToDictionary(
		PipelineSaveData data)
	{
		GDictionary result =
			new();


		result["compute_level"] =
			data.ComputeLevel;

		result["data_level"] =
			data.DataLevel;

		result["model_level"] =
			data.ModelLevel;

		result["output_level"] =
			data.OutputLevel;

		result["raw_input_buffer"] =
			data.RawInputBuffer;

		result["compute_buffer"] =
			data.ComputeBuffer;

		result["data_buffer"] =
			data.DataBuffer;

		result["model_buffer"] =
			data.ModelBuffer;

		result["compute_cycle_remaining"] =
			data.ComputeCycleRemaining;

		result["data_cycle_remaining"] =
			data.DataCycleRemaining;

		result["model_cycle_remaining"] =
			data.ModelCycleRemaining;

		result["output_cycle_remaining"] =
			data.OutputCycleRemaining;


		return result;
	}


	private static PipelineSaveData PipelineFromDictionary(
		GDictionary data)
	{
		return new PipelineSaveData
		{
			ComputeLevel =
				GetInt(
					data,
					"compute_level",
					1
				),

			DataLevel =
				GetInt(
					data,
					"data_level",
					1
				),

			ModelLevel =
				GetInt(
					data,
					"model_level",
					1
				),

			OutputLevel =
				GetInt(
					data,
					"output_level",
					1
				),

			RawInputBuffer =
				GetDouble(
					data,
					"raw_input_buffer",
					0
				),

			ComputeBuffer =
				GetDouble(
					data,
					"compute_buffer",
					0
				),

			DataBuffer =
				GetDouble(
					data,
					"data_buffer",
					0
				),

			ModelBuffer =
				GetDouble(
					data,
					"model_buffer",
					0
				),

			ComputeCycleRemaining =
				GetDouble(
					data,
					"compute_cycle_remaining",
					0
				),

			DataCycleRemaining =
				GetDouble(
					data,
					"data_cycle_remaining",
					0
				),

			ModelCycleRemaining =
				GetDouble(
					data,
					"model_cycle_remaining",
					0
				),

			OutputCycleRemaining =
				GetDouble(
					data,
					"output_cycle_remaining",
					0
				)
		};
	}


	// ==================================================
	// INFRASTRUCTURE
	// ==================================================

	private static GDictionary InfrastructureToDictionary(
		InfrastructureSaveData data)
	{
		GDictionary result =
			new();


		result["power_level"] =
			data.PowerLevel;

		result["cooling_level"] =
			data.CoolingLevel;

		result["storage_level"] =
			data.StorageLevel;


		return result;
	}


	private static InfrastructureSaveData InfrastructureFromDictionary(
		GDictionary data)
	{
		return new InfrastructureSaveData
		{
			PowerLevel =
				GetInt(
					data,
					"power_level",
					1
				),

			CoolingLevel =
				GetInt(
					data,
					"cooling_level",
					1
				),

			StorageLevel =
				GetInt(
					data,
					"storage_level",
					1
				)
		};
	}


	// ==================================================
	// QUANTUM
	// ==================================================

	private static GDictionary QuantumToDictionary(
		QuantumSaveData data)
	{
		GDictionary result =
			new();


		result["stability"] =
			data.Stability;

		result["energy"] =
			data.Energy;

		result["overclock_index"] =
			data.OverclockIndex;

		result["stabilizer_level"] =
			data.StabilizerLevel;

		result["energy_core_level"] =
			data.EnergyCoreLevel;

		result["amplifier_level"] =
			data.AmplifierLevel;

		result["recovery_mode"] =
			data.RecoveryMode;


		return result;
	}


	private static QuantumSaveData QuantumFromDictionary(
		GDictionary data)
	{
		return new QuantumSaveData
		{
			Stability =
				GetDouble(
					data,
					"stability",
					GameConfig
						.QuantumMaximumStability
				),

			Energy =
				GetDouble(
					data,
					"energy",
					GameConfig
						.QuantumStartingEnergy
				),

			OverclockIndex =
				GetInt(
					data,
					"overclock_index",
					0
				),

			StabilizerLevel =
				GetInt(
					data,
					"stabilizer_level",
					1
				),

			EnergyCoreLevel =
				GetInt(
					data,
					"energy_core_level",
					1
				),

			AmplifierLevel =
				GetInt(
					data,
					"amplifier_level",
					1
				),

			RecoveryMode =
				GetBool(
					data,
					"recovery_mode",
					false
				)
		};
	}


	// ==================================================
	// SLOT
	// ==================================================

	private static GDictionary SlotToDictionary(
		SlotSaveData data)
	{
		GDictionary result =
			new();


		result["unlocked"] =
			data.Unlocked;

		result["machine_tier"] =
			data.MachineTier;

		result["machine_level"] =
			data.MachineLevel;

		result["bot_rarity"] =
			data.BotRarity.HasValue
				? (int)data.BotRarity.Value
				: -1;

		result["bot_purchase_price"] =
			data.BotPurchasePrice;

		result["is_running"] =
			data.IsRunning;

		result["cycle_remaining"] =
			data.CycleRemaining;


		GArray milestones =
			new();


		if (
			data.ClaimedDataShardMilestones
			!= null
		)
		{
			foreach (
				int milestone
				in data.ClaimedDataShardMilestones
			)
			{
				milestones.Add(
					milestone
				);
			}
		}


		result[
			"claimed_data_shard_milestones"
		] =
			milestones;


		return result;
	}


	private static SlotSaveData SlotFromDictionary(
		GDictionary data)
	{
		int rarity =
			GetInt(
				data,
				"bot_rarity",
				-1
			);


		SlotSaveData result =
			new()
			{
				Unlocked =
					GetBool(
						data,
						"unlocked",
						false
					),

				MachineTier =
					GetInt(
						data,
						"machine_tier",
						0
					),

				MachineLevel =
					GetInt(
						data,
						"machine_level",
						1
					),

				BotRarity =
					rarity >= 0
						? (BotRarity)rarity
						: null,

				BotPurchasePrice =
					GetDouble(
						data,
						"bot_purchase_price",
						0
					),

				IsRunning =
					GetBool(
						data,
						"is_running",
						false
					),

				CycleRemaining =
					GetDouble(
						data,
						"cycle_remaining",
						0
					)
			};


		GArray milestones =
			GetArray(
				data,
				"claimed_data_shard_milestones"
			);


		foreach (
			Variant milestone
				in milestones
		)
		{
			result
				.ClaimedDataShardMilestones
				.Add(
					milestone
						.AsInt32()
				);
		}


		return result;
	}


	// ==================================================
	// STATS
	// ==================================================

	private static GDictionary StatsToDictionary(
		StatsSaveData data)
	{
		GDictionary result =
			new();


		result["total_earned"] =
			data.TotalEarned;

		result["offline_earned"] =
			data.OfflineEarned;

		result["total_spent"] =
			data.TotalSpent;

		result["slot_unlock_spent"] =
			data.SlotUnlockSpent;


		GArray machineSpending =
			new();


		if (
			data.MachineSpending
			!= null
		)
		{
			foreach (
				KeyValuePair<string, double> pair
					in data.MachineSpending
			)
			{
				GDictionary entry =
					new();


				entry["name"] =
					pair.Key;


				entry["amount"] =
					pair.Value;


				machineSpending.Add(
					entry
				);
			}
		}


		result["machine_spending"] =
			machineSpending;


		return result;
	}


	private static StatsSaveData StatsFromDictionary(
		GDictionary data)
	{
		StatsSaveData result =
			new()
			{
				TotalEarned =
					GetDouble(
						data,
						"total_earned",
						0
					),

				OfflineEarned =
					GetDouble(
						data,
						"offline_earned",
						0
					),

				TotalSpent =
					GetDouble(
						data,
						"total_spent",
						0
					),

				SlotUnlockSpent =
					GetDouble(
						data,
						"slot_unlock_spent",
						0
					)
			};


		GArray machineSpending =
			GetArray(
				data,
				"machine_spending"
			);


		foreach (
			Variant entryVariant
				in machineSpending
		)
		{
			GDictionary entry =
				entryVariant
					.AsGodotDictionary();


			string name =
				GetString(
					entry,
					"name",
					""
				);


			if (
				string.IsNullOrWhiteSpace(
					name
				)
			)
			{
				continue;
			}


			result.MachineSpending[
				name
			] =
				GetDouble(
					entry,
					"amount",
					0
				);
		}


		return result;
	}


	// ==================================================
	// COLLECTION HELPERS
	// ==================================================

	private static GArray StringListToArray(
		IEnumerable<string>? values)
	{
		GArray result =
			new();


		if (values == null)
			return result;


		foreach (
			string value
				in values
		)
		{
			result.Add(
				value
			);
		}


		return result;
	}


	private static List<string> ArrayToStringList(
		GArray array)
	{
		List<string> result =
			[];


		foreach (
			Variant item
				in array
		)
		{
			string value =
				item.AsString();


			if (
				!string.IsNullOrWhiteSpace(
					value
				)
			)
			{
				result.Add(
					value
				);
			}
		}


		return result;
	}


	// ==================================================
	// VALUE HELPERS
	// ==================================================

	private static bool GetBool(
		GDictionary data,
		string key,
		bool fallback)
	{
		if (
			!data.TryGetValue(
				key,
				out Variant value
			)
		)
		{
			return fallback;
		}


		return value.AsBool();
	}


	private static int GetInt(
		GDictionary data,
		string key,
		int fallback)
	{
		if (
			!data.TryGetValue(
				key,
				out Variant value
			)
		)
		{
			return fallback;
		}


		return value.AsInt32();
	}


	private static long GetLong(
		GDictionary data,
		string key,
		long fallback)
	{
		if (
			!data.TryGetValue(
				key,
				out Variant value
			)
		)
		{
			return fallback;
		}


		return value.AsInt64();
	}


	private static double GetDouble(
		GDictionary data,
		string key,
		double fallback)
	{
		if (
			!data.TryGetValue(
				key,
				out Variant value
			)
		)
		{
			return fallback;
		}


		return value.AsDouble();
	}


	private static string GetString(
		GDictionary data,
		string key,
		string fallback)
	{
		if (
			!data.TryGetValue(
				key,
				out Variant value
			)
		)
		{
			return fallback;
		}


		return value.AsString();
	}


	private static GArray GetArray(
		GDictionary data,
		string key)
	{
		if (
			!data.TryGetValue(
				key,
				out Variant value
			)
		)
		{
			return new GArray();
		}


		return value.AsGodotArray();
	}


	private static GDictionary GetDictionary(
		GDictionary data,
		string key)
	{
		if (
			!data.TryGetValue(
				key,
				out Variant value
			)
		)
		{
			return new GDictionary();
		}


		return value.AsGodotDictionary();
	}


	// ==================================================
	// FILE HELPERS
	// ==================================================

	private static void DeleteFileIfExists(
		string path)
	{
		if (
			!Godot.FileAccess.FileExists(
				path
			)
		)
		{
			return;
		}


		Error error =
			DirAccess.RemoveAbsolute(
				ProjectSettings.GlobalizePath(
					path
				)
			);


		if (
			error != Error.Ok
			&& error != Error.DoesNotExist
		)
		{
			GD.PushWarning(
				"Could not delete save file "
					+ path
					+ ": "
					+ error
			);
		}
	}
}
