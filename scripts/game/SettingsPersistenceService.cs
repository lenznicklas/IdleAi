using System;
using System.IO;
using System.Text.Json;

namespace IdleAi;

public sealed class SettingsPersistenceService
{
    public const int SettingsSaveVersion = 1;
    public const string SettingsSavePath = "user://idle_ai_settings.json";
    public const string TemporarySettingsSavePath = "user://idle_ai_settings.tmp.json";
    private readonly string _path;
    private readonly string _temporaryPath;
    private readonly Action<string> _warn;

    public SettingsPersistenceService(string path, string temporaryPath, Action<string> warn)
    {
        _path = path;
        _temporaryPath = temporaryPath;
        _warn = warn;
    }

    public GameSettingsData Load()
    {
        if (!File.Exists(_path)) return new();
        try
        {
            return JsonSerializer.Deserialize<GameSettingsData>(File.ReadAllText(_path)) ?? new();
        }
        catch (Exception exception)
        {
            _warn("Could not load Idle AI settings: " + exception.Message);
            return new();
        }
    }

    public void Save(GameSettingsData settings)
    {
        try
        {
            string json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_temporaryPath, json);
            File.Move(_temporaryPath, _path, overwrite: true);
        }
        catch (Exception exception)
        {
            _warn("Could not save Idle AI settings: " + exception.Message);
            try { if (File.Exists(_temporaryPath)) File.Delete(_temporaryPath); }
            catch { /* Preserve the useful original error. */ }
        }
    }
}
