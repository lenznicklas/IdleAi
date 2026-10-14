using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IdleAi;

internal static class EventShopPresentation
{
    internal static string CurrencyName(EventDefinition d, EventShopCurrency currency) => currency switch
    {
        EventShopCurrency.EventCurrency => d.CurrencyName, EventShopCurrency.PrestigeResource => d.Prestige.ResourceName,
        EventShopCurrency.DataShards => "Data Shards", _ => "Unavailable currency"
    };
    internal static string CurrencyIcon(EventDefinition d, EventShopItem item) => item.CostIconPath.Length > 0 ? item.CostIconPath : item.CostCurrency switch
    {
        EventShopCurrency.DataShards => "res://assets/shop/data_shard.png",
        EventShopCurrency.PrestigeResource => d.Prestige.IconPath,
        _ => d.CurrencyIconPath.Length > 0 ? d.CurrencyIconPath : "res://assets/shop/shop_production.png"
    };
    internal static string Requirements(EventDefinition d, EventShopItem item)
    {
        var parts = new List<string>();
        var a = item.Availability; var u = item.Unlock;
        if (a.RequiredCurrencyEarned > 0) parts.Add("Earn " + NumberFormatter.Format(a.RequiredCurrencyEarned) + " " + d.CurrencyName
            + (a.CurrencyScope == EventUnlockCurrencyScope.CurrentRun ? " this run" : " lifetime"));
        if (a.RequiredPrestigeCount > 0) parts.Add("Event Prestige " + a.RequiredPrestigeCount);
        if (u.RequiredPrestigeResource > 0) parts.Add("Hold " + NumberFormatter.Format(u.RequiredPrestigeResource) + " " + d.Prestige.ResourceName);
        if (a.RequiredGeneratorId.Length > 0) parts.Add((d.Generators.FirstOrDefault(g => g.Id == a.RequiredGeneratorId)?.Name ?? d.Progression.GeneratorName) + " level " + a.RequiredGeneratorLevel);
        if (u.RequiredBotId.Length > 0) parts.Add("Unlock " + (d.Bots.FirstOrDefault(b => b.Id == u.RequiredBotId)?.Name ?? "Event bot"));
        if (u.RequiredChallengeId.Length > 0) parts.Add("Complete " + (d.Challenges.FirstOrDefault(c => c.Id == u.RequiredChallengeId)?.Title ?? "Event challenge"));
        var milestone = d.Milestones.FirstOrDefault(m => m.Id == u.RequiredMilestoneId);
        if (milestone != null) parts.Add("Reach the " + NumberFormatter.Format(milestone.TargetAmount) + " " + d.CurrencyName + " track reward");
        return parts.Count == 0 ? "Available from the start" : "Requires: " + string.Join(" • ", parts);
    }
    internal static string Reward(EventReward reward, EventDefinition d)
    {
        if (reward.Name.Length > 0) return reward.Name;
        return reward.Type switch
        {
            EventRewardType.EventBot => d.Bots.FirstOrDefault(b => b.Id == reward.Id)?.Name ?? "Event Bot unlock",
            EventRewardType.Cosmetic or EventRewardType.MachineSkin or EventRewardType.BotSkin => "Permanent cosmetic unlock",
            EventRewardType.Custom => "Special Event reward", EventRewardType.Bundle => string.Join(" + ", reward.Contents.Select(r => Reward(r, d))),
            _ => EventPresentation.RewardName(reward, d)
        };
    }
    internal static string State(EventShopState state) => state switch
    {
        EventShopState.Available => "AVAILABLE", EventShopState.InsufficientCurrency => "NOT ENOUGH CURRENCY",
        EventShopState.Owned => "OWNED", EventShopState.SoldOut => "SOLD OUT", EventShopState.Locked => "LOCKED",
        EventShopState.EventEnded => "EVENT ENDED", _ => "UNAVAILABLE"
    };
}

/// <summary>Cached fixed-slot card. Only purchase state changes; images and text never rebuild on ticks.</summary>
internal sealed class EventShopCard
{
    internal Control Root { get; }
    private readonly Label _status, _limit, _requirements;
    private readonly Button _buy;
    private readonly string _requirementText;
    private readonly Action<int> _purchase;
    private EventShopSnapshot _snapshot;
    private bool _busy;
    private ulong? _lastPurchaseInput;
    internal EventShopCard(EventDefinition d, EventShopItem item, EventTextureCache textures, Action<int> purchase)
    {
        _purchase = purchase;
        var accent = item.Featured ? EventUiStyle.Ending : Color.FromHtml(d.Theme.AccentHex);
        Root = new Control { Name = "EventShop_" + item.Id + "Card", CustomMinimumSize = new(0, 456), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        var panel = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        panel.AddThemeStyleboxOverride("panel", MapController.CreateNodeStyle(new(.035f, .025f, .05f, .97f), accent, 2, item.Featured ? 9 : 5)); Root.AddChild(panel);
        var image = new EventImageView(textures.Get(item.IconPath), new(88, 108)) { Position = new(16, 18), Size = new(88, 108) }; Root.AddChild(image);
        Slot("ShopItemName", item.Name, 17, 116, 18, 70, 2);
        Slot("ShopItemCategory", (item.Featured ? "FEATURED • " : "") + item.Category.ToUpperInvariant(), 12, 116, 78, 122, 2).Modulate = accent;
        Slot("ShopItemDescription", item.Description, 13, 16, 138, 196, 3);
        Slot("ShopItemReward", "Reward: " + EventShopPresentation.Reward(item.Reward, d), 13, 16, 202, 242, 2);
        var currency = new EventImageView(textures.Get(EventShopPresentation.CurrencyIcon(d, item)), new(26, 26)) { Position = new(16, 250), Size = new(26, 26) }; Root.AddChild(currency);
        Slot("ShopItemPrice", NumberFormatter.Format(item.Cost) + " " + EventShopPresentation.CurrencyName(d, item.CostCurrency), 15, 52, 250, 280);
        _requirementText = EventShopPresentation.Requirements(d, item);
        _requirements = Slot("ShopItemRequirement", "", 12, 16, 286, 340, 3);
        _limit = Slot("ShopItemLimit", "", 12, 16, 344, 368);
        _status = Slot("ShopItemStatus", "", 12, 16, 372, 392); _status.Modulate = accent;
        _buy = EventUiStyle.Button("EventShop_" + item.Id, "BUY", Purchase);
        _buy.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopWide);
        _buy.OffsetLeft = 16; _buy.OffsetRight = -16; _buy.OffsetTop = 396; _buy.OffsetBottom = 440;
        _buy.CustomMinimumSize = new(0, 44); EventUiStyle.StyleButton(_buy, accent); Root.AddChild(_buy);
    }
    private Label Slot(string name, string text, int size, int left, int top, int bottom, int lines = 1)
    {
        var label = EventUiStyle.Label(name, text, size); label.MouseFilter = Control.MouseFilterEnum.Ignore;
        label.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopWide);
        label.OffsetLeft = left; label.OffsetRight = -16; label.OffsetTop = top; label.OffsetBottom = bottom;
        label.ClipText = true; label.MaxLinesVisible = lines;
        label.AutowrapMode = lines > 1 ? TextServer.AutowrapMode.WordSmart : TextServer.AutowrapMode.Off;
        label.TooltipText = text; Root.AddChild(label); return label;
    }
    private void Purchase()
    {
        ulong now = Time.GetTicksMsec();
        // Local input debounce; no timers/tasks survive a scene change.
        if (_busy || !_snapshot.CanBuy || _lastPurchaseInput.HasValue && now - _lastPurchaseInput.Value < 300) return;
        _lastPurchaseInput = now;
        _busy = true; EventUiStyle.SetDisabled(_buy, true);
        try
        {
            _purchase(_snapshot.PurchasedCount);
        }
        finally
        {
            _busy = false;
            if (GodotObject.IsInstanceValid(Root) && Root.IsInsideTree()) EventUiStyle.SetDisabled(_buy, !_snapshot.CanBuy);
        }
    }
    internal void Refresh(EventShopSnapshot snapshot)
    {
        _snapshot = snapshot;
        EventUiStyle.SetText(_status, (snapshot.State == EventShopState.EventEnded && snapshot.Owned ? "OWNED • " : "") + EventShopPresentation.State(snapshot.State));
        EventUiStyle.SetText(_limit, snapshot.PurchaseLimit == 0 ? snapshot.PurchasedCount + " purchased • No limit" : snapshot.PurchasedCount + " / " + snapshot.PurchaseLimit + " purchased");
        EventUiStyle.SetText(_requirements, snapshot.State == EventShopState.Locked ? _requirementText : "");
        _requirements.TooltipText = _requirementText;
        EventUiStyle.SetText(_buy, snapshot.CanBuy ? "BUY" : EventShopPresentation.State(snapshot.State));
        EventUiStyle.SetDisabled(_buy, _busy || !snapshot.CanBuy);
    }
}
