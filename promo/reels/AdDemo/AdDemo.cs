using System;
using System.Globalization;
using Godot;

namespace IdleAi.Promo;

public partial class AdDemo : Control
{
    public double PromoMoney { get; private set; }
    public double PromoIncomePerSecond { get; private set; }
    public int CurrentStage { get; private set; }
    public int CurrentLevel { get; private set; } = 1;
    public bool HasReachedComingSoonState { get; private set; }

    private Control _content = null!;
    private Control _tapOverlay = null!;
    private PackedScene _tapIndicatorScene = null!;
    private Control _lowerUi = null!;
    private Label _comingSoon = null!;
    private PromoMachine _machine = null!;
    private Label _money = null!;
    private Label _income = null!;
    private Label _name = null!;
    private Label _level = null!;
    private Label _progressText = null!;
    private Label _cue = null!;
    private ProgressBar _progress = null!;
    private Button _upgrade = null!;
    private ColorRect _flash = null!;
    private Font _font = null!;
    private Tween? _evolution;
    private Tween? _progressTween;
    private Tween? _buttonTween;
    private Tween? _flashTween;
    private Tween? _teaserTween;
    private bool _evolving;
    private double _cooldown;
    private double _displayMoney;
    private double _displayIncome;
    private float _clock;
    private float _shake;
    private readonly RandomNumberGenerator _random = new();
    private PromoMachineData Stage => PromoMachineData.Stages[CurrentStage];
    private bool CanEvolve => CurrentStage < PromoMachineData.Stages.Length - 1 && CurrentLevel > Stage.LevelsUntilEvolution;
    // Five taps fill the bar; the sixth is the dedicated EVOLVE tap.
    private double Cost => Math.Min(1e90, Stage.BaseCost * Math.Pow(2.6, Math.Min(CurrentLevel - 1, 180)));

    public override void _Ready()
    {
        _random.Randomize();
        _font = GD.Load<Font>("res://promo_assets/Orbitron-Regular.ttf");
        _tapIndicatorScene = GD.Load<PackedScene>("res://shared/TapIndicator.tscn");
        BuildUi();
        ResetPromo();
    }

    private void BuildUi()
    {
        var background = new TextureRect
        {
            Texture = GD.Load<Texture2D>("res://promo_assets/background.png"),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
            MouseFilter = MouseFilterEnum.Ignore
        };
        AddChild(background);
        background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var shade = new ColorRect { Color = new Color(.015f, .025f, .06f, .88f), MouseFilter = MouseFilterEnum.Ignore };
        AddChild(shade);
        shade.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);

        // Fixed 9:16 composition, scaled by the viewport. Reserve top/bottom and right-side Reel overlays.
        _content = new Control { Size = new Vector2(720, 1280), MouseFilter = MouseFilterEnum.Ignore };
        AddChild(_content);
        Text("IDLE AI", 100, 136, 460, 22, new Color("a9bdcf"));
        _money = Text("", 80, 175, 500, 62, Colors.White);
        _income = Text("", 80, 257, 500, 28, new Color("60ffb8"));
        _cue = Text("", 80, 319, 500, 20, Colors.White);
        _machine = new PromoMachine { Position = new Vector2(90, 363), Size = new Vector2(480, 400) };
        _content.AddChild(_machine);
        _name = Text("", 60, 770, 540, 27, Colors.White);
        _level = Text("", 80, 811, 500, 22, new Color("a9bdcf"));
        _progressText = Text("", 80, 869, 500, 22, Colors.White);
        _progress = new ProgressBar
        {
            Position = new Vector2(100, 912), Size = new Vector2(460, 24),
            MaxValue = 5, ShowPercentage = false, MouseFilter = MouseFilterEnum.Ignore
        };
        _progress.AddThemeStyleboxOverride("background", Box(new Color("15283f"), new Color("30445a"), 12));
        _content.AddChild(_progress);
        _upgrade = new Button
        {
            Position = new Vector2(90, 976), Size = new Vector2(480, 126),
            PivotOffset = new Vector2(240, 63), FocusMode = FocusModeEnum.None
        };
        _upgrade.AddThemeFontOverride("font", _font);
        _upgrade.AddThemeFontSizeOverride("font_size", 30);
        _upgrade.AddThemeColorOverride("font_color", new Color("061523"));
        _upgrade.AddThemeColorOverride("font_hover_color", new Color("061523"));
        _upgrade.AddThemeColorOverride("font_pressed_color", new Color("061523"));
        _upgrade.Pressed += Upgrade;
        _content.AddChild(_upgrade);
        _lowerUi = new Control
        {
            Name = "LowerUi", Size = new Vector2(720, 1280), MouseFilter = MouseFilterEnum.Ignore
        };
        _content.AddChild(_lowerUi);
        foreach (Control control in new Control[] { _name, _level, _progressText, _progress, _upgrade })
            control.Reparent(_lowerUi, false);
        _comingSoon = Text("COMING SOON", 60, 867, 540, 42, new Color("ffd978"));
        _comingSoon.Name = "ComingSoon";
        _comingSoon.PivotOffset = _comingSoon.Size / 2;
        _comingSoon.AddThemeColorOverride("font_shadow_color", new Color(1, .65f, .15f, .45f));
        _comingSoon.AddThemeConstantOverride("shadow_outline_size", 5);
        _comingSoon.Visible = false;
        _flash = new ColorRect { Color = new Color(1, 1, 1, 0), MouseFilter = MouseFilterEnum.Ignore };
        AddChild(_flash);
        _flash.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        // Separate canvas keeps ripples above flashes/UI and independent of machine screen shake.
        var tapLayer = new CanvasLayer { Name = "TapFeedback", Layer = 100 };
        AddChild(tapLayer);
        _tapOverlay = new Control { Name = "TapOverlay", MouseFilter = MouseFilterEnum.Ignore };
        tapLayer.AddChild(_tapOverlay);
        _tapOverlay.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
    }

    public override void _Input(InputEvent @event)
    {
        // Godot may synthesize a mouse event from a touch (or a touch from a mouse).
        // Only the original physical press gets a ripple, including each multitouch finger.
        if (@event.Device == InputEvent.DeviceIdEmulation) return;
        Vector2 position;
        switch (@event)
        {
            case InputEventScreenTouch { Pressed: true } touch:
                position = touch.Position;
                break;
            case InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } mouse:
                position = mouse.Position;
                break;
            default:
                return;
        }
        var indicator = _tapIndicatorScene.Instantiate<TapIndicator>();
        indicator.Position = _tapOverlay.GetGlobalTransformWithCanvas().AffineInverse() * position;
        _tapOverlay.AddChild(indicator);
        // Leave input unhandled so the upgrade button receives the same press normally.
    }

    private Label Text(string text, float x, float y, float width, int fontSize, Color color)
    {
        var label = new Label
        {
            Text = text, Position = new Vector2(x, y), Size = new Vector2(width, fontSize + 20),
            HorizontalAlignment = HorizontalAlignment.Center, MouseFilter = MouseFilterEnum.Ignore
        };
        label.AddThemeFontOverride("font", _font);
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color);
        _content.AddChild(label);
        return label;
    }

    private static StyleBoxFlat Box(Color fill, Color border, int radius) => new()
    {
        BgColor = fill, BorderColor = border,
        BorderWidthLeft = 2, BorderWidthRight = 2, BorderWidthTop = 2, BorderWidthBottom = 2,
        CornerRadiusTopLeft = radius, CornerRadiusTopRight = radius,
        CornerRadiusBottomLeft = radius, CornerRadiusBottomRight = radius,
        ShadowColor = new Color(border, .25f), ShadowSize = 12
    };

    public override void _Process(double delta)
    {
        _clock += (float)delta;
        _cooldown = Math.Max(0, _cooldown - delta);
        PromoMoney = Math.Min(1e100, PromoMoney + PromoIncomePerSecond * delta);
        double weight = 1 - Math.Exp(-delta * 14);
        _displayMoney += (PromoMoney - _displayMoney) * weight;
        _displayIncome += (PromoIncomePerSecond - _displayIncome) * weight;
        _money.Text = "$ " + FormatNumber(_displayMoney);
        _income.Text = "+" + FormatNumber(_displayIncome) + " / SEC";
        _shake = Mathf.MoveToward(_shake, 0, (float)delta * 22);
        _content.Position = _shake > 0 ? new Vector2(_random.RandfRange(-_shake, _shake), _random.RandfRange(-_shake, _shake)) : Vector2.Zero;
        if (!_evolving && !HasReachedComingSoonState && _cooldown <= 0)
            _upgrade.Scale = Vector2.One * (1 + Mathf.Sin(_clock * 4) * .012f);
    }

    public void Upgrade()
    {
        if (_evolving || HasReachedComingSoonState || _cooldown > 0) return;
        if (CanEvolve) { Evolve(); return; }
        _cooldown = .25;
        // Recording economy: sponsor enough money for each tap and retain a visible reserve.
        PromoMoney = Math.Max(PromoMoney, Cost * 1.5) - Cost;
        double previous = PromoIncomePerSecond;
        CurrentLevel++;
        UpdateIncome();
        PromoMoney = Math.Max(PromoMoney, Cost * 1.25);
        RefreshUi(true);
        _machine.Punch(Math.Min(CurrentLevel, 10));
        _buttonTween?.Kill();
        _buttonTween = CreateTween();
        _buttonTween.TweenProperty(_upgrade, "scale", Vector2.One * .95f, .06);
        _buttonTween.TweenProperty(_upgrade, "scale", Vector2.One, .16).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        Floating("+" + FormatNumber(PromoIncomePerSecond - previous) + "/SEC", Stage.Accent);
        Burst(12, Stage.Accent);
        if (CurrentLevel >= 4) { _shake = 2; Flash(.06f); }
    }

    public void Evolve()
    {
        if (_evolving || HasReachedComingSoonState || CurrentStage == PromoMachineData.Stages.Length - 1) return;
        _evolving = true;
        _upgrade.Disabled = true;
        _upgrade.Text = "EVOLVING...";
        _cue.Text = "UNLOCKING NEXT MACHINE";
        _progressTween?.Kill();
        _progress.Value = 5;
        _machine.ResetMotion();
        _evolution = CreateTween();
        _evolution.TweenProperty(_machine, "scale", Vector2.One * 1.14f, .20).SetTrans(Tween.TransitionType.Quad);
        _evolution.Parallel().TweenProperty(_machine, "modulate", new Color(1.8f, 1.8f, 1.8f), .20);
        _evolution.TweenCallback(Callable.From(() => { Burst(36, Stage.Accent); Flash(.65f); _shake = 5; }));
        _evolution.TweenProperty(_machine, "scale", Vector2.One * .05f, .12);
        _evolution.Parallel().TweenProperty(_machine, "modulate:a", 0f, .12);
        _evolution.TweenCallback(Callable.From(() =>
        {
            CurrentStage++;
            CurrentLevel = 1;
            UpdateIncome();
            PromoMoney = Math.Max(PromoMoney, Cost * 2);
            _machine.ShowStage(Stage);
            _machine.Scale = Vector2.One * .55f;
            RefreshUi(false);
            if (Stage.IsFinalTeaserStage) EnterComingSoonState();
            else
            {
                _cue.Text = "EVOLVED  •  " + FormatNumber(PromoIncomePerSecond) + "/SEC";
                Floating("INCOME MULTIPLIED!", Stage.Accent);
            }
        }));
        _evolution.TweenProperty(_machine, "scale", Vector2.One, .30).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        _evolution.TweenCallback(Callable.From(() => { _evolving = false; _upgrade.Disabled = HasReachedComingSoonState; _cooldown = .08; }));
    }

    private void EnterComingSoonState()
    {
        HasReachedComingSoonState = true;
        _buttonTween?.Kill();
        _progressTween?.Kill();
        _upgrade.Disabled = true;
        _upgrade.MouseFilter = MouseFilterEnum.Ignore;
        _upgrade.Scale = Vector2.One;
        _cue.Text = "";
        _comingSoon.Visible = true;
        _comingSoon.Modulate = new Color(1, 1, 1, 0);
        _comingSoon.Scale = Vector2.One * .96f;
        _teaserTween?.Kill();
        _teaserTween = CreateTween().SetParallel().SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        _teaserTween.TweenProperty(_lowerUi, "modulate:a", 0f, .65);
        _teaserTween.TweenProperty(_comingSoon, "modulate:a", 1f, .65).SetDelay(.20);
        _teaserTween.TweenProperty(_comingSoon, "scale", Vector2.One, .65).SetDelay(.20);
        _teaserTween.Chain().TweenCallback(Callable.From(() => _lowerUi.Visible = false));
    }

    public void ResetPromo() => SelectStage(0);

    public void SelectStage(int stage)
    {
        _evolution?.Kill();
        _buttonTween?.Kill();
        _flashTween?.Kill();
        _teaserTween?.Kill();
        foreach (Node node in GetTree().GetNodesInGroup("promo_transient")) node.QueueFree();
        _evolving = false;
        HasReachedComingSoonState = false;
        _lowerUi.Visible = true;
        _lowerUi.Modulate = Colors.White;
        _comingSoon.Visible = false;
        _comingSoon.Modulate = new Color(1, 1, 1, 0);
        _comingSoon.Scale = Vector2.One;
        _cooldown = 0;
        _shake = 0;
        CurrentStage = Math.Clamp(stage, 0, PromoMachineData.Stages.Length - 1);
        CurrentLevel = 1;
        UpdateIncome();
        PromoMoney = Stage.BaseCost * 2;
        _displayMoney = PromoMoney;
        _displayIncome = PromoIncomePerSecond;
        _machine.ResetMotion();
        _machine.ShowStage(Stage);
        _upgrade.Disabled = false;
        _upgrade.MouseFilter = MouseFilterEnum.Stop;
        _upgrade.Scale = Vector2.One;
        _flash.Color = new Color(1, 1, 1, 0);
        RefreshUi(false);
        if (Stage.IsFinalTeaserStage) EnterComingSoonState();
    }

    private void UpdateIncome()
    {
        double[] multipliers = { 1, 3, 8, 24, 70, 140 };
        PromoIncomePerSecond = Math.Min(1e90, Stage.BaseIncome *
            (CurrentLevel <= 6 ? multipliers[CurrentLevel - 1] : 140 * Math.Pow(3, Math.Min(CurrentLevel - 6, 160))));
    }

    private void RefreshUi(bool animate)
    {
        _machine.SetPowerLevel(CurrentLevel);
        _name.Text = Stage.Name;
        _level.Text = "LEVEL " + CurrentLevel;
        int filled = Math.Min(CurrentLevel - 1, Stage.LevelsUntilEvolution);
        _progressText.Text = $"{filled} / {Stage.LevelsUntilEvolution} UPGRADES";
        _cue.Text = CanEvolve ? "NEXT MACHINE READY!" : filled >= 3 ? "EVOLUTION APPROACHING" : "TAP TO AMPLIFY";
        _cue.AddThemeColorOverride("font_color", Stage.Accent);
        _income.AddThemeColorOverride("font_color", Stage.Accent);
        _progress.AddThemeStyleboxOverride("fill", Box(Stage.Accent, Stage.Accent, 12));
        _upgrade.Text = Stage.IsFinalTeaserStage ? "REVEALED" : CanEvolve ? "EVOLVE\nNEXT MACHINE" : "UPGRADE\n$" + FormatNumber(Cost);
        Color buttonColor = CanEvolve ? new Color("ffd978") : Stage.Accent;
        _upgrade.AddThemeStyleboxOverride("normal", Box(buttonColor, buttonColor.Lightened(.2f), 22));
        _upgrade.AddThemeStyleboxOverride("hover", Box(buttonColor.Lightened(.1f), buttonColor, 22));
        _upgrade.AddThemeStyleboxOverride("pressed", Box(buttonColor.Darkened(.15f), buttonColor, 22));
        _upgrade.AddThemeStyleboxOverride("disabled", Box(new Color("637080"), buttonColor, 22));
        _progressTween?.Kill();
        if (animate)
        {
            _progressTween = CreateTween();
            _progressTween.TweenProperty(_progress, "value", (double)filled, .18);
        }
        else _progress.Value = filled;
    }

    private void Floating(string text, Color color)
    {
        var label = Text(text, 40, 415, 580, 23, color);
        label.AddToGroup("promo_transient");
        var tween = label.CreateTween().SetParallel();
        tween.TweenProperty(label, "position:y", 343f, .65).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(label, "modulate:a", 0f, .65);
        tween.Chain().TweenCallback(Callable.From(label.QueueFree));
    }

    private void Burst(int count, Color color)
    {
        PromoVisuals.Burst(_content, new Vector2(330, 563), count, color, _random);
    }

    private void Flash(float strength)
    {
        _flashTween?.Kill();
        _flash.Color = new Color(1, 1, 1, strength);
        _flashTween = CreateTween();
        _flashTween.TweenProperty(_flash, "color:a", 0f, .25);
    }

    public static string FormatNumber(double value)
    {
        string[] suffixes = { "", "K", "M", "B", "T", "Qa", "Qi", "Sx", "Sp", "Oc", "No", "Dc" };
        int index = 0;
        while (value >= 1000 && index < suffixes.Length - 1) { value /= 1000; index++; }
        if (value >= 1000) return (value * Math.Pow(1000, index)).ToString("0.0e+0", CultureInfo.InvariantCulture);
        return value.ToString(value >= 100 ? "0" : value >= 10 ? "0.#" : "0.##", CultureInfo.InvariantCulture) + suffixes[index];
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false } key) return;
        switch (key.Keycode)
        {
            case Key.Space: Upgrade(); break;
            case Key.E: Evolve(); break;
            case Key.R: ResetPromo(); break;
            case Key.Key1: SelectStage(0); break;
            case Key.Key2: SelectStage(1); break;
            case Key.Key3: SelectStage(2); break;
            case Key.Key4: SelectStage(3); break;
            case Key.Key5: SelectStage(4); break;
            case Key.F:
                DisplayServer.WindowSetMode(DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Fullscreen
                    ? DisplayServer.WindowMode.Windowed : DisplayServer.WindowMode.Fullscreen);
                break;
            default: return;
        }
        GetViewport().SetInputAsHandled();
    }
}
