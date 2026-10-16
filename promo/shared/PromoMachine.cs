using System;
using Godot;

namespace IdleAi.Promo;

// Purely visual: no game machine scripts, services, or persistent state.
public partial class PromoMachine : Control
{
    private TextureRect _image = null!;
    private Tween? _punch;
    private float _clock;
    private float _energy;
    public float VisualIntensity { get; set; }
    public Color Accent { get; private set; } = Colors.Cyan;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        _image = new TextureRect
        {
            Position = new Vector2(32, 0), Size = new Vector2(416, 390),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore, PivotOffset = new Vector2(208, 195)
        };
        AddChild(_image);
        PivotOffset = new Vector2(240, 200);
    }

    public void ShowStage(PromoMachineData data)
    {
        Accent = data.Accent;
        _image.Texture = GD.Load<Texture2D>(data.TexturePath);
        Scale = Vector2.One;
        Modulate = Colors.White;
        QueueRedraw();
    }

    public void Punch(int level)
    {
        _punch?.Kill();
        _energy = 1;
        _punch = CreateTween();
        _punch.TweenProperty(this, "scale", Vector2.One * (1.06f + level * .006f), .08);
        _punch.TweenProperty(this, "scale", Vector2.One, .22).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
    }

    public void SetPowerLevel(int level)
    {
        _image.Scale = Vector2.One * (1 + Math.Min(level - 1, 10) * .018f);
    }

    public void ResetMotion()
    {
        _punch?.Kill();
        Scale = Vector2.One;
        Modulate = Colors.White;
        _energy = 0;
    }

    public override void _Process(double delta)
    {
        _clock += (float)delta;
        _energy = Mathf.MoveToward(_energy, 0, (float)delta * 2);
        _image.Position = new Vector2(32, Mathf.Sin(_clock * 2) * 5);
        QueueRedraw();
    }

    public override void _Draw()
    {
        var center = new Vector2(240, 220);
        for (int ring = 7; ring >= 1; ring--)
            DrawCircle(center, 112 + ring * 15 + _energy * 12, new Color(Accent, .015f + _energy * .009f + VisualIntensity * .018f));
        var platform = new Vector2[97];
        for (int i = 0; i < platform.Length; i++)
        {
            float angle = i * Mathf.Tau / 96;
            platform[i] = new Vector2(240 + Mathf.Cos(angle) * 180, 360 + Mathf.Sin(angle) * 25);
        }
        DrawPolyline(platform, new Color(Accent, .45f), 2, true);
        int particles = 12 + (int)(VisualIntensity * 24);
        for (int i = 0; i < particles; i++)
        {
            float angle = _clock * .3f + i * Mathf.Tau / particles;
            var point = center + new Vector2(Mathf.Cos(angle) * 208, Mathf.Sin(angle) * 164);
            DrawCircle(point, 2 + _energy * 2 + VisualIntensity * 2, new Color(Accent, .55f));
        }
    }
}
