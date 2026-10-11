using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IdleAi;

public sealed record AiArchiveEntry(string Id, string Name, string Detail, bool Complete,
    Texture2D? Icon = null, bool Concealed = false, Color? Accent = null);
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

/// <summary>A read-only projection of existing catalogs and progress. Owns no gameplay state.</summary>
public sealed class AiArchiveProgressService
{
    private readonly GameState _state;
    private readonly ProgressionService _progression;
    private readonly LabService _lab;
    private readonly Func<string, bool> _botOwned;
    private readonly Func<string, bool> _machineOwned;
    private readonly Func<string, bool> _botEquipped;
    private readonly Func<int, string, bool> _machineEquipped;
    private readonly Func<bool> _singularityUnlocked;

    public AiArchiveProgressService(GameState state, ProgressionService progression, LabService lab,
        Func<string, bool> botOwned, Func<string, bool> machineOwned,
        Func<string, bool>? botEquipped = null, Func<int, string, bool>? machineEquipped = null,
        Func<bool>? singularityUnlocked = null)
    {
        _state = state; _progression = progression; _lab = lab;
        _botOwned = botOwned; _machineOwned = machineOwned;
        _botEquipped = botEquipped ?? (_ => false);
        _machineEquipped = machineEquipped ?? ((_, _) => false);
        _singularityUnlocked = singularityUnlocked ?? (() => false);
    }

    public AiArchiveSnapshot Capture()
    {
        var bots = Enum.GetValues<BotRarity>().Select(rarity => {
            var bot = BotCatalog.Get(rarity);
            bool known = _state.RoomStates.Any(room => room.Slots.Any(slot => slot.BotRarity == rarity));
            return new AiArchiveEntry(rarity.ToString(), bot.Name, rarity.ToString(), known, bot.DefaultTexture, !known);
        }).ToList();
        var machines = new List<AiArchiveEntry>();
        var rooms = new List<AiArchiveEntry>();
        for (int r = 0; r < _state.Rooms.Count; r++)
        {
            var room = _state.Rooms[r];
            var progress = r < _state.RoomStates.Count ? _state.RoomStates[r] : null;
            bool known = progress != null && (progress.Unlocked || progress.DataShardUnlockRewardClaimed);
            rooms.Add(new($"room:{r}", room.Name, known ? "Discovered" : "Locked", known,
                room.Background, false, RoomThemePalette.GetAccentColor(r)));
            for (int tier = 0; tier < room.Machines.Count; tier++)
            {
                var machine = room.Machines[tier];
                bool reached = progress != null && progress.Slots.Any(slot => slot.Unlocked && slot.MachineTier >= tier);
                machines.Add(new($"machine:{r}:{tier}", machine.MachineName, $"{room.Name} · Tier {tier + 1}",
                    reached, machine.DefaultTexture, !reached));
            }
        }
        bool singularity = _singularityUnlocked();
        rooms.Add(new("singularity", "The Singularity", singularity ? "Discovered" : "Locked", singularity,
            Accent: new Color("e8bd61")));
        var skins = new List<AiArchiveEntry>();
        var botSkins = BotSkinCatalog.GetAll().ToList();
        if (_botOwned(BotSkinCatalog.KorpoSkinId) && BotSkinCatalog.TryGet(BotSkinCatalog.KorpoSkinId, out var secret))
            botSkins.Add(secret);
        foreach (var skin in botSkins)
        {
            bool owned = _botOwned(skin.Id);
            skins.Add(new("bot:" + skin.Id, skin.Name, "Bot skin · " + (owned ? (_botEquipped(skin.Id) ? "Equipped" : "Owned") : "Locked"),
                owned, skin.GetBotTexture(BotRarity.Common)));
        }
        foreach (var skin in MachineSkinCatalog.GetAll().Where(skin => skin.IsComplete))
        {
            bool owned = _machineOwned(skin.Id);
            string room = _state.Rooms[skin.RoomIndex].Name;
            skins.Add(new("machine:" + skin.Id, skin.Name, room + " · " + (owned ? (_machineEquipped(skin.RoomIndex, skin.Id) ? "Equipped" : "Owned") : "Locked"),
                owned, skin.GetMachineTexture(0)));
        }
        var research = ResearchCatalog.All.Select(definition => {
            bool complete = _state.Lab.IsResearchCompleted(definition.Id);
            string status = complete ? "Completed" : _state.Lab.ActiveResearchId == definition.Id ? "Researching"
                : _lab.IsResearchAvailable(definition) ? "Available" : "Locked / unavailable";
            return new AiArchiveEntry(definition.Id, definition.Name, $"{definition.Branch} · {status}", complete);
        }).ToList();
        var achievements = new List<AiArchiveEntry>();
        void Achievement(string id, string name, string condition) => achievements.Add(new(id, name, condition,
            GooglePlayGamesAchievementService.IsLocallyEarned(id, _state, _progression)));
        Achievement(GooglePlayGamesAchievementService.HelloWorldId, "Hello, World!", "Start your first machine.");
        Achievement(GooglePlayGamesAchievementService.AutomatedIntelligenceId, "Automated Intelligence", "Own your first bot.");
        Achievement(GooglePlayGamesAchievementService.MovingUpId, "Moving Up", "Reach total Level 150.");
        Achievement(GooglePlayGamesAchievementService.ResearcherId, "Researcher", "Complete your first research.");
        Achievement(GooglePlayGamesAchievementService.PrestigiousId, "Prestigious", "Perform your first Prestige.");
        Achievement(GooglePlayGamesAchievementService.ServerOnlineId, "Server Online", "Unlock Server Room.");
        Achievement(GooglePlayGamesAchievementService.QuantumLeapId, "Quantum Leap", "Unlock Quantum Lab.");
        return new(new AiArchiveCategory[] {
            new("BOTS", "Rarities of currently assigned bots. Sold bots are not tracked historically.", bots),
            new("MACHINES", "Machine tiers reached in this run. Higher tiers include earlier tiers.", machines),
            new("SKINS", "Owned cosmetic packs. Secret packs appear only after unlock.", skins),
            new("RESEARCH", "Completed research across Hardware, Robotics and Infrastructure.", research),
            new("ACHIEVEMENTS", "Local achievement conditions; online synchronization is separate.", achievements),
            new("ROOMS", "Unlocked rooms, including retained room reward history.", rooms)
        });
    }
}
