using Godot;
using IdleAi;
using System;

public partial class Runner : Node
{
    private int _checks;
    private void Check(bool condition, string message) { _checks++; if (!condition) throw new Exception(message); }
    public override void _Ready()
    {
        int master = AudioServer.GetBusIndex("Master");
        bool originalMute = AudioServer.IsBusMute(master);
        try
        {
            var audio = AudioService.ForRoot(this);
            audio.SetSoundEnabled(false);
            var player = GetNode<AudioStreamPlayer>("IdleAiBackgroundMusic");
            Check(GetChildCount() == 1, "one player");
            Check(player.Stream != null && player.Stream.ResourcePath == "res://assets/music/score.wav", "real stream resolves");
            Check(player.VolumeDb == -8.0f, "volume");
            Check(!player.Playing && AudioServer.IsBusMute(master), "disabled startup");
            Check(ReferenceEquals(audio, AudioService.ForRoot(this)), "root scoped ownership");
            var persistence = new SettingsPersistenceService("/missing/settings", "/missing/temp", _ => {});
            var haptics = new HapticsService((_, _) => {});
            var first = new GameSettingsService(persistence, audio.SetSoundEnabled, haptics);
            var second = new GameSettingsService(persistence, AudioService.ForRoot(this).SetSoundEnabled, haptics);
            Check(GetChildCount() == 1, "repeated settings no duplicate");
            Check(player.Playing && !AudioServer.IsBusMute(master), "enabled playback and Master");
            player.Stop(); player.EmitSignal(AudioStreamPlayer.SignalName.Finished);
            Check(player.Playing, "Finished restarts enabled music");
            second.SetSoundEnabled(false);
            player.EmitSignal(AudioStreamPlayer.SignalName.Finished);
            Check(!player.Playing && AudioServer.IsBusMute(master), "Finished suppressed disabled");
            second.SetSoundEnabled(true);
            Check(player.Playing && !AudioServer.IsBusMute(master), "re-enable");
            Console.WriteLine($"SettingsAudio: {_checks} assertions passed.");
            audio.SetSoundEnabled(false);
            var stream = player.Stream;
            player.Stream = null;
            player.Free();
            stream.Dispose();
            GetTree().Quit();
        }
        catch (Exception exception) { Console.Error.WriteLine(exception); GetTree().Quit(1); }
        finally { AudioServer.SetBusMute(master, originalMute); }
    }
}
