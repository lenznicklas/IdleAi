namespace IdleAi;

/// <summary>Compatibility boundary for all existing UI vibration calls.</summary>
public static class Input
{
    internal static HapticsService CreateHaptics() => new((duration, amplitude) =>
        Godot.Input.VibrateHandheld(duration, amplitude));

    private static HapticsService _haptics = CreateHaptics();

    internal static void UseHaptics(HapticsService haptics) => _haptics = haptics;

    public static void VibrateHandheld(int durationMs = 500, float amplitude = -1.0f) =>
        _haptics.VibrateHandheld(durationMs, amplitude);
}
