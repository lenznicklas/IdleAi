using System;

namespace IdleAi;

/// <summary>One evolving, automatic badge. Does not grant or spend currency.</summary>
public static class LevelBadgeProgress
{
    public const int LevelInterval = 150;
    public const string IconPath = "res://assets/ui/level_badge.png";
    public static void Update(GameState state) => state.Archive.HighestLevelBadgeTier = Math.Max(
        state.Archive.HighestLevelBadgeTier, Math.Max(0, ProgressionService.CalculateTotalLevel(state)) / LevelInterval);
}
