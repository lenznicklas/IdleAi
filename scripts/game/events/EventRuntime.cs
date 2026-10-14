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
            _manager.Tick();
            _controller.Refresh();
        };
        AddChild(timer);
    }
    public override void _ExitTree() => _controller.Dispose();

    public override void _Notification(int what)
    {
        if (what == MainLoop.NotificationApplicationPaused)
        {
            _manager.Tick();
            _paused = true;
            _save();
        }
        else if (what == MainLoop.NotificationApplicationResumed)
        {
            _paused = false;
            _manager.Tick();
            _save();
            _controller.Refresh();
        }
    }
}
