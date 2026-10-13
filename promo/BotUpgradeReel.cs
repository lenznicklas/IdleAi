using Godot;
using System;

namespace IdleAi.Promo;

// Recording-only state: no GameState, economy services, catalog mutations or save I/O.
public partial class BotUpgradeReel : Control
{
    private static readonly string[] Rarities = ["Common", "Rare", "Epic", "Legendary"];
    private static readonly Color[] ColorsByRarity = [new("a9bdcf"), new("54dcff"), new("bd78ff"), new("ffd36b")];
    private static readonly double[] Rates = [10, 1_000, 100_000, 10_000_000];
    public int RarityIndex { get; private set; }
    public double Money { get; private set; }
    public double IncomePerSecond => Rates[RarityIndex];

    private Label _money = null!, _income = null!, _rarity = null!;
    private TextureRect _bot = null!, _machine = null!;
    private Control _machineFrame = null!, _botFrame = null!;
    private PanelContainer _botCard = null!;
    private StyleBoxFlat _botStyle = null!;
    private Button _upgrade = null!;
    private ProgressBar _production = null!;
    private Tween? _upgradeAnimation;
    private MarginContainer _margin = null!;
    private Texture2D[] _botTextures = [];
    private double _cycle;
    private Control _effects = null!, _touchOverlay = null!, _endCard = null!;
    private Label _endText = null!;
    private ColorRect _endShade = null!;
    private double _hoverSeconds;
    private PackedScene _tapScene = null!;
    private double _popupElapsed, _popupMoney, _legendaryElapsed;
    private VBoxContainer _column = null!;
    private Control _bottomSpacer = null!;
    private int _popupSequence;
    public bool HasShownEndCard { get; private set; }

    public override void _Ready()
    {
        Theme = new Theme { DefaultFont = GD.Load<Font>("res://promo_assets/Orbitron-Regular.ttf"), DefaultFontSize = 20 };
        _botTextures = Array.ConvertAll(Rarities, rarity => GD.Load<Texture2D>($"res://promo_assets/bots/{rarity.ToLowerInvariant()}.png"));
        _tapScene = GD.Load<PackedScene>("res://TapIndicator.tscn");
        var background = new TextureRect
        {
            Texture = GD.Load<Texture2D>("res://promo_assets/background.png"),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered, MouseFilter = MouseFilterEnum.Ignore
        };
        AddChild(background); background.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var shade = new ColorRect { Color = new Color(.015f, .025f, .06f, .88f), MouseFilter = MouseFilterEnum.Ignore };
        AddChild(shade); shade.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var margin = _margin = new MarginContainer { MouseFilter = MouseFilterEnum.Ignore };
        AddChild(margin); margin.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        foreach (string side in new[] { "left", "right", "top", "bottom" }) margin.AddThemeConstantOverride("margin_" + side, 48);
        var column = _column = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        column.AddThemeConstantOverride("separation", 16); margin.AddChild(column);
        column.AddChild(Text("IDLE AI", 22, new Color("a9bdcf")));
        _money = Text("$0", 60, Godot.Colors.White); column.AddChild(_money);
        _income = Text("", 23, new Color("60ffb8")); column.AddChild(_income);
        column.AddChild(new Control { SizeFlagsVertical = SizeFlags.ExpandFill, SizeFlagsStretchRatio = .6f, MouseFilter = MouseFilterEnum.Ignore });
        _machineFrame = ArtFrame("res://promo_assets/server.png", 290, out _machine); column.AddChild(_machineFrame);
        _production = new ProgressBar { CustomMinimumSize = new Vector2(0, 8), ShowPercentage = false, MouseFilter = MouseFilterEnum.Ignore };
        var track = Box(new Color("15283f")); var fill = Box(new Color("60ffb8"));
        track.ContentMarginLeft = track.ContentMarginRight = track.ContentMarginTop = track.ContentMarginBottom = 0;
        fill.ContentMarginLeft = fill.ContentMarginRight = fill.ContentMarginTop = fill.ContentMarginBottom = 0;
        _production.AddThemeStyleboxOverride("background", track);
        _production.AddThemeStyleboxOverride("fill", fill);
        column.AddChild(_production);
        column.AddChild(Text("AUTOMATED PRODUCTION", 16, new Color("a9bdcf")));
        column.AddChild(new Control { SizeFlagsVertical = SizeFlags.ExpandFill, SizeFlagsStretchRatio = .35f, MouseFilter = MouseFilterEnum.Ignore });
        _rarity = Text("", 30, ColorsByRarity[0]); column.AddChild(_rarity);
        _botStyle = Box(new Color("101e32")); _botStyle.ShadowSize = 12;
        _botCard = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore };
        _botCard.AddThemeStyleboxOverride("panel", _botStyle); column.AddChild(_botCard);
        _botFrame = ArtFrame("res://promo_assets/bots/common.png", 200, out _bot); _botCard.AddChild(_botFrame);
        _upgrade = new Button { Name = "UpgradeBot", Text = "UPGRADE BOT", CustomMinimumSize = new Vector2(0, 90), FocusMode = FocusModeEnum.None };
        _upgrade.AddThemeFontSizeOverride("font_size", 27);
        var buttonStyle = Box(new Color("174967"));
        foreach (string state in new[] { "normal", "hover", "pressed", "hover_pressed" })
        {
            _upgrade.AddThemeStyleboxOverride(state, buttonStyle);
            _upgrade.AddThemeColorOverride(state == "normal" ? "font_color" : $"font_{state}_color", Godot.Colors.White);
        }
        _upgrade.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
        _upgrade.AddThemeStyleboxOverride("disabled", Box(new Color("242a37")));
        _upgrade.AddThemeConstantOverride("h_pressed_offset", 0); _upgrade.AddThemeConstantOverride("v_pressed_offset", 0);
        _upgrade.Pressed += UpgradeBot; column.AddChild(_upgrade);
        // Leave a generous bottom recording-safe area instead of pinning the CTA down.
        _bottomSpacer = new Control { SizeFlagsVertical = SizeFlags.ExpandFill, SizeFlagsStretchRatio = 1.4f, MouseFilter = MouseFilterEnum.Ignore };
        column.AddChild(_bottomSpacer);
        _effects = Layer("MoneyEffects", 10);
        _touchOverlay = Layer("TouchFeedback", 20);
        _endCard = Layer("EndCard", 30);
        var endShade = _endShade = new ColorRect { Name = "EndShade", Color = new Color(.015f, .025f, .06f, .88f), MouseFilter = MouseFilterEnum.Ignore };
        _endCard.AddChild(endShade); endShade.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var endCenter = new CenterContainer { ZIndex = 1, MouseFilter = MouseFilterEnum.Ignore };
        _endCard.AddChild(endCenter); endCenter.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _endText = Text("COMING SOON", 42, new Color("ffd978"));
        _endText.Name = "EndMessage";
        // ClipText removes Label's minimum width. A CenterContainer then allocates
        // zero width and clips the whole message, despite correct sibling order.
        _endText.ClipText = false;
        _endText.AddThemeColorOverride("font_shadow_color", new Color(1, .65f, .15f, .45f));
        _endText.AddThemeConstantOverride("shadow_outline_size", 5);
        endCenter.AddChild(_endText); _endCard.Hide();
        RefreshRarity(); ResizeArt(); Resized += ResizeArt;
    }

    private void ResizeArt()
    {
        // Container composition stays inside narrow/short portrait viewports.
        float factor = Mathf.Clamp(Size.Y / 1280f, .35f, 1.2f);
        _column.AddThemeConstantOverride("separation", (int)Mathf.Clamp(Size.Y / 80, 8, 16));
        _bottomSpacer.CustomMinimumSize = new Vector2(0, Size.Y * .06f);
        foreach (string side in new[] { "left", "right", "top", "bottom" })
            _margin.AddThemeConstantOverride("margin_" + side, (int)Mathf.Clamp(Size.X / 15, 16, 48));
        _machineFrame.CustomMinimumSize = new Vector2(0, 290 * factor);
        _botFrame.CustomMinimumSize = new Vector2(0, 200 * factor);
        _money.AddThemeFontSizeOverride("font_size", (int)Mathf.Clamp(Size.X / 12, 24, 60));
        _upgrade.AddThemeFontSizeOverride("font_size", (int)Mathf.Clamp(Size.X / 22, 16, 27));
        _endText.AddThemeFontSizeOverride("font_size", (int)Mathf.Clamp(Size.X / 18, 18, 42));
        _bot.PivotOffset = _bot.Size / 2;
    }

    public override void _Process(double delta)
    {
        _hoverSeconds += delta;
        // Only artwork moves; container bounds, card borders and button stay fixed.
        float hoverScale = Mathf.Clamp(Size.Y / 1280f, .5f, 1f);
        _machine.Position = new Vector2(0, (float)Math.Sin(_hoverSeconds * Math.Tau / 3.8) * 4 * hoverScale);
        _bot.Position = new Vector2(0, (float)Math.Sin(_hoverSeconds * Math.Tau / 3.2 + .7) * 5 * hoverScale);
        _bot.PivotOffset = _bot.Size / 2;
        Money += IncomePerSecond * delta;
        _money.Text = "$" + IdleAi.NumberFormatter.Format(Money);
        _cycle = (_cycle + delta * Math.Min(8, Math.Pow(2, RarityIndex))) % 1;
        _production.Value = _cycle * 100;
        _popupElapsed += delta;
        _popupMoney += IncomePerSecond * delta;
        double interval = RarityIndex switch { 0 => .9, 1 => .55, 2 => .32, _ => .2 };
        if (_popupElapsed >= interval)
        {
            // At most one popup per frame: no catch-up bursts after a recording hitch.
            SpawnMoneyPopup(_popupMoney); _popupElapsed = 0; _popupMoney = 0;
        }
        if (!HasShownEndCard && RarityIndex == 3 && (_legendaryElapsed += delta) >= 2.5) ShowEndCard();
    }

    public void UpgradeBot()
    {
        if (RarityIndex >= 3) return;
        RarityIndex++;
        _popupElapsed = _popupMoney = 0;
        RefreshRarity();
        // Feedback belongs to the completed rarity change, never to press/hold.
        _upgradeAnimation?.Kill();
        _bot.PivotOffset = _bot.Size / 2; _bot.Scale = Vector2.One * 1.05f;
        _botStyle.ShadowColor = new Color(ColorsByRarity[RarityIndex], .65f);
        _upgradeAnimation = CreateTween().SetParallel();
        _upgradeAnimation.TweenProperty(_bot, "scale", Vector2.One, .28).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        _upgradeAnimation.TweenProperty(_botStyle, "shadow_color", new Color(ColorsByRarity[RarityIndex], RarityIndex == 3 ? .40f : .16f), .35);
    }

    private Control Layer(string name, int zIndex)
    {
        var layer = new Control { Name = name, ZIndex = zIndex, MouseFilter = MouseFilterEnum.Ignore };
        AddChild(layer); layer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect); return layer;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event.Device == InputEvent.DeviceIdEmulation || _upgrade.Disabled) return;
        Vector2 position;
        if (@event is InputEventScreenTouch { Pressed: true } touch) position = touch.Position;
        else if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } mouse) position = mouse.Position;
        else return;
        var local = _upgrade.GetGlobalTransformWithCanvas().AffineInverse() * position;
        if (!new Rect2(Vector2.Zero, _upgrade.Size).HasPoint(local)) return;
        var ripple = _tapScene.Instantiate<TapIndicator>();
        ripple.Position = _touchOverlay.GetGlobalTransformWithCanvas().AffineInverse() * position;
        _touchOverlay.AddChild(ripple); // Native input still reaches the Button unchanged.
    }

    private void SpawnMoneyPopup(double amount)
    {
        if (_effects.GetChildCount() >= 6) return;
        var number = Text("+$" + IdleAi.NumberFormatter.Format(amount), RarityIndex == 3 ? 32 : 25, ColorsByRarity[RarityIndex]);
        number.Name = "MoneyPopup";
        number.AddThemeColorOverride("font_outline_color", new Color("081220"));
        number.AddThemeConstantOverride("outline_size", 5);
        float width = Mathf.Min(400, Size.X - 32);
        var machineRect = _machine.GetGlobalRect();
        var center = _effects.GetGlobalTransformWithCanvas().AffineInverse() * machineRect.GetCenter();
        float stagger = (_popupSequence++ % 3 - 1) * Mathf.Min(24, Size.X / 30);
        number.Position = new Vector2(Mathf.Clamp(center.X - width / 2 + stagger, 16, Size.X - width - 16), center.Y);
        number.Size = new Vector2(width, 48); _effects.AddChild(number);
        var tween = number.CreateTween().SetParallel();
        tween.TweenProperty(number, "position:y", number.Position.Y - 100, .65).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(number, "modulate:a", 0f, .65);
        tween.Chain().TweenCallback(Callable.From(number.QueueFree));
    }

    private void ShowEndCard()
    {
        HasShownEndCard = true;
        _endCard.Show();
        _endShade.Modulate = _endText.Modulate = new Color(1, 1, 1, 0);
        var tween = _endCard.CreateTween().SetParallel();
        tween.TweenProperty(_endShade, "modulate:a", 1f, .45).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(_endText, "modulate:a", 1f, .55).SetDelay(.2).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
    }

    private void RefreshRarity()
    {
        _bot.Texture = _botTextures[RarityIndex];
        _rarity.Text = RarityIndex == 3 ? "LEGENDARY" : Rarities[RarityIndex].ToUpperInvariant();
        _rarity.AddThemeColorOverride("font_color", ColorsByRarity[RarityIndex]);
        _income.Text = "+$" + IdleAi.NumberFormatter.Format(IncomePerSecond) + " / SEC";
        _botStyle.ShadowColor = new Color(ColorsByRarity[RarityIndex], RarityIndex == 3 ? .40f : .16f);
        _upgrade.Disabled = RarityIndex == 3;
        _upgrade.Text = RarityIndex == 3 ? "LEGENDARY" : "UPGRADE BOT";
    }

    private static Label Text(string text, int size, Color color)
    {
        var label = new Label { Text = text, HorizontalAlignment = HorizontalAlignment.Center, ClipText = true, MouseFilter = MouseFilterEnum.Ignore };
        label.AddThemeFontSizeOverride("font_size", size); label.AddThemeColorOverride("font_color", color);
        return label;
    }

    private static Control ArtFrame(string path, float height, out TextureRect image)
    {
        var frame = new Control { CustomMinimumSize = new Vector2(0, height), MouseFilter = MouseFilterEnum.Ignore };
        image = new TextureRect
        {
            Texture = GD.Load<Texture2D>(path), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered, MouseFilter = MouseFilterEnum.Ignore
        };
        frame.AddChild(image); image.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        return frame;
    }

    private static StyleBoxFlat Box(Color fill) => new()
    {
        BgColor = fill, CornerRadiusTopLeft = 22, CornerRadiusTopRight = 22,
        CornerRadiusBottomLeft = 22, CornerRadiusBottomRight = 22,
        ContentMarginLeft = 12, ContentMarginRight = 12, ContentMarginTop = 12, ContentMarginBottom = 12
    };

    public override void _ExitTree() => _upgradeAnimation?.Kill();
}
