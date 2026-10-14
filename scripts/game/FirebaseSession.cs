#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IdleAi;

public enum FirebaseConnectionState { NotStarted, Initializing, Authenticating, Ready, TemporaryFailure, ConfigurationFailure, AuthenticationFailure, ServiceFailure }
public sealed class FirebaseCredentials(string token, string refreshToken, string uid, long expiresAt)
{
    public string Token { get; } = token;
    public string RefreshToken { get; } = refreshToken;
    public string Uid { get; } = uid;
    public long ExpiresAt { get; } = expiresAt;
}
public readonly record struct FirebaseHttpResponse(long Status, string Body);

/// <summary>Main-context single-flight coordination, including prerequisite proof, retries and refresh.</summary>
public sealed class FirebaseSession(Func<Task<FirebaseCredentials>> signIn, Func<string, Task<FirebaseCredentials>> refresh,
    Func<long> now, Func<Task>? ensureProvider = null, Func<string>? providerIdentity = null,
    Func<bool>? providerReady = null, TimeSpan? connectionTimeout = null,
    Func<CancellationToken, Task<FirebaseCredentials>>? signInWithCancellation = null,
    Func<string, CancellationToken, Task<FirebaseCredentials>>? refreshWithCancellation = null,
    Func<CancellationToken, Task>? ensureProviderWithCancellation = null)
{
    private TaskCompletionSource<bool>? _connecting;
    private FirebaseCredentials? _credentials;
    private string _credentialProvider = "";
    private long _retryAfter;
    private int _failures;
    private readonly CancellationTokenSource _lifetime = new();
    private bool _disposed;
    private FirebaseConnectionState _state;
    public FirebaseConnectionState State => _state == FirebaseConnectionState.Ready && !IsFresh ? FirebaseConnectionState.NotStarted : _state;
    public FirebaseRequestException? LastFailure { get; private set; }
    public event Action<FirebaseConnectionState>? StateChanged;
    public event Action<string>? Diagnostic;
    public string Token => _credentials?.Token ?? "";
    public string Uid => _credentials?.Uid ?? "";
    private bool CredentialsFresh => !string.IsNullOrWhiteSpace(Token) && !string.IsNullOrWhiteSpace(Uid) && now() < _credentials!.ExpiresAt;
    public bool Connecting => _connecting != null;
    public bool IsFresh => !_disposed && CredentialsFresh && (providerReady?.Invoke() ?? true);
    public Task EnsureReadyAsync()
    {
        if (_disposed) return Task.FromException(new ObjectDisposedException(nameof(FirebaseSession)));
        if (_connecting != null) return _connecting.Task;
        if (IsFresh && now() >= _retryAfter) { SetState(FirebaseConnectionState.Ready); return Task.CompletedTask; }
        if (_state == FirebaseConnectionState.ConfigurationFailure) return Task.FromException(LastFailure ?? new FirebaseRequestException(400, "CONFIGURATION_FAILURE"));
        var source = new TaskCompletionSource<bool>(); _connecting = source;
        _ = ConnectAsync(source);
        return source.Task;
    }
    private async Task ConnectAsync(TaskCompletionSource<bool> source)
    {
        try
        {
            SetState(FirebaseConnectionState.Initializing);
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
            deadline.CancelAfter(connectionTimeout ?? TimeSpan.FromSeconds(100));
            var result = await AcquireAsync(deadline.Token).WaitAsync(deadline.Token);
            if (_disposed || _connecting != source) return;
            _credentials = result.Credentials; _credentialProvider = result.Provider;
            _failures = 0; _retryAfter = 0; LastFailure = null;
            _connecting = null; SetState(FirebaseConnectionState.Ready); source.TrySetResult(true);
        }
        catch (Exception e)
        {
            if (_connecting != source) return;
            var failure = e as FirebaseRequestException ?? new FirebaseRequestException(0,
                e is TimeoutException || e is OperationCanceledException && !_disposed ? "AUTHENTICATION_TIMEOUT" : e is OperationCanceledException ? "CANCELLED" : "AUTHENTICATION_FAILED");
            LastFailure = failure; _failures++;
            _retryAfter = now() + Math.Min(15, 1 << Math.Min(4, _failures));
            _connecting = null;
            Diagnostic?.Invoke($"Initialization failed; code={failure.Code}; HTTP={failure.Status}; kind={failure.Kind}");
            if (!_disposed) SetState(FailureState(failure));
            source.TrySetException(failure);
        }
    }
    private async Task<(FirebaseCredentials Credentials, string Provider)> AcquireAsync(CancellationToken cancellation)
    {
        if (_retryAfter > now()) await Task.Delay(TimeSpan.FromSeconds(_retryAfter - now()), cancellation);
        for (int attempt = 0; ; attempt++)
        {
            cancellation.ThrowIfCancellationRequested();
            SetState(FirebaseConnectionState.Initializing);
            try
            {
                if (ensureProviderWithCancellation != null) await ensureProviderWithCancellation(cancellation).WaitAsync(cancellation);
                else if (ensureProvider != null) await ensureProvider().WaitAsync(cancellation);
                cancellation.ThrowIfCancellationRequested();
                string identity = providerIdentity?.Invoke() ?? "";
                var current = identity == _credentialProvider ? _credentials : null;
                SetState(FirebaseConnectionState.Authenticating);
                FirebaseCredentials credentials;
                if (current != null && CredentialsFresh) credentials = current;
                else if (!string.IsNullOrEmpty(current?.RefreshToken))
                {
                    try { credentials = await (refreshWithCancellation?.Invoke(current.RefreshToken, cancellation) ?? refresh(current.RefreshToken)).WaitAsync(cancellation); }
                    catch (FirebaseRequestException e) when (e.CanReauthenticate)
                    { Diagnostic?.Invoke("Refresh rejected; obtaining a new Play Games code"); credentials = await (signInWithCancellation?.Invoke(cancellation) ?? signIn()).WaitAsync(cancellation); }
                }
                else credentials = await (signInWithCancellation?.Invoke(cancellation) ?? signIn()).WaitAsync(cancellation);
                if (string.IsNullOrWhiteSpace(credentials.Token) || string.IsNullOrWhiteSpace(credentials.Uid) || credentials.ExpiresAt <= now())
                    throw new FirebaseRequestException(0, "INVALID_AUTH_RESPONSE");
                cancellation.ThrowIfCancellationRequested();
                if (!(providerReady?.Invoke() ?? true) || (providerIdentity?.Invoke() ?? "") != identity)
                    throw new FirebaseRequestException(401, "GPGS_PROOF_CHANGED");
                return (credentials, identity);
            }
            catch (FirebaseRequestException e) when (attempt == 0 && e.RetryInitialization)
            {
                Diagnostic?.Invoke("Initialization retry; code=" + e.Code);
                await Task.Delay(500, cancellation);
            }
        }
    }
    public void Invalidate(string rejectedToken)
    {
        if (_credentials == null || Token != rejectedToken) return;
        _credentials = new("", _credentials.RefreshToken, _credentials.Uid, 0);
        SetState(FirebaseConnectionState.Initializing);
    }
    public Task<FirebaseHttpResponse> RequestAsync(Func<string, Task<FirebaseHttpResponse>> send, bool retryNetwork) =>
        RequestAsync((token, _) => send(token), retryNetwork);
    public async Task<FirebaseHttpResponse> RequestAsync(Func<string, bool, Task<FirebaseHttpResponse>> send, bool retryNetwork, string? expectedUid = null)
    {
        await EnsureReadyAsync();
        if (expectedUid != null && Uid != expectedUid) throw new FirebaseRequestException(409, "AUTH_USER_CHANGED");
        string uid = Uid;
        bool authRetried = false, networkRetried = false;
        while (true)
        {
            string usedToken = Token;
            var response = await send(usedToken, authRetried);
            if (FirebaseRequestException.IsAuthenticationFailure(response) && !authRetried)
            {
                authRetried = true; Diagnostic?.Invoke("Request rejected credentials; refreshing once");
                Invalidate(usedToken); await EnsureReadyAsync();
                if (Uid != uid) throw new FirebaseRequestException(409, "AUTH_USER_CHANGED");
                continue;
            }
            if (retryNetwork && !networkRetried && FirebaseRequestException.Parse(response).Kind is FirebaseFailureKind.Network or FirebaseFailureKind.Service)
            {
                networkRetried = true; await Task.Delay(500, _lifetime.Token);
                await EnsureReadyAsync();
                if (Uid != uid) throw new FirebaseRequestException(409, "AUTH_USER_CHANGED");
                continue;
            }
            if (FirebaseRequestException.IsAuthenticationFailure(response))
            { Invalidate(usedToken); LastFailure = FirebaseRequestException.Parse(response); SetState(FirebaseConnectionState.AuthenticationFailure); }
            else if (response.Status == 0 || response.Status == 429 || response.Status >= 500)
            {
                LastFailure = FirebaseRequestException.Parse(response); _retryAfter = now() + 15;
                if (LastFailure.Kind is FirebaseFailureKind.Network or FirebaseFailureKind.Service) SetState(FailureState(LastFailure));
            }
            return response;
        }
    }
    private static FirebaseConnectionState FailureState(FirebaseRequestException e) => e.Kind switch
    {
        FirebaseFailureKind.Configuration => FirebaseConnectionState.ConfigurationFailure,
        FirebaseFailureKind.Network => FirebaseConnectionState.TemporaryFailure,
        FirebaseFailureKind.Service => FirebaseConnectionState.ServiceFailure,
        _ => FirebaseConnectionState.AuthenticationFailure
    };
    public void Dispose()
    {
        _disposed = true; _credentials = null;
        var pending = _connecting; _connecting = null;
        pending?.TrySetException(new ObjectDisposedException(nameof(FirebaseSession)));
        _lifetime.Cancel();
    }
    private void SetState(FirebaseConnectionState state)
    { if (_disposed || _state == state) return; _state = state; StateChanged?.Invoke(State); }
}
