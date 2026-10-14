using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IdleAi;

public enum EventType { Weekend, Seasonal, Major }
public enum EventRewardType { DeveloperCredits, DataShards, EventCurrency, PrestigeResource, EventBot, Cosmetic, MachineSkin, BotSkin, Bundle }

public sealed record EventReward(EventRewardType Type, double Amount, string Id = "")
{
    public string Name { get; init; } = "";
    public string IconPath { get; init; } = "";
    public IReadOnlyList<EventReward> Contents { get; init; } = [];
}
public sealed record EventMilestone(string Id, double TargetAmount, EventReward Reward);

public sealed record EventProgressionConfiguration
{
    public string GeneratorName { get; init; } = "Protocol Generator";
    public double CycleSeconds { get; init; } = 5;
    public double BaseProduction { get; init; } = 1;
    public double ProductionPerLevel { get; init; } = 1;
    public double BaseUpgradeCost { get; init; } = 10;
    public double UpgradeCostGrowth { get; init; } = 1.6;
    public int MaxLevel { get; init; } = 1000;
    public double Production(int level) => BaseProduction + Math.Max(0, level - 1) * ProductionPerLevel;
    public double UpgradeCost(int level) => BaseUpgradeCost * Math.Pow(UpgradeCostGrowth, Math.Max(0, level - 1));
}

/// <summary>Theme/content hooks; the initial view implements only the configured generator.</summary>
public sealed record EventContentConfiguration
{
    public string[] Machines { get; init; } = [];
    public string[] Bots { get; init; } = [];
    public string[] Skins { get; init; } = [];
    public string[] ShopItems { get; init; } = [];
    public string[] Challenges { get; init; } = [];
    public string StoryText { get; init; } = "";
    public bool PrestigeEnabled { get; init; }
    [JsonExtensionData] public Dictionary<string, JsonElement> Extra { get; init; } = [];
}

public sealed record EventDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string Description { get; init; } = "";
    public required long StartUnix { get; init; }
    public required long EndUnix { get; init; }
    public EventType Type { get; init; }
    public bool Enabled { get; init; } = true;
    public string CurrencyName { get; init; } = "Event Data";
    public string CurrencyIconPath { get; init; } = "";
    public string IconPath { get; init; } = "";
    public string BackgroundPath { get; init; } = "";
    public long EndingSoonSeconds { get; init; } = 86400;
    public EventProgressionConfiguration Progression { get; init; } = new();
    public IReadOnlyList<EventGeneratorDefinition> Generators { get; init; } = [];
    public EventPrestigeConfiguration Prestige { get; init; } = new();
    public double ProductionMultiplier { get; init; } = 1;
    public IReadOnlyList<EventBotDefinition> Bots { get; init; } = [];
    public IReadOnlyList<EventShopItem> Shop { get; init; } = [];
    public IReadOnlyList<EventStoryMessage> Story { get; init; } = [];
    public EventThemeConfiguration Theme { get; init; } = new();
    public IReadOnlyList<EventMilestone> Milestones { get; init; } = [];
    public EventContentConfiguration Content { get; init; } = new();
}
