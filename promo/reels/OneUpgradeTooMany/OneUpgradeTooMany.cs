using Godot;
using System;

namespace IdleAi.Promo;

// An eleven-second, hands-free recording timeline. No gameplay or save dependencies.
public partial class OneUpgradeTooMany : Control
{
    public const double Duration = 11;
    private static readonly double[] UpgradeTimes = [.65, 1.4, 2.15, 2.95, 3.75, 4.6, 5.45];
    private static readonly double[] Payouts = [12, 12_000, 2_400_000, 8_700_000_000, 640e12, 91e15, 7.2e21, 999e30];
    private static readonly int[] Levels = [1, 10, 30, 80, 160, 320, 640, 1000];
    private static readonly Color[] Accents = [new("54dcff"), new("60ffb8"), new("54dcff"), new("ac86ff"), new("ff85db"), new("ffd978"), new("ff7ae7"), new("e9fcff")];
    public int UpgradeCount { get; private set; }
    public double Elapsed { get; private set; }
    public float Overload { get; private set; }
    public double PopupInterval => UpgradeCount >= 7 ? .045 : UpgradeCount >= 5 ? .09 : .64 - UpgradeCount * .11;
    private const double CutAt = 7.45;
    public bool Finished => Elapsed >= Duration;
    private Control _stage = null!, _world = null!, _effects = null!, _touch = null!, _ending = null!;
    private PromoMachine _machine = null!;
    private Label _rate = null!, _level = null!, _broken = null!, _footer = null!;
    private Button _upgrade = null!;
    private PromoOverloadEnergy _energy = null!;
    private ShaderMaterial _distortion = null!;
    private Control _glitch = null!;
    private float _impact;
    private ColorRect _dark = null!;
    private PackedScene _tapScene = null!;
    private StyleBoxFlat _buttonStyle = null!;
    private readonly RandomNumberGenerator _random = new();
    private Tween? _buttonTween;
    private double _popupClock;
    private float _shake;
    private int _popupSequence;

    public override void _Ready()
    {
        Theme = new Theme { DefaultFont = GD.Load<Font>("res://promo_assets/Orbitron-Regular.ttf") };
        _tapScene = GD.Load<PackedScene>("res://shared/TapIndicator.tscn");
        _random.Seed = 107;
        _stage = Layer(this, "ReelStage");
        _stage.Size = new(720, 1280);
        PromoVisuals.Background(_stage);
        _world = Layer(_stage, "MachineWorld");
        var hook = Text(_stage, "Hook", "What happens if I\nupgrade this 1000×?", 36, new(48, 164), new(552, 116), Colors.White);
        hook.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _rate = Text(_stage, "Production", "", 51, new(48, 306), new(552, 82), Accents[0]);
        _level = Text(_stage, "MachineLevel", "", 18, new(48, 394), new(552, 36), new("a9bdcf"));
        _machine = new PromoMachine { Name = "PromoMachine", Position = new(84, 462), Size = new(480, 400) };
        _world.AddChild(_machine);
        _energy = new PromoOverloadEnergy { Name = "OverloadEnergy", Size = new(720, 1280) };
        _world.AddChild(_energy);
        _effects = Layer(_world, "MoneyEffects");
        _upgrade = new Button
        {
            Name = "UpgradeButton", Text = "UPGRADE",
            Position = new(94, 918), Size = new(460, 92), PivotOffset = new(230, 46),
            FocusMode = FocusModeEnum.None, MouseFilter = MouseFilterEnum.Ignore
        };
        _upgrade.AddThemeFontSizeOverride("font_size", 29);
        _buttonStyle = new StyleBoxFlat
        {
            BgColor = new("174967"), BorderColor = Accents[0],
            BorderWidthLeft = 2, BorderWidthRight = 2, BorderWidthTop = 2, BorderWidthBottom = 2,
            CornerRadiusTopLeft = 22, CornerRadiusTopRight = 22, CornerRadiusBottomLeft = 22, CornerRadiusBottomRight = 22,
            ShadowColor = new(Accents[0], .25f), ShadowSize = 14
        };
        _upgrade.AddThemeStyleboxOverride("normal", _buttonStyle);
        _upgrade.AddThemeColorOverride("font_color", Colors.White);
        _stage.AddChild(_upgrade);
        _touch = Layer(_stage, "TouchFeedback");
        _glitch = new ColorRect { Name = "OverloadDistortion", Size = new(720, 1280),
            MouseFilter = MouseFilterEnum.Ignore };
        _distortion = new ShaderMaterial { Shader = GD.Load<Shader>("res://shared/PromoOverload.gdshader") };
        _glitch.Material = _distortion;
        _stage.AddChild(_glitch);
        _ending = Layer(_stage, "Ending");
        _dark = new ColorRect { Name = "DarkCut", Color = new(.004f, .008f, .018f), Size = new(720, 1280), MouseFilter = MouseFilterEnum.Ignore };
        _ending.AddChild(_dark);
        _broken = Text(_ending, "BrokenMessage", "I think I broke\nthe game.", 45, new(48, 490), new(552, 132), Colors.White);
        _footer = Text(_ending, "ComingSoon", "Idle AI — Coming Soon", 18, new(48, 690), new(552, 40), new("a9bdcf"));
        Resized += FitStage;
        FitStage();
        ResetReel();
    }

    private void FitStage()
    {
        float scale = Mathf.Min(Size.X / 720, Size.Y / 1280);
        _stage.Scale = Vector2.One * scale;
        _stage.Position = (Size - new Vector2(720, 1280) * scale) / 2;
    }

    private static Control Layer(Control parent, string name)
    {
        var layer = new Control { Name = name, Size = new(720, 1280), MouseFilter = MouseFilterEnum.Ignore };
        parent.AddChild(layer);
        return layer;
    }

    private static Label Text(Control parent, string name, string text, int fontSize, Vector2 position, Vector2 size, Color color)
    {
        var label = new Label { Name = name, Text = text, Position = position, Size = size,
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color);
        parent.AddChild(label);
        return label;
    }

    public override void _Process(double delta)
    {
        if (Finished) return;
        Elapsed = Math.Min(Duration, Elapsed + delta);
        while (UpgradeCount < UpgradeTimes.Length && Elapsed >= UpgradeTimes[UpgradeCount])
            ApplyUpgrade();
        float dt = (float)delta;
        _impact = Mathf.MoveToward(_impact, 0, dt * 2.4f);
        _shake = Mathf.MoveToward(_shake, 0, dt * 18);
        float late = Mathf.Clamp((float)(Elapsed - 3.75) / 1.7f, 0, 1);
        // A second surge after the last upgrade pushes the already absurd state over the edge.
        float surge = Mathf.Clamp((float)(Elapsed - 6.15) / .75f, 0, 1);
        Overload = Mathf.Clamp(late * .65f + surge * .35f, 0, 1);
        float shaking = _shake + Overload * (12 + surge * 12);
        _world.Position = new(Mathf.Sin((float)Elapsed * 83) * shaking, Mathf.Sin((float)Elapsed * 109) * shaking * .65f);
        _world.Scale = Vector2.One * (1 + Overload * .035f + surge * .025f * Mathf.Sin((float)Elapsed * 24));
        _world.PivotOffset = new(324, 665);
        // Keep the hook steady. Lower UI "struggles" without making its words unreadable.
        _rate.Position = new Vector2(48, 306) + new Vector2(Mathf.Sin((float)Elapsed * 47) * Overload * 7, 0);
        _upgrade.Position = new Vector2(94, 918) + new Vector2(Mathf.Sin((float)Elapsed * 61) * Overload * 8, Mathf.Sin((float)Elapsed * 41) * Overload * 4);
        _level.Modulate = new(1, 1, 1, 1 - Overload * (.25f + .2f * Mathf.Sin((float)Elapsed * 31)));
        _machine.Rotation = Mathf.Sin((float)Elapsed * 27) * Overload * .045f;
        _machine.VisualIntensity = UpgradeCount * UpgradeCount * .06f + surge * 2;
        if (Overload > 0) _machine.Scale = Vector2.One * (1 + Overload * (.12f + .07f * Mathf.Sin((float)Elapsed * 23)));
        _energy.Intensity = Overload;
        _energy.Clock = (float)Elapsed;
        _energy.QueueRedraw();
        _distortion.SetShaderParameter("intensity", Overload);
        _distortion.SetShaderParameter("clock", (float)Elapsed);
        _distortion.SetShaderParameter("impact", _impact);
        _glitch.Visible = Overload > 0 || _impact > 0;
        if (Elapsed < CutAt)
        {
            _popupClock += delta;
            if (_popupClock >= PopupInterval)
            {
                _popupClock = 0;
                int count = UpgradeCount >= 7 ? 3 : UpgradeCount >= 5 ? 2 : 1;
                for (int i = 0; i < count; i++) SpawnMoney();
                if (UpgradeCount >= 5)
                    PromoVisuals.Burst(_effects, new(324, 640), 5 + (int)(surge * 8), Accents[UpgradeCount], _random);
            }
        }
        _ending.Visible = Elapsed >= CutAt;
        if (_ending.Visible)
        {
            _world.Hide(); _glitch.Hide(); _touch.Hide();
            _rate.Position = new(48, 306); _upgrade.Position = new(94, 918);
            _dark.Modulate = new(1, 1, 1, Mathf.Clamp((float)(Elapsed - CutAt) / .12f, 0, 1));
            Reveal(_broken, 7.7);
            Reveal(_footer, 8.5);
        }
    }

    private void ApplyUpgrade()
    {
        UpgradeCount++;
        var color = Accents[UpgradeCount];
        _machine.ShowStage(new("SERVER RACK", "res://promo_assets/server.png", 0, 0, color));
        _machine.VisualIntensity = UpgradeCount * UpgradeCount * .06f;
        _machine.SetPowerLevel(Levels[UpgradeCount]);
        _machine.Punch(UpgradeCount * UpgradeCount);
        _impact = UpgradeCount < 3 ? 0 : .12f + UpgradeCount * .07f;
        _machine.Modulate = color.Lightened(.72f);
        _shake = UpgradeCount < 3 ? 0 : UpgradeCount * 3.5f;
        _buttonStyle.BorderColor = color;
        _buttonStyle.ShadowColor = new(color, .45f);
        _upgrade.Text = UpgradeCount == 7 ? "UPGRADE?!" : "UPGRADE";
        _buttonTween?.Kill();
        _upgrade.Scale = Vector2.One * .96f;
        _buttonTween = CreateTween();
        _buttonTween.TweenProperty(_upgrade, "scale", Vector2.One, .18).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        var ripple = _tapScene.Instantiate<TapIndicator>();
        ripple.Position = _upgrade.Position + _upgrade.Size / 2;
        _touch.AddChild(ripple);
        PromoVisuals.Burst(_effects, new(324, 640), 6 + UpgradeCount * UpgradeCount * 2, color, _random);
        UpdateLabels();
        _popupClock = 0;
        SpawnMoney();
    }

    private void SpawnMoney()
    {
        int popups = 0;
        foreach (Node child in _effects.GetChildren()) if (child is Label) popups++;
        if (popups >= 48) return;
        float stagger = (_popupSequence++ % 3 - 1) * (UpgradeCount >= 5 ? 85 : 35);
        PromoVisuals.Money(_effects, "+$" + IdleAi.NumberFormatter.Format(Payouts[UpgradeCount]),
            new(104 + stagger, 556 + (_popupSequence % (UpgradeCount >= 5 ? 5 : 2)) * 40), new(440, 78),
            UpgradeCount >= 7 ? 64 : 35 + UpgradeCount * 3, Accents[UpgradeCount]);
    }

    private void UpdateLabels()
    {
        _rate.Text = "+$" + IdleAi.NumberFormatter.Format(Payouts[UpgradeCount]);
        _rate.AddThemeColorOverride("font_color", Accents[UpgradeCount]);
        _level.Text = UpgradeCount == 7 ? "OUTPUT OVERFLOW  •  SYSTEM UNSTABLE" : UpgradeCount >= 5 ? "LEVEL " + Levels[UpgradeCount] + "  •  LIMIT EXCEEDED" : "LEVEL " + Levels[UpgradeCount] + "  •  SERVER RACK";
    }

    private void Reveal(Control control, double at)
    {
        control.Visible = Elapsed >= at;
        control.Modulate = new(1, 1, 1, Mathf.Clamp((float)(Elapsed - at) / .18f, 0, 1));
    }

    public void ResetReel()
    {
        _buttonTween?.Kill();
        foreach (var layer in new[] { _effects, _touch })
            foreach (Node child in layer.GetChildren()) { layer.RemoveChild(child); child.QueueFree(); }
        Elapsed = 0; UpgradeCount = 0; _popupClock = 0; _popupSequence = 0; _shake = 0; Overload = 0; _impact = 0;
        _world.Position = Vector2.Zero; _world.Scale = Vector2.One; _world.Show(); _touch.Show();
        _machine.Rotation = 0;
        _rate.Position = new(48, 306); _upgrade.Position = new(94, 918); _level.Modulate = Colors.White;
        _energy.Intensity = 0; _energy.QueueRedraw();
        _distortion.SetShaderParameter("intensity", 0f);
        _distortion.SetShaderParameter("impact", 0f); _glitch.Hide();
        _machine.ResetMotion();
        _machine.ShowStage(new("SERVER RACK", "res://promo_assets/server.png", 12, 0, Accents[0]));
        _machine.VisualIntensity = 0;
        _machine.SetPowerLevel(1);
        _upgrade.Text = "UPGRADE"; _upgrade.Scale = Vector2.One;
        _buttonStyle.BorderColor = Accents[0]; _buttonStyle.ShadowColor = new(Accents[0], .25f);
        _ending.Hide(); _broken.Hide(); _footer.Hide();
        UpdateLabels();
        SpawnMoney(); // Production on the very first frame; no logo or intro.
    }

    public override void _UnhandledKeyInput(InputEvent input)
    {
        if (input is InputEventKey { Pressed: true, Echo: false, Keycode: Key.R })
        { ResetReel(); GetViewport().SetInputAsHandled(); }
    }

    public override void _ExitTree() => _buttonTween?.Kill();
}
