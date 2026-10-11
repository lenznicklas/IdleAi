#nullable enable
namespace IdleAi;

/// <summary>Session composition only; integration workflows live in their coordinators.</summary>
public partial class Game
{
    private RewardedAdCoordinator? _rewardedAds;
    private IapPurchaseCoordinator _purchases = null!;
    internal RewardedAdCoordinator RewardedAds
    {
        get
        {
            if (_rewardedAds != null) return _rewardedAds;
            _rewardedAds = new RewardedAdCoordinator(_shopService);
            _rewardedAds.RewardEarned += () => { SaveGame(); _ui.UpdateAll(); };
            return _rewardedAds;
        }
    }
    internal IapPurchaseCoordinator Purchases => _purchases;

    private void InitializePurchasesAndRecover()
    {
        _purchases = new IapPurchaseCoordinator(_shopService,
            () => _services.Save.CreateSave(GetCurrentUnixTime()), TrySaveGame, _saveManager);
        _purchases.RecoverBeforeLoad();
    }
}
