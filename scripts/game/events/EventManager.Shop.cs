using System;
using System.Collections.Generic;
using System.Linq;

namespace IdleAi;

public enum EventShopState { Unavailable, Available, InsufficientCurrency, Locked, Owned, SoldOut, EventEnded }
public readonly record struct EventShopSnapshot(EventShopState State, int PurchasedCount, int PurchaseLimit, double Balance)
{
    public bool CanBuy => State == EventShopState.Available;
    public bool Owned { get; init; }
}

public sealed partial class EventManager
{
    private readonly Func<double>? _dataShardBalance;
    // External spending returns compensation just like external reward grants; same atomic main save.
    private readonly Func<double, Action?>? _spendDataShards;
    private readonly Func<EventReward, bool>? _cosmeticOwned;
    public int PurchasedCount(string eventId, string itemId) => _save.ProgressByEventId.TryGetValue(eventId, out var p)
        && p.ShopPurchases.TryGetValue(itemId, out int count) ? count : 0;
    public IReadOnlyList<EventShopItem> GetShopItems(string eventId) => _byId.TryGetValue(eventId, out var d)
        ? d.Shop.Where(i => i.Enabled).OrderByDescending(i => i.Featured).ThenBy(i => i.Category, StringComparer.Ordinal)
            .ThenBy(i => i.SortOrder).ThenBy(i => i.Id, StringComparer.Ordinal).ToArray() : [];
    public double GetShopBalance(string eventId, EventShopCurrency currency)
    {
        if (!_save.ProgressByEventId.TryGetValue(eventId, out var p)) return 0;
        double balance = currency switch
        {
            EventShopCurrency.EventCurrency => p.Currency, EventShopCurrency.PrestigeResource => p.PrestigeResource,
            EventShopCurrency.DataShards when _spendDataShards != null => _dataShardBalance?.Invoke() ?? 0,
            _ => 0
        };
        return double.IsFinite(balance) && balance >= 0 ? balance : 0;
    }
    private bool ShopRequirementsMet(EventDefinition d, EventProgressSaveData p, EventShopItem item) =>
        _production[d.Id].RequirementsMet(item.Availability, p) && p.PrestigeResource >= item.Unlock.RequiredPrestigeResource
        && (item.Unlock.RequiredBotId.Length == 0 || p.UnlockedBots.Contains(item.Unlock.RequiredBotId) || p.OwnedBots.Contains(item.Unlock.RequiredBotId))
        && (item.Unlock.RequiredChallengeId.Length == 0 || EventBotRules.ChallengeComplete(p, item.Unlock.RequiredChallengeId))
        && (item.Unlock.RequiredMilestoneId.Length == 0 || p.UnlockedMilestones.Contains(item.Unlock.RequiredMilestoneId));
    private bool RefreshShopUnlocks(EventDefinition d, EventProgressSaveData p, long timestamp)
    {
        if (p.Ended || timestamp < d.StartUnix || timestamp > d.EndUnix) return false;
        bool changed = false;
        foreach (var item in d.Shop)
            if (item.Enabled && item.PermanentUnlock && !p.UnlockedShopItems.Contains(item.Id)
                && (PurchasedCount(d.Id, item.Id) > 0 || ShopRequirementsMet(d, p, item)))
                changed |= p.UnlockedShopItems.Add(item.Id);
        return changed;
    }
    private bool RewardAlreadyOwned(EventReward reward, EventProgressSaveData p)
    {
        if (reward.Type == EventRewardType.EventBot && p.OwnedBots.Contains(reward.Id)) return true;
        if (reward.Type == EventRewardType.Cosmetic && p.Cosmetics.Contains(reward.Id)) return true;
        if (reward.Type is EventRewardType.MachineSkin or EventRewardType.BotSkin)
        {
            string key = (reward.Type == EventRewardType.MachineSkin ? "machine:" : "bot:") + reward.Id;
            if (_save.CosmeticEntitlements.Contains(key) || p.Cosmetics.Contains(key) || _cosmeticOwned?.Invoke(reward) == true) return true;
        }
        return reward.Contents.Any(part => RewardAlreadyOwned(part, p));
    }
    private bool RewardAvailable(EventReward reward, EventDefinition d) => reward.Type switch
    {
        EventRewardType.EventBot => d.Bots.Any(b => b.Id == reward.Id && b.Enabled),
        EventRewardType.MachineSkin or EventRewardType.BotSkin => _validateCosmetic?.Invoke(reward) == true,
        EventRewardType.Bundle => reward.Contents.All(part => RewardAvailable(part, d)),
        EventRewardType.DataShards or EventRewardType.Custom => _grantExternalReward != null,
        _ => true
    };
    public EventShopSnapshot GetShopItem(string eventId, string itemId)
    {
        if (!_byId.TryGetValue(eventId, out var d) || !_save.ProgressByEventId.TryGetValue(eventId, out var p)) return default;
        var item = d.Shop.FirstOrDefault(i => i.Id == itemId);
        if (item == null) return new(EventShopState.Unavailable, 0, 0, 0);
        int count = PurchasedCount(eventId, itemId);
        double balance = GetShopBalance(eventId, item.CostCurrency);
        bool owned = RewardAlreadyOwned(item.Reward, p);
        EventShopState state;
        if (p.Ended || NowUnix >= d.EndUnix) state = EventShopState.EventEnded;
        else if (!item.Enabled || !IsActive(d) || !RewardAvailable(item.Reward, d)
            || item.CostCurrency == EventShopCurrency.DataShards && (_dataShardBalance == null || _spendDataShards == null)) state = EventShopState.Unavailable;
        else if (owned) state = EventShopState.Owned;
        else if (count == int.MaxValue || item.PurchaseLimit > 0 && count >= item.PurchaseLimit) state = EventShopState.SoldOut;
        else if (!p.UnlockedShopItems.Contains(item.Id) && !ShopRequirementsMet(d, p, item)) state = EventShopState.Locked;
        else state = balance >= item.Cost ? EventShopState.Available : EventShopState.InsufficientCurrency;
        return new(state, count, item.PurchaseLimit, balance) { Owned = owned };
    }
    public bool CanBuy(string eventId, string itemId) => GetShopItem(eventId, itemId).CanBuy;
    public EventActionResult Buy(string eventId, string itemId, int? expectedPurchasedCount = null)
    {
        if (_actionInProgress) return Busy();
        Tick();
        var quote = GetShopItem(eventId, itemId);
        if (expectedPurchasedCount.HasValue && expectedPurchasedCount != quote.PurchasedCount) return new(false, "Purchase changed. Please try again.");
        if (!quote.CanBuy) return new(false, quote.State switch
        {
            EventShopState.Owned => "Already owned.", EventShopState.SoldOut => "Sold out.",
            EventShopState.EventEnded => "Event ended. Purchases are no longer available.",
            EventShopState.Locked => "Item is locked.", EventShopState.InsufficientCurrency => "Not enough currency.",
            _ => "Shop item unavailable."
        });
        var d = _byId[eventId];
        var item = d.Shop.First(i => i.Id == itemId);
        var p = _save.ProgressByEventId[eventId];
        return Transaction(d, undo =>
        {
            switch (item.CostCurrency)
            {
                case EventShopCurrency.EventCurrency: p.Currency -= item.Cost; break;
                case EventShopCurrency.PrestigeResource: p.PrestigeResource -= (long)item.Cost; break;
                case EventShopCurrency.DataShards:
                    var refund = _spendDataShards?.Invoke(item.Cost);
                    if (refund == null) return false;
                    undo.Add(refund); break;
                default: return false;
            }
            if (!GrantReward(d, p, item.Reward, undo)) return false;
            p.ShopPurchases[itemId] = checked(quote.PurchasedCount + 1);
            _production[eventId].RefreshHistory(p);
            RefreshChallenges(d, p, EventChallengeSignal.Shop | EventChallengeSignal.Cores | RewardChallengeSignals(item.Reward), NowUnix);
            return true;
        }, "Event item purchased.");
    }
}
