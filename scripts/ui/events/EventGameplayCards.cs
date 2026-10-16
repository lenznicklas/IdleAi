using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IdleAi;

internal static class EventPresentation
{
    internal static string UnlockText(EventUnlockCondition c, EventDefinition d, IReadOnlyList<EventGeneratorDefinition> generators)
    {
        var parts = new List<string>(3);
        if (c.RequiredCurrencyEarned > 0) parts.Add(NumberFormatter.Format(c.RequiredCurrencyEarned) + " " + d.CurrencyName
            + (c.CurrencyScope == EventUnlockCurrencyScope.CurrentRun ? " earned this run" : " lifetime earned"));
        if (c.RequiredPrestigeCount > 0) parts.Add("Event Prestige " + c.RequiredPrestigeCount);
        if (!string.IsNullOrEmpty(c.RequiredGeneratorId))
        {
            string name = c.RequiredGeneratorId;
            foreach (var g in generators) if (g.Id == c.RequiredGeneratorId) { name = g.Name; break; }
            parts.Add(name + " Lv " + c.RequiredGeneratorLevel);
        }
        return parts.Count == 0 ? "Available immediately" : "Requires: " + string.Join("\n", parts);
    }
    // Names come from the same reward config as AI Archive. Badge art remains in Stats.
    internal static bool HasBadge(EventReward reward) => EventRewardCollection.Leaves(reward).Any(r => r.Type == EventRewardType.Cosmetic);
    internal static string RewardIcon(EventReward reward, EventDefinition d) => HasBadge(reward) ? d.IconPath
        : reward.IconPath.Length > 0 ? reward.IconPath : d.IconPath;
    internal static string RewardName(EventReward reward, EventDefinition d) => HasBadge(reward)
        ? reward.Type == EventRewardType.Bundle
            ? string.Join(" + ", reward.Contents.Select(r => RewardName(r, d))) : EventRewardCollection.DisplayName(reward)
        : !string.IsNullOrEmpty(reward.Name) ? reward.Name :
        NumberFormatter.Format(reward.Amount) + " " + (reward.Type switch
        {
            EventRewardType.DeveloperCredits => "developer credits", EventRewardType.EventCurrency => d.CurrencyName,
            EventRewardType.PrestigeResource => d.Prestige.ResourceName, EventRewardType.DataShards => "Data Shards",
            _ => reward.Id
        });
}

/// <summary>Fixed-height card; dynamic text is clipped within fixed slots, never resizing the list.</summary>
internal sealed class EventGeneratorCard
{
    internal Control Root { get; }
    private readonly Label _level, _output, _requirements;
    private readonly Button _upgrade;
    private readonly ProgressBar _cycleProgress;
    private readonly EventGeneratorDefinition _definition;
    private readonly string _unlockText;
    private readonly EventImageView _costIcon;
    private readonly bool _hasCostIcon;
    internal EventGeneratorCard(EventDefinition eventDefinition, EventGeneratorDefinition generator, EventTextureCache textures,
        IReadOnlyList<EventGeneratorDefinition> generators, Action upgrade, bool first)
    {
        _definition = generator;
        _unlockText = EventPresentation.UnlockText(generator.Unlock, eventDefinition, generators);
        Root = new Control { Name = "EventGenerator_" + generator.Id, CustomMinimumSize = new(0, 490), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        var panel = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        panel.AddThemeStyleboxOverride("panel", MapController.CreateNodeStyle(new(.04f, .025f, .065f, .97f), Color.FromHtml(eventDefinition.Theme.AccentHex), 2, 6));
        Root.AddChild(panel);
        var icon = new EventImageView(textures.Get(generator.IconPath), new(240, 160));
        icon.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopWide);
        icon.OffsetLeft = 18; icon.OffsetRight = -18; icon.OffsetTop = 66; icon.OffsetBottom = 226;
        Root.AddChild(icon);
        Slot("GeneratorName", generator.Name, 18, 18, 16, 60, 2);
        Slot("GeneratorDescription", generator.Description, 12, 18, 234, 278, 2);
        _level = Slot("GeneratorLevel", "", 15, 18, 286, 312);
        _output = Slot("GeneratorOutput", "", 13, 18, 318, 344);
        _requirements = Slot("GeneratorRequirements", "", 12, 18, 350, 407, 3);
        var costTexture = textures.Get(eventDefinition.CurrencyIconPath); _hasCostIcon = costTexture != null;
        _costIcon = new EventImageView(costTexture, new(24, 24))
            { Name = "GeneratorCostIcon", Position = new(18, 350), Size = new(24, 24), Visible = false }; Root.AddChild(_costIcon);
        var accent = Color.FromHtml(eventDefinition.Theme.AccentHex);
        _cycleProgress = new ProgressBar { Name = "EventMachineCycleProgress_" + generator.Id,
            ShowPercentage = false, MinValue = 0, MaxValue = 1, Step = 0,
            MouseFilter = Control.MouseFilterEnum.Ignore, Visible = false };
        _cycleProgress.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopWide);
        _cycleProgress.OffsetLeft = 18; _cycleProgress.OffsetRight = -18;
        _cycleProgress.OffsetTop = 400; _cycleProgress.OffsetBottom = 412;
        _cycleProgress.AddThemeStyleboxOverride("background", MapController.CreateNodeStyle(new(.04f, .05f, .07f), EventUiStyle.Muted, 0, 0));
        _cycleProgress.AddThemeStyleboxOverride("fill", MapController.CreateNodeStyle(accent, accent, 0, 0));
        Root.AddChild(_cycleProgress);
        _upgrade = EventUiStyle.Button(first ? "EventUpgradeButton" : "EventUpgrade_" + generator.Id, "", upgrade);
        _upgrade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopWide);
        _upgrade.OffsetLeft = 18; _upgrade.OffsetRight = -18; _upgrade.OffsetTop = 424; _upgrade.OffsetBottom = 472;
        _upgrade.CustomMinimumSize = new(0, 48);
        EventUiStyle.StyleButton(_upgrade, Color.FromHtml(eventDefinition.Theme.AccentHex));
        Root.AddChild(_upgrade);
    }
    private Label Slot(string name, string text, int size, float left, float top, float bottom, int lines = 1)
    {
        var label = EventUiStyle.Label(name, text, size);
        label.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopWide);
        label.OffsetLeft = left; label.OffsetRight = -18; label.OffsetTop = top; label.OffsetBottom = bottom;
        label.ClipText = true; label.MaxLinesVisible = lines;
        label.AutowrapMode = lines > 1 ? TextServer.AutowrapMode.WordSmart : TextServer.AutowrapMode.Off;
        Root.AddChild(label);
        return label;
    }
    internal void RefreshCycle(double progress) => _cycleProgress.Value = progress;

    internal void Refresh(EventGeneratorSnapshot state, bool ended, string currency)
    {
        _cycleProgress.Visible = state.Unlocked && _definition.Enabled;
        _requirements.OffsetBottom = _cycleProgress.Visible ? 392 : 407;
        EventUiStyle.SetText(_level, !_definition.Enabled ? "DISABLED" : state.Unlocked ? "Level " + state.Level : "LOCKED");
        EventUiStyle.SetText(_output, "Output: " + NumberFormatter.Format(state.ProductionPerCycle) + " / cycle • "
            + NumberFormatter.Format(state.ProductionPerSecond) + "/s  (" + _definition.CycleSeconds.ToString("0.#") + "s)");
        EventUiStyle.SetText(_requirements, !state.Unlocked ? _unlockText : "Next upgrade: " + NumberFormatter.Format(state.UpgradeCost) + " " + currency);
        _costIcon.Visible = state.Unlocked && _definition.Enabled && _hasCostIcon;
        _requirements.OffsetLeft = _costIcon.Visible ? 50 : 18;
        _requirements.TooltipText = !state.Unlocked ? _unlockText : "";
        EventUiStyle.SetText(_upgrade, ended ? "EVENT ENDED" : !state.Unlocked ? "LOCKED" : state.Level >= _definition.MaxLevel ? "MAX LEVEL" : "Upgrade • " + NumberFormatter.Format(state.UpgradeCost));
        EventUiStyle.SetDisabled(_upgrade, !state.CanUpgrade);
    }
}

internal sealed class EventItemCard
{
    internal Control Root { get; }
    internal Button Button { get; }
    private readonly Label _info;
    internal EventItemCard(string id, string title, string iconPath, EventTextureCache textures, Color accent, Action action)
    {
        Root = new Control { Name = id + "Card", CustomMinimumSize = new(0, 220), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        var panel = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        panel.AddThemeStyleboxOverride("panel", MapController.CreateNodeStyle(new(.035f, .025f, .05f, .97f), accent, 2, 6));
        Root.AddChild(panel);
        var image = new EventImageView(textures.Get(iconPath), new(78, 98))
        { Position = new(18, 18), Size = new(78, 98) };
        Root.AddChild(image);
        var name = EventUiStyle.Label("ItemName", title, 15);
        name.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopWide);
        name.OffsetLeft = 112; name.OffsetRight = -18; name.OffsetTop = 15; name.OffsetBottom = 58;
        name.ClipText = true; name.AutowrapMode = TextServer.AutowrapMode.WordSmart; name.MaxLinesVisible = 2;
        Root.AddChild(name);
        _info = EventUiStyle.Label("ItemInfo", "", 12);
        _info.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopWide);
        _info.OffsetLeft = 112; _info.OffsetRight = -18; _info.OffsetTop = 62; _info.OffsetBottom = 138;
        _info.ClipText = true; _info.AutowrapMode = TextServer.AutowrapMode.WordSmart; _info.MaxLinesVisible = 3;
        Root.AddChild(_info);
        Button = EventUiStyle.Button(id, "", action);
        Button.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopWide);
        Button.OffsetLeft = 18; Button.OffsetRight = -18; Button.OffsetTop = 156; Button.OffsetBottom = 204;
        Button.CustomMinimumSize = new(0, 48);
        EventUiStyle.StyleButton(Button, accent);
        Root.AddChild(Button);
    }
    internal void Refresh(string info, string action, bool enabled)
    {
        EventUiStyle.SetText(_info, info);
        _info.TooltipText = info;
        EventUiStyle.SetText(Button, action);
        EventUiStyle.SetDisabled(Button, !enabled);
    }
}
