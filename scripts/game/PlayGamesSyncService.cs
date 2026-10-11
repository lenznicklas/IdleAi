#nullable enable
using Godot;
using System;

namespace IdleAi;

/// <summary>Owns the one-second integration timer, with one registration per session.</summary>
public sealed partial class PlayGamesSyncService : Node
{
    private PlayGamesSyncWorkflow? _workflow;
    public event Action? TickCompleted;
    public bool Initialized => _workflow != null;
    public void Initialize(GooglePlayGamesAchievementService playGames, OnlineProfileService profile,
        GameState state, ProgressionService progression)
    {
        if (Initialized) return;
        Initialize(new PlayGamesSyncWorkflow(
            () => playGames.SyncAchievements(state, progression),
            () => playGames.SyncLeaderboards(state, progression),
            () => profile.SyncScores(progression.GetTotalLevel(), state.Prestige.PrestigeCount),
            exception => GD.PushWarning("Online sync failed: " + exception.Message)));
    }
    public void Initialize(PlayGamesSyncWorkflow workflow)
    {
        if (Initialized) return;
        _workflow = workflow;
        var timer = new Timer { Name = "PlayGamesAchievementSyncTimer", WaitTime = 1.0, OneShot = false, Autostart = true };
        timer.Timeout += Tick;
        AddChild(timer);
    }
    public void Tick()
    {
        _workflow?.Tick();
        TickCompleted?.Invoke();
    }
}
