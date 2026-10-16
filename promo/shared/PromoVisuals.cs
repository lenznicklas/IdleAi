using Godot;

namespace IdleAi.Promo;

// Shared recording-only visuals; no game services or saved state.
internal static class PromoVisuals
{
    internal static void Background(Control parent)
    {
        var image = new TextureRect
        {
            Texture = GD.Load<Texture2D>("res://promo_assets/background.png"),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        parent.AddChild(image); image.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var shade = new ColorRect { Color = new(.015f, .025f, .06f, .88f), MouseFilter = Control.MouseFilterEnum.Ignore };
        parent.AddChild(shade); shade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
    }

    internal static Label Money(Control parent, string text, Vector2 position, Vector2 size, int fontSize, Color color)
    {
        var label = new Label
        {
            Name = "MoneyPopup", Text = text, Position = position, Size = size,
            HorizontalAlignment = HorizontalAlignment.Center, ClipText = true,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeColorOverride("font_outline_color", new Color("081220"));
        label.AddThemeConstantOverride("outline_size", 5);
        parent.AddChild(label);
        var tween = label.CreateTween().SetParallel();
        tween.TweenProperty(label, "position:y", position.Y - 100, .65).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(label, "modulate:a", 0f, .65);
        tween.Chain().TweenCallback(Callable.From(label.QueueFree));
        return label;
    }

    internal static void Burst(Control parent, Vector2 origin, int count, Color color, RandomNumberGenerator random)
    {
        for (int i = 0; i < count; i++)
        {
            var mote = new ColorRect { Position = origin, Size = new(4, 4), Color = color, MouseFilter = Control.MouseFilterEnum.Ignore };
            parent.AddChild(mote);
            mote.AddToGroup("promo_transient");
            var target = origin + Vector2.FromAngle(random.RandfRange(0, Mathf.Tau)) * random.RandfRange(80, 250);
            var tween = mote.CreateTween().SetParallel();
            tween.TweenProperty(mote, "position", target, .45).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
            tween.TweenProperty(mote, "modulate:a", 0f, .45);
            tween.Chain().TweenCallback(Callable.From(mote.QueueFree));
        }
    }
}
