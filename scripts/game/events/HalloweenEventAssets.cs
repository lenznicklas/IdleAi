namespace IdleAi;

/// <summary>Case-sensitive source PNG paths. Shared by catalog content; UI has no Halloween path knowledge.</summary>
public static class HalloweenEventAssets
{
    private const string Root = "res://assets/halloween_event/";
    private const string BadgeRoot = "res://assets/event_badges/";
    public const string CorruptedSignalBadge = BadgeRoot + "halloween_corrupted_signal.png";
    public const string HauntedNetworkBadge = BadgeRoot + "halloween_haunted_network.png";
    // Match the supplied filename exactly (the source asset spells breach as brach).
    public const string ContainmentBreachBadge = BadgeRoot + "halloween_containment_brach.png";
    public const string Icon = Root + "event_icon.png";
    public const string Background = Root + "background.png";
    public const string Souls = Root + "souls.png";
    public const string CursedCore = Root + "cursed_core.png";
    public const string AbandonedTerminal = Root + "abandoned_terminal.png";
    public const string HauntedWorkstation = Root + "haunted_workstation.png";
    public const string PossessedServer = Root + "possessed_server.png";
    public const string CursedDatacenter = Root + "cursed_datacenter.png";
    public const string SoulProcessor = Root + "soul_processor.png";
    public const string NightmareAiCore = Root + "nightmare_ai_core.png";
    public const string GhostBot = Root + "ghost_bot.png";
    public const string PumpkinBot = Root + "pumpkin_bot.png";
    public const string SkeletonBot = Root + "skeleton_bot.png";
    public const string ReaperBot = Root + "reaper_bot.png";
    public const string RewardChest = Root + "reward_chest.png";
    public const string MachineSkinPack = Root + "machine_skin_pack.png";
    // Single-room skin art is an existing real cosmetic asset, not the four-room pack preview.
    public const string GarageSkin = "res://assets/machines/room_1/skin_halloween/machine_4.png";
    public const string BotSkinPack = Root + "bot_skin_pack.png";
}
