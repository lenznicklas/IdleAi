using System;
using System.Collections.Generic;
using System.Linq;

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
        foreach (var m in d.Milestones) if (m.Id == milestoneId) return CanGrantAfterEnd(m.Reward) || (!p.Ended && IsActive(d));
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
                if (!d.Bots.Any(bot => bot.Id == reward.Id && bot.Enabled)) return false;
                p.UnlockedBots.Add(reward.Id);
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
}
