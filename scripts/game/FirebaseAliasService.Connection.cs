#nullable enable
using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace IdleAi;

public sealed partial class FirebaseAliasService
{
    private FirebaseSession? _session;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly Dictionary<HttpRequest, TaskCompletionSource<FirebaseHttpResponse>> _requests = [];
    private GodotObject? _nativeAuth;
    private FirebasePlayGamesGateway? _gateway;
    private bool _applicationPaused;
    private Task? _profileLoadTask;
    private bool _reconnecting;
    private bool _exiting;
    private string _profileUid = "";
    private FirebaseSession Session
    {
        get
        {
            if (_session != null) return _session;
            _session = new FirebaseSession(SignInAsync, RefreshAsync, CurrentUnixTime,
                () => EnsureProviderAsync(), () => _gateway?.PlayerId ?? "", () => _gateway?.IsReady == true,
                signInWithCancellation: SignInAsync, refreshWithCancellation: RefreshAsync, ensureProviderWithCancellation: EnsureProviderAsync);
            _session.Diagnostic += message => DebugLog("Firebase", message);
            _session.StateChanged += state =>
            {
                if (state == FirebaseConnectionState.Ready && _profileUid != _session.Uid)
                {
                    _profileUid = _session.Uid;
                    CurrentUsername = ""; _profileKnownMissing = false;
                    _leaderboardLastSyncedTotalLevel = long.MinValue;
                    _leaderboardLastSyncedPrestiges = long.MinValue;
                }
                DebugLog("Firebase", "state=" + state);
                ConnectionStateChanged?.Invoke(state);
                StatusMessage?.Invoke(ConnectionMessage);
            };
            return _session;
        }
    }
    public FirebaseConnectionState ConnectionState => Session.State;
    public event Action<FirebaseConnectionState>? ConnectionStateChanged;
    public string ConnectionMessage => ConnectionState switch
    {
        FirebaseConnectionState.Ready => "Connected",
        FirebaseConnectionState.Authenticating => "Connecting…",
        FirebaseConnectionState.ConfigurationFailure => "Online services unavailable",
        FirebaseConnectionState.TemporaryFailure => "Unable to reach online services. Retrying…",
        FirebaseConnectionState.AuthenticationFailure => Session.LastFailure?.Code.StartsWith("GPGS_", StringComparison.Ordinal) == true
            ? "Could not connect to Google Play Games. Please try again." : "Could not authenticate online services. Please try again.",
        FirebaseConnectionState.ServiceFailure => "Online services are busy. Retrying…",
        _ => "Connecting…"
    };

    public async Task EnsureReadyAsync()
    {
        Initialize();
        await Session.EnsureReadyAsync().WaitAsync(_lifetime.Token);
    }

    private async void RequestFirebaseSignIn()
    {
        if (_reconnecting || _applicationPaused || !IsInsideTree() || OS.GetName() != "Android") return;
        _reconnecting = true;
        try
        {
            await EnsureReadyAsync();
            await LoadCurrentProfileAsync();
            SyncInGameLeaderboardScores(_leaderboardLocalTotalLevel, _leaderboardLocalPrestiges);
        }
        catch (Exception e) { LogFailure("Reconnect/profile", e); if (!_exiting) StatusMessage?.Invoke(PlayerError(e)); }
        finally { _reconnecting = false; }
    }

    private void InitializeNativeAuth()
    {
        if (!Engine.HasSingleton("IdleAiFirebaseAuth")) return;
        _nativeAuth = Engine.GetSingleton("IdleAiFirebaseAuth");
        _gateway = new FirebasePlayGamesGateway(
            id => _nativeAuth.Call("checkSignIn", id),
            id => _nativeAuth.Call("requestServerAuthCode", id, FirebaseConfig.PlayGamesWebClientId), _lifetime.Token);
        _gateway.Diagnostic += message => DebugLog("GPGS", message);
        _nativeAuth.Connect("signInChecked", Callable.From<string, bool, string, int>(OnSignInChecked));
        _nativeAuth.Connect("serverAuthCodeCompleted", Callable.From<string, string, int>(OnServerAuthCodeCompleted));
    }
    private async Task EnsureProviderAsync(CancellationToken cancellation = default)
    {
        if (_playGames == null) throw new FirebaseRequestException(400, "UNSUPPORTED_PLATFORM");
        if (_gateway == null) throw new FirebaseRequestException(400, "NATIVE_AUTH_BRIDGE_MISSING");
        await _gateway.EnsureReadyAsync(cancellation);
        _currentPlayGamesPlayerId = _gateway.PlayerId;
    }
    private void OnSignInChecked(string id, bool authenticated, string playerId, int status) => _gateway?.OnChecked(id, authenticated, playerId, status);
    private void OnServerAuthCodeCompleted(string id, string code, int status) => _gateway?.OnCode(id, code, status);
    private Task<FirebaseCredentials> SignInAsync() => SignInAsync(_lifetime.Token);
    private async Task<FirebaseCredentials> SignInAsync(CancellationToken cancellation)
    {
        if (_gateway == null || !_gateway.IsReady) throw new FirebaseRequestException(401, "GPGS_NOT_READY");
        string code = await _gateway.RequestCodeAsync(cancellation);
        DebugLog("Firebase", "Exchanging Play Games credentials");
        string body = JsonSerializer.Serialize(new
        {
            postBody = "code=" + Uri.EscapeDataString(code) + "&providerId=" + PlayGamesProviderId,
            requestUri = FirebaseConfig.AuthRequestUri, returnIdpCredential = true, returnSecureToken = true
        });
        var response = await SendRawHttpAsync("SignInWithPlayGames", false, "https://identitytoolkit.googleapis.com/v1/accounts:signInWithIdp?key="
            + Uri.EscapeDataString(FirebaseConfig.WebApiKey), ["Content-Type: application/json"], HttpClient.Method.Post, body, cancellation, code);
        if (response.Status is < 200 or >= 300) throw FirebaseRequestException.Parse(response);
        var credentials = ParseCredentials(response.Body, refreshed: false);
        DebugLog("Firebase", "Authentication successful");
        return credentials;
    }

    private Task<FirebaseCredentials> RefreshAsync(string refreshToken) => RefreshAsync(refreshToken, _lifetime.Token);
    private async Task<FirebaseCredentials> RefreshAsync(string refreshToken, CancellationToken cancellation)
    {
        DebugLog("Firebase", "Refreshing session");
        var response = await SendRawHttpAsync("RefreshFirebaseToken", true, "https://securetoken.googleapis.com/v1/token?key="
            + Uri.EscapeDataString(FirebaseConfig.WebApiKey), ["Content-Type: application/x-www-form-urlencoded"],
            HttpClient.Method.Post, "grant_type=refresh_token&refresh_token=" + Uri.EscapeDataString(refreshToken), cancellation, refreshToken);
        if (response.Status is < 200 or >= 300) throw FirebaseRequestException.Parse(response);
        return ParseCredentials(response.Body, refreshed: true);
    }

    private static FirebaseCredentials ParseCredentials(string body, bool refreshed)
    {
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        string token = GetJsonString(root, refreshed ? "id_token" : "idToken");
        string refreshToken = GetJsonString(root, refreshed ? "refresh_token" : "refreshToken");
        string uid = GetJsonString(root, refreshed ? "user_id" : "localId");
        if (!long.TryParse(GetJsonString(root, refreshed ? "expires_in" : "expiresIn"), NumberStyles.None,
                CultureInfo.InvariantCulture, out long seconds) || seconds is <= 0 or > 86400)
            throw new FirebaseRequestException(0, "INVALID_AUTH_RESPONSE");
        return new(token, refreshToken, uid, CurrentUnixTime() + Math.Max(1, seconds - 60));
    }

    private void OnPlayGamesAuthenticated(bool authenticated)
    {
        DebugLog("GPGS", "Native login notification; authenticated=" + authenticated);
        _gateway?.OnNativeAuthentication(authenticated);
        if (authenticated) RequestFirebaseSignIn();
    }
    private async Task EnsurePlayerLoadedAsync()
    {
        if (_gateway != null) { await EnsureProviderAsync(); return; }
        if (string.IsNullOrWhiteSpace(_currentPlayGamesPlayerId)) throw new FirebaseRequestException(401, "GPGS_PLAYER_MISSING");
    }

    private async void TryLoadCurrentProfile()
    {
        if (!Session.IsFresh || HasUsername || _profileKnownMissing) return;
        try { await LoadCurrentProfileAsync(); }
        catch (Exception e) { LogFailure("LoadProfile", e); }
    }

    private Task LoadCurrentProfileAsync()
    {
        if (HasUsername || _profileKnownMissing) return Task.CompletedTask;
        if (_profileLoadTask != null) return _profileLoadTask;
        var source = new TaskCompletionSource<bool>();
        _profileLoadTask = source.Task;
        _ = LoadProfileCoreAsync(source);
        return source.Task;
    }

    private async Task LoadProfileCoreAsync(TaskCompletionSource<bool> source)
    {
        try
        {
            DebugLog("Profile", "Loading profile");
            for (int attempt = 0; ; attempt++)
            {
            await EnsureReadyAsync();
            string profileUid = _firebaseUid;
            FirebaseHttpResponse response;
            try
            {
            response = await Session.RequestAsync((token, refreshed) => SendRawHttpAsync("LoadProfile", refreshed, FirestoreDocumentsBaseUrl + "/profiles/"
                + Uri.EscapeDataString(profileUid), ["Authorization: Bearer " + token, "Content-Type: application/json"],
                HttpClient.Method.Get, "", _lifetime.Token), retryNetwork: true, expectedUid: profileUid);
            }
            catch (FirebaseRequestException e) when (e.Code == "AUTH_USER_CHANGED" && attempt == 0) { continue; }
            if (_exiting) throw new OperationCanceledException("Profile service disposed.");
            if (profileUid != _firebaseUid)
            {
                if (attempt == 0) continue;
                throw new FirebaseRequestException(409, "AUTH_USER_CHANGED");
            }
            if (response.Status == 404)
            {
                _profileKnownMissing = true;
                UsernameRequired?.Invoke();
            }
            else
            {
                if (response.Status is < 200 or >= 300) throw FirebaseRequestException.Parse(response);
                using var doc = JsonDocument.Parse(response.Body);
                var fields = doc.RootElement.GetProperty("fields");
                string raw = GetFirestoreString(fields, "username");
                if (!UsernamePolicy.TryNormalize(raw, out string username, out _, out _, existing: true))
                    throw new FirebaseRequestException(0, "INVALID_PROFILE_USERNAME");
                CurrentUsername = username;
                string playerId = GetFirestoreString(fields, "playGamesPlayerId");
                if (!string.IsNullOrEmpty(playerId)) _aliasByPlayGamesPlayerId[playerId] = username;
                if (!string.IsNullOrEmpty(_currentPlayGamesPlayerId)) _aliasByPlayGamesPlayerId[_currentPlayGamesPlayerId] = username;
                UsernameChanged?.Invoke(username); AliasDataChanged?.Invoke();
                DebugLog("Profile", "Profile loaded");
            }
            break;
            }
            source.TrySetResult(true);
        }
        catch (Exception e) { source.TrySetException(e); }
        finally { _profileLoadTask = null; }
    }

#if DEBUG
    // Test transport substitutes responses; production always uses independently owned HTTPRequest nodes.
    internal Func<string, HttpClient.Method, string, Task<FirebaseHttpResponse>>? HttpTransportOverride { get; set; }
    internal Action<string>? HttpDiagnosticObserver { get; set; }
#endif
    private async Task<FirebaseHttpResponse> SendRawHttpAsync(string operation, bool refreshed, string url, string[] headers, HttpClient.Method method, string body, CancellationToken cancellation, params string[] secrets)
    {
        if (_exiting || !IsInsideTree()) throw new ObjectDisposedException(nameof(FirebaseAliasService));
        bool firestore = new Uri(url).Host == "firestore.googleapis.com";
        bool uidPresent = !string.IsNullOrWhiteSpace(_firebaseUid), tokenPresent = !string.IsNullOrWhiteSpace(_firebaseIdToken), ready = Session.IsFresh;
        if (firestore && !ready)
        {
            DebugLog("Firestore", $"operation={operation}; blockedBeforeReady=true; uidPresent={uidPresent}; tokenPresent={tokenPresent}; refreshAttempted={refreshed}");
            throw new FirebaseRequestException(401, "AUTH_NOT_READY");
        }
#if DEBUG
        if (HttpTransportOverride != null)
        {
            var testResponse = await HttpTransportOverride(url, method, body);
            LogHttpFailure(operation, refreshed, method, url, headers, testResponse, uidPresent, tokenPresent, ready, secrets);
            return testResponse;
        }
#endif
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token, cancellation);
        var source = new TaskCompletionSource<FirebaseHttpResponse>();
        var request = new HttpRequest { UseThreads = true, Timeout = 20, ProcessMode = ProcessModeEnum.Always };
        AddChild(request); _requests.Add(request, source);
        void Finish(FirebaseHttpResponse response) { if (_requests.Remove(request)) source.TrySetResult(response); }
        request.RequestCompleted += (long result, long status, string[] _, byte[] responseBody) =>
        {
            if (result != (long)HttpRequest.Result.Success)
                DebugLog("Firebase HTTP", $"operation={operation}; transportResult={(HttpRequest.Result)result}");
            Finish(result == (long)HttpRequest.Result.Success ? new(status, Encoding.UTF8.GetString(responseBody))
                : new(0, "{\"error\":{\"status\":\"NETWORK_ERROR\"}}"));
        };
        FirebaseHttpResponse response;
        try
        {
            var error = request.Request(url, headers, method, body);
            if (error != Error.Ok)
            {
                DebugLog("Firebase HTTP", $"operation={operation}; requestStartError={error}");
                Finish(new(0, "{\"error\":{\"status\":\"REQUEST_START_FAILED\"}}"));
            }
            try { response = await source.Task.WaitAsync(TimeSpan.FromSeconds(25), linked.Token); }
            catch (TimeoutException) { response = new(0, "{\"error\":{\"status\":\"NETWORK_ERROR\",\"message\":\"HTTP deadline exceeded\"}}"); }
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception e)
        {
            response = new(0, JsonSerializer.Serialize(new { error = new { status = "HTTP_REQUEST_EXCEPTION", message = e.GetType().Name } }));
        }
        finally
        {
            _requests.Remove(request);
            if (GodotObject.IsInstanceValid(request)) { request.CancelRequest(); if (!request.IsQueuedForDeletion()) request.QueueFree(); }
        }
        LogHttpFailure(operation, refreshed, method, url, headers, response, uidPresent, tokenPresent, ready, secrets);
        return response;
    }

    [System.Diagnostics.Conditional("DEBUG")]
    private void LogHttpFailure(string operation, bool refreshed, HttpClient.Method method, string url, string[] headers, FirebaseHttpResponse response,
        bool uidPresent, bool tokenPresent, bool ready, string[] secrets)
    {
        bool firestore = new Uri(url).Host == "firestore.googleapis.com";
        if (response.Status is < 200 or >= 300)
        {
            var endpoint = new Uri(url);
            string safeEndpoint = endpoint.GetLeftPart(UriPartial.Path) + (firestore ? endpoint.Query : "");
            var sensitive = new List<string>(secrets) { _firebaseIdToken, FirebaseConfig.WebApiKey };
            foreach (var header in headers)
                if (header.StartsWith("Authorization: Bearer ", StringComparison.Ordinal)) sensitive.Add(header[22..]);
            string diagnostic = $"operation={operation}; method={method}; endpoint={safeEndpoint}; HTTP={response.Status}; uidPresent={uidPresent}; tokenPresent={tokenPresent}; readyAtDispatch={ready}; attemptedBeforeReady={!ready}; requiresReady={firestore}; refreshAttempted={refreshed}; "
                + FirebaseRequestException.SafeDiagnostic(response, sensitive.ToArray());
#if DEBUG
            HttpDiagnosticObserver?.Invoke(diagnostic);
#endif
            DebugLog("Firebase HTTP", diagnostic);
        }
    }

    public override void _Notification(int what)
    {
        if (what == MainLoop.NotificationApplicationPaused) { _applicationPaused = true; return; }
        if (what != MainLoop.NotificationApplicationResumed || _exiting) return;
        _applicationPaused = false;
        // Returning from GPGS consent/sign-in must join the pending attempt, never invalidate its callback.
        if (!Session.Connecting || _gateway?.IsAcquiringNativeCredentials == false) _gateway?.Resume();
        DebugLog("Firebase", "Resume; joiningPendingAuth=" + Session.Connecting);
        RequestFirebaseSignIn();
    }

    public override void _ExitTree()
    {
        _exiting = true;
        _lifetime.Cancel();
        _session?.Dispose();
        foreach (var pair in _requests.ToArray())
        { pair.Key.CancelRequest(); pair.Value.TrySetException(new ObjectDisposedException(nameof(FirebaseAliasService))); }
        _requests.Clear();
        // Native singleton outlives this scene. Disconnect only this service's own callbacks.
        if (_playGames != null)
        {
            Disconnect("userAuthenticated", Callable.From<bool>(OnPlayGamesAuthenticated));
            Disconnect("currentPlayerLoaded", Callable.From<string>(OnCurrentPlayerLoaded));
            Disconnect("topScoresLoaded", Callable.From<string, string>(OnRawTopScoresLoaded));
            Disconnect("scoreLoaded", Callable.From<string, string>(OnRawPlayerScoreLoaded));
        }
        if (_nativeAuth != null)
        {
            var check = Callable.From<string, bool, string, int>(OnSignInChecked);
            var code = Callable.From<string, string, int>(OnServerAuthCodeCompleted);
            if (_nativeAuth.IsConnected("signInChecked", check)) _nativeAuth.Disconnect("signInChecked", check);
            if (_nativeAuth.IsConnected("serverAuthCodeCompleted", code)) _nativeAuth.Disconnect("serverAuthCodeCompleted", code);
        }
        // Pending continuations may still observe this token; cancellation is enough here.
    }
    private void Disconnect(string signal, Callable callback)
    { if (_playGames!.IsConnected(signal, callback)) _playGames.Disconnect(signal, callback); }
    private static void LogFailure(string operation, Exception e) => DebugLog("Firebase", "operation=" + operation + "; "
        + (e is FirebaseRequestException f ? "HTTP=" + f.Status + "; code=" + f.Code + "; kind=" + f.Kind : "exception=" + e.GetType().Name));
    private static string PlayerError(Exception e) => e is FirebaseRequestException f ? f.PlayerMessage
        : e is TimeoutException ? "Connection timed out. Please try again." : "Online services unavailable. Please try again.";
    [System.Diagnostics.Conditional("DEBUG")]
    private static void DebugLog(string area, string message) => GD.Print($"[{area}] {message}");
}
