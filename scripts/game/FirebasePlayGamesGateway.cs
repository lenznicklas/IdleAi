#nullable enable
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IdleAi;

/// <summary>Tagged native requests. A bool notification never proves identity by itself.
/// Interactive login remains owned by GooglePlayGamesAchievementService.</summary>
public sealed class FirebasePlayGamesGateway(Action<string> check, Action<string> requestCode,
    CancellationToken lifetime, TimeSpan? loginTimeout = null, TimeSpan? codeTimeout = null)
{
    private readonly HashSet<string> _deliveredCodes = new(StringComparer.Ordinal);
    private TaskCompletionSource<bool>? _login;
    private TaskCompletionSource<string>? _code;
    private string _checkId = "", _codeId = "";
    public bool IsAcquiringNativeCredentials => _login != null || _code != null;
    public bool IsReady { get; private set; }
    public string PlayerId { get; private set; } = "";
    public event Action<string>? Diagnostic;
    public Task EnsureReadyAsync(CancellationToken cancellation = default)
    {
        if (IsReady) return Task.CompletedTask;
        if (_login != null) return _login.Task;
        var source = new TaskCompletionSource<bool>();
        _login = source;
        _ = CheckAsync(source, cancellation);
        return source.Task;
    }
    private void StartCheck()
    {
        _checkId = Guid.NewGuid().ToString("N");
        Diagnostic?.Invoke("Login check started; request=" + _checkId);
        check(_checkId);
    }
    private async Task CheckAsync(TaskCompletionSource<bool> source, CancellationToken cancellation)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(lifetime, cancellation);
        // A separate completion source gives every caller the same bounded operation.
        var completion = new TaskCompletionSource<bool>(); _checkCompletion = completion;
        try
        {
            linked.Token.ThrowIfCancellationRequested();
            StartCheck();
            await completion.Task.WaitAsync(loginTimeout ?? TimeSpan.FromSeconds(45), linked.Token);
            if (_login == source) { _login = null; _checkCompletion = null; _checkId = ""; }
            source.TrySetResult(true);
        }
        catch (TimeoutException) { Diagnostic?.Invoke("Login check timed out"); ClearLogin(source); source.TrySetException(new FirebaseRequestException(0, "GPGS_LOGIN_TIMEOUT")); }
        catch (Exception e) { ClearLogin(source); source.TrySetException(e); }
        finally { if (_login == source) { _login = null; _checkCompletion = null; _checkId = ""; } }
    }
    private void ClearLogin(TaskCompletionSource<bool> source) { if (_login == source) { _login = null; _checkCompletion = null; _checkId = ""; } }
    private void ClearCode(TaskCompletionSource<string> source) { if (_code == source) { _code = null; _codeCompletion = null; _codeId = ""; } }
    private TaskCompletionSource<bool>? _checkCompletion;
    public void OnNativeAuthentication(bool authenticated)
    {
        if (!authenticated) { IsReady = false; return; }
        // Native bool signals can arrive from an older SDK check. Verify current identity with a tagged call.
        if (_login != null) StartCheck();
    }
    public void OnChecked(string id, bool authenticated, string playerId, int status)
    {
        if (id != _checkId || _checkCompletion == null) { Diagnostic?.Invoke("Ignored late login callback; request=" + id); return; }
        Diagnostic?.Invoke($"Login result; request={id}; authenticated={authenticated}; playerPresent={!string.IsNullOrWhiteSpace(playerId)}; nativeStatus={status}");
        if (status != 0) { _checkCompletion.TrySetException(NativeFailure(status)); return; }
        if (!authenticated) return; // Allow the existing owner's interactive login to finish.
        if (string.IsNullOrWhiteSpace(playerId)) { _checkCompletion.TrySetException(new FirebaseRequestException(401, "GPGS_PLAYER_MISSING")); return; }
        PlayerId = playerId; IsReady = true; _checkCompletion.TrySetResult(true);
    }
    public Task<string> RequestCodeAsync(CancellationToken cancellation = default)
    {
        if (!IsReady) return Task.FromException<string>(new FirebaseRequestException(401, "GPGS_NOT_READY"));
        if (_code != null) return _code.Task;
        var source = new TaskCompletionSource<string>(); _code = source;
        _ = CodeAsync(source, cancellation);
        return source.Task;
    }
    private TaskCompletionSource<string>? _codeCompletion;
    private async Task CodeAsync(TaskCompletionSource<string> source, CancellationToken cancellation)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(lifetime, cancellation);
        var completion = new TaskCompletionSource<string>(); _codeCompletion = completion;
        _codeId = Guid.NewGuid().ToString("N");
        try
        {
            Diagnostic?.Invoke("Server code requested; request=" + _codeId);
            linked.Token.ThrowIfCancellationRequested();
            requestCode(_codeId);
            string result = await completion.Task.WaitAsync(codeTimeout ?? TimeSpan.FromSeconds(30), linked.Token);
            if (_code == source) { _code = null; _codeCompletion = null; _codeId = ""; }
            source.TrySetResult(result);
        }
        catch (TimeoutException) { Diagnostic?.Invoke("Server code callback timed out; request=" + _codeId); ClearCode(source); source.TrySetException(new FirebaseRequestException(0, "GPGS_AUTH_CODE_TIMEOUT")); }
        catch (Exception e) { ClearCode(source); source.TrySetException(e); }
        finally { if (_code == source) { _code = null; _codeCompletion = null; _codeId = ""; } }
    }
    public void OnCode(string id, string code, int status)
    {
        bool current = id == _codeId && _codeCompletion != null;
        Diagnostic?.Invoke($"Server code result; request={id}; current={current}; nativeStatus={status}; codePresent={!string.IsNullOrWhiteSpace(code)}");
        if (!current) return;
        if (status != 0) _codeCompletion!.TrySetException(NativeFailure(status));
        else if (string.IsNullOrWhiteSpace(code)) _codeCompletion!.TrySetException(new FirebaseRequestException(401, "GPGS_EMPTY_AUTH_CODE"));
        else if (!_deliveredCodes.Add(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code)))))
            _codeCompletion!.TrySetException(new FirebaseRequestException(401, "GPGS_REUSED_AUTH_CODE"));
        else _codeCompletion!.TrySetResult(code);
    }
    public void Resume() { IsReady = false; }
    private static FirebaseRequestException NativeFailure(int status) => new(0,
        status == 7 ? "GPGS_NETWORK_ERROR" : "GPGS_API_" + status);
}
