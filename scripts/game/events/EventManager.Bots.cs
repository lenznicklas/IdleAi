using System;
using System.Linq;
using System.Collections.Generic;

namespace IdleAi;

public readonly record struct EventBotSnapshot(bool Unlocked, bool Owned, bool Equipped, bool CanEquip, bool CanAcquire);

public sealed partial class EventManager
{
    private readonly Dictionary<string, EventProgressSaveData> _botEnrolled = [];
    public bool IsBotOwned(string eventId, string botId) => GetBot(eventId, botId).Owned;
    public bool IsBotActive(string eventId, string botId) => GetBot(eventId, botId).Equipped;
    public EventBotSnapshot GetBot(string eventId, string botId)
    {
        if (!_byId.TryGetValue(eventId, out var d) || !_save.ProgressByEventId.TryGetValue(eventId, out var p)) return default;
        var bot = d.Bots.FirstOrDefault(b => b.Id == botId);
        if (bot == null) return default;
        bool owned = p.OwnedBots.Contains(botId), unlocked = owned || p.UnlockedBots.Contains(botId);
        bool available = bot.Enabled && IsActive(d);
        return new(unlocked, owned, owned && bot.Enabled && p.ActiveBotId == botId,
            available && owned && p.ActiveBotId != botId, available && unlocked && !owned);
    }
    // Called on enrollment and affected economy/actions, never by UI or a per-frame listener.
    private bool RefreshBotProgress(EventDefinition d, EventProgressSaveData p, long timestamp)
    {
        if (p.Ended || timestamp > d.EndUnix || timestamp < d.StartUnix) return false;
        bool changed = false;
        // Bounded dependency closure: completing an unlock-bot challenge may unlock another bot.
        for (int pass = 0; pass <= d.Bots.Count; pass++)
        {
            bool gained = false;
            foreach (var bot in d.Bots)
            {
                if (!bot.Enabled || p.UnlockedBots.Contains(bot.Id)) continue;
                if (!bot.AvailableFromStart && !bot.UnlockPaths.Any(path => EventBotRules.Meets(path, p))) continue;
                p.UnlockedBots.Add(bot.Id);
                if (bot.GrantOnUnlock) p.OwnedBots.Add(bot.Id);
                gained = true;
            }
            if (!gained) break;
            changed = true;
            RefreshChallenges(d, p, EventChallengeSignal.Bots | EventChallengeSignal.Production, timestamp);
        }
        return changed;
    }
    public EventActionResult AcquireBot(string eventId, string botId)
    {
        if (_actionInProgress) return Busy();
        Tick();
        if (!GetBot(eventId, botId).CanAcquire) return new(false, "Bot unavailable.");
        var d = _byId[eventId];
        return Transaction(d, undo =>
        {
            var p = _save.ProgressByEventId[eventId];
            if (!GrantReward(d, p, new(EventRewardType.EventBot, 1, botId), undo)) return false;
            RefreshChallenges(d, p, EventChallengeSignal.Bots, NowUnix);
            return true;
        }, "Event bot acquired.");
    }
    public EventActionResult EquipBot(string eventId, string botId)
    {
        if (_actionInProgress) return Busy();
        Tick();
        if (!GetBot(eventId, botId).CanEquip) return new(false, "Bot unavailable or already equipped.");
        var d = _byId[eventId];
        return Transaction(d, _ =>
        {
            var p = _save.ProgressByEventId[eventId];
            p.ActiveBotId = botId;
            _production[eventId].RefreshHistory(p);
            RefreshChallenges(d, p, EventChallengeSignal.Bots | EventChallengeSignal.Production, NowUnix);
            return true;
        }, "Event bot equipped.");
    }
    public bool ShopBotAlreadyOwned(string eventId, string itemId) => _byId.TryGetValue(eventId, out var d)
        && _save.ProgressByEventId.TryGetValue(eventId, out var p)
        && d.Shop.Any(item => item.Id == itemId && RewardContainsOwnedBot(item.Reward, p));
    private static bool RewardContainsOwnedBot(EventReward reward, EventProgressSaveData p) =>
        reward.Type == EventRewardType.EventBot && p.OwnedBots.Contains(reward.Id)
        || reward.Contents.Any(part => RewardContainsOwnedBot(part, p));
}
