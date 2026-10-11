using System;

namespace IdleAi;

/// <summary>Testable vibration policy; the injected action owns device access.</summary>
public sealed class HapticsService
{
    private readonly Action<int, float> _vibrate;
    public bool VibrationEnabled { get; set; } = true;

    public HapticsService(Action<int, float> vibrate) { _vibrate = vibrate; }

    public void VibrateHandheld(int durationMs = 500, float amplitude = -1.0f)
    {
        if (VibrationEnabled) _vibrate(durationMs, amplitude);
    }
}
