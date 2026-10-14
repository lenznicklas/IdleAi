using System;
using System.Collections.Generic;

namespace IdleAi;

public enum EventProductionScaling { Linear, Exponential }
public enum EventUnlockCurrencyScope { Lifetime, CurrentRun }
public enum EventShopCurrency { EventCurrency, PrestigeResource, DataShards }

public sealed record EventUnlockCondition
{
    public double RequiredCurrencyEarned { get; init; }
    public EventUnlockCurrencyScope CurrencyScope { get; init; }
    public int RequiredPrestigeCount { get; init; }
    public string RequiredGeneratorId { get; init; } = "";
    public int RequiredGeneratorLevel { get; init; } = 1;
}

public sealed record EventGeneratorDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string Description { get; init; } = "";
    public string IconPath { get; init; } = "";
    public bool Enabled { get; init; } = true;
    public int StartingLevel { get; init; } = 1;
    public int MaxLevel { get; init; } = 1000;
    public double BaseProduction { get; init; } = 1;
    public EventProductionScaling Scaling { get; init; }
    public double ProductionPerLevel { get; init; } = 1;
    public double ProductionGrowth { get; init; } = 1.15;
    public double Multiplier { get; init; } = 1;
    public double BaseUpgradeCost { get; init; } = 10;
    public double UpgradeCostGrowth { get; init; } = 1.6;
    public double CycleSeconds { get; init; } = 5;
    public EventUnlockCondition Unlock { get; init; } = new();
}

public sealed record EventPrestigeConfiguration
{
    public bool Enabled { get; init; }
    public string ResourceName { get; init; } = "Protocol Cores";
    public string IconPath { get; init; } = "res://assets/ui/core_level.png";
    public double Requirement { get; init; } = 1_000_000;
    public double RequirementGrowth { get; init; } = 1;
    public double ProductionBonusPerResource { get; init; } = .25;
    public double RewardExponent { get; init; } = .5;
    public double RewardMultiplier { get; init; } = 1;
}

// Alternative paths are OR; populated requirements within a path are AND.
public sealed record EventBotUnlockCondition
{
    public double LifetimeCurrency { get; init; }
    public int PrestigeCount { get; init; }
    public long PrestigeCurrency { get; init; } // lifetime earned; spending never re-locks a bot
    public string MilestoneId { get; init; } = ""; // claimed reward
    public string ChallengeId { get; init; } = ""; // completed, not necessarily claimed
    public string ShopItemId { get; init; } = "";
}
public sealed record EventBotSecondaryBonus
{
    // Fraction, e.g. .1 reduces generator upgrade costs by 10%.
    public double GeneratorUpgradeDiscount { get; init; }
}
public sealed record EventBotDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    // Legacy strings remain compatible; validated against the shared BotRarity enum.
    public string Rarity { get; init; } = "Common";
    public string EventId { get; init; } = ""; // empty inherits the containing event (legacy definitions)
    public string Description { get; init; } = "";
    public bool Enabled { get; init; } = true;
    public bool AvailableFromStart { get; init; }
    public bool GrantOnUnlock { get; init; } = true;
    public IReadOnlyList<EventBotUnlockCondition> UnlockPaths { get; init; } = Array.Empty<EventBotUnlockCondition>();
    public EventBotSecondaryBonus? SecondaryBonus { get; init; }
    public string IconPath { get; init; } = "";
    public double ProductionMultiplier { get; init; } = 1.1;
    public string UnlockMilestoneId { get; init; } = "";
}

public sealed record EventShopUnlockCondition
{
    public long RequiredPrestigeResource { get; init; }
    public string RequiredBotId { get; init; } = "";
    public string RequiredChallengeId { get; init; } = "";
    public string RequiredMilestoneId { get; init; } = ""; // earned milestone, not necessarily claimed
}

public sealed record EventShopItem
{
    public required string Id { get; init; }
    public string EventId { get; init; } = ""; // empty inherits containing Event (legacy content)
    public string Category { get; init; } = "Resources";
    public bool Featured { get; init; }
    public int SortOrder { get; init; }
    public bool PermanentUnlock { get; init; } = true;
    public string CostIconPath { get; init; } = "";
    public EventShopUnlockCondition Unlock { get; init; } = new();
    public required string Name { get; init; }
    public string Description { get; init; } = "";
    public string IconPath { get; init; } = "";
    public double Cost { get; init; }
    public EventShopCurrency CostCurrency { get; init; }
    public required EventReward Reward { get; init; }
    public int PurchaseLimit { get; init; } = 1; // 0 = unlimited, otherwise maximum successful purchases
    public bool Enabled { get; init; } = true;
    public EventUnlockCondition Availability { get; init; } = new();
}
public sealed record EventStoryMessage(double TargetEarned, string Text);
public sealed record EventThemeConfiguration
{
    public string AccentHex { get; init; } = "#33b3f2";
    public string SecondaryHex { get; init; } = "#7850ba";
    public string BackgroundHex { get; init; } = "#020509";
    public bool UppercaseTitle { get; init; }
}
