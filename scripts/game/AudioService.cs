#nullable enable
using Godot;
using System.Runtime.CompilerServices;

namespace IdleAi;

/// <summary>One scene-owned soundtrack and global sound policy per root.</summary>
public sealed class AudioService
{
    private static readonly ConditionalWeakTable<Node, AudioService> Sessions = new();
    private const string BackgroundMusicPath = "res://assets/music/score.wav";
    private readonly Node _root;
    private AudioStreamPlayer? _musicPlayer;
    private bool _initialized;
    public bool SoundEnabled { get; private set; } = true;

    private AudioService(Node root) { _root = root; }

    public static AudioService ForRoot(Node root) => Sessions.GetValue(root, node => new AudioService(node));

    public void SetSoundEnabled(bool enabled)
    {
        SoundEnabled = enabled;
        if (!_initialized)
        {
            _initialized = true;
            CreateBackgroundMusic();
        }
        ApplySoundState();
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


}
