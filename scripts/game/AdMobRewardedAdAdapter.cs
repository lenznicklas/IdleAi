#nullable enable
using Godot;
using System;
using PoingStudios.AdMob.Api;
using PoingStudios.AdMob.Api.Core;
using PoingStudios.AdMob.Api.Listeners;

namespace IdleAi;

/// <summary>Poing SDK types and unchanged platform/test-unit selection.</summary>
public sealed class AdMobRewardedAdAdapter : IRewardedAdAdapter
{
    private const string AndroidRewardedAdUnitId = "ca-app-pub-3940256099942544/5224354917";
    private const string IosRewardedAdUnitId = "ca-app-pub-3940256099942544/1712485313";
    public void Initialize() => PoingStudios.AdMob.Api.MobileAds.Initialize();
    public void Load(Action<IRewardedAdHandle> loaded, Action<string> failed)
    {
        new RewardedAdLoader().Load(OS.GetName() == "iOS" ? IosRewardedAdUnitId : AndroidRewardedAdUnitId,
            new AdRequest(), new RewardedAdLoadCallback
            {
                OnAdLoaded = ad => loaded(new Handle(ad)),
                OnAdFailedToLoad = error => failed(error.Message)
            });
    }
    internal static void LogLoadError(string error) => GD.PushWarning("Rewarded Ad failed to load: " + error);
    internal static void LogShowError(string error) => GD.PushWarning("Rewarded Ad failed to show: " + error);
    private sealed class Handle : IRewardedAdHandle
    {
        private readonly RewardedAd _ad;
        private FullScreenContentCallback? _fullscreen;
        private OnUserEarnedRewardListener? _listener;
        public Handle(RewardedAd ad) { _ad = ad; }
        public void Show(Action earned, Action dismissed, Action<string> failed)
        {
            _fullscreen = new FullScreenContentCallback
            {
                OnAdDismissedFullScreenContent = dismissed,
                OnAdFailedToShowFullScreenContent = error => failed(error.Message)
            };
            _ad.FullScreenContentCallback = _fullscreen;
            _listener = new OnUserEarnedRewardListener { OnUserEarnedReward = _ => earned() };
            _ad.Show(_listener);
        }
        public void Destroy() => _ad.Destroy();
    }
}
