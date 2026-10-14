#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IdleAi;

/// <summary>Stable UI boundary for username and custom leaderboard operations.</summary>
public sealed class OnlineProfileService
{
    private readonly FirebaseAliasService _aliases;
    public OnlineProfileService(FirebaseAliasService aliases) { _aliases = aliases; }
    public Task EnsureReadyAsync() => _aliases.EnsureReadyAsync();
    public FirebaseConnectionState ConnectionState => _aliases.ConnectionState;
    public string ConnectionMessage => _aliases.ConnectionMessage;
    public event Action<FirebaseConnectionState>? ConnectionStateChanged
    { add => _aliases.ConnectionStateChanged += value; remove => _aliases.ConnectionStateChanged -= value; }
    public bool HasUsername => _aliases.HasUsername;
    public string CurrentUsername => _aliases.CurrentUsername;
    public bool IsFirebaseAuthenticated => _aliases.IsFirebaseAuthenticated;
    public event Action? UsernameRequired { add => _aliases.UsernameRequired += value; remove => _aliases.UsernameRequired -= value; }
    public event Action<string>? UsernameChanged { add => _aliases.UsernameChanged += value; remove => _aliases.UsernameChanged -= value; }
    public event Action<bool, string>? UsernameSaveFinished { add => _aliases.UsernameSaveFinished += value; remove => _aliases.UsernameSaveFinished -= value; }
    public event Action<string>? StatusMessage { add => _aliases.StatusMessage += value; remove => _aliases.StatusMessage -= value; }
    public event Action? AliasDataChanged { add => _aliases.AliasDataChanged += value; remove => _aliases.AliasDataChanged -= value; }
    public event Action<IdleAiLeaderboardKind, IReadOnlyList<IdleAiLeaderboardEntry>, IdleAiLeaderboardEntry?>? InGameLeaderboardLoaded
    { add => _aliases.InGameLeaderboardLoaded += value; remove => _aliases.InGameLeaderboardLoaded -= value; }
    public event Action<IdleAiLeaderboardKind, string>? InGameLeaderboardLoadFailed
    { add => _aliases.InGameLeaderboardLoadFailed += value; remove => _aliases.InGameLeaderboardLoadFailed -= value; }
    public static bool ValidateUsername(string username, out string error) => FirebaseAliasService.ValidateUsername(username, out error);
    public void SetUsername(string username) => _aliases.SetUsername(username);
    public bool RequestInGameLeaderboard(IdleAiLeaderboardKind kind) => _aliases.RequestInGameLeaderboard(kind);
    public void SyncScores(long totalLevel, long prestigeCount) => _aliases.SyncInGameLeaderboardScores(totalLevel, prestigeCount);
}
