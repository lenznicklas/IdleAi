using Godot;
using System;

namespace IdleAi;

internal sealed class EventMapEntry
{
    private readonly EventManager _manager;
    private readonly EventTextureCache _textures;
    private readonly Button _card;
    private readonly Label _name, _time, _status;
    private readonly TextureRect _icon;
    private string? _iconPath;
    private int _state = -1;
    private string? _eventId;
    internal EventMapEntry(MapController map, EventManager manager, EventTextureCache textures, Action open)
    {
        _manager = manager;
        _textures = textures;
        _card = EventUiStyle.Button("EventMapEntry", "", open);
        var margin = new MarginContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        margin.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        foreach (var side in new[] { "left", "right", "top", "bottom" }) margin.AddThemeConstantOverride("margin_" + side, 14);
        _card.AddChild(margin);
        var box = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        box.AddThemeConstantOverride("separation", 6);
        margin.AddChild(box);
        var row = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        box.AddChild(row);
        _icon = new TextureRect { CustomMinimumSize = new Vector2(40, 40),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = Control.MouseFilterEnum.Ignore };
        row.AddChild(_icon);
        row.AddChild(EventUiStyle.Label("EventEntryTitle", "Event", 20));
        _name = EventUiStyle.Label("EventEntryName", "", 14);
        _name.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _name.MaxLinesVisible = 2;
        box.AddChild(_name);
        _time = EventUiStyle.Label("EventEntryTime", "", 11);
        box.AddChild(_time);
        _status = EventUiStyle.Label("EventEntryStatus", "", 11);
        box.AddChild(_status);
        map.AddEventMapCard(_card);
        Refresh(true);
    }
    internal void Refresh(bool force = false)
    {
        if (!force && !_card.IsVisibleInTree()) return;
        var active = _manager.ActiveEvent;
        long remaining = active == null ? 0 : _manager.RemainingSeconds(active);
        int state = active == null ? 0 : remaining <= active.EndingSoonSeconds ? 2 : 1;
        if (_state != state || _eventId != active?.Id)
        {
            _state = state;
            _eventId = active?.Id;
            EventUiStyle.StyleButton(_card, state == 0 ? EventUiStyle.Muted : state == 2 ? EventUiStyle.Ending : Color.FromHtml(active?.Theme.AccentHex ?? "#33b3f2"));
        }
        EventUiStyle.SetText(_name, active?.Name ?? "No active event");
        EventUiStyle.SetText(_time, active == null ? "" : EventUiStyle.Countdown(remaining));
        EventUiStyle.SetText(_status, state == 0 ? "AVAILABLE SOON" : state == 2 ? "ENDING SOON" : "ACTIVE");
        string path = active?.IconPath ?? "res://assets/map/room_coming_soon.png";
        if (_iconPath != path) { _iconPath = path; _icon.Texture = _textures.Get(path); _icon.Visible = _icon.Texture != null; }
    }
}
