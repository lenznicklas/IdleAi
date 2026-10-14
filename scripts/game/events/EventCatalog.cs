using System;
using System.Collections.Generic;

namespace IdleAi;

public static class EventCatalog
{
    public const string TestEventId = "event_test_001";
    // Fixed UTC dates: never extend the event on restart. Disable/remove before a live event release.
    public static IReadOnlyList<EventDefinition> Create(long? halloweenStartUnix = null, long? halloweenEndUnix = null) =>
    [
        new EventDefinition
        {
            Id = TestEventId, Name = "Test Protocol", Type = EventType.Weekend,
            Description = "A test environment for temporary protocols. Generate Event Data, upgrade your generator and unlock milestones.",
            StartUnix = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds(),
            EndUnix = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds(),
            IconPath = "res://assets/map/room_coming_soon.png",
            Prestige = new() { Enabled = true },
            Generators =
            [
                new() { Id = "data_terminal", Name = "Data Terminal", Description = "Collects basic protocol data.", IconPath = "res://assets/machines/room_1/machine1.png" },
                new() { Id = "compute_rack", Name = "Compute Rack", Description = "A dedicated data processing rack.", IconPath = "res://assets/map/room_server_room.png",
                    BaseProduction = 25, BaseUpgradeCost = 250, Scaling = EventProductionScaling.Exponential,
                    Unlock = new() { RequiredCurrencyEarned = 100, CurrencyScope = EventUnlockCurrencyScope.CurrentRun } },
                new() { Id = "neural_server", Name = "Neural Server", Description = "Learns from the protocol's output.", IconPath = "res://assets/map/room_data_center.png",
                    BaseProduction = 500, BaseUpgradeCost = 5_000, Scaling = EventProductionScaling.Exponential,
                    Unlock = new() { RequiredCurrencyEarned = 5_000, CurrencyScope = EventUnlockCurrencyScope.CurrentRun,
                        RequiredGeneratorId = "compute_rack", RequiredGeneratorLevel = 5 } },
                new() { Id = "quantum_cluster", Name = "Quantum Cluster", Description = "Processes simultaneous protocol states.", IconPath = "res://assets/map/room_quantum_lab.png",
                    BaseProduction = 15_000, BaseUpgradeCost = 150_000, Scaling = EventProductionScaling.Exponential,
                    Unlock = new() { RequiredPrestigeCount = 1 } },
                new() { Id = "prototype_core", Name = "Prototype Core", Description = "The next generation protocol core.", IconPath = "res://assets/map/room_singularity.png",
                    BaseProduction = 500_000, BaseUpgradeCost = 5_000_000, Scaling = EventProductionScaling.Exponential,
                    Unlock = new() { RequiredPrestigeCount = 2 } }
            ],
            Milestones =
            [
                new("data_100", 100, new(EventRewardType.DeveloperCredits, 1)),
                new("data_500", 500, new(EventRewardType.DeveloperCredits, 2)),
                new("data_2500", 2500, new(EventRewardType.DeveloperCredits, 5)),
                new("data_10000", 10000, new(EventRewardType.DeveloperCredits, 10))
            ]
        },
        HalloweenEventDefinition.Create(halloweenStartUnix, halloweenEndUnix)
    ];
}
