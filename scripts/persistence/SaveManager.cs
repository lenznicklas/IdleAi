using Godot;

using System;
using System.IO;
using System.Text.Json;

namespace IdleAi;

public partial class SaveManager : Node
{
	private const string SavePath =
		"user://idle_ai_save.json";


	private Timer? _autosaveTimer;


	public event Action? AutosaveRequested;


	public void StartAutosave(
		double intervalSeconds)
	{
		if (_autosaveTimer != null)
		{
			_autosaveTimer.QueueFree();
		}


		_autosaveTimer = new Timer
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
					WriteIndented = true
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


	public SaveGameData? LoadData()
	{
		if (!HasSave())
			return null;


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


			return JsonSerializer.Deserialize<SaveGameData>(
				json
			);
		}
		catch (Exception exception)
		{
			GD.PushError(
				$"Could not load save: {exception}"
			);

			return null;
		}
	}


	public bool HasSave()
	{
		return File.Exists(
			ProjectSettings.GlobalizePath(
				SavePath
			)
		);
	}


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
