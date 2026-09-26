using Godot;
using System;
using System.Text.Json;

namespace IdleAi;


public sealed class GameSettingsService
{
	private const int SettingsSaveVersion =
		1;

	private const string SettingsSavePath =
		"user://idle_ai_settings.json";

	private const string TemporarySettingsSavePath =
		"user://idle_ai_settings.tmp.json";

	private const string BackgroundMusicPath =
		"res://assets/music/score.wav";


	private static GameSettingsService? _current;


	private readonly Game _root;


	private AudioStreamPlayer? _musicPlayer;


	public bool VibrationEnabled { get; private set; } =
		true;

	public bool SoundEnabled { get; private set; } =
		true;


	public static bool IsVibrationEnabled =>
		_current?.VibrationEnabled
			?? true;


	public event Action? Changed;


	public GameSettingsService(
		Game root)
	{
		_root =
			root;

		_current =
			this;

		Load();

		CreateBackgroundMusic();

		ApplySoundState();
	}


	// ==================================================
	// VIBRATION
	// ==================================================

	public void SetVibrationEnabled(
		bool enabled)
	{
		if (
			VibrationEnabled
			== enabled
		)
		{
			return;
		}

		VibrationEnabled =
			enabled;

		Save();

		Changed?.Invoke();
	}


	// ==================================================
	// SOUND
	// ==================================================

	public void SetSoundEnabled(
		bool enabled)
	{
		if (
			SoundEnabled
			== enabled
		)
		{
			return;
		}

		SoundEnabled =
			enabled;

		ApplySoundState();

		Save();

		Changed?.Invoke();
	}


	private void CreateBackgroundMusic()
	{
		if (
			!ResourceLoader.Exists(
				BackgroundMusicPath
			)
		)
		{
			GD.PushWarning(
				"Background music not found: "
					+ BackgroundMusicPath
			);

			return;
		}

		AudioStream? stream =
			GD.Load<AudioStream>(
				BackgroundMusicPath
			);

		if (stream == null)
		{
			GD.PushWarning(
				"Could not load background music: "
					+ BackgroundMusicPath
			);

			return;
		}

		_musicPlayer =
			new AudioStreamPlayer
			{
				Name =
					"IdleAiBackgroundMusic",

				Stream =
					stream,

				VolumeDb =
					-8.0f
			};

		_musicPlayer.Finished +=
			OnBackgroundMusicFinished;

		_root.AddChild(
			_musicPlayer
		);
	}


	private void OnBackgroundMusicFinished()
	{
		if (
			!SoundEnabled
			|| _musicPlayer == null
		)
		{
			return;
		}

		/*
		 * score.wav does not need to be imported as a looping WAV.
		 * Restarting on Finished guarantees an endless soundtrack.
		 */
		_musicPlayer.Play();
	}


	private void ApplySoundState()
	{
		int masterBus =
			AudioServer.GetBusIndex(
				"Master"
			);

		if (masterBus >= 0)
		{
			/*
			 * This affects the whole Godot game, including future
			 * sound effects added later.
			 */
			AudioServer.SetBusMute(
				masterBus,
				!SoundEnabled
			);
		}

		if (_musicPlayer == null)
			return;

		if (SoundEnabled)
		{
			if (!_musicPlayer.Playing)
			{
				_musicPlayer.Play();
			}
		}
		else
		{
			_musicPlayer.Stop();
		}
	}


	// ==================================================
	// LOAD / SAVE
	// ==================================================

	private void Load()
	{
		VibrationEnabled =
			true;

		SoundEnabled =
			true;

		string path =
			ProjectSettings.GlobalizePath(
				SettingsSavePath
			);

		if (!System.IO.File.Exists(path))
			return;

		try
		{
			string json =
				System.IO.File.ReadAllText(
					path
				);

			GameSettingsSaveData? save =
				JsonSerializer.Deserialize<GameSettingsSaveData>(
					json
				);

			if (save == null)
				return;

			VibrationEnabled =
				save.VibrationEnabled;

			SoundEnabled =
				save.SoundEnabled;
		}
		catch (Exception exception)
		{
			GD.PushWarning(
				"Could not load Idle AI settings: "
					+ exception.Message
			);

			VibrationEnabled =
				true;

			SoundEnabled =
				true;
		}
	}


	private void Save()
	{
		string path =
			ProjectSettings.GlobalizePath(
				SettingsSavePath
			);

		string temporaryPath =
			ProjectSettings.GlobalizePath(
				TemporarySettingsSavePath
			);

		try
		{
			GameSettingsSaveData save =
				new()
				{
					SaveVersion =
						SettingsSaveVersion,

					VibrationEnabled =
						VibrationEnabled,

					SoundEnabled =
						SoundEnabled
				};

			string json =
				JsonSerializer.Serialize(
					save,
					new JsonSerializerOptions
					{
						WriteIndented =
							true
					}
				);

			System.IO.File.WriteAllText(
				temporaryPath,
				json
			);

			System.IO.File.Move(
				temporaryPath,
				path,
				overwrite: true
			);
		}
		catch (Exception exception)
		{
			GD.PushWarning(
				"Could not save Idle AI settings: "
					+ exception.Message
			);

			try
			{
				if (
					System.IO.File.Exists(
						temporaryPath
					)
				)
				{
					System.IO.File.Delete(
						temporaryPath
					);
				}
			}
			catch
			{
				// Preserve the useful original error.
			}
		}
	}


	private sealed class GameSettingsSaveData
	{
		public int SaveVersion { get; set; }

		public bool VibrationEnabled { get; set; } =
			true;

		public bool SoundEnabled { get; set; } =
			true;
	}
}
