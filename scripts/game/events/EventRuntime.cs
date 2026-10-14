using Godot;
using System;

namespace IdleAi;

/// <summary>One low-frequency timer shared by production, schedule checks and countdowns.</summary>
internal sealed partial class EventRuntime : Node
{
    private readonly EventManager _manager;
    private readonly EventController _controller;
    private readonly Func<bool> _save;
    private bool _paused;
    private ulong _lastPlayTick;
    private string? _playEventId;
    private void SamplePlayTime()
    {
        ulong now = Time.GetTicksMsec();
        string? current = !_paused && _controller.Visible ? _manager.ActiveEvent?.Id : null;
        if (current != null && current == _playEventId && _lastPlayTick > 0)
            _manager.RecordEventPlayTime(current, (now - _lastPlayTick) / 1000.0);
        _lastPlayTick = now; _playEventId = current;
    }
    internal EventRuntime(EventManager manager, EventController controller, Func<bool> save)
    {
        Name = "EventRuntime";
        _manager = manager;
        _controller = controller;
        _save = save;
    }
    public override void _Ready()
    {
        var timer = new Timer { Name = "EventTickTimer", WaitTime = 1, Autostart = true };
        timer.Timeout += () =>
        {
            if (_paused) return;
            SamplePlayTime();
            _manager.Tick();
            _controller.Refresh();
        };
        AddChild(timer);
    }
    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_cancel") && _controller.TryGoBack())
            GetViewport().SetInputAsHandled();
    }
    public override void _ExitTree() => _controller.Dispose();

    public override void _Notification(int what)
    {
        if (what == MainLoop.NotificationApplicationPaused)
        {
            SamplePlayTime();
            _manager.Tick();
            _paused = true; _playEventId = null;
            _save();
        }
        else if (what == MainLoop.NotificationApplicationResumed)
        {
            _paused = false; _lastPlayTick = Time.GetTicksMsec(); _playEventId = null;
            _manager.Tick();
            _save();
            _controller.Refresh();
        }
    }
}
