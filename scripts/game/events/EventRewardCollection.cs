using System.Collections.Generic;
using System.Linq;

namespace IdleAi;

/// <summary>Read-only catalog traversal for the Stats AI Archive; grants remain in EventManager.</summary>
public static class EventRewardCollection
{
    public static string DisplayName(EventReward reward) => reward.Name.Length > 0 ? reward.Name : reward.Id;

    public static IEnumerable<EventReward> Leaves(EventReward reward) => reward.Type == EventRewardType.Bundle
        ? reward.Contents.SelectMany(part => Leaves(part with
        {
            IconPath = part.IconPath.Length > 0 ? part.IconPath : reward.IconPath
        })) : new[] { reward };

    public static IEnumerable<EventReward> Rewards(EventDefinition definition) =>
        definition.Milestones.Select(m => m.Reward)
            .Concat(definition.Shop.Select(s => s.Reward))
            .Concat(definition.Challenges.Select(c => c.Reward))
            .SelectMany(Leaves);
}
