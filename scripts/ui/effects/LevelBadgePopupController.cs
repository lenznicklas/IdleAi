using Godot;
using System;

namespace IdleAi;

/// <summary>Session-only presentation of new permanent tiers. Loading establishes a silent baseline.</summary>
public sealed partial class LevelBadgePopupController : Node
{
    private readonly Control _host;
    private readonly GameState _state;
    private bool _armed, _scheduled;
    private int _observed, _pending, _revision;
    private Control? _overlay, _body;
    private NumberedBadgeView? _badge;
    private LevelBadgeSparks? _sparks;
    private Tween? _animation, _sparkAnimation;
    public int ShowingTier => _badge?.Number ?? 0;
    public event Action<int>? Presented;

    public LevelBadgePopupController(Control host, GameState state)
    {
        Name = "LevelBadgePopupController";
        _host = host; _state = state;
    }

    public void ArmAfterLoad()
    {
        Clear();
        LevelBadgeProgress.Update(_state);
        _observed = _state.Archive.HighestLevelBadgeTier;
        _pending = 0; _scheduled = false;
        _armed = true;
    }

    public void Observe()
    {
        if (!_armed || _state.Archive.HighestLevelBadgeTier <= _observed) return;
        _pending = _observed = _state.Archive.HighestLevelBadgeTier;
        if (_scheduled) return;
        _scheduled = true;
        // Several upgrades/refreshes in one frame produce one presentation of the highest tier.
        Callable.From(() =>
        {
            if (!GodotObject.IsInstanceValid(this) || !IsInsideTree()) return;
            _scheduled = false;
            int tier = _pending; _pending = 0;
            if (_armed && tier > 0) ShowTier(tier);
        }).CallDeferred();
    }

    private void ShowTier(int tier)
    {
        _animation?.Kill(); _sparkAnimation?.Kill();
        int revision = ++_revision;
        if (_overlay == null)
        {
            _overlay = new Control { Name = "LevelBadgePopup", ZIndex = 3000, MouseFilter = Control.MouseFilterEnum.Ignore };
            _host.AddChild(_overlay); _overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
            _body = new Control { MouseFilter = Control.MouseFilterEnum.Ignore, Modulate = new(1, 1, 1, 0), Scale = Vector2.One * .96f };
            _overlay.AddChild(_body);
            _sparks = new LevelBadgeSparks(); _body.AddChild(_sparks);
            _badge = new NumberedBadgeView(GD.Load<Texture2D>(LevelBadgeProgress.IconPath), tier);
            _body.AddChild(_badge);
            _overlay.Resized += Fit;
            Fit();
        }
        // A new tier arriving during the hold replaces the number in the same popup.
        _badge!.SetNumber(tier);
        _sparks!.Progress = 0;
        _sparks.QueueRedraw();
        _sparkAnimation = CreateTween();
        _sparkAnimation.TweenInterval(.12);
        _sparkAnimation.TweenMethod(Callable.From<float>(value =>
        {
            _sparks.Progress = value;
            _sparks.QueueRedraw();
        }), 0f, 1f, .75);
        _animation = CreateTween();
        _animation.TweenProperty(_body, "modulate:a", 1f, .22).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        _animation.Parallel().TweenProperty(_body, "scale", Vector2.One, .26).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        _animation.TweenInterval(1.35);
        _animation.TweenProperty(_body, "modulate:a", 0f, .35).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.In);
        _animation.TweenCallback(Callable.From(() => { if (revision == _revision) Clear(); }));
        Presented?.Invoke(tier);
    }

    private void Fit()
    {
        if (_overlay == null || _body == null) return;
        float width = Mathf.Clamp(_overlay.Size.X - 32, 160, 300);
        float image = Mathf.Min(192, width * .74f);
        _body.Size = new(width, width);
        _body.PivotOffset = _body.Size / 2;
        _body.Position = (_overlay.Size - _body.Size) / 2;
        _badge!.Size = new(image, image); _badge.Position = (_body.Size - _badge.Size) / 2;
        _sparks!.Size = _badge.Size; _sparks.Position = _badge.Position;
    }

    private void Clear()
    {
        _revision++;
        _animation?.Kill(); _sparkAnimation?.Kill();
        _animation = null; _sparkAnimation = null;
        if (GodotObject.IsInstanceValid(_overlay)) { _overlay!.Hide(); _overlay.QueueFree(); }
        _overlay = null; _body = null; _badge = null; _sparks = null;
    }

    public override void _ExitTree() { _armed = false; _pending = 0; Clear(); }
}

/// <summary>Ten short-lived border glints; no textures, emitters or idle processing.</summary>
internal sealed partial class LevelBadgeSparks : Control
{
    private static readonly Color Gold = new("e8bd61"), Cyan = new("72dafa");
    internal float Progress { get; set; }
    internal LevelBadgeSparks() { Name = "LevelBadgeSparks"; MouseFilter = MouseFilterEnum.Ignore; }
    public override void _Draw()
    {
        if (Progress <= 0 || Progress >= 1) return;
        float side = Mathf.Min(Size.X, Size.Y);
        float alpha = Mathf.Sin(Progress * Mathf.Pi) * .8f;
        for (int i = 0; i < 10; i++)
        {
            Vector2 direction = Vector2.FromAngle(i * Mathf.Tau / 10 + .15f);
            Vector2 point = Size / 2 + direction * side * (.57f + .10f * Progress);
            var color = i % 3 == 0 ? Gold : Cyan; color.A = alpha;
            float length = 2 + (i % 3) * .7f;
            DrawLine(point - direction * length, point + direction * length, color, 1.3f, true);
            Vector2 cross = direction.Orthogonal() * length * .65f;
            DrawLine(point - cross, point + cross, color, 1, true);
        }
    }
}
