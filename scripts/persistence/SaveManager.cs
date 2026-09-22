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
			string json =
				JsonSerializer.Serialize(
					data,
					SaveJsonContext
						.Default
						.SaveGameData
				);


			if (
				string.IsNullOrWhiteSpace(
					json
				)
			)
			{
				GD.PushError(
					"Save serialization produced empty JSON."
				);


				return false;
			}


			/*
			 * Stage 1: write a temporary file.
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
			 * Stage 2: read and deserialize the temp file.
			 * Only a verified save may replace the primary.
			 */
			SaveGameData? verified =
				TryLoadFromPath(
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
			 * Stage 3: preserve the current valid primary.
			 */
			if (
				Godot.FileAccess.FileExists(
					SavePath
				)
			)
			{
				SaveGameData? oldPrimary =
					TryLoadFromPath(
						SavePath,
						logErrors:
							false
					);


				if (oldPrimary != null)
				{
					string? currentJson =
						ReadTextFile(
							SavePath
						);


					if (
						!string.IsNullOrWhiteSpace(
							currentJson
						)
					)
					{
						WriteTextFile(
							BackupSavePath,
							currentJson
						);
					}
				}
			}


			/*
			 * Stage 4: write verified data to primary.
			 */
			if (
				!WriteTextFile(
					SavePath,
					json
				)
			)
			{
				return false;
			}


			DeleteFileIfExists(
				TempSavePath
			);


			GD.Print(
				"Idle AI save written: ",
				ProjectSettings.GlobalizePath(
					SavePath
				)
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
		SaveGameData? primary =
			TryLoadFromPath(
				SavePath,
				logErrors:
					true
			);


		if (primary != null)
		{
			GD.Print(
				"Idle AI save loaded from primary."
			);


			return primary;
		}


		SaveGameData? backup =
			TryLoadFromPath(
				BackupSavePath,
				logErrors:
					true
			);


		if (backup != null)
		{
			GD.PushWarning(
				"Primary save was unavailable. Backup save loaded."
			);


			return backup;
		}


		/*
		 * If corrupt files from an older broken build are
		 * present, move them out of the active save names.
		 *
		 * This is intentionally different from permanently
		 * blocking all future saves: that old behavior caused
		 * a broken file to make the game unable to save at all.
		 */
		QuarantineIfInvalid(
			SavePath
		);


		QuarantineIfInvalid(
			BackupSavePath
		);


		GD.Print(
			"No readable Idle AI save found. Starting a new save."
		);


		return null;
	}


	private SaveGameData? TryLoadFromPath(
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
				if (logErrors)
				{
					GD.PushError(
						$"Save file is empty: {path}"
					);
				}


				return null;
			}


			SaveGameData? data =
				JsonSerializer.Deserialize(
					json,
					SaveJsonContext
						.Default
						.SaveGameData
				);


			if (
				data == null
			)
			{
				if (logErrors)
				{
					GD.PushError(
						$"Save file deserialized to null: {path}"
					);
				}


				return null;
			}


			return data;
		}
		catch (Exception exception)
		{
			if (logErrors)
			{
				GD.PushError(
					$"Could not load save from {path}: {exception}"
				);
			}


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
	}


	// ==================================================
	// CORRUPT SAVE HANDLING
	// ==================================================

	private static void QuarantineIfInvalid(
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


		string absolute =
			ProjectSettings.GlobalizePath(
				path
			);


		string quarantine =
			absolute
			+ ".corrupt_"
			+ DateTimeOffset.UtcNow
				.ToUnixTimeSeconds();


		Error error =
			DirAccess.RenameAbsolute(
				absolute,
				quarantine
			);


		if (error != Error.Ok)
		{
			GD.PushWarning(
				$"Could not quarantine invalid save {path}: {error}"
			);
		}
		else
		{
			GD.PushWarning(
				$"Invalid save moved to: {quarantine}"
			);
		}
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
			return null;
		}


		return file.GetAsText();
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
				$"Could not delete file {path}: {error}"
			);
		}
	}
}
