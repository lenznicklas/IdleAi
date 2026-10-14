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
            if (string.IsNullOrWhiteSpace(bot.Id) || !ids.Add(bot.Id) || !Finite(bot.ProductionMultiplier, 1)) Fail("bot");
        foreach (var milestone in d.Milestones) ValidateReward(d, milestone.Reward, 0);
        ids.Clear();
        foreach (var item in d.Shop)
        {
            if (string.IsNullOrWhiteSpace(item.Id) || !ids.Add(item.Id) || !Finite(item.Cost, double.Epsilon)
                || item.PurchaseLimit < 1 || !Enum.IsDefined(item.CostCurrency)
                || item.CostCurrency == EventShopCurrency.PrestigeResource && item.Cost != Math.Floor(item.Cost)) Fail("shop");
            ValidateCondition(item.Availability);
            if (!string.IsNullOrEmpty(item.Availability.RequiredGeneratorId) && !byId.ContainsKey(item.Availability.RequiredGeneratorId)) Fail("shop dependency");
            ValidateReward(d, item.Reward, 0);
        }
        foreach (var bot in d.Bots)
        {
            bool exists = false;
            foreach (var m in d.Milestones) if (m.Id == bot.UnlockMilestoneId) { exists = true; break; }
            if (!exists) Fail("bot milestone");
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
        if (r.Type is EventRewardType.EventBot or EventRewardType.Cosmetic or EventRewardType.MachineSkin or EventRewardType.BotSkin && string.IsNullOrWhiteSpace(r.Id)) Fail("reward ID");
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
