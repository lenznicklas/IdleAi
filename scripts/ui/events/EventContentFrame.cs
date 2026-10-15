using Godot;

namespace IdleAi;

/// <summary>Shared visual transition around the Event scroller. Overlays never receive input.</summary>
internal sealed partial class EventContentFrame : Control
{
    internal const int BottomClearance = 48;
    private const int Gap = 12;
    private readonly Gradient _backdrop, _top, _bottom;
    private readonly Panel _topFade, _bottomFade;

    internal EventContentFrame(ScrollContainer scroll)
    {
        Name = "EventContentFrame";
        MouseFilter = MouseFilterEnum.Ignore;
        SizeFlagsVertical = SizeFlags.ExpandFill;
        var backdrop = Layer("EventContentBackdrop", out _backdrop);
        backdrop.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        // Continue through the existing layout gaps without moving the header or tabs.
        backdrop.OffsetTop = -Gap; backdrop.OffsetBottom = Gap;
        AddChild(backdrop);
        scroll.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(scroll);
        _topFade = Layer("EventContentTopFade", out _top);
        _topFade.SetAnchorsAndOffsetsPreset(LayoutPreset.TopWide);
        _topFade.OffsetTop = -Gap;
        AddChild(_topFade);
        _bottomFade = Layer("EventContentBottomFade", out _bottom);
        _bottomFade.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomWide);
        _bottomFade.OffsetBottom = Gap;
        AddChild(_bottomFade);
        Resized += FitEdges;
        scroll.GetVScrollBar().ValueChanged += value => _topFade.Visible = value > .5;
        _topFade.Visible = false;
        SetTheme(new(.008f, .018f, .035f), EventUiStyle.Accent);
    }

    public override void _Ready() => FitEdges();

    private void FitEdges()
    {
        // Keep the transition small on short portrait viewports.
        float top = Mathf.Min(24, Size.Y * .07f);
        float bottom = Mathf.Min(BottomClearance, Size.Y * .12f);
        _topFade.OffsetBottom = top;
        _bottomFade.OffsetTop = -bottom;
        _top.Offsets = [0, Gap / (Gap + top), 1];
        float edge = bottom / (bottom + Gap);
        _bottom.Offsets = [0, edge * .55f, edge, 1];
    }

    internal void SetTheme(Color background, Color accent)
    {
        var surface = background.Lerp(accent.Darkened(.8f), .18f);
        _backdrop.Offsets = [0, .16f, .80f, 1];
        _backdrop.Colors = [WithAlpha(surface, 0), WithAlpha(surface, .38f), WithAlpha(surface, .38f), WithAlpha(surface, 0)];
        _top.Colors = [WithAlpha(background, 0), WithAlpha(background, .90f), WithAlpha(background, 0)];
        _bottom.Colors = [WithAlpha(background, 0), WithAlpha(background, .30f), WithAlpha(background, .94f), WithAlpha(background, 0)];
        FitEdges();
    }

    private static Color WithAlpha(Color color, float alpha) => new(color.R, color.G, color.B, alpha);

    private static Panel Layer(string name, out Gradient gradient)
    {
        gradient = new Gradient();
        var texture = new GradientTexture2D { Gradient = gradient, Width = 2, Height = 128,
            FillFrom = new(.5f, 0), FillTo = new(.5f, 1) };
        var panel = new Panel { Name = name, MouseFilter = MouseFilterEnum.Ignore };
        panel.AddThemeStyleboxOverride("panel", new StyleBoxTexture { Texture = texture });
        return panel;
    }
}
