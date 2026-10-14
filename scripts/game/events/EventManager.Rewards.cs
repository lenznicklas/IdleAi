using System;
using System.Collections.Generic;

namespace IdleAi;

public sealed partial class EventManager
{
    private static bool CanGrantAfterEnd(EventReward reward)
    {
        if (reward.Type is EventRewardType.EventCurrency or EventRewardType.PrestigeResource) return false;
        foreach (var part in reward.Contents) if (!CanGrantAfterEnd(part)) return false;
        return true;
    }
    public bool CanClaimReward(string eventId, string milestoneId)
    {
        if (ReadOnly || !_byId.TryGetValue(eventId, out var d) || !_save.ProgressByEventId.TryGetValue(eventId, out var p)
            || !p.UnlockedMilestones.Contains(milestoneId) || p.ClaimedRewards.Contains(milestoneId)) return false;
        foreach (var m in d.Milestones) if (m.Id == milestoneId) return !p.Ended || CanGrantAfterEnd(m.Reward);
        return false;
    }
    private bool GrantReward(EventDefinition d, EventProgressSaveData p, EventReward reward, List<Action> undo)
    {
        switch (reward.Type)
        {
            case EventRewardType.DeveloperCredits: p.DeveloperCredits += reward.Amount; break;
            case EventRewardType.EventCurrency: AddCurrency(d, p, reward.Amount); break;
            case EventRewardType.PrestigeResource:
                if (reward.Amount != Math.Floor(reward.Amount) || reward.Amount >= long.MaxValue - (double)p.TotalPrestigeResourceEarned) return false;
                p.PrestigeResource += (long)reward.Amount;
                p.TotalPrestigeResourceEarned += (long)reward.Amount;
                _production[d.Id].RefreshHistory(p);
                break;
            case EventRewardType.EventBot:
                bool known = false;
                foreach (var bot in d.Bots) if (bot.Id == reward.Id) { known = true; break; }
                if (!known) return false;
                p.OwnedBots.Add(reward.Id);
                break;
            case EventRewardType.Cosmetic: p.Cosmetics.Add(reward.Id); break;
            case EventRewardType.MachineSkin:
            case EventRewardType.BotSkin:
                if (_validateCosmetic == null || !_validateCosmetic(reward)) return false;
                string entitlement = (reward.Type == EventRewardType.MachineSkin ? "machine:" : "bot:") + reward.Id;
                p.Cosmetics.Add(entitlement);
                _save.CosmeticEntitlements.Add(entitlement);
                break;
            case EventRewardType.Bundle:
                foreach (var item in reward.Contents) if (!GrantReward(d, p, item, undo)) return false;
                break;
            default:
                var revert = _grantExternalReward?.Invoke(reward);
                if (revert == null) return false;
                undo.Add(revert);
                break;
        }
        return true;
    }
    public bool IsBotOwned(string eventId, string botId) => _save.ProgressByEventId.TryGetValue(eventId, out var p) && p.OwnedBots.Contains(botId);
    public bool IsBotActive(string eventId, string botId) => _save.ProgressByEventId.TryGetValue(eventId, out var p) && p.ActiveBotId == botId;
    public EventActionResult EquipBot(string eventId, string botId)
    {
        if (_actionInProgress) return Busy();
        Tick();
        if (!_byId.TryGetValue(eventId, out var d) || !IsActive(d) || !IsBotOwned(eventId, botId)) return new(false, "Bot unavailable.");
        var p = _save.ProgressByEventId[eventId];
        return Transaction(d, _ =>
        {
            p.ActiveBotId = p.ActiveBotId == botId ? null : botId;
            _production[eventId].RefreshHistory(p);
            return true;
        }, "Event bot updated.");
    }
    public int PurchasedCount(string eventId, string itemId) => _save.ProgressByEventId.TryGetValue(eventId, out var p)
        && p.ShopPurchases.TryGetValue(itemId, out int count) ? count : 0;
    public bool CanBuy(string eventId, string itemId)
    {
        if (!_byId.TryGetValue(eventId, out var d) || !IsActive(d) || !_save.ProgressByEventId.TryGetValue(eventId, out var p)) return false;
        foreach (var item in d.Shop)
            if (item.Id == itemId) return item.Enabled && PurchasedCount(eventId, itemId) < item.PurchaseLimit
                && _production[eventId].RequirementsMet(item.Availability, p)
                && (item.CostCurrency == EventShopCurrency.EventCurrency ? p.Currency : p.PrestigeResource) >= item.Cost;
        return false;
    }
    public EventActionResult Buy(string eventId, string itemId)
    {
        if (_actionInProgress) return Busy();
        Tick();
        if (!CanBuy(eventId, itemId)) return new(false, "Item unavailable, limit reached, or not enough currency.");
        var d = _byId[eventId];
        EventShopItem? item = null;
        foreach (var candidate in d.Shop) if (candidate.Id == itemId) { item = candidate; break; }
        if (item == null) return new(false, "Unknown shop item.");
        var p = _save.ProgressByEventId[eventId];
        return Transaction(d, undo =>
        {
            if (item.CostCurrency == EventShopCurrency.EventCurrency) p.Currency -= item.Cost; else p.PrestigeResource -= (long)item.Cost;
            if (!GrantReward(d, p, item.Reward, undo)) return false;
            p.ShopPurchases[itemId] = PurchasedCount(eventId, itemId) + 1;
            _production[eventId].RefreshHistory(p);
            return true;
        }, "Event item purchased.");
    }
}
