using System;
using System.Collections.Generic;
using System.Linq;

namespace IdleAi;

public sealed class EventChallengeProgress
{
    public double Progress { get; set; }
    public double Baseline { get; set; }
    public bool Complete { get; set; }
    public bool Claimed { get; set; }
    public int TimesClaimed { get; set; }
    public EventChallengeProgress Copy() => (EventChallengeProgress)MemberwiseClone();
    public bool IsValid() => double.IsFinite(Progress) && Progress >= 0 && double.IsFinite(Baseline) && Baseline >= 0
        && TimesClaimed >= 0 && (!Claimed || Complete && TimesClaimed > 0);
}

public sealed class EventChallengeSaveData
{
    public long TotalGeneratorUpgrades { get; set; }
    public double ActivePlaySeconds { get; set; }
    public long DailyUtcDay { get; set; } = -1;
    public List<string> DailyIds { get; set; } = [];
    public Dictionary<string, EventChallengeProgress> Progress { get; set; } = [];
    // Retain earned/claimed previous days, using "UTC-day:challenge-id" as the key.
    public Dictionary<string, EventChallengeProgress> DailyHistory { get; set; } = [];
    public long Revision { get; set; }
    public EventChallengeSaveData Copy() => new()
    {
        TotalGeneratorUpgrades = TotalGeneratorUpgrades, ActivePlaySeconds = ActivePlaySeconds,
        DailyUtcDay = DailyUtcDay, DailyIds = new(DailyIds), Revision = Revision,
        Progress = Progress.ToDictionary(p => p.Key, p => p.Value.Copy()),
        DailyHistory = DailyHistory.ToDictionary(p => p.Key, p => p.Value.Copy())
    };
    public bool IsValid() => TotalGeneratorUpgrades >= 0 && double.IsFinite(ActivePlaySeconds) && ActivePlaySeconds >= 0
        && DailyUtcDay >= -1 && DailyUtcDay <= 3652058 && Revision >= 0 && DailyIds != null && DailyIds.All(id => !string.IsNullOrWhiteSpace(id))
        && DailyIds.Distinct(StringComparer.Ordinal).Count() == DailyIds.Count
        && Valid(Progress) && Valid(DailyHistory) && DailyIds.All(Progress.ContainsKey);
    private static bool Valid(Dictionary<string, EventChallengeProgress>? values) => values != null
        && values.All(p => !string.IsNullOrWhiteSpace(p.Key) && p.Value != null && p.Value.IsValid());
}
