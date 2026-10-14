using Godot;
using System;

namespace IdleAi;

/// <summary>Reused modal. No tween, scaling, border resizing or gameplay initialization.</summary>
internal sealed class EventBotDetailPanel
{
    private readonly Control _overlay;
    private readonly Label _name, _rarity, _description, _bonus, _requirement, _status;
    private readonly EventImageView _image;
    private readonly Button _action;
    private readonly EventManager _manager;
    private readonly EventTextureCache _textures;
    private readonly Action<string> _equip;
    private string? _eventId, _botId;
    internal bool Visible => _overlay.Visible;
    internal event Action? VisibilityChanged;
    internal EventBotDetailPanel(Control page, EventManager manager, EventTextureCache textures, Action<string> equip)
    {
        _manager = manager; _textures = textures; _equip = equip;
        _overlay = new Control { Name = "EventBotDetail", Visible = false, ZIndex = 11, MouseFilter = Control.MouseFilterEnum.Stop };
        _overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect); page.AddChild(_overlay);
        _overlay.VisibilityChanged += () => VisibilityChanged?.Invoke();
        var dim = new ColorRect { Color = new(0, 0, 0, .85f), MouseFilter = Control.MouseFilterEnum.Stop };
        dim.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect); _overlay.AddChild(dim);
        var margin = new MarginContainer(); margin.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        foreach (string side in new[] { "left", "right", "top", "bottom" }) margin.AddThemeConstantOverride("margin_" + side, 24);
        _overlay.AddChild(margin);
        var scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        margin.AddChild(scroll);
        var box = EventUiStyle.Panel(scroll, "EventBotDetailPanel");
        ((Control)box.GetParent().GetParent()).SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _name = Text(box, "BotDetailName", 20);
        _rarity = Text(box, "BotDetailRarity", 14);
        _image = new EventImageView(null, new(240, 180)) { CustomMinimumSize = new(0, 180) }; box.AddChild(_image);
        _description = Text(box, "BotDetailDescription", 14);
        _bonus = Text(box, "BotDetailBonus", 15);
        _requirement = Text(box, "BotDetailRequirement", 13);
        _status = Text(box, "BotDetailStatus", 14);
        _action = EventUiStyle.Button("EventBotDetailEquip", "", () => { if (_botId != null) _equip(_botId); Refresh(); }); box.AddChild(_action);
        box.AddChild(EventUiStyle.Button("EventBotDetailClose", "Close", Dismiss));
    }
    private static Label Text(Control parent, string name, int size)
    {
        var label = EventUiStyle.Label(name, "", size); label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        label.ClipText = true; parent.AddChild(label); return label;
    }
    internal void Open(EventDefinition d, EventBotDefinition bot)
    {
        _eventId = d.Id; _botId = bot.Id;
        EventUiStyle.SetText(_name, bot.Name); EventUiStyle.SetText(_rarity, bot.Rarity.ToUpperInvariant());
        _rarity.Modulate = EventBotPresentation.RarityColor(bot.Rarity);
        _image.SetTexture(_textures.Get(bot.IconPath));
        EventUiStyle.SetText(_description, bot.Description); EventUiStyle.SetText(_bonus, EventBotPresentation.Bonus(bot));
        EventUiStyle.SetText(_requirement, EventBotPresentation.Requirement(d, bot));
        EventUiStyle.StyleButton(_action, EventBotPresentation.RarityColor(bot.Rarity));
        _overlay.Show(); _overlay.MoveToFront(); Refresh();
    }
    internal void Refresh()
    {
        if (!Visible || _eventId == null || _botId == null) return;
        var snapshot = _manager.GetBot(_eventId, _botId);
        var d = _manager.GetDefinition(_eventId);
        EventUiStyle.SetText(_status, EventBotPresentation.Status(snapshot, d != null && _manager.IsActive(d)));
        EventUiStyle.SetText(_action, EventBotPresentation.Action(snapshot));
        EventUiStyle.SetDisabled(_action, !snapshot.CanEquip && !snapshot.CanAcquire);
    }
    internal void Dismiss() => _overlay.Hide();
    internal bool DismissOnBack() { if (!Visible) return false; Dismiss(); return true; }
}
