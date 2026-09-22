using Godot;

using System;
using System.IO;
using System.Text.Json;

namespace IdleAi;

public partial class SaveManager : Node
{
	/*
	 * This is intentionally the same save mechanism that was
	 * in the repository BEFORE the TopBar/Room overlay push.
	 *
	 * It was already working on both desktop and Android:
	 *
	 * user:// -> ProjectSettings.GlobalizePath()
	 * System.IO -> File.WriteAllText / File.ReadAllText
	 * System.Text.Json -> SaveGameData
	 */
	private const string SavePath =
		"user://idle_ai_save.json";


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
		try
		{
			string absolutePath =
				ProjectSettings.GlobalizePath(
					SavePath
				);


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


			File.WriteAllText(
				absolutePath,
				json
			);


			/*
			 * Diagnostic output so we can see the exact
			 * persistent path and the first machine level.
			 */
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
		if (!HasSave())
		{
			GD.Print(
				"Idle AI: no existing save file."
			);


			return null;
		}


		try
		{
			string absolutePath =
				ProjectSettings.GlobalizePath(
					SavePath
				);


			string json =
				File.ReadAllText(
					absolutePath
				);


			SaveGameData? data =
				JsonSerializer.Deserialize<SaveGameData>(
					json
				);


			if (data == null)
			{
				GD.PushError(
					"Idle AI save deserialized to null."
				);


				return null;
			}


			int firstMachineLevel =
				data.Rooms.Count > 0
				&& data.Rooms[0].Slots.Count > 0
					? data.Rooms[0]
						.Slots[0]
						.MachineLevel
					: -1;


			GD.Print(
				"Idle AI LOAD OK | ",
				absolutePath,
				" | room0-slot0-level=",
				firstMachineLevel
			);


			return data;
		}
		catch (Exception exception)
		{
			GD.PushError(
				$"Could not load save: {exception}"
			);


			return null;
		}
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
		);
	}


	// ==================================================
	// DELETE
	// ==================================================

	public void DeleteSave()
	{
		string path =
			ProjectSettings.GlobalizePath(
				SavePath
			);


		if (File.Exists(path))
		{
			File.Delete(path);
		}
	}
}
