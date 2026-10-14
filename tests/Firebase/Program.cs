using System;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using IdleAi;

internal static partial class Program
{
    private static int _checks;
    private static void Check(bool ok, string label) { if (!ok) throw new Exception(label); _checks++; }
    private static FirebaseCredentials Credentials(string token = "token", long expiry = 4000) => new(token, "refresh", "uid", expiry);
    private static Task<FirebaseCredentials> NoRefresh(string _) => throw new Exception("Unexpected refresh");
    public static async Task Main()
    {
        await Coordination();
        await DeviceScenarios();
        await RefreshAndRetry();
        await FailureAndReconnect();
        NamesAndPayloads();
        Errors();
        Console.WriteLine($"Firebase: {_checks} assertions passed.");
    }
    private static async Task Coordination()
    {
        var login = new TaskCompletionSource<FirebaseCredentials>(); int starts = 0, requests = 0;
        var session = new FirebaseSession(() => { starts++; return login.Task; }, NoRefresh, () => 1000);
        var states = new System.Collections.Generic.List<FirebaseConnectionState>(); session.StateChanged += states.Add;
        var leaderboard = session.RequestAsync(_ => { requests++; return Task.FromResult(new FirebaseHttpResponse(200, "scores")); }, true);
        var profile = session.EnsureReadyAsync(); var username = session.EnsureReadyAsync();
        Check(ReferenceEquals(profile, username), "All readiness callers share task");
        Check(starts == 1 && requests == 0 && !leaderboard.IsCompleted, "Early leaderboard waits without duplicate authentication");
        login.SetResult(Credentials()); await Task.WhenAll(leaderboard, profile, username);
        Check(starts == 1 && requests == 1 && session.State == FirebaseConnectionState.Ready, "Early leaderboard automatically continues");
        await session.EnsureReadyAsync(); Check(starts == 1, "Ready callers do not start auth again");
        Check(states.SequenceEqual(new[] { FirebaseConnectionState.Initializing, FirebaseConnectionState.Authenticating, FirebaseConnectionState.Ready }), "Explicit state progression");
        session.Dispose(); Check(!session.IsFresh, "Disposal clears credentials");
    }
    private static async Task RefreshAndRetry()
    {
        int starts = 0, refreshes = 0, sends = 0; long now = 1000;
        var refresh = new TaskCompletionSource<FirebaseCredentials>();
        var session = new FirebaseSession(() => { starts++; return Task.FromResult(Credentials()); }, _ => { refreshes++; return refresh.Task; }, () => now);
        await session.EnsureReadyAsync(); now = 5000;
        var first = session.EnsureReadyAsync(); var second = session.EnsureReadyAsync();
        Check(starts == 1 && refreshes == 1 && ReferenceEquals(first, second), "Expired session refreshes once for simultaneous callers");
        refresh.SetResult(Credentials("fresh", 9000)); await first;
        Check(session.Token == "fresh", "Replacement token is used");
        var retry = new FirebaseSession(() => Task.FromResult(Credentials()), _ => { refreshes++; return Task.FromResult(Credentials("new")); }, () => 1000);
        var response = await retry.RequestAsync(token => { sends++; return Task.FromResult(token == "token"
            ? new FirebaseHttpResponse(401, "{\"error\":{\"status\":\"UNAUTHENTICATED\"}}") : new FirebaseHttpResponse(200, "ok")); }, true);
        Check(response.Status == 200 && sends == 2 && retry.Token == "new", "401 refreshes and retries with replacement token");
        retry.Invalidate("token"); Check(retry.Token == "new", "Stale request cannot invalidate replacement token");
        sends = 0;
        response = await retry.RequestAsync(_ => { sends++; return Task.FromResult(new FirebaseHttpResponse(401, "")); }, true);
        Check(response.Status == 401 && sends == 2, "Repeated auth failure stops after one retry");
        Check(!retry.IsFresh && retry.State == FirebaseConnectionState.AuthenticationFailure, "Rejected replacement token is invalidated for reconnect");
        int reauth = 0;
        var fallback = new FirebaseSession(() => { reauth++; return Task.FromResult(Credentials()); },
            _ => throw new FirebaseRequestException(400, "INVALID_REFRESH_TOKEN"), () => now);
        now = 1000; await fallback.EnsureReadyAsync(); now = 5000;
        // Fresh sign-in produces a valid new expiry in this scenario.
        var recovering = new FirebaseSession(() => { reauth++; return Task.FromResult(Credentials("new", now + 3000)); },
            _ => throw new FirebaseRequestException(400, "INVALID_REFRESH_TOKEN"), () => now);
        await recovering.EnsureReadyAsync(); recovering.Invalidate("new"); await recovering.EnsureReadyAsync();
        Check(reauth == 3 && recovering.IsFresh, "Revoked refresh token falls back to fresh Play Games credentials");
    }
    private static async Task FailureAndReconnect()
    {
        int starts = 0; long now = 1000;
        var session = new FirebaseSession(() => { starts++; return starts == 1
            ? Task.FromException<FirebaseCredentials>(new FirebaseRequestException(0, "NETWORK"))
            : Task.FromResult(Credentials("reconnected", now + 3000)); }, NoRefresh, () => now);
        await session.EnsureReadyAsync();
        Check(starts == 2 && session.State == FirebaseConnectionState.Ready, "Transient initialization failure retries within the shared operation");
        now += 20; await session.EnsureReadyAsync();
        Check(starts == 2 && session.IsFresh && session.State == FirebaseConnectionState.Ready, "Reconnect succeeds without restart");
        int sends = 0;
        var response = await session.RequestAsync(_ => Task.FromResult(new FirebaseHttpResponse(++sends == 1 ? 0 : 200, "")), true);
        Check(sends == 2 && response.Status == 200, "Read retries temporary network failure once");
        sends = 0; await session.RequestAsync(_ => { sends++; return Task.FromResult(new FirebaseHttpResponse(0, "")); }, false);
        Check(sends == 1, "Ambiguous username commit is not blindly replayed");
        Check(session.State == FirebaseConnectionState.TemporaryFailure, "Network outage exposes a temporary failure state");
        now += 20; await session.EnsureReadyAsync();
        Check(session.State == FirebaseConnectionState.Ready, "Network reconnect restores readiness without unnecessary sign-in");
        sends = 0;
        await session.RequestAsync(_ => { sends++; return Task.FromResult(new FirebaseHttpResponse(400,
            "{\"error\":{\"status\":\"INVALID_ARGUMENT\"}}")); }, true);
        Check(sends == 1, "Malformed request 400 is surfaced without an auth/retry loop");
        starts = 0;
        var badConfig = new FirebaseSession(() => { starts++; throw new FirebaseRequestException(400, "OPERATION_NOT_ALLOWED"); }, NoRefresh, () => now);
        for (int i = 0; i < 2; i++) try { await badConfig.EnsureReadyAsync(); } catch (FirebaseRequestException) { }
        Check(starts == 1 && badConfig.State == FirebaseConnectionState.ConfigurationFailure, "Permanent configuration failure does not loop sign-in");
        var pending = new TaskCompletionSource<FirebaseCredentials>();
        var disposed = new FirebaseSession(() => pending.Task, NoRefresh, () => now); var wait = disposed.EnsureReadyAsync(); disposed.Dispose();
        try { await wait; throw new Exception("Expected disposal"); } catch (ObjectDisposedException) { }
        pending.SetResult(Credentials("late", now + 3000)); Check(!disposed.IsFresh, "Late completion cannot revive disposed service");
    }
    private static void NamesAndPayloads()
    {
        Check(UsernamePolicy.TryNormalize("  Player_12 \t", out var name, out var lower, out _) && name == "Player_12" && lower == "player_12", "Trim and case normalization");
        foreach (var invalid in new string?[] { null, "", "  ", "ab", new string('a', 17), "1Player", "Player Name", "Name/Id", "Name\"x", "Name&x", "Náme", "Na\u0301me", "Name\nX", "A\uD800b", "Admin42" })
            Check(!UsernamePolicy.TryNormalize(invalid, out _, out _, out _), "Reject malformed/restricted name");
        Check(UsernamePolicy.TryNormalize("AdminOld", out _, out _, out _, existing: true), "Existing reserved names retained");
        var original = CultureInfo.CurrentCulture;
        try
        {
            foreach (string locale in new[] { "en-US", "tr-TR", "ar-SA", "th-TH" })
            {
                CultureInfo.CurrentCulture = new CultureInfo(locale);
                string body = FirestoreProfileRequests.CreateUsernameCommit("project", "uid", "id\"with\\escapes", " Player_12 ", new DateTime(2026, 10, 14, 12, 30, 0, DateTimeKind.Utc));
                using var doc = JsonDocument.Parse(body); var writes = doc.RootElement.GetProperty("writes");
                Check(writes.GetArrayLength() == 2 && writes.EnumerateArray().All(w => !w.GetProperty("currentDocument").GetProperty("exists").GetBoolean()), "Atomic uniqueness preconditions, " + locale);
                var reservation = writes[0].GetProperty("update");
                Check(reservation.GetProperty("name").GetString() == "projects/project/databases/(default)/documents/usernames/player_12", "Uniqueness document uses canonical lowercase, " + locale);
                var fields = writes[1].GetProperty("update").GetProperty("fields");
                Check(fields.GetProperty("username").GetProperty("stringValue").GetString() == "Player_12"
                    && fields.GetProperty("playGamesPlayerId").GetProperty("stringValue").GetString() == "id\"with\\escapes", "Firestore strings serialize and escape correctly, " + locale);
                Check(fields.GetProperty("createdAt").GetProperty("timestampValue").GetString() == "2026-10-14T12:30:00.0000000Z", "Invariant timestamp, " + locale);
            }
        }
        finally { CultureInfo.CurrentCulture = original; }
    }
    private static void Errors()
    {
        var response = new FirebaseHttpResponse(400, "{\"error\":{\"code\":400,\"status\":\"INVALID_ARGUMENT\",\"message\":\"Invalid field timestampValue\",\"details\":[{\"reason\":\"BAD_TIMESTAMP\"}]}}");
        var error = FirebaseRequestException.Parse(response);
        Check(error.Status == 400 && error.Code == "INVALID_ARGUMENT" && error.Detail.Contains("timestampValue"), "HTTP 400 code and message parsed");
        Check(FirebaseRequestException.SafeDiagnostic(response).Contains("BAD_TIMESTAMP"), "DEBUG diagnostic preserves Firebase error details");
        Check(FirebaseRequestException.Parse(new(400, "{\"error\":{\"message\":\"INVALID_IDP_RESPONSE : bad credential\"}}")).Code == "INVALID_IDP_RESPONSE", "Auth-style 400 parsed");
        foreach (string body in new[] { "", "<html>offline</html>", "[]", "null", "{\"error\":{\"message\":123}}" })
            Check(FirebaseRequestException.Parse(new(0, body)).Status == 0, "Malformed error body tolerated");
        string secret = "super-secret-value";
        string diagnostic = FirebaseRequestException.SafeDiagnostic(new(400, JsonSerializer.Serialize(new { error = new { message = "code=" + secret + "&providerId=playgames.google.com" } })), secret);
        Check(!diagnostic.Contains(secret) && diagnostic.Contains("playgames.google.com"), "Credentials redacted, useful provider detail retained");
        Check(FirebaseRequestException.SafeDiagnostic(new(400, "{\"error\":{\"message\":\"INVALID_CREDENTIAL_OR_PROVIDER_ID\"}}")).Contains("INVALID_CREDENTIAL_OR_PROVIDER_ID"), "Long Firebase error codes are preserved");
        Check(!FirebaseRequestException.IsAuthenticationFailure(new(403, "{\"error\":{\"status\":\"PERMISSION_DENIED\"}}")), "Rules denial is not misdiagnosed as token expiry");
    }
}
