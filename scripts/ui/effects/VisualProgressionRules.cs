using System;

namespace IdleAi;

/// <summary>Presentation only: no stored state and no economy dependencies.</summary>
public static class VisualProgressionRules
{
    public static int MachineStage(int tier) => Math.Clamp(tier, 0, 5);

    // One starting slot is the base room. Expansion and average tier contribute
    // independently, so unlocking a fresh room never implies advanced machinery.
    public static int RoomStage(bool unlocked, int slotCount, int unlockedSlots, double averageTier)
    {
        if (!unlocked || slotCount <= 0 || unlockedSlots <= 0) return 0;
        unlockedSlots = Math.Clamp(unlockedSlots, 0, slotCount);
        double expansion = slotCount > 1 ? (double)(unlockedSlots - 1) / (slotCount - 1) : 0;
        double tier = double.IsFinite(averageTier) ? Math.Clamp(averageTier, 0, 5) : 0;
        return Math.Clamp((int)Math.Floor(expansion * 2 + tier * 2 / 3), 0, 4);
    }

    public static double PulseSeconds(double cycleDuration)
        => double.IsFinite(cycleDuration) && cycleDuration > 0
            ? Math.Clamp(cycleDuration * 0.18, 0.45, 2.4) : 2.4;
}
