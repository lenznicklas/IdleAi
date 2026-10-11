using System;
using IdleAi;

int checks = 0;
void Equal(double actual, double expected, string label)
{
    if (actual != expected) throw new Exception($"{label}: {actual} != {expected}");
    checks++;
}
for (int tier = 0; tier <= 5; tier++) Equal(VisualProgressionRules.MachineStage(tier), tier, $"tier {tier}");
Equal(VisualProgressionRules.MachineStage(-1), 0, "negative tier");
Equal(VisualProgressionRules.MachineStage(int.MaxValue), 5, "high tier clamp");
Equal(VisualProgressionRules.MachineStage(int.MinValue), 0, "low tier clamp");
Equal(VisualProgressionRules.RoomStage(true, 8, 1, 0), 0, "fresh room");
Equal(VisualProgressionRules.RoomStage(true, 8, 5, 0), 1, "some slots unlocked");
Equal(VisualProgressionRules.RoomStage(true, 8, 1, 3), 2, "high tier machine");
Equal(VisualProgressionRules.RoomStage(true, 8, 8, 3), 4, "fully developed room");
Equal(VisualProgressionRules.RoomStage(true, 8, 1, 0), 0, "starting room after prestige");
Equal(VisualProgressionRules.RoomStage(false, 8, 0, 0), 0, "later room after prestige");
Equal(VisualProgressionRules.RoomStage(false, 8, 8, 5), 0, "locked room ignores stale progression");
Equal(VisualProgressionRules.RoomStage(true, 0, 0, 0), 0, "empty room");
Equal(VisualProgressionRules.RoomStage(true, 8, 99, 99), 4, "room clamp");
Equal(VisualProgressionRules.RoomStage(true, 8, -1, -1), 0, "invalid room values");
Equal(VisualProgressionRules.RoomStage(true, 1, 1, double.NaN), 0, "nonfinite tier");
Equal(VisualProgressionRules.PulseSeconds(0.01), 0.45, "fast cycle clamp");
Equal(VisualProgressionRules.PulseSeconds(100), 2.4, "slow cycle clamp");
Equal(VisualProgressionRules.PulseSeconds(double.NaN), 2.4, "invalid cycle");
Equal(VisualProgressionRules.PulseSeconds(0), 2.4, "uninitialized cycle");
Console.WriteLine($"Visual progression: {checks} checks passed.");
