using Godot;

namespace IdleAi;

/// <summary>Presentation only: receives the original percentage and completion event.</summary>
public partial class MachineCycleBarView : Control
{
    private const float PulseSeconds = 0.22f;
    private readonly StyleBoxFlat _track = MakeStyle(new Color(0.018f, 0.028f, 0.045f), 4);
    private readonly StyleBoxFlat _fill = MakeStyle(Colors.White, 3);
    private readonly StyleBoxFlat _glow = MakeStyle(Colors.Transparent, 3);
    private readonly StyleBoxFlat _pulse = MakeStyle(Colors.Transparent, 3);
    private Color _accent;
    private float _progress = 1;
    private float _phase;
    private float _pulseRemaining;
    private bool _running;
    private Control? _viewportClip;

    public MachineCycleBarView()
    {
        Name = "MachineCycleBar";
        CustomMinimumSize = new Vector2(0, 18);
        MouseFilter = MouseFilterEnum.Ignore;
        _track.SetBorderWidthAll(1);
        _glow.ShadowSize = 4;
        _pulse.ShadowSize = 4;
        _glow.ShadowOffset = Vector2.Zero;
        _pulse.ShadowOffset = Vector2.Zero;
        SetProcess(false);
    }

    private static StyleBoxFlat MakeStyle(Color color, int radius)
    {
        var style = new StyleBoxFlat { BgColor = color, AntiAliasing = true };
        style.SetCornerRadiusAll(radius);
        return style;
    }

    public override void _Ready()
    {
        _viewportClip = FindParent("ScrollContainer") as Control;
        // Godot automatically enables processing for _Process overrides on entry.
        SetProcess(_running || _pulseRemaining > 0);
    }

    public void SetAccent(Color accent)
    {
        // Lift the existing room hue into an emissive accent without recoloring the card.
        _accent = accent.Lightened(0.28f);
        _track.BorderColor = new Color(_accent, 0.30f);
        QueueRedraw();
    }

    public void UpdateProgress(double percentage, bool running)
    {
        float next = (float)(percentage / 100.0);
        if (_running == running && _progress == next) return;
        if (_running != running) _phase = 0;
        _running = running;
        // MachineSlot supplies progress every frame already. Follow it exactly: no
        // extra prediction or tween lag, especially on fast cycles and manual boosts.
        _progress = Mathf.Clamp(next, 0, 1);
        SetProcess(_running || _pulseRemaining > 0);
        QueueRedraw();
    }

    private bool Onscreen() => IsVisibleInTree() && GetGlobalRect().Intersects(
        _viewportClip?.GetGlobalRect() ?? GetViewport().GetVisibleRect());

    public void CycleCompleted()
    {
        if (!Onscreen() || _pulseRemaining > PulseSeconds * 0.5f) return;
        // Coalesce completions from fast cycles/large simulation steps. This is
        // independent of progress, so the next cycle can reset immediately.
        _pulseRemaining = PulseSeconds;
        SetProcess(true);
        QueueRedraw();
    }

    public void Clear()
    {
        _running = false;
        _progress = 1;
        _phase = 0;
        _pulseRemaining = 0;
        SetProcess(false);
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        _pulseRemaining = Mathf.Max(0, _pulseRemaining - (float)delta);
        if (!_running && _pulseRemaining == 0) SetProcess(false);
        if (!Onscreen()) return;
        _phase = Mathf.PosMod(_phase + (float)delta / 1.65f, 1);
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (Size.X <= 4 || Size.Y <= 4) return;
        // Reserve two pixels for the close glow; retain the original 18px layout.
        Rect2 track = new(new Vector2(1, 2), Size - new Vector2(2, 4));
        DrawStyleBox(_track, track);
        Rect2 interior = new(track.Position + Vector2.One, track.Size - Vector2.One * 2);
        Rect2 filled = new(interior.Position, new Vector2(interior.Size.X * _progress, interior.Size.Y));
        float pulse = _pulseRemaining / PulseSeconds;

        if (filled.Size.X > 0)
        {
            _glow.ShadowColor = new Color(_accent, (_running ? 0.30f : 0.025f) + pulse * 0.25f);
            DrawStyleBox(_glow, filled);
            _fill.BgColor = _running ? _accent : _accent.Darkened(0.65f);
            DrawStyleBox(_fill, filled);

            // A crisp inset rail remains readable at mobile scale.
            float inset = Mathf.Min(3, filled.Size.X * 0.5f);
            DrawRect(new Rect2(filled.Position + new Vector2(inset, 2),
                new Vector2(Mathf.Max(0, filled.Size.X - inset * 2), 1)),
                new Color(_accent.Lightened(0.65f), _running ? 0.45f : 0.10f));

            if (_running)
            {
                // Seven translucent strips form a soft sweep. Each intersection
                // clips to the current fill; no textures, shaders or child churn.
                float sweep = interior.Position.X + _phase * (interior.Size.X + 28) - 14;
                for (int i = 0; i < 7; i++)
                {
                    Rect2 strip = new(new Vector2(sweep + (i - 3) * 3, interior.Position.Y + 3),
                        new Vector2(3, interior.Size.Y - 6));
                    Rect2 clipped = strip.Intersection(new Rect2(filled.Position + new Vector2(1, 0),
                        new Vector2(Mathf.Max(0, filled.Size.X - 2), filled.Size.Y)));
                    if (clipped.HasArea()) DrawRect(clipped,
                        new Color(Colors.White, (4 - Mathf.Abs(i - 3)) * 0.035f));
                }

                float edgeWidth = Mathf.Min(2, filled.Size.X);
                Vector2 edge = new(filled.End.X - edgeWidth, filled.Position.Y + 2);
                DrawRect(new Rect2(edge, new Vector2(edgeWidth, filled.Size.Y - 4)),
                    new Color(_accent.Lightened(0.8f), 0.85f));
            }
        }

        if (pulse > 0)
        {
            // A brief translucent afterimage of the completed fill, separate from
            // the new cycle's readable fill and confined to the bar itself.
            _pulse.BgColor = new Color(_accent.Lightened(0.65f), pulse * 0.16f);
            _pulse.ShadowColor = new Color(_accent, pulse * 0.38f);
            DrawStyleBox(_pulse, interior);
        }
    }
}
