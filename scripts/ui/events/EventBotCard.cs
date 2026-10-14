using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IdleAi;

internal static class EventBotPresentation
{
    internal static Color RarityColor(string rarity) => Enum.Parse<BotRarity>(rarity) switch
    {
        BotRarity.Rare => EventUiStyle.Accent, BotRarity.Epic => Color.FromHtml("#b180ef"),
        BotRarity.Legendary => EventUiStyle.Ending, _ => Color.FromHtml("#adbac7")
    };
    internal static string Bonus(EventBotDefinition bot) => "+" + ((bot.ProductionMultiplier - 1) * 100).ToString("0.#") + "% Event production"
        + (bot.SecondaryBonus is { GeneratorUpgradeDiscount: > 0 } bonus ? "\n" + (bonus.GeneratorUpgradeDiscount * 100).ToString("0.#") + "% cheaper generator upgrades" : "");
    internal static string Requirement(EventDefinition d, EventBotDefinition bot)
    {
        if (bot.AvailableFromStart) return "Available from the start";
        var paths = new List<string>();
        foreach (var path in bot.UnlockPaths)
        {
            var parts = new List<string>();
            if (path.LifetimeCurrency > 0) parts.Add("Earn " + NumberFormatter.Format(path.LifetimeCurrency) + " lifetime " + d.CurrencyName);
            if (path.PrestigeCount > 0) parts.Add("Perform " + path.PrestigeCount + " Event Prestige" + (path.PrestigeCount > 1 ? "s" : ""));
            if (path.PrestigeCurrency > 0) parts.Add("Earn " + NumberFormatter.Format(path.PrestigeCurrency) + " " + d.Prestige.ResourceName);
            if (path.MilestoneId.Length > 0) parts.Add(Milestone(d, path.MilestoneId));
            if (path.ChallengeId.Length > 0) parts.Add("Complete challenge: " + (d.Challenges.FirstOrDefault(c => c.Id == path.ChallengeId)?.Title ?? "Event challenge"));
            if (path.ShopItemId.Length > 0) parts.Add("Purchase: " + (d.Shop.FirstOrDefault(s => s.Id == path.ShopItemId)?.Name ?? "Event Shop item"));
            paths.Add(parts.Count == 0 ? "Available immediately" : string.Join(" and ", parts));
        }
        if (paths.Count == 0 && bot.UnlockMilestoneId.Length > 0) paths.Add(Milestone(d, bot.UnlockMilestoneId));
        if (paths.Count == 0) paths.Add("Earn this bot from Event rewards or the Event Shop");
        return "Requires: " + string.Join("\nOR ", paths);
    }
    private static string Milestone(EventDefinition d, string id)
    {
        var milestone = d.Milestones.FirstOrDefault(m => m.Id == id);
        return milestone == null ? "Claim its Event reward" : "Claim the " + NumberFormatter.Format(milestone.TargetAmount) + " " + d.CurrencyName + " track reward";
    }
    internal static string Status(EventBotSnapshot s, bool active) => s.Equipped ? (active ? "EQUIPPED" : "EQUIPPED • INACTIVE")
        : s.Owned ? "OWNED" : s.Unlocked ? "UNLOCKED" : "LOCKED";
    internal static string Action(EventBotSnapshot s) => s.Equipped ? "EQUIPPED" : s.Owned ? "EQUIP" : s.Unlocked ? "ACQUIRE" : "LOCKED";
}

/// <summary>One cached, fixed-height bot card with bounded image/text slots.</summary>
internal sealed class EventBotCard
{
    internal Control Root { get; }
    private readonly Label _status;
    private readonly Button _equip;
    private EventBotSnapshot? _last;
    private bool _lastActive;
    internal EventBotCard(EventDefinition d, EventBotDefinition bot, EventTextureCache textures, Action details, Action equip)
    {
        var color = EventBotPresentation.RarityColor(bot.Rarity);
        Root = new Control { Name = "EventBot_" + bot.Id + "Card", CustomMinimumSize = new(0, 364), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        var panel = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        panel.AddThemeStyleboxOverride("panel", MapController.CreateNodeStyle(new(.035f, .025f, .05f, .97f), color, 2, 6)); Root.AddChild(panel);
        var image = new EventImageView(textures.Get(bot.IconPath), new(88, 112)) { Position = new(16, 18), Size = new(88, 112) }; Root.AddChild(image);
        Slot("BotName", bot.Name, 17, 116, 18, 70, 2);
        var rarity = Slot("BotRarity", bot.Rarity.ToUpperInvariant(), 13, 116, 78, 106); rarity.Modulate = color;
        Slot("BotBonus", EventBotPresentation.Bonus(bot), 13, 16, 140, 190, 2);
        Slot("BotRequirement", EventBotPresentation.Requirement(d, bot), 12, 16, 198, 266, 3);
        _status = Slot("BotStatus", "", 13, 16, 274, 300);
        var actions = new HBoxContainer(); actions.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopWide);
        actions.OffsetLeft = 16; actions.OffsetRight = -16; actions.OffsetTop = 308; actions.OffsetBottom = 352;
        actions.AddThemeConstantOverride("separation", 10); Root.AddChild(actions);
        var detail = EventUiStyle.Button("EventBotDetails_" + bot.Id, "Details", details);
        detail.CustomMinimumSize = new(0, 44); detail.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill; actions.AddChild(detail);
        _equip = EventUiStyle.Button("EventBot_" + bot.Id, "", equip);
        _equip.CustomMinimumSize = new(0, 44); _equip.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill; EventUiStyle.StyleButton(_equip, color); actions.AddChild(_equip);
        Root.GuiInput += input => { if (input is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false }) details(); };
    }
    private Label Slot(string name, string text, int size, int left, int top, int bottom, int lines = 1)
    {
        var label = EventUiStyle.Label(name, text, size); label.MouseFilter = Control.MouseFilterEnum.Ignore;
        label.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopWide);
        label.OffsetLeft = left; label.OffsetRight = -16; label.OffsetTop = top; label.OffsetBottom = bottom;
        label.ClipText = true; label.MaxLinesVisible = lines; label.AutowrapMode = lines > 1 ? TextServer.AutowrapMode.WordSmart : TextServer.AutowrapMode.Off;
        label.TooltipText = text; Root.AddChild(label); return label;
    }
    internal void Refresh(EventBotSnapshot snapshot, bool active)
    {
        if (_last == snapshot && _lastActive == active) return;
        _last = snapshot; _lastActive = active;
        EventUiStyle.SetText(_status, EventBotPresentation.Status(snapshot, active));
        EventUiStyle.SetText(_equip, EventBotPresentation.Action(snapshot));
        EventUiStyle.SetDisabled(_equip, !snapshot.CanEquip && !snapshot.CanAcquire);
    }
}
