#nullable enable
using System;

namespace IdleAi;

public interface IRewardedAdHandle
{
    void Show(Action earned, Action dismissed, Action<string> failed);
    void Destroy();
}

public interface IRewardedAdAdapter
{
    void Initialize();
    void Load(Action<IRewardedAdHandle> loaded, Action<string> failed);
}

/// <summary>Lazy ad lifecycle, stale callback rejection, and exactly one Shop reward per show.</summary>
public sealed class RewardedAdCoordinator : IDisposable
{
    public const int RewardedAdShardReward = 5;
    private readonly IRewardedAdAdapter _adapter;
    private readonly Func<int, ShopResult> _grant;
    private IRewardedAdHandle? _ad;
    private bool _initialized;
    private bool _disposed;
    private bool _earned;
    private int _generation;
    public bool IsReady => _ad != null && !IsShowing;
    public bool IsLoading { get; private set; }
    public bool IsShowing { get; private set; }
    public event Action? StateChanged;
    public event Action? RewardEarned;
    public event Action<string>? MessageRequested;

    public RewardedAdCoordinator(ShopService shop) : this(new AdMobRewardedAdAdapter(), shop.GrantDataShards) { }
    public RewardedAdCoordinator(IRewardedAdAdapter adapter, Func<int, ShopResult> grant)
    { _adapter = adapter; _grant = grant; }

    public void ShowRewardedVideo()
    {
        if (_disposed || IsShowing) return;
        if (_ad == null)
        {
            if (!IsLoading) LoadRewardedAd();
            MessageRequested?.Invoke("Reward video is loading. Tap again when it is ready.");
            return;
        }
        var ad = _ad;
        int generation = ++_generation;
        _earned = false;
        IsShowing = true;
        StateChanged?.Invoke();
        try
        {
            ad.Show(() => Earn(generation), () => EndShow(generation, null), error => EndShow(generation, error));
        }
        catch (Exception exception) { EndShow(generation, exception.Message); }
    }

    private void LoadRewardedAd()
    {
        if (_disposed || IsLoading || IsShowing) return;
        int generation = ++_generation;
        DestroyAd();
        IsLoading = true;
        StateChanged?.Invoke();
        try
        {
            // Opening Shop only constructs this coordinator. The first button intent initializes SDK.
            if (!_initialized) { _adapter.Initialize(); _initialized = true; }
            _adapter.Load(ad =>
            {
                if (_disposed || generation != _generation || !IsLoading) { if (!ReferenceEquals(ad, _ad)) ad.Destroy(); return; }
                _ad = ad;
                IsLoading = false;
                StateChanged?.Invoke();
            }, error => LoadFailed(generation, error));
        }
        catch (Exception exception) { LoadFailed(generation, exception.Message); }
    }

    private void LoadFailed(int generation, string error)
    {
        if (_disposed || generation != _generation || !IsLoading) return;
        IsLoading = false;
        StateChanged?.Invoke();
        // Preserve existing SDK load error logging; the button returns to its retry state.
        AdMobRewardedAdAdapter.LogLoadError(error);
    }

    private void Earn(int generation)
    {
        if (_disposed || generation != _generation || !IsShowing || _earned) return;
        _earned = true; // Set before calling gameplay or notifying observers (reentrancy safe).
        ShopResult result = _grant(RewardedAdShardReward);
        MessageRequested?.Invoke(result.Message);
        if (result.Changed) RewardEarned?.Invoke();
    }

    private void EndShow(int generation, string? error)
    {
        if (_disposed || generation != _generation || !IsShowing) return;
        ++_generation; // Late earned/dismiss/failure callbacks cannot affect the next ad.
        IsShowing = false;
        DestroyAd();
        StateChanged?.Invoke();
        if (error != null)
        {
            MessageRequested?.Invoke("Video could not be shown. Please try again.");
            AdMobRewardedAdAdapter.LogShowError(error);
        }
        else LoadRewardedAd();
    }

    private void DestroyAd()
    {
        var ad = _ad;
        _ad = null;
        if (ad == null) return;
        try { ad.Destroy(); }
        catch (Exception exception) { AdMobRewardedAdAdapter.LogShowError(exception.Message); }
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        ++_generation;
        IsLoading = false;
        IsShowing = false;
        DestroyAd();
    }
}
