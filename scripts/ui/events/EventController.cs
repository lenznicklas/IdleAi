using Godot;
using System;

namespace IdleAi;

public sealed class EventController : IDisposable
{
    private readonly Game _root;
    private readonly MapController _map;
    private readonly EventManager _manager;
    private readonly EventView _view;
    private readonly Viewport _viewport;
    private readonly EventMapEntry _entry;
    public bool Visible => _view.Visible;
    public event Action<string>? MessageRequested;

    public EventController(Game root, MapController map, EventManager manager, Action leaveSingularity)
    {
        _root = root;
        _map = map;
        _manager = manager;
        _viewport = root.GetViewport();
        var textures = new EventTextureCache();
        _view = new EventView(manager, textures, CloseToMap, Upgrade, Claim, EquipBot, Buy, Prestige);
        root.AddChild(_view);
        _entry = new EventMapEntry(map, manager, textures, () =>
        {
            _view.Show(); // External navigation sees the destination before Map hides.
            leaveSingularity();
            _map.Hide();
            _view.MoveToFront();
            Resize();
            _view.OpenCurrent();
        });
        _manager.Changed += Refresh;
        _viewport.SizeChanged += Resize;
        _map.RoomSelectedRequested += _ => Hide();
        root.GetNode<Control>("MapPage").VisibilityChanged += () =>
        {
            if (map.Visible) { Hide(); _entry.Refresh(true); }
        };
        foreach (string name in new[] { "MapButton", "ShopButton", "LabButton" })
        {
            var button = root.GetNodeOrNull<BaseButton>("BottomBar/Margin/HBox/" + name);
            if (button == null) continue;
            button.ButtonDown += Hide;
            button.Pressed += Hide; // Also cover programmatic navigation.
        }
        root.GetNodeOrNull<UiReadabilityController>("UiReadabilityController")?.ApplyToSubtree(_view);
        Resize();
    }

    public void Dispose()
    {
        _manager.Changed -= Refresh;
        if (GodotObject.IsInstanceValid(_viewport)) _viewport.SizeChanged -= Resize;
    }
    public void Hide() => _view.Hide();
    private void CloseToMap() { _map.Open(); Hide(); }
    private void Upgrade(string generatorId)
    {
        var definition = _view.CurrentEvent;
        if (definition != null) Report(_manager.Upgrade(definition.Id, generatorId));
    }
    private void Claim(string milestone)
    {
        var definition = _view.CurrentEvent;
        if (definition != null) Report(_manager.ClaimReward(definition.Id, milestone));
    }
    private void EquipBot(string botId)
    {
        var definition = _view.CurrentEvent;
        if (definition != null) Report(_manager.EquipBot(definition.Id, botId));
    }
    private void Buy(string itemId)
    {
        var definition = _view.CurrentEvent;
        if (definition != null) Report(_manager.Buy(definition.Id, itemId));
    }
    private void Prestige(int count, long gain)
    {
        var definition = _view.CurrentEvent;
        if (definition != null) Report(_manager.Prestige(definition.Id, count, gain));
    }
    private void Report(EventActionResult result)
    {
        MessageRequested?.Invoke(result.Message);
        Refresh();
    }
    public void Refresh() { _entry.Refresh(); _view.Refresh(); }
    private void Resize()
    {
        float bottom = Math.Max(130, _root.Size.Y - _root.GetNode<Control>("BottomBar").Position.Y);
        // Keep the Event page's drawing and input rectangle above the existing
        // BottomBar, rather than covering its native TextureButtons.
        _view.OffsetBottom = -bottom;
        _view.ContentMargin.AddThemeConstantOverride("margin_top", (int)_map.GetSafeTopInset() + 16);
        _view.ContentMargin.AddThemeConstantOverride("margin_bottom", 12);
    }
}
