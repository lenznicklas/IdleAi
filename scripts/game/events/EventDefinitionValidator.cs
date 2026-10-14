using System;
using System.Collections.Generic;

namespace IdleAi;

internal static class EventDefinitionValidator
{
    internal static void Validate(EventDefinition d, IReadOnlyList<EventGeneratorDefinition> generators)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var byId = new Dictionary<string, EventGeneratorDefinition>(StringComparer.Ordinal);
        foreach (var g in generators)
        {
            if (string.IsNullOrWhiteSpace(g.Id) || !ids.Add(g.Id) || string.IsNullOrWhiteSpace(g.Name)
                || g.StartingLevel < 1 || g.MaxLevel < g.StartingLevel || !Enum.IsDefined(g.Scaling)
                || !Finite(g.BaseProduction, 0) || !Finite(g.ProductionPerLevel, 0) || !Finite(g.ProductionGrowth, 1)
                || !Finite(g.Multiplier, double.Epsilon) || !Finite(g.BaseUpgradeCost, double.Epsilon)
                || !Finite(g.UpgradeCostGrowth, 1) || !Finite(g.CycleSeconds, .1)) Fail("generator");
            double levels = g.MaxLevel - g.StartingLevel;
            double maximumProduction = g.Scaling == EventProductionScaling.Exponential ? g.BaseProduction * Math.Pow(g.ProductionGrowth, levels)
                : g.BaseProduction + levels * g.ProductionPerLevel;
            if (!double.IsFinite(maximumProduction * g.Multiplier)
                || !double.IsFinite(g.BaseUpgradeCost * Math.Pow(g.UpgradeCostGrowth, levels))) Fail("generator overflow");
            ValidateCondition(g.Unlock);
            byId.Add(g.Id, g);
        }
        foreach (var g in generators)
        {
            string required = g.Unlock.RequiredGeneratorId;
            for (int depth = 0; !string.IsNullOrEmpty(required); depth++)
            {
                if (depth >= generators.Count || !byId.TryGetValue(required, out var dependency)) Fail("generator dependency");
                required = byId[required].Unlock.RequiredGeneratorId;
            }
        }
        var prestige = d.Prestige;
        if (!Finite(d.ProductionMultiplier, double.Epsilon) || !Finite(prestige.Requirement, double.Epsilon)
            || !Finite(prestige.RequirementGrowth, 1) || !Finite(prestige.ProductionBonusPerResource, 0)
            || !Finite(prestige.RewardExponent, double.Epsilon) || !Finite(prestige.RewardMultiplier, double.Epsilon)) Fail("prestige");
        ids.Clear();
        foreach (var bot in d.Bots)
            if (string.IsNullOrWhiteSpace(bot.Id) || !ids.Add(bot.Id) || !Finite(bot.ProductionMultiplier, 1)
                || string.IsNullOrWhiteSpace(bot.Name) || !Enum.TryParse<BotRarity>(bot.Rarity, out var rarity) || !Enum.IsDefined(rarity)
                || bot.EventId.Length > 0 && bot.EventId != d.Id
                || bot.SecondaryBonus != null && (!Finite(bot.SecondaryBonus.GeneratorUpgradeDiscount, 0) || bot.SecondaryBonus.GeneratorUpgradeDiscount >= 1)) Fail("bot");
        foreach (var milestone in d.Milestones) ValidateReward(d, milestone.Reward, 0);
        ids.Clear();
        foreach (var item in d.Shop)
        {
            if (string.IsNullOrWhiteSpace(item.Id) || !ids.Add(item.Id) || !Finite(item.Cost, double.Epsilon)
                || string.IsNullOrWhiteSpace(item.Name) || string.IsNullOrWhiteSpace(item.Category)
                || item.EventId.Length > 0 && item.EventId != d.Id
                || item.PurchaseLimit < 0 || !Enum.IsDefined(item.CostCurrency)
                || item.CostCurrency == EventShopCurrency.PrestigeResource && (item.Cost != Math.Floor(item.Cost) || item.Cost >= long.MaxValue)) Fail("shop");
            if (item.Unlock.RequiredPrestigeResource < 0) Fail("shop unlock");
            if (item.Unlock.RequiredBotId.Length > 0 && !System.Linq.Enumerable.Any(d.Bots, b => b.Id == item.Unlock.RequiredBotId)) Fail("shop bot");
            if (item.Unlock.RequiredChallengeId.Length > 0 && !System.Linq.Enumerable.Any(d.Challenges, c => c.Id == item.Unlock.RequiredChallengeId && c.Enabled)) Fail("shop challenge");
            if (item.Unlock.RequiredMilestoneId.Length > 0 && !System.Linq.Enumerable.Any(d.Milestones, m => m.Id == item.Unlock.RequiredMilestoneId)) Fail("shop milestone");
            ValidateCondition(item.Availability);
            if (!string.IsNullOrEmpty(item.Availability.RequiredGeneratorId) && !byId.ContainsKey(item.Availability.RequiredGeneratorId)) Fail("shop dependency");
            ValidateReward(d, item.Reward, 0);
        }
        ids.Clear();
        if (d.DailyChallengeCount < 0 || d.DailyChallengeCount > 20) Fail("daily challenge count");
        foreach (var c in d.Challenges)
        {
            if (string.IsNullOrWhiteSpace(c.Id) || !ids.Add(c.Id) || c.EventId != d.Id || string.IsNullOrWhiteSpace(c.Title)
                || !Enum.IsDefined(c.Type) || !Enum.IsDefined(c.Category) || !Finite(c.Target, double.Epsilon)
                || c.Repeatable && !EventChallengeRules.IsCumulative(c.Type)
                || c.UseCurrentState && EventChallengeRules.IsCumulative(c.Type)) Fail("challenge");
            if (c.Type == EventChallengeType.UnlockGenerator && string.IsNullOrWhiteSpace(c.GeneratorId)
                || c.GeneratorId.Length > 0 && !byId.ContainsKey(c.GeneratorId)) Fail("challenge generator");
            if (c.Type is EventChallengeType.UnlockBot or EventChallengeType.OwnBot or EventChallengeType.EquipBot && !System.Linq.Enumerable.Any(d.Bots, b => b.Id == c.BotId)) Fail("challenge bot");
            if (c.Type is EventChallengeType.UnlockGenerator or EventChallengeType.UnlockBot or EventChallengeType.OwnBot or EventChallengeType.EquipBot && c.Target != 1) Fail("challenge unlock target");
            if (c.ShopItemId.Length > 0 && (c.Type != EventChallengeType.PurchaseShopItems || !System.Linq.Enumerable.Any(d.Shop, item => item.Id == c.ShopItemId))) Fail("challenge shop item");
            ValidateCondition(c.Availability);
            if (c.Availability.RequiredGeneratorId.Length > 0 && !byId.ContainsKey(c.Availability.RequiredGeneratorId)) Fail("challenge availability");
            ValidateReward(d, c.Reward, 0);
        }
        foreach (var bot in d.Bots)
        {
            if (bot.UnlockMilestoneId.Length > 0 && !System.Linq.Enumerable.Any(d.Milestones, m => m.Id == bot.UnlockMilestoneId)) Fail("bot milestone");
            foreach (var path in bot.UnlockPaths)
            {
                if (!Finite(path.LifetimeCurrency, 0) || path.PrestigeCount < 0 || path.PrestigeCurrency < 0) Fail("bot unlock");
                if (path.MilestoneId.Length > 0 && !System.Linq.Enumerable.Any(d.Milestones, m => m.Id == path.MilestoneId)) Fail("bot milestone");
                if (path.ChallengeId.Length > 0 && !System.Linq.Enumerable.Any(d.Challenges, c => c.Id == path.ChallengeId && c.Enabled)) Fail("bot challenge");
                if (path.ShopItemId.Length > 0 && !System.Linq.Enumerable.Any(d.Shop, item => item.Id == path.ShopItemId && item.Enabled)) Fail("bot shop");
            }
        }
    }

    private static bool Finite(double value, double min) => double.IsFinite(value) && value >= min;
    private static void ValidateCondition(EventUnlockCondition c)
    {
        if (!Finite(c.RequiredCurrencyEarned, 0) || c.RequiredPrestigeCount < 0 || c.RequiredGeneratorLevel < 1 || !Enum.IsDefined(c.CurrencyScope)) Fail("unlock");
    }
    private static void ValidateReward(EventDefinition d, EventReward r, int depth)
    {
        if (depth > 8 || !Enum.IsDefined(r.Type) || !Finite(r.Amount, 0)) Fail("reward");
        if (r.Type == EventRewardType.PrestigeResource && (r.Amount != Math.Floor(r.Amount) || r.Amount >= long.MaxValue)) Fail("prestige reward");
        if (r.Type is EventRewardType.EventBot or EventRewardType.Cosmetic or EventRewardType.MachineSkin or EventRewardType.BotSkin or EventRewardType.Custom && string.IsNullOrWhiteSpace(r.Id)) Fail("reward ID");
        if (r.Type == EventRewardType.EventBot)
        {
            bool exists = false;
            foreach (var bot in d.Bots) if (bot.Id == r.Id) { exists = true; break; }
            if (!exists) Fail("reward bot");
        }
        if (r.Type == EventRewardType.Bundle)
        {
            if (r.Contents.Count == 0) Fail("empty bundle");
            foreach (var item in r.Contents) ValidateReward(d, item, depth + 1);
        }
    }
    private static void Fail(string field) => throw new ArgumentException("Invalid event " + field + " configuration.");
}
