#nullable enable
using Godot;
using System;

namespace IdleAi;

/// <summary>Deferred online composition, isolated from unrelated Game feature bootstraps.</summary>
public sealed partial class OnlineIntegrationCoordinator : Node
{
    private bool _initialized;
    private StatsLeaderboardButtonsController? _buttons;
    public event Action? TickCompleted;
    public void Initialize(Game root, GameState state, ProgressionService progression)
    {
        if (_initialized) return;
        _initialized = true;
        var aliases = new FirebaseAliasService { Name = "FirebaseAliasService" };
        AddChild(aliases);
        aliases.Initialize();
        var profile = new OnlineProfileService(aliases);
        var playGames = new GooglePlayGamesAchievementService();
        playGames.Initialize();
        var sync = new PlayGamesSyncService();
        AddChild(sync);
        sync.Initialize(playGames, profile, state, progression);
        var username = new UsernameSetupOverlayController(root, profile);
        username.Initialize();
        var leaderboard = new LeaderboardOverlayController(root, profile, username);
        leaderboard.Initialize();
        _buttons = new StatsLeaderboardButtonsController(root, state, progression, leaderboard);
        _buttons.Initialize();
        sync.TickCompleted += () =>
        {
            _buttons.Refresh();
            TickCompleted?.Invoke();
        };
    }
}
