using System;
using System.Collections.Generic;
using Godot;

namespace IdleAi.Promo;

public sealed record ChooseMachineOption(string Name, string Texture, double ProductionPerSecond,
    Color Accent);

public sealed record ChooseMachineRoundData(ChooseMachineOption LeftMachine, ChooseMachineOption RightMachine);

// Recording-only values and effects. No game services or persistent state.
public partial class ChooseMachineAd : Control
{
    private enum Phase { Choosing, Confirmation, Producing, Transitioning, Finished }
    private const double ProductionDuration = 2.5;
    private readonly ChooseMachineRoundData[] _rounds =
    {
        new(new("BASIC\nPROCESSOR", "res://promo_assets/basic.png", 25, new Color("54dcff")),
            new("AI\nDESKTOP", "res://promo_assets/desktop.png", 100, new Color("ff85db"))),
        new(new("SERVER\nRACK", "res://promo_assets/server.png", 500, new Color("54dcff")),
            new("AI\nSUPERCOMPUTER", "res://promo_assets/supercomputer.png", 2500, new Color("ff85db"))),
        new(new("QUANTUM\nSERVER", "res://promo_assets/quantum.png", 10000, new Color("54dcff")),
            new("SINGULARITY\nCORE", "res://promo_assets/singularity.png", 50000, new Color("ff85db")))
    };
    public int CurrentRound { get; private set; }
    public double PromoMoney { get; private set; }
    public bool IsChoosing => _phase == Phase.Choosing;
    public bool IsFinished => _phase == Phase.Finished;
    private ChooseMachineOption Option(int index) => index == 0
        ? _rounds[CurrentRound].LeftMachine : _rounds[CurrentRound].RightMachine;
    private static double Interval(int index) => index == 0 ? .5 : .2;
    private readonly Control[] _cards = new Control[2];
    private readonly TextureRect[] _machines = new TextureRect[2];
    private readonly StyleBoxFlat[] _styles = new StyleBoxFlat[2];
    private readonly Label[] _names = new Label[2];
    private readonly Label[] _rates = new Label[2];
    private readonly double[] _nextProduction = new double[2];
    private readonly float[] _energy = new float[2];
    private readonly List<Tween> _tweens = new();
    private readonly RandomNumberGenerator _random = new();
    private Control _content = null!, _effects = null!, _tapOverlay = null!;
    private Label _headline = null!, _cue = null!, _money = null!, _roundLabel = null!;
    private Font _font = null!;
    private PackedScene _tapScene = null!;
    private Phase _phase;
    private int _selected = -1;
    private double _elapsed;
    private float _clock, _shake;

    public override void _Ready()
    {
        _random.Randomize();
        _font = GD.Load<Font>("res://promo_assets/Orbitron-Regular.ttf");
        _tapScene = GD.Load<PackedScene>("res://TapIndicator.tscn");
        var background = new TextureRect
        {
            Texture = GD.Load<Texture2D>("res://promo_assets/background.png"),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
            MouseFilter = MouseFilterEnum.Ignore
        };
        AddChild(background);
        background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var shade = new ColorRect { Color = new Color(.015f, .025f, .06f, .91f), MouseFilter = MouseFilterEnum.Ignore };
        AddChild(shade);
        shade.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        // x=48..600 leaves the right-hand Reel controls clear; y=170..1040 is the safe area.
        _content = new Control { Size = new Vector2(720, 1280), MouseFilter = MouseFilterEnum.Ignore };
        AddChild(_content);
        _headline = Text(_content, "CHOOSE YOUR\nMACHINE", new Vector2(48, 180), new Vector2(552, 120), 40, Colors.White);
        _roundLabel = Text(_content, "", new Vector2(48, 140), new Vector2(552, 30), 18, new Color("a9bdcf"));
        _money = Text(_content, "$ 0", new Vector2(48, 315), new Vector2(552, 72), 46, new Color("60ffb8"));
        _money.Name = "MoneyCounter";
        for (int i = 0; i < 2; i++) BuildCard(i);
        _effects = new Control { Size = _content.Size, MouseFilter = MouseFilterEnum.Ignore };
        _content.AddChild(_effects);
        _cue = Text(_content, "", new Vector2(48, 945), new Vector2(552, 90), 26, Colors.White);
        var tapLayer = new CanvasLayer { Name = "TapFeedback", Layer = 100 };
        AddChild(tapLayer);
        _tapOverlay = new Control { MouseFilter = MouseFilterEnum.Ignore };
        tapLayer.AddChild(_tapOverlay);
        _tapOverlay.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        ResetScene();
    }

    private Label Text(Control parent, string value, Vector2 position, Vector2 size, int fontSize, Color color)
    {
        var label = new Label { Text = value, Position = position, Size = size,
            HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = MouseFilterEnum.Ignore };
        label.AddThemeFontOverride("font", _font);
        label.AddThemeFontSizeOverride("font_size", fontSize);
        label.AddThemeColorOverride("font_color", color);
        parent.AddChild(label);
        return label;
    }

    private void BuildCard(int index)
    {
        var option = Option(index);
        var style = new StyleBoxFlat { BgColor = new Color("101e32"), BorderColor = option.Accent,
            BorderWidthLeft = 2, BorderWidthRight = 2, BorderWidthTop = 2, BorderWidthBottom = 2,
            CornerRadiusTopLeft = 24, CornerRadiusTopRight = 24, CornerRadiusBottomLeft = 24,
            CornerRadiusBottomRight = 24, ShadowColor = new Color(option.Accent, .2f), ShadowSize = 12 };
        var card = new Panel { Name = index == 0 ? "WeakMachine" : "StrongMachine",
            Position = new Vector2(48 + index * 284, 410), Size = new Vector2(268, 480),
            PivotOffset = new Vector2(134, 240), MouseFilter = MouseFilterEnum.Ignore };
        card.AddThemeStyleboxOverride("panel", style);
        _content.AddChild(card);
        _cards[index] = card;
        _styles[index] = style;
        // Bound the artwork independently of its native texture size. Leave room for
        // idle motion / production punches so clipping is a safety net, not normal sizing.
        var imageArea = new Control { Name = "MachineImageContainer", ClipContents = true,
            MouseFilter = MouseFilterEnum.Ignore };
        card.AddChild(imageArea);
        imageArea.AnchorLeft = .08f;
        imageArea.AnchorRight = .92f;
        imageArea.AnchorTop = .04f;
        imageArea.AnchorBottom = .62f;
        var image = new TextureRect { Name = "MachineImage",
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = MouseFilterEnum.Ignore,
            Texture = GD.Load<Texture2D>(option.Texture) };
        imageArea.AddChild(image);
        image.AnchorLeft = .06f;
        image.AnchorRight = .94f;
        image.AnchorTop = .08f;
        image.AnchorBottom = .92f;
        image.Resized += () => image.PivotOffset = image.Size / 2;
        image.PivotOffset = image.Size / 2;
        _machines[index] = image;
        _names[index] = Text(card, option.Name, new Vector2(4, 307), new Vector2(260, 64), 21, Colors.White);
        _rates[index] = Text(card, "+" + FormatNumber(option.ProductionPerSecond) + " / SEC", new Vector2(4, 378), new Vector2(260, 35), 24, option.Accent);
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.Device == InputEvent.DeviceIdEmulation) return;
        Vector2 position;
        if (@event is InputEventScreenTouch { Pressed: true } touch) position = touch.Position;
        else if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } mouse) position = mouse.Position;
        else return;
        var ripple = _tapScene.Instantiate<TapIndicator>();
        ripple.Position = _tapOverlay.GetGlobalTransformWithCanvas().AffineInverse() * position;
        _tapOverlay.AddChild(ripple);
        if (_phase == Phase.Choosing)
            for (int i = 0; i < 2; i++)
                if (new Rect2(Vector2.Zero, _cards[i].Size).HasPoint(
                    _cards[i].GetGlobalTransformWithCanvas().AffineInverse() * position))
                { SelectMachine(i); break; }
        GetViewport().SetInputAsHandled();
    }

    public void SelectMachine(int index)
    {
        if (_phase != Phase.Choosing || index < 0 || index > 1) return;
        _selected = index;
        _phase = Phase.Confirmation; // Lock immediately, including simultaneous touches.
        _elapsed = 0;
        _cue.Text = "SELECTED";
        _cue.AddThemeColorOverride("font_color", Option(index).Accent);
        var tween = Track(CreateTween().SetParallel());
        tween.TweenProperty(_cards[index], "scale", Vector2.One * 1.055f, .22)
            .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(_cards[1 - index], "modulate", new Color(.68f, .68f, .68f), .22);
        Burst(index, index == 1 ? 16 : 6);
    }

    public override void _Process(double delta)
    {
        _clock += (float)delta;
        _elapsed += delta;
        _shake = Mathf.MoveToward(_shake, 0, (float)delta * 18);
        _content.Position = _shake > 0 ? new Vector2(_random.RandfRange(-_shake, _shake), _random.RandfRange(-_shake, _shake)) : Vector2.Zero;
        for (int i = 0; i < 2; i++)
        {
            _energy[i] = Mathf.MoveToward(_energy[i], 0, (float)delta * 5);
            float bob = Mathf.Sin(_clock * 2 + i) * 4;
            _machines[i].OffsetTop = bob;
            _machines[i].OffsetBottom = bob;
            _machines[i].Scale = Vector2.One * (1 + _energy[i] * .045f);
            _styles[i].ShadowSize = (int)(12 + (_selected == i ? 12 : 0) + _energy[i] * 10);
            _styles[i].ShadowColor = new Color(Option(i).Accent,
                (_selected == i ? .5f : .18f) + Mathf.Sin(_clock * 3) * .06f);
            _styles[i].BorderWidthLeft = _styles[i].BorderWidthRight =
                _styles[i].BorderWidthTop = _styles[i].BorderWidthBottom = _selected == i ? 4 : 2;
        }
        if (_phase == Phase.Confirmation && _elapsed >= .25)
        {
            _phase = Phase.Producing;
            _elapsed = 0;
            _cue.Text = "";
            _nextProduction[_selected] = Interval(_selected);
        }
        else if (_phase == Phase.Producing)
        {
            // Integrate only the active portion of the frame, including the last partial frame.
            double activeDelta = Math.Max(0, Math.Min(_elapsed, ProductionDuration) - Math.Max(0, _elapsed - delta));
            PromoMoney += Option(_selected).ProductionPerSecond * activeDelta;
            _money.Text = "$ " + FormatNumber(PromoMoney);
            while (_nextProduction[_selected] <= Math.Min(_elapsed, ProductionDuration) + .00001)
            {
                Produce(_selected);
                _nextProduction[_selected] += Interval(_selected);
            }
            if (_elapsed >= ProductionDuration)
            {
                if (CurrentRound < _rounds.Length - 1) TransitionToNextRound();
                else
                {
                    _phase = Phase.Finished;
                    _cue.Text = "BUILD YOUR\nAI EMPIRE";
                    _cue.AddThemeColorOverride("font_color", Option(_selected).Accent);
                }
            }
        }
        _tweens.RemoveAll(t => !t.IsValid() || !t.IsRunning());
    }

    private Tween Track(Tween tween) { _tweens.Add(tween); return tween; }

    private void Produce(int index)
    {
        var option = Option(index);
        _energy[index] = 1;
        var number = Text(_effects, "+$" + FormatNumber(option.ProductionPerSecond * Interval(index)),
            new Vector2(48 + index * 284 + _random.RandfRange(-16, 16), 565), new Vector2(268, 50),
            index == 1 ? 34 : 25, option.Accent);
        number.AddThemeColorOverride("font_outline_color", new Color("081220"));
        number.AddThemeConstantOverride("outline_size", 5);
        var tween = number.CreateTween().SetParallel();
        tween.TweenProperty(number, "position:y", 425f, .65).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(number, "modulate:a", 0f, .65);
        tween.Chain().TweenCallback(Callable.From(number.QueueFree));
        Burst(index, index == 1 ? 5 : 2);
        if (index == 1 && _selected == index) _shake = 1.4f;
    }

    private void Burst(int index, int count)
    {
        for (int i = 0; i < count; i++)
        {
            var mote = new ColorRect { Position = new Vector2(182 + index * 284, 650),
                Size = Vector2.One * (index == 1 ? 5 : 3), Color = Option(index).Accent, MouseFilter = MouseFilterEnum.Ignore };
            _effects.AddChild(mote);
            var tween = mote.CreateTween().SetParallel();
            tween.TweenProperty(mote, "position", mote.Position + Vector2.FromAngle(_random.RandfRange(0, Mathf.Tau)) * _random.RandfRange(25, 100), .4)
                .SetEase(Tween.EaseType.Out);
            tween.TweenProperty(mote, "modulate:a", 0f, .4);
            tween.Chain().TweenCallback(Callable.From(mote.QueueFree));
        }
    }

    private void ClearEffects()
    {
        foreach (Node effect in _effects.GetChildren()) { _effects.RemoveChild(effect); effect.QueueFree(); }
    }

    private void ShowRound()
    {
        ClearEffects();
        _selected = -1;
        _elapsed = 0;
        _shake = 0;
        _content.Position = Vector2.Zero;
        _roundLabel.Text = $"IDLE AI  •  ROUND {CurrentRound + 1} / 3";
        _cue.Text = "TAP A MACHINE";
        _cue.AddThemeColorOverride("font_color", Colors.White);
        for (int i = 0; i < 2; i++)
        {
            var option = Option(i);
            _machines[i].Texture = GD.Load<Texture2D>(option.Texture);
            _names[i].Text = option.Name;
            _rates[i].Text = "+" + FormatNumber(option.ProductionPerSecond) + " / SEC";
            _rates[i].AddThemeColorOverride("font_color", option.Accent);
            _styles[i].BorderColor = option.Accent;
            _cards[i].Scale = Vector2.One;
            _cards[i].Modulate = Colors.White;
            _machines[i].Scale = Vector2.One;
            _machines[i].OffsetTop = _machines[i].OffsetBottom = 0;
            _energy[i] = 0;
            _nextProduction[i] = 0;
        }
    }

    private void TransitionToNextRound()
    {
        _phase = Phase.Transitioning;
        _cue.Text = "";
        // Fade only the cards; money stays visible and never resets between rounds.
        var tween = Track(CreateTween().SetParallel());
        for (int i = 0; i < 2; i++) tween.TweenProperty(_cards[i], "modulate:a", 0f, .22);
        tween.Chain().TweenCallback(Callable.From(() =>
        {
            CurrentRound++;
            ShowRound();
            _cue.Text = "";
            foreach (var card in _cards) card.Modulate = new Color(1, 1, 1, 0);
        }));
        tween.Chain().TweenProperty(_cards[0], "modulate:a", 1f, .22);
        tween.Parallel().TweenProperty(_cards[1], "modulate:a", 1f, .22);
        tween.Chain().TweenCallback(Callable.From(() =>
        {
            _phase = Phase.Choosing;
            _cue.Text = "TAP A MACHINE";
        }));
    }

    private static string FormatNumber(double value)
    {
        string[] suffixes = { "", "K", "M", "B", "T" };
        int index = 0;
        while (value >= 1000 && index < suffixes.Length - 1) { value /= 1000; index++; }
        // Floor at the displayed precision so rounding never suggests unearned money.
        value = Math.Floor(value * 10 + .000001) / 10;
        return value.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + suffixes[index];
    }

    public void ResetScene()
    {
        foreach (var tween in _tweens) if (tween.IsValid()) tween.Kill();
        _tweens.Clear();
        foreach (Node ripple in _tapOverlay.GetChildren()) ripple.QueueFree();
        CurrentRound = 0;
        PromoMoney = 0;
        _clock = 0;
        _money.Text = "$ 0";
        _content.Modulate = Colors.White;
        ShowRound();
        _phase = Phase.Choosing;
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false } key) return;
        switch (key.Keycode)
        {
            case Key.R: ResetScene(); break;
            case Key.Key1: SelectMachine(0); break;
            case Key.Key2: SelectMachine(1); break;
            default: return;
        }
        GetViewport().SetInputAsHandled();
    }
}
