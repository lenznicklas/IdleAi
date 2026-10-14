using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IdleAi;

internal static partial class Program
{
    private static async Task ExpectCode(Task task, string code)
    {
        try { await task; throw new Exception("Expected " + code); }
        catch (FirebaseRequestException e) { Check(e.Code == code, "Exact failure: " + code + ", actual " + e.Code); }
    }
    private static async Task DeviceScenarios()
    {
        var checks = new List<string>(); var codes = new List<string>();
        using var lifetime = new CancellationTokenSource();
        var gateway = new FirebasePlayGamesGateway(checks.Add, codes.Add, lifetime.Token,
            TimeSpan.FromSeconds(2), TimeSpan.FromMilliseconds(30));
        var pending = gateway.EnsureReadyAsync();
        Check(ReferenceEquals(pending, gateway.EnsureReadyAsync()) && checks.Count == 1, "Delayed native login shared");
        gateway.OnChecked(checks[0], false, "", 0);
        Check(!pending.IsCompleted && !gateway.IsReady, "Initial silent false waits for real interactive result");
        gateway.OnNativeAuthentication(true);
        gateway.OnChecked(checks[0], true, "old", 0);
        Check(!pending.IsCompleted, "Late native login callback ignored");
        gateway.OnChecked(checks[^1], true, "player", 0); await pending;
        Check(gateway.IsReady && gateway.PlayerId == "player", "Readiness requires tagged current player identity");
        var empty = gateway.RequestCodeAsync();
        gateway.OnCode(codes[^1], "", 0); await ExpectCode(empty, "GPGS_EMPTY_AUTH_CODE");
        var timeout = gateway.RequestCodeAsync(); string old = codes[^1];
        await ExpectCode(timeout, "GPGS_AUTH_CODE_TIMEOUT");
        var fresh = gateway.RequestCodeAsync(); string latest = codes[^1];
        gateway.OnCode(old, "stale-code", 0);
        Check(!fresh.IsCompleted && latest != old, "Timed-out code callback cannot satisfy replacement request");
        gateway.OnCode(latest, "first-fresh-code", 0);
        Check(await fresh == "first-fresh-code", "Replacement gets only its own native result");
        var reused = gateway.RequestCodeAsync(); gateway.OnCode(codes[^1], "first-fresh-code", 0);
        await ExpectCode(reused, "GPGS_REUSED_AUTH_CODE");
        var error = gateway.RequestCodeAsync(); gateway.OnCode(codes[^1], "", 10);
        await ExpectCode(error, "GPGS_API_10");
        Check(new FirebaseRequestException(0, "GPGS_API_10").Kind != FirebaseFailureKind.Network, "Native developer error is not offline");
        var actualNetwork = gateway.RequestCodeAsync(); gateway.OnCode(codes[^1], "", 7);
        await ExpectCode(actualNetwork, "GPGS_NETWORK_ERROR");
        Check(new FirebaseRequestException(0, "GPGS_NETWORK_ERROR").Kind == FirebaseFailureKind.Network, "Only native network status classified as connectivity");
        using var abort = new CancellationTokenSource(); var cancelled = gateway.RequestCodeAsync(abort.Token); string cancelledId = codes[^1];
        abort.Cancel(); try { await cancelled; throw new Exception("Expected cancellation"); } catch (OperationCanceledException) { }
        var afterCancel = gateway.RequestCodeAsync(); gateway.OnCode(cancelledId, "cancelled-code", 0);
        Check(!afterCancel.IsCompleted, "Cancelled code operation cannot corrupt next request");
        gateway.OnCode(codes[^1], "after-cancel", 0); await afterCancel;

        long now = 1000; int starts = 0, refreshes = 0, sends = 0;
        var serverLogin = new TaskCompletionSource<FirebaseCredentials>();
        var session = new FirebaseSession(() => { starts++; return serverLogin.Task; },
            _ => { refreshes++; return Task.FromResult(Credentials("refreshed", now + 3000)); }, () => now,
            () => gateway.EnsureReadyAsync(), () => gateway.PlayerId, () => gateway.IsReady);
        gateway.Resume();
        var username = session.RequestAsync(_ => { sends++; return Task.FromResult(new FirebaseHttpResponse(200, "ok")); }, true);
        var leaderboard = session.EnsureReadyAsync();
        Check(starts == 0 && sends == 0 && !session.IsFresh, "Startup requests wait for delayed GPGS proof");
        gateway.OnChecked(checks[^1], true, "player", 0);
        Check(starts == 1 && sends == 0 && session.Connecting, "Firebase exchange also awaited, no premature Firestore request");
        serverLogin.SetResult(Credentials()); await Task.WhenAll(username, leaderboard);
        Check(starts == 1 && sends == 1 && session.IsFresh, "Slow startup continues without duplicate exchanges");
        gateway.Resume(); now = 5000;
        var resume = session.EnsureReadyAsync(); var resumeOther = session.EnsureReadyAsync();
        Check(!session.IsFresh && !resume.IsCompleted && ReferenceEquals(resume, resumeOther), "Resume revalidates provider even with cached UID/token");
        gateway.OnChecked(checks[^1], true, "player", 0); await resume;
        Check(refreshes == 1 && session.IsFresh, "Same player refreshes expired Firebase token after resume");

        int playerSignIns = 0, playerRefreshes = 0;
        var playerSession = new FirebaseSession(() => { playerSignIns++; return Task.FromResult(new FirebaseCredentials("player-token", "r", gateway.PlayerId, now + 3000)); },
            _ => { playerRefreshes++; throw new Exception("New player must not refresh old identity"); }, () => now,
            () => gateway.EnsureReadyAsync(), () => gateway.PlayerId, () => gateway.IsReady);
        await playerSession.EnsureReadyAsync(); gateway.Resume();
        var changedPlayer = playerSession.EnsureReadyAsync();
        gateway.OnChecked(checks[^1], true, "different-player", 0); await changedPlayer;
        Check(playerSignIns == 2 && playerRefreshes == 0 && playerSession.Uid == "different-player", "Resume account change reauthenticates instead of refreshing old user's credentials");
        playerSession.Dispose();

        var changedLogin = new FirebaseSession(() => Task.FromResult(new FirebaseCredentials("old", "r", "old-uid", 9000)),
            _ => Task.FromResult(new FirebaseCredentials("new", "r", "new-uid", 9000)), () => now);
        sends = 0;
        await ExpectCode(changedLogin.RequestAsync(_ => { sends++; return Task.FromResult(new FirebaseHttpResponse(401, "")); }, false), "AUTH_USER_CHANGED");
        Check(sends == 1, "Account change stops replay of an old username/profile body");
        await ExpectCode(changedLogin.RequestAsync((_, _) => { sends++; return Task.FromResult(new FirebaseHttpResponse(200, "")); }, false, expectedUid: "old-uid"), "AUTH_USER_CHANGED");
        Check(sends == 1, "Expected UID checked before initial dispatch as well as retries");

        int phases = 0; var mixed = new FirebaseSession(() => Task.FromResult(Credentials()), _ => Task.FromResult(Credentials("new")), () => 1000);
        var response = await mixed.RequestAsync((_, refreshed) =>
        {
            phases++;
            if (phases < 3) Check(!refreshed, "Refresh flag remains false before auth retry"); else Check(refreshed, "Refresh flag included on retry diagnostics");
            return Task.FromResult(new FirebaseHttpResponse(phases == 1 ? 0 : phases == 2 ? 401 : 200, ""));
        }, true);
        Check(response.Status == 200 && phases == 3, "401 after network retry still refreshes exactly once");
        var missingUid = new FirebaseSession(() => Task.FromResult(new FirebaseCredentials("token", "r", "", 9000)), NoRefresh, () => now);
        await ExpectCode(missingUid.EnsureReadyAsync(), "INVALID_AUTH_RESPONSE");
        Check(!missingUid.IsFresh && missingUid.State == FirebaseConnectionState.AuthenticationFailure, "Missing UID cannot mean ready or offline");
        var forever = new TaskCompletionSource<FirebaseCredentials>();
        var bounded = new FirebaseSession(() => forever.Task, NoRefresh, () => now, connectionTimeout: TimeSpan.FromMilliseconds(30));
        await ExpectCode(bounded.EnsureReadyAsync(), "AUTHENTICATION_TIMEOUT");
        forever.SetResult(Credentials("late", 9000));
        Check(!bounded.IsFresh && bounded.State == FirebaseConnectionState.AuthenticationFailure, "Late auth completion cannot revive timed-out session");
        foreach (var http in new[] { 400, 401, 403, 429, 500 })
            Check(FirebaseRequestException.Parse(new(http, "{}")).Kind != FirebaseFailureKind.Network, "HTTP " + http + " is not offline");
        foreach (var code in new[] { "GPGS_LOGIN_TIMEOUT", "GPGS_AUTH_CODE_TIMEOUT", "INVALID_AUTH_RESPONSE", "AUTH_NOT_READY" })
            Check(new FirebaseRequestException(0, code).Kind != FirebaseFailureKind.Network, code + " is not offline");
        session.Dispose(); changedLogin.Dispose(); mixed.Dispose(); bounded.Dispose(); missingUid.Dispose();
    }
}
