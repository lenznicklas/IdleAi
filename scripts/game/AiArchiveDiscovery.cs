using System;

namespace IdleAi;

/// <summary>Seeds durable discoveries from proven save evidence; never grants gameplay rewards.</summary>
public static class AiArchiveDiscovery
{
    public static string Machine(int room, int tier) => $"machine:{room}:{tier}";
    public static string Bot(BotRarity rarity) => "bot:" + rarity;
    public static string BotSkin(string id) => "bot-skin:" + id;
    public static string MachineSkin(string id) => "machine-skin:" + id;
    public static string Badge(string eventId, string id) => "badge:" + eventId + ":" + id;

    public static void ObserveRun(GameState state)
    {
        LevelBadgeProgress.Update(state);
        for (int r = 0; r < Math.Min(state.Rooms.Count, state.RoomStates.Count); r++)
        {
            var progress = state.RoomStates[r];
            if (progress.Unlocked || progress.DataShardUnlockRewardClaimed) state.Archive.Discover("room:" + r);
            // Legacy room rewards and lifetime machine spending survive Prestige.
            if (progress.DataShardUnlockRewardClaimed && state.Rooms[r].Machines.Count > 0)
                state.Archive.Discover(Machine(r, 0));
            for (int tier = 0; tier < state.Rooms[r].Machines.Count; tier++)
                if (state.Stats.GetMachineSpending(state.Rooms[r].Machines[tier].MachineName) > 0)
                {
                    state.Archive.Discover("room:" + r);
                    for (int previous = 0; previous <= tier; previous++) state.Archive.Discover(Machine(r, previous));
                }
            foreach (var slot in progress.Slots)
            {
                if (slot.BotRarity is { } rarity) state.Archive.Discover(Bot(rarity));
                if (!slot.Unlocked) continue;
                for (int tier = 0; tier <= Math.Min(slot.MachineTier, state.Rooms[r].Machines.Count - 1); tier++)
                    state.Archive.Discover(Machine(r, tier));
            }
        }
    }

    // Run only after load or from committed UI state, never while an Event reward
    // transaction is being saved: failed claims must not leak discoveries.
    public static void ObservePermanentRewards(GameState state)
    {
        foreach (string entitlement in state.Events.CosmeticEntitlements)
        {
            if (entitlement.StartsWith("bot:", StringComparison.Ordinal)) state.Archive.Discover(BotSkin(entitlement[4..]));
            if (entitlement.StartsWith("machine:", StringComparison.Ordinal)) state.Archive.Discover(MachineSkin(entitlement[8..]));
        }
        foreach (var pair in state.Events.ProgressByEventId)
            foreach (string id in pair.Value.Cosmetics)
            {
                if (id.StartsWith("bot:", StringComparison.Ordinal)) state.Archive.Discover(BotSkin(id[4..]));
                else if (id.StartsWith("machine:", StringComparison.Ordinal)) state.Archive.Discover(MachineSkin(id[8..]));
                else state.Archive.Discover(Badge(pair.Key, id));
            }
    }
}
