namespace IdleAi;

/// <summary>Independent settings JSON contract; property names and defaults are stable.</summary>
public sealed class GameSettingsData
{
    public int SaveVersion { get; set; }
    public bool VibrationEnabled { get; set; } = true;
    public bool SoundEnabled { get; set; } = true;
}
