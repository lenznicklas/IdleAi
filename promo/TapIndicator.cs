using Godot;

namespace IdleAi.Promo;

// A purely visual ripple; the containing overlay never consumes pointer input.
public partial class TapIndicator : Node2D
{
    public override void _Ready()
    {
        Scale = Vector2.One * .45f;
        Modulate = new Color(1, 1, 1, .85f);
        var tween = CreateTween().SetParallel();
        tween.TweenProperty(this, "scale", Vector2.One * 1.6f, .34)
            .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(this, "modulate:a", 0f, .34)
            .SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.In);
        tween.Chain().TweenCallback(Callable.From(QueueFree));
    }

    public override void _Draw()
    {
        DrawCircle(Vector2.Zero, 6, new Color(1, 1, 1, .28f));
        DrawArc(Vector2.Zero, 15, 0, Mathf.Tau, 64, new Color(1, 1, 1, .30f), 1.2f, true);
        DrawArc(Vector2.Zero, 22, 0, Mathf.Tau, 64, new Color(1, 1, 1, .90f), 2, true);
    }
}
