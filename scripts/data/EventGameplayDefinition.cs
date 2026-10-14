using System;
using System.Collections.Generic;

namespace IdleAi;

public enum EventProductionScaling { Linear, Exponential }
public enum EventUnlockCurrencyScope { Lifetime, CurrentRun }
public enum EventShopCurrency { EventCurrency, PrestigeResource }

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
    public double Requirement { get; init; } = 1_000_000;
    public double RequirementGrowth { get; init; } = 1;
    public double ProductionBonusPerResource { get; init; } = .25;
    public double RewardExponent { get; init; } = .5;
    public double RewardMultiplier { get; init; } = 1;
}

public sealed record EventBotDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string Rarity { get; init; } = "Common";
    public string IconPath { get; init; } = "";
    public double ProductionMultiplier { get; init; } = 1.1;
    public string UnlockMilestoneId { get; init; } = "";
}

public sealed record EventShopItem
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string Description { get; init; } = "";
    public string IconPath { get; init; } = "";
    public double Cost { get; init; }
    public EventShopCurrency CostCurrency { get; init; }
    public required EventReward Reward { get; init; }
    public int PurchaseLimit { get; init; } = 1;
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
