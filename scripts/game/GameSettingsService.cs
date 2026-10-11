#nullable enable
using System;

namespace IdleAi;

/// <summary>Public preferences facade. Runtime actions are injected for deterministic testing.</summary>
public sealed class GameSettingsService
{
    private readonly Action<GameSettingsData> _save;
    private readonly Action<bool> _applySound;
    private readonly HapticsService _haptics;
    public bool VibrationEnabled { get; private set; } = true;
    public bool SoundEnabled { get; private set; } = true;
    public event Action? Changed;

    public GameSettingsService(SettingsPersistenceService persistence, Action<bool> applySound, HapticsService haptics)
        : this(persistence.Load, persistence.Save, applySound, haptics) { }

    public GameSettingsService(Func<GameSettingsData> load, Action<GameSettingsData> save,
        Action<bool> applySound, HapticsService haptics)
    {
        _save = save;
        _applySound = applySound;
        _haptics = haptics;
        GameSettingsData settings = load();
        VibrationEnabled = settings.VibrationEnabled;
        SoundEnabled = settings.SoundEnabled;
        _haptics.VibrationEnabled = VibrationEnabled;
        _applySound(SoundEnabled);
    }

    public void SetVibrationEnabled(bool enabled)
    {
        if (VibrationEnabled == enabled) return;
        VibrationEnabled = enabled;
        _haptics.VibrationEnabled = enabled;
        Save();
        Changed?.Invoke();
    }

    public void SetSoundEnabled(bool enabled)
    {
        if (SoundEnabled == enabled) return;
        SoundEnabled = enabled;
        _applySound(enabled);
        Save();
        Changed?.Invoke();
    }

    private void Save() => _save(new GameSettingsData
    {
        SaveVersion = SettingsPersistenceService.SettingsSaveVersion,
        VibrationEnabled = VibrationEnabled,
        SoundEnabled = SoundEnabled
    });
}
