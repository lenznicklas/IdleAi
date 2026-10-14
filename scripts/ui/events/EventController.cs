using Godot;
using System;
using System.Collections.Generic;

namespace IdleAi;

public sealed class EventController : IDisposable
{
    private readonly Game _root;
    private readonly MapController _map;
    private readonly EventManager _manager;
    private readonly EventView _view;
    private readonly Viewport _viewport;
    private readonly EventMapEntry _entry;
    private readonly Control _bottomBar;
    private readonly SceneTree _tree;
    private readonly Window _window;
    private readonly List<BaseButton> _navigationButtons = [];
    private readonly List<(Control Page, Action Handler)> _pageHooks = [];
    private bool _ownsBackOverride, _previousQuitOnGoBack, _disposed;
    public bool Visible => _view.Visible;
    public event Action<string>? MessageRequested;

    public EventController(Game root, MapController map, EventManager manager, Action leaveSingularity)
    {
        _root = root;
        _map = map;
        _manager = manager;
        _viewport = root.GetViewport();
        _bottomBar = root.GetNode<Control>("BottomBar");
        _tree = root.GetTree();
        _window = root.GetWindow();
        var textures = new EventTextureCache();
        _view = new EventView(manager, textures, CloseToMap, Upgrade, Claim, ClaimAll, EquipBot, Buy, Prestige, ClaimChallenge, ClaimAllChallenges);
        root.AddChild(_view);
        _view.VisibilityChanged += SyncBottomBarVisibility;
        _bottomBar.VisibilityChanged += OnBottomBarVisibilityChanged;
        _window.GoBackRequested += OnSystemBack;
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
        _map.RoomSelectedRequested += OnRoomSelected;
        foreach (string name in new[] { "MapPage", "ShopPage", "LabPage", "SingularityPage" })
        {
            var page = root.GetNodeOrNull<Control>(name);
            if (page == null) continue;
            Action handler = () =>
            {
                if (!page.Visible) return;
                Hide();
                if (name == "MapPage") _entry.Refresh(true);
            };
            page.VisibilityChanged += handler;
            _pageHooks.Add((page, handler));
        }
        foreach (string name in new[] { "MapButton", "ShopButton", "LabButton" })
        {
            var button = root.GetNodeOrNull<BaseButton>("BottomBar/Margin/HBox/" + name);
            if (button == null) continue;
            _navigationButtons.Add(button);
            button.ButtonDown += Hide;
            button.Pressed += Hide; // Also cover programmatic navigation.
        }
        root.GetNodeOrNull<UiReadabilityController>("UiReadabilityController")?.ApplyToSubtree(_view);
        Resize();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _manager.Changed -= Refresh;
        _map.RoomSelectedRequested -= OnRoomSelected;
        if (GodotObject.IsInstanceValid(_viewport)) _viewport.SizeChanged -= Resize;
        if (GodotObject.IsInstanceValid(_window)) _window.GoBackRequested -= OnSystemBack;
        if (GodotObject.IsInstanceValid(_view)) _view.VisibilityChanged -= SyncBottomBarVisibility;
        if (GodotObject.IsInstanceValid(_bottomBar))
        {
            _bottomBar.VisibilityChanged -= OnBottomBarVisibilityChanged;
            SyncBottomBarVisibility();
        }
        foreach (var (page, handler) in _pageHooks)
            if (GodotObject.IsInstanceValid(page)) page.VisibilityChanged -= handler;
        foreach (var button in _navigationButtons)
            if (GodotObject.IsInstanceValid(button)) { button.ButtonDown -= Hide; button.Pressed -= Hide; }
        RestoreSystemBack();
    }
    public void Hide() => _view.Hide();
    private void OnRoomSelected(int _) => Hide();
    private void CloseToMap() { _map.Open(); Hide(); }
    internal bool TryGoBack()
    {
        if (_disposed || !Visible) return false;
        if (!_view.DismissConfirmationOnBack()) CloseToMap();
        return true;
    }
    private void OnSystemBack() => TryGoBack();

    // The Event page owns global navigation visibility, independently of its selected tab.
    private void SyncBottomBarVisibility()
    {
        bool eventVisible = !_disposed && Visible;
        _bottomBar.Visible = !eventVisible;
        if (_disposed) return;
        if (eventVisible)
        {
            if (!_ownsBackOverride)
            {
                _previousQuitOnGoBack = _tree.QuitOnGoBack;
                _ownsBackOverride = true;
            }
            _tree.QuitOnGoBack = false;
        }
        else if (_ownsBackOverride)
        {
            // Restore after the current OS back event finishes, so it cannot also quit the app.
            Callable.From(RestoreSystemBack).CallDeferred();
        }
    }
    private void OnBottomBarVisibilityChanged()
    {
        // Singularity may restore its chrome deferred. Never allow that to reveal the bar inside Event.
        if (!_disposed && Visible && _bottomBar.Visible) SyncBottomBarVisibility();
    }
    private void RestoreSystemBack()
    {
        if (!_ownsBackOverride || (!_disposed && Visible)) return;
        if (GodotObject.IsInstanceValid(_tree)) _tree.QuitOnGoBack = _previousQuitOnGoBack;
        _ownsBackOverride = false;
    }
    private void Upgrade(string generatorId)
    {
        var definition = _view.CurrentEvent;
        if (definition != null) Report(_manager.Upgrade(definition.Id, generatorId));
    }
    private void ClaimChallenge(string id)
    {
        var definition = _view.CurrentEvent;
        if (definition != null) Report(_manager.ClaimChallenge(definition.Id, id));
    }
    private void ClaimAllChallenges()
    {
        var definition = _view.CurrentEvent;
        if (definition != null) Report(_manager.ClaimAllChallenges(definition.Id));
    }
    private void ClaimAll()
    {
        var definition = _view.CurrentEvent;
        if (definition != null) Report(_manager.ClaimAllRewards(definition.Id));
    }
    private void Claim(string milestone)
    {
        var definition = _view.CurrentEvent;
        if (definition != null) Report(_manager.ClaimReward(definition.Id, milestone));
    }
    private void EquipBot(string botId)
    {
        var definition = _view.CurrentEvent;
        if (definition != null)
        {
            var state = _manager.GetBot(definition.Id, botId);
            Report(state.CanAcquire ? _manager.AcquireBot(definition.Id, botId) : _manager.EquipBot(definition.Id, botId));
        }
    }
    private void Buy(string itemId, int expectedCount)
    {
        var definition = _view.CurrentEvent;
        if (definition != null) Report(_manager.Buy(definition.Id, itemId, expectedCount));
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
    public void Refresh() { _entry.Refresh(); _view.Refresh(); if (Visible) SyncBottomBarVisibility(); }
    private void Resize()
    {
        // Event owns the whole screen; the hidden global bar reserves no height.
        _view.OffsetBottom = 0;
        _view.ContentMargin.AddThemeConstantOverride("margin_top", (int)_map.GetSafeTopInset() + 16);
        _view.ContentMargin.AddThemeConstantOverride("margin_bottom", 12);
    }
}
