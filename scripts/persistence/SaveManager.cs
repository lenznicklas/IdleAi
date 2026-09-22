using Godot;

using System;
using System.Text.Json;

namespace IdleAi;

public partial class SaveManager : Node
{
	private const string SavePath =
		"user://idle_ai_save.json";


	private const string BackupSavePath =
		"user://idle_ai_save_backup.json";


	private const string TempSavePath =
		"user://idle_ai_save_tmp.json";


	private Timer? _autosaveTimer;


	private bool _loadAttempted;

	private bool _loadSucceeded;

	private bool _protectExistingSaveAfterLoadFailure;


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
					intervalSeconds,

				OneShot =
					false,

				Autostart =
					true
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
		/*
		 * Critical protection:
		 *
		 * If a save existed when loading was attempted but
		 * BOTH the primary save and backup could not be read,
		 * never overwrite them with a fresh default game.
		 *
		 * This prevents a temporary/corrupt load failure from
		 * turning into permanent progress loss.
		 */
		if (_protectExistingSaveAfterLoadFailure)
		{
			GD.PushError(
				"Save blocked because an existing save failed to load. "
				+ "The old save files were left untouched."
			);


			return false;
		}


		try
		{
			JsonSerializerOptions options =
				new()
				{
					WriteIndented =
						true
				};


			string json =
				JsonSerializer.Serialize(
					data,
					options
				);


			/*
			 * Write a temporary file first.
			 * We only replace the real save after the temp
			 * file has been written successfully.
			 */
			if (
				!WriteTextFile(
					TempSavePath,
					json
				)
			)
			{
				return false;
			}


			/*
			 * Keep the previous valid primary file as backup.
			 * A fresh install has no primary yet, which is fine.
			 */
			if (
				Godot.FileAccess.FileExists(
					SavePath
				)
			)
			{
				string? currentPrimary =
					ReadTextFile(
						SavePath
					);


				if (
					!string.IsNullOrWhiteSpace(
						currentPrimary
					)
					&& CanDeserialize(
						currentPrimary
					)
				)
				{
					WriteTextFile(
						BackupSavePath,
						currentPrimary
					);
				}
			}


			string? tempJson =
				ReadTextFile(
					TempSavePath
				);


			if (
				string.IsNullOrWhiteSpace(
					tempJson
				)
				|| !CanDeserialize(
					tempJson
				)
			)
			{
				GD.PushError(
					"Temporary save verification failed. "
					+ "Existing save was not replaced."
				);


				return false;
			}


			if (
				!WriteTextFile(
					SavePath,
					tempJson
				)
			)
			{
				return false;
			}


			DeleteFileIfExists(
				TempSavePath
			);


			return true;
		}
		catch (Exception exception)
		{
			GD.PushError(
				$"Could not save game: {exception}"
			);


			return false;
		}
	}


	// ==================================================
	// LOAD
	// ==================================================

	public SaveGameData? LoadData()
	{
		_loadAttempted =
			true;


		_loadSucceeded =
			false;


		bool primaryExists =
			Godot.FileAccess.FileExists(
				SavePath
			);


		bool backupExists =
			Godot.FileAccess.FileExists(
				BackupSavePath
			);


		if (
			!primaryExists
			&& !backupExists
		)
		{
			/*
			 * Real first launch.
			 * Saving a new game is allowed.
			 */
			_protectExistingSaveAfterLoadFailure =
				false;


			return null;
		}


		SaveGameData? primary =
			TryLoadFromPath(
				SavePath
			);


		if (primary != null)
		{
			_loadSucceeded =
				true;


			_protectExistingSaveAfterLoadFailure =
				false;


			return primary;
		}


		GD.PushWarning(
			"Primary save could not be loaded. Trying backup save."
		);


		SaveGameData? backup =
			TryLoadFromPath(
				BackupSavePath
			);


		if (backup != null)
		{
			_loadSucceeded =
				true;


			_protectExistingSaveAfterLoadFailure =
				false;


			GD.PushWarning(
				"Backup save loaded successfully."
			);


			/*
			 * Do not rewrite immediately here.
			 * Game.cs will save normally after the whole state
			 * has been restored.
			 */
			return backup;
		}


		/*
		 * A save existed but neither copy could be loaded.
		 * Protect those files from the automatic SaveGame()
		 * at the end of Game._Ready().
		 */
		_protectExistingSaveAfterLoadFailure =
			true;


		GD.PushError(
			"Existing save data could not be loaded. "
			+ "Autosave/manual save is blocked so the old files "
			+ "cannot be overwritten."
		);


		return null;
	}


	private SaveGameData? TryLoadFromPath(
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
			string? json =
				ReadTextFile(
					path
				);


			if (
				string.IsNullOrWhiteSpace(
					json
				)
			)
			{
				GD.PushError(
					$"Save file is empty: {path}"
				);


				return null;
			}


			SaveGameData? data =
				JsonSerializer.Deserialize<SaveGameData>(
					json
				);


			if (data == null)
			{
				GD.PushError(
					$"Save file deserialized to null: {path}"
				);


				return null;
			}


			return data;
		}
		catch (Exception exception)
		{
			GD.PushError(
				$"Could not load save from {path}: {exception}"
			);


			return null;
		}
	}


	// ==================================================
	// SAVE STATE
	// ==================================================

	public bool HasSave()
	{
		return Godot.FileAccess.FileExists(
				SavePath
			)
			|| Godot.FileAccess.FileExists(
				BackupSavePath
			);
	}


	public bool LastLoadSucceeded =>
		_loadAttempted
		&& _loadSucceeded;


	public bool SaveProtectionActive =>
		_protectExistingSaveAfterLoadFailure;


	// ==================================================
	// DELETE
	// ==================================================

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


		_loadAttempted =
			false;


		_loadSucceeded =
			false;


		_protectExistingSaveAfterLoadFailure =
			false;
	}


	// ==================================================
	// FILE HELPERS
	// ==================================================

	private static bool WriteTextFile(
		string path,
		string content)
	{
		using Godot.FileAccess? file =
			Godot.FileAccess.Open(
				path,
				Godot.FileAccess.ModeFlags.Write
			);


		if (file == null)
		{
			GD.PushError(
				$"Could not open save file for writing: {path}. "
				+ $"Godot error: {Godot.FileAccess.GetOpenError()}"
			);


			return false;
		}


		file.StoreString(
			content
		);


		file.Flush();


		return true;
	}


	private static string? ReadTextFile(
		string path)
	{
		using Godot.FileAccess? file =
			Godot.FileAccess.Open(
				path,
				Godot.FileAccess.ModeFlags.Read
			);


		if (file == null)
		{
			GD.PushError(
				$"Could not open save file for reading: {path}. "
				+ $"Godot error: {Godot.FileAccess.GetOpenError()}"
			);


			return null;
		}


		return file.GetAsText();
	}


	private static bool CanDeserialize(
		string json)
	{
		try
		{
			return JsonSerializer
				.Deserialize<SaveGameData>(
					json
				)
				!= null;
		}
		catch
		{
			return false;
		}
	}


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


		string absolutePath =
			ProjectSettings.GlobalizePath(
				path
			);


		Error error =
			DirAccess.RemoveAbsolute(
				absolutePath
			);


		if (
			error != Error.Ok
			&& error != Error.DoesNotExist
		)
		{
			GD.PushWarning(
				$"Could not delete file {path}: {error}"
			);
		}
	}
}
