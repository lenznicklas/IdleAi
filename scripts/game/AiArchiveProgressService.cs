using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IdleAi;

public sealed record AiArchiveEntry(string Id, string Name, string Detail, bool Complete,
    Texture2D? Icon = null, bool Concealed = false, Color? Accent = null, int BadgeNumber = 0);
public sealed record AiArchiveCategory(string Name, string Description, IReadOnlyList<AiArchiveEntry> Entries)
{
    public int Completed => Entries.Count(entry => entry.Complete);
    public int Total => Entries.Count;
}
public sealed record AiArchiveSnapshot(IReadOnlyList<AiArchiveCategory> Categories)
{
    public int Completed => Categories.Sum(category => category.Completed);
    public int Total => Categories.Sum(category => category.Total);
    public int Percent => Total == 0 ? 0 : (int)Math.Floor(100.0 * Completed / Total);
}

/// <summary>Projects permanent collection history, seeding legacy discoveries from proven state.</summary>
public sealed class AiArchiveProgressService
{
    private readonly GameState _state;
    private readonly ProgressionService _progression;
    private readonly LabService _lab;
    private readonly Func<string, bool> _botOwned;
    private readonly Func<string, bool> _machineOwned;
    private readonly Func<bool> _singularityUnlocked;

    public AiArchiveProgressService(GameState state, ProgressionService progression, LabService lab,
        Func<string, bool> botOwned, Func<string, bool> machineOwned,
        Func<string, bool>? botEquipped = null, Func<int, string, bool>? machineEquipped = null,
        Func<bool>? singularityUnlocked = null)
    {
        _state = state; _progression = progression; _lab = lab;
        _botOwned = botOwned; _machineOwned = machineOwned;
        _singularityUnlocked = singularityUnlocked ?? (() => false);
    }

    public AiArchiveSnapshot Capture()
    {
        AiArchiveDiscovery.ObserveRun(_state);
        AiArchiveDiscovery.ObservePermanentRewards(_state);
        // Legacy cosmetic ownership is evidence for discovery, never the display source.
        foreach (var skin in BotSkinCatalog.GetAll())
            if (_botOwned(skin.Id)) _state.Archive.Discover(AiArchiveDiscovery.BotSkin(skin.Id));
        if (_botOwned(BotSkinCatalog.KorpoSkinId)) _state.Archive.Discover(AiArchiveDiscovery.BotSkin(BotSkinCatalog.KorpoSkinId));
        foreach (var skin in MachineSkinCatalog.GetAll())
            if (_machineOwned(skin.Id)) _state.Archive.Discover(AiArchiveDiscovery.MachineSkin(skin.Id));
        var bots = Enum.GetValues<BotRarity>().Select(rarity => {
            var bot = BotCatalog.Get(rarity);
            bool known = _state.Archive.Contains(AiArchiveDiscovery.Bot(rarity));
            return new AiArchiveEntry(rarity.ToString(), bot.Name, rarity.ToString(), known, bot.DefaultTexture, !known);
        }).ToList();
        var machines = new List<AiArchiveEntry>();
        var rooms = new List<AiArchiveEntry>();
        for (int r = 0; r < _state.Rooms.Count; r++)
        {
            var room = _state.Rooms[r];
            bool known = _state.Archive.Contains("room:" + r);
            rooms.Add(new($"room:{r}", room.Name, known ? "Discovered" : "Locked", known,
                room.Background, false, RoomThemePalette.GetAccentColor(r)));
            for (int tier = 0; tier < room.Machines.Count; tier++)
            {
                var machine = room.Machines[tier];
                bool reached = _state.Archive.Contains(AiArchiveDiscovery.Machine(r, tier));
                machines.Add(new($"machine:{r}:{tier}", machine.MachineName, $"{room.Name} · Tier {tier + 1}",
                    reached, machine.DefaultTexture, !reached));
            }
        }
        if (_singularityUnlocked()) _state.Archive.Discover("singularity");
        bool singularity = _state.Archive.Contains("singularity");
        rooms.Add(new("singularity", "The Singularity", singularity ? "Discovered" : "Locked", singularity,
            Accent: new Color("e8bd61")));
        var botSkinEntries = new List<AiArchiveEntry>();
        var machineSkinEntries = new List<AiArchiveEntry>();
        var botSkins = BotSkinCatalog.GetAll().ToList();
        if (_state.Archive.Contains(AiArchiveDiscovery.BotSkin(BotSkinCatalog.KorpoSkinId)) && BotSkinCatalog.TryGet(BotSkinCatalog.KorpoSkinId, out var secret))
            botSkins.Add(secret);
        foreach (var skin in botSkins)
        {
            bool owned = _state.Archive.Contains(AiArchiveDiscovery.BotSkin(skin.Id));
            botSkinEntries.Add(new("bot:" + skin.Id, skin.Name, "Bot skin · " + (owned ? "Unlocked permanently" : "Locked"),
                owned, skin.GetBotTexture(BotRarity.Common)));
        }
        foreach (var skin in MachineSkinCatalog.GetAll().Where(skin => skin.IsComplete))
        {
            bool owned = _state.Archive.Contains(AiArchiveDiscovery.MachineSkin(skin.Id));
            string room = _state.Rooms[skin.RoomIndex].Name;
            machineSkinEntries.Add(new("machine:" + skin.Id, skin.Name, room + " · " + (owned ? "Unlocked permanently" : "Locked"),
                owned, skin.GetMachineTexture(0)));
        }
        var research = ResearchCatalog.All.Select(definition => {
            bool complete = _state.Lab.IsResearchCompleted(definition.Id);
            string status = complete ? "Completed" : _state.Lab.ActiveResearchId == definition.Id ? "Researching"
                : _lab.IsResearchAvailable(definition) ? "Available" : "Locked / unavailable";
            return new AiArchiveEntry(definition.Id, definition.Name, $"{definition.Branch} · {status}", complete);
        }).ToList();
        var achievements = new List<AiArchiveEntry>();
        void Achievement(string id, string name, string condition)
        {
            if (GooglePlayGamesAchievementService.IsLocallyEarned(id, _state, _progression)
                || id == GooglePlayGamesAchievementService.AutomatedIntelligenceId && bots.Any(bot => bot.Complete))
                _state.Archive.Discover("achievement:" + id);
            achievements.Add(new(id, name, condition, _state.Archive.Contains("achievement:" + id)));
        }
        Achievement(GooglePlayGamesAchievementService.HelloWorldId, "Hello, World!", "Start your first machine.");
        Achievement(GooglePlayGamesAchievementService.AutomatedIntelligenceId, "Automated Intelligence", "Own your first bot.");
        Achievement(GooglePlayGamesAchievementService.MovingUpId, "Moving Up", "Reach total Level 150.");
        Achievement(GooglePlayGamesAchievementService.ResearcherId, "Researcher", "Complete your first research.");
        Achievement(GooglePlayGamesAchievementService.PrestigiousId, "Prestigious", "Perform your first Prestige.");
        Achievement(GooglePlayGamesAchievementService.ServerOnlineId, "Server Online", "Unlock Server Room.");
        Achievement(GooglePlayGamesAchievementService.QuantumLeapId, "Quantum Leap", "Unlock Quantum Lab.");
        var badges = EventCatalog.Create().Where(d => d.Enabled || _state.Events.ProgressByEventId.ContainsKey(d.Id)
                || _state.Archive.Discovered.Any(id => id.StartsWith("badge:" + d.Id + ":", StringComparison.Ordinal)))
            .SelectMany(d => EventRewardCollection.Rewards(d).Where(r => r.Type == EventRewardType.Cosmetic)
                .DistinctBy(r => r.Id).Select(reward => {
                    bool owned = _state.Archive.Contains(AiArchiveDiscovery.Badge(d.Id, reward.Id));
                    return new AiArchiveEntry(d.Id + ":" + reward.Id,
                        EventRewardCollection.DisplayName(reward),
                        d.Name + " · " + (owned ? "Unlocked permanently" : "Locked"), owned,
                        reward.IconPath.Length > 0 ? GD.Load<Texture2D>(reward.IconPath) : null);
                })).ToList();
        var normalBadges = new List<AiArchiveEntry>();
        int levelTier = _state.Archive.HighestLevelBadgeTier;
        if (levelTier > 0)
            normalBadges.Add(new("level_badge", "Level Badge", $"Tier {levelTier} · Reached {(long)levelTier * LevelBadgeProgress.LevelInterval} total levels",
                true, GD.Load<Texture2D>(LevelBadgeProgress.IconPath), BadgeNumber: levelTier));
        return new(new AiArchiveCategory[] {
            new("BOTS", "Bot rarities obtained at least once, including sold or removed bots.", bots),
            new("MACHINES", "Machine tiers discovered across all runs. Prestige keeps discoveries.", machines),
            new("MACHINE SKINS", "Machine skins unlocked at least once.", machineSkinEntries),
            new("BOT SKINS", "Bot skins unlocked at least once. Secret packs appear after discovery.", botSkinEntries),
            new("RESEARCH", "Completed research across Hardware, Robotics and Infrastructure.", research),
            new("ACHIEVEMENTS", "Discovered local achievements; online synchronization is separate.", achievements),
            new("BADGES", "Permanent non-event badges. Level Badge upgrades every 150 total levels.", normalBadges),
            new("EVENT BADGES", "Permanent event collectibles, including past events.", badges),
            new("ROOMS", "Unlocked rooms, including retained room reward history.", rooms)
        });
    }
}
