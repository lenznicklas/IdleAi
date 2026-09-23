using Godot;

using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace IdleAi;

public partial class SaveManager : Node
{
	private const string SavePath =
		"user://idle_ai_save.json";

	private const string BackupSavePath =
		"user://idle_ai_save.bak.json";

	private const string TemporarySavePath =
		"user://idle_ai_save.tmp.json";

	private Timer? _autosaveTimer;

	public event Action? AutosaveRequested;

	/// <summary>
	/// True only when save files existed but neither the main save nor its
	/// backup could be loaded. While this is true SaveData refuses to overwrite
	/// the files, so a broken save cannot silently be replaced by a fresh game.
	/// DeleteSave() clears the lock.
	/// </summary>
	public bool LastLoadFailed { get; private set; }

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
				WaitTime = intervalSeconds,
				OneShot = false,
				Autostart = true
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
		if (LastLoadFailed)
		{
			GD.PushError(
				"Idle AI: autosave blocked because the existing save could not be recovered. "
				+ "The damaged save has NOT been overwritten."
			);

			return false;
		}

		string absolutePath =
			ProjectSettings.GlobalizePath(
				SavePath
			);

		string backupPath =
			ProjectSettings.GlobalizePath(
				BackupSavePath
			);

		string temporaryPath =
			ProjectSettings.GlobalizePath(
				TemporarySavePath
			);

		try
		{
			JsonSerializerOptions options =
				new()
				{
					WriteIndented = true
				};

			string json =
				JsonSerializer.Serialize(
					data,
					options
				);

			/*
			 * Never write directly over the active save.
			 * Write + flush a temporary file first, then replace the main file.
			 */
			WriteTextAndFlush(
				temporaryPath,
				json
			);

			/*
			 * Keep the previous known-good main save as backup. A corrupted main
			 * save is deliberately NOT copied over an older working backup.
			 */
			if (
				File.Exists(absolutePath)
				&& TryReadSaveFile(
					absolutePath,
					out _
				)
			)
			{
				File.Copy(
					absolutePath,
					backupPath,
					overwrite: true
				);
			}

			File.Move(
				temporaryPath,
				absolutePath,
				overwrite: true
			);

			int firstMachineLevel =
				data.Rooms.Count > 0
				&& data.Rooms[0].Slots.Count > 0
					? data.Rooms[0]
						.Slots[0]
						.MachineLevel
					: -1;

			GD.Print(
				"Idle AI SAVE OK | ",
				absolutePath,
				" | room0-slot0-level=",
				firstMachineLevel
			);

			return true;
		}
		catch (Exception exception)
		{
			TryDeleteTemporaryFile(
				temporaryPath
			);

			GD.PushError(
				$"Could not save game: {exception}"
			);

			return false;
		}
	}

	private static void WriteTextAndFlush(
		string path,
		string contents)
	{
		using FileStream stream =
			new(
				path,
				FileMode.Create,
				System.IO.FileAccess.Write,
				FileShare.None
			);

		using StreamWriter writer =
			new(
				stream,
				new UTF8Encoding(
					encoderShouldEmitUTF8Identifier: false
				),
				bufferSize: 4096,
				leaveOpen: true
			);

		writer.Write(
			contents
		);

		writer.Flush();
		stream.Flush(
			flushToDisk: true
		);
	}

	// ==================================================
	// LOAD
	// ==================================================

	public SaveGameData? LoadData()
	{
		LastLoadFailed =
			false;

		string absolutePath =
			ProjectSettings.GlobalizePath(
				SavePath
			);

		string backupPath =
			ProjectSettings.GlobalizePath(
				BackupSavePath
			);

		bool mainExists =
			File.Exists(
				absolutePath
			);

		bool backupExists =
			File.Exists(
				backupPath
			);

		if (
			!mainExists
			&& !backupExists
		)
		{
			GD.Print(
				"Idle AI: no existing save file."
			);

			return null;
		}

		if (
			mainExists
			&& TryReadSaveFile(
				absolutePath,
				out SaveGameData? mainSave
			)
		)
		{
			PrintLoadedSave(
				absolutePath,
				mainSave!
			);

			return mainSave;
		}

		if (mainExists)
		{
			GD.PushWarning(
				"Idle AI: main save is invalid. Trying backup."
			);
		}

		if (
			backupExists
			&& TryReadSaveFile(
				backupPath,
				out SaveGameData? backupSave
			)
		)
		{
			GD.PushWarning(
				"Idle AI: recovered save from backup."
			);

			PrintLoadedSave(
				backupPath,
				backupSave!
			);

			/* Restore the valid backup as the active save. */
			try
			{
				File.Copy(
					backupPath,
					absolutePath,
					overwrite: true
				);
			}
			catch (Exception exception)
			{
				GD.PushWarning(
					"Idle AI: backup loaded, but main save could not be restored: "
					+ exception.Message
				);
			}

			return backupSave;
		}

		LastLoadFailed =
			true;

		GD.PushError(
			"Idle AI: save recovery failed. Existing save files are being preserved and autosave is blocked."
		);

		return null;
	}

	private static bool TryReadSaveFile(
		string path,
		out SaveGameData? data)
	{
		data =
			null;

		try
		{
			string json =
				File.ReadAllText(
					path
				);

			data =
				JsonSerializer.Deserialize<SaveGameData>(
					json
				);

			return data != null;
		}
		catch (Exception exception)
		{
			GD.PushWarning(
				$"Could not read save file '{path}': {exception.Message}"
			);

			return false;
		}
	}

	private static void PrintLoadedSave(
		string path,
		SaveGameData data)
	{
		int firstMachineLevel =
			data.Rooms.Count > 0
			&& data.Rooms[0].Slots.Count > 0
				? data.Rooms[0]
					.Slots[0]
					.MachineLevel
				: -1;

		GD.Print(
			"Idle AI LOAD OK | ",
			path,
			" | room0-slot0-level=",
			firstMachineLevel
		);
	}

	// ==================================================
	// EXISTS
	// ==================================================

	public bool HasSave()
	{
		return File.Exists(
			ProjectSettings.GlobalizePath(
				SavePath
			)
		)
		|| File.Exists(
			ProjectSettings.GlobalizePath(
				BackupSavePath
			)
		);
	}

	// ==================================================
	// DELETE
	// ==================================================

	public void DeleteSave()
	{
		DeleteIfExists(
			ProjectSettings.GlobalizePath(
				SavePath
			)
		);

		DeleteIfExists(
			ProjectSettings.GlobalizePath(
				BackupSavePath
			)
		);

		DeleteIfExists(
			ProjectSettings.GlobalizePath(
				TemporarySavePath
			)
		);

		LastLoadFailed =
			false;
	}

	private static void DeleteIfExists(
		string path)
	{
		if (File.Exists(path))
		{
			File.Delete(path);
		}
	}

	private static void TryDeleteTemporaryFile(
		string path)
	{
		try
		{
			DeleteIfExists(path);
		}
		catch
		{
			// Do not hide the original save exception because temp cleanup failed.
		}
	}
}
