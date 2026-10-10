using Godot;
using System;
using System.Collections.Generic;

namespace IdleAi;

public sealed class SkinPreviewOverlay
{
    private readonly Game _root;
    private readonly string _overlayName;

    private Control _overlay = null!;
    private PanelContainer _panel = null!;
    private Label _title = null!;
    private GridContainer _gallery = null!;
    private Label _hint = null!;

    private Tween? _openTween;
    private ulong _blockCloseUntil;

    public bool Visible =>
        _overlay != null
        && _overlay.Visible;

    public SkinPreviewOverlay(
        Game root,
        string overlayName = "SkinPreviewOverlay")
    {
        _root = root;
        _overlayName = overlayName;
    }

    public void Initialize()
    {
        CreateUi();
        Close();
    }

    // Backwards-compatible single-image preview used by existing callers.
    public void Open(
        Texture2D? texture,
        string title)
    {
        if (texture == null)
            return;

        OpenGallery(
            title,
            new Texture2D?[] { texture },
            new string[] { "PREVIEW" }
        );
    }

    // New gallery mode used by the compact 2-column skin cards.
    // Bot packs pass Common/Rare/Epic/Legendary, machine packs pass M1-M4.
    public void OpenGallery(
        string title,
        IReadOnlyList<Texture2D?> textures,
        IReadOnlyList<string>? labels = null)
    {
        ClearGallery();

        int visibleCount = 0;

        for (int i = 0; i < textures.Count; i++)
        {
            Texture2D? texture = textures[i];

            if (texture == null)
                continue;

            string label =
                labels != null && i < labels.Count
                    ? labels[i]
                    : "PREVIEW " + (i + 1);

            AddGalleryCell(
                texture,
                label
            );

            visibleCount++;
        }

        if (visibleCount == 0)
            return;

        _gallery.Columns =
            visibleCount == 1
                ? 1
                : 2;

        _title.Text = title;

        _hint.Text =
            visibleCount == 1
                ? "TAP ANYWHERE TO CLOSE"
                : "ALL VARIANTS • TAP ANYWHERE TO CLOSE";

        _blockCloseUntil =
            Time.GetTicksMsec()
            + 120;

        _overlay.Show();
        _overlay.MoveToFront();

        PlayOpenAnimation();
    }

    public void Close()
    {
        _openTween?.Kill();
        _openTween = null;

        if (_panel != null)
        {
            _panel.Scale = Vector2.One;
            _panel.Modulate = Colors.White;
        }

        if (_overlay != null)
            _overlay.Hide();
    }

    private void CreateUi()
    {
        _overlay =
            new Control
            {
                Name = _overlayName,
                MouseFilter = Control.MouseFilterEnum.Stop,
                ZIndex = 1000
            };

        _overlay.SetAnchorsAndOffsetsPreset(
            Control.LayoutPreset.FullRect
        );

        _overlay.GuiInput += OnOverlayInput;
        _root.AddChild(_overlay);

        ColorRect dim =
            new()
            {
                Color = new Color(0, 0, 0, 0.82f),
                MouseFilter = Control.MouseFilterEnum.Ignore
            };

        dim.SetAnchorsAndOffsetsPreset(
            Control.LayoutPreset.FullRect
        );

        _overlay.AddChild(dim);

        MarginContainer outer =
            new()
            {
                MouseFilter = Control.MouseFilterEnum.Ignore
            };

        outer.SetAnchorsAndOffsetsPreset(
            Control.LayoutPreset.FullRect
        );

        outer.AddThemeConstantOverride("margin_left", 22);
        outer.AddThemeConstantOverride("margin_top", 34);
        outer.AddThemeConstantOverride("margin_right", 22);
        outer.AddThemeConstantOverride("margin_bottom", 34);

        _overlay.AddChild(outer);

        _panel =
            new PanelContainer
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore
            };

        _panel.AddThemeStyleboxOverride(
            "panel",
            CreatePanelStyle()
        );

        outer.AddChild(_panel);

        MarginContainer margin =
            new()
            {
                MouseFilter = Control.MouseFilterEnum.Ignore
            };

        margin.AddThemeConstantOverride("margin_left", 20);
        margin.AddThemeConstantOverride("margin_top", 20);
        margin.AddThemeConstantOverride("margin_right", 20);
        margin.AddThemeConstantOverride("margin_bottom", 18);

        _panel.AddChild(margin);

        VBoxContainer box =
            new()
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore
            };

        box.AddThemeConstantOverride("separation", 14);
        margin.AddChild(box);

        _title =
            new Label
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                CustomMinimumSize = new Vector2(0, 54),
                MouseFilter = Control.MouseFilterEnum.Ignore
            };

        _title.AddThemeFontSizeOverride("font_size", 23);
        _title.AddThemeColorOverride("font_color", Colors.White);
        box.AddChild(_title);

        _gallery =
            new GridContainer
            {
                Columns = 2,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore
            };

        _gallery.AddThemeConstantOverride("h_separation", 12);
        _gallery.AddThemeConstantOverride("v_separation", 12);
        box.AddChild(_gallery);

        _hint =
            new Label
            {
                Text = "TAP ANYWHERE TO CLOSE",
                HorizontalAlignment = HorizontalAlignment.Center,
                CustomMinimumSize = new Vector2(0, 32),
                MouseFilter = Control.MouseFilterEnum.Ignore
            };

        _hint.AddThemeFontSizeOverride("font_size", 12);
        _hint.AddThemeColorOverride(
            "font_color",
            new Color(0.68f, 0.72f, 0.80f, 1.0f)
        );

        box.AddChild(_hint);
    }

    private void ClearGallery()
    {
        if (_gallery == null)
            return;

        foreach (Node child in _gallery.GetChildren())
        {
            _gallery.RemoveChild(child);
            child.QueueFree();
        }
    }

    private void AddGalleryCell(
        Texture2D texture,
        string labelText)
    {
        PanelContainer frame =
            new()
            {
                CustomMinimumSize = new Vector2(0, 250),
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore
            };

        frame.AddThemeStyleboxOverride(
            "panel",
            CreateImageFrameStyle()
        );

        _gallery.AddChild(frame);

        MarginContainer margin = new();
        margin.MouseFilter = Control.MouseFilterEnum.Ignore;
        margin.AddThemeConstantOverride("margin_left", 10);
        margin.AddThemeConstantOverride("margin_top", 10);
        margin.AddThemeConstantOverride("margin_right", 10);
        margin.AddThemeConstantOverride("margin_bottom", 10);
        frame.AddChild(margin);

        VBoxContainer box =
            new()
            {
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore
            };

        box.AddThemeConstantOverride("separation", 6);
        margin.AddChild(box);

        Label label =
            new()
            {
                Text = labelText,
                HorizontalAlignment = HorizontalAlignment.Center,
                CustomMinimumSize = new Vector2(0, 26),
                MouseFilter = Control.MouseFilterEnum.Ignore
            };

        label.AddThemeFontSizeOverride("font_size", 12);
        label.AddThemeColorOverride("font_color", ShopUi.TextSecondary);
        box.AddChild(label);

        TextureRect image =
            new()
            {
                Texture = texture,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Ignore
            };

        box.AddChild(image);
    }

    private void OnOverlayInput(
        InputEvent @event)
    {
        if (Time.GetTicksMsec() < _blockCloseUntil)
            return;

        bool released =
            @event is InputEventScreenTouch touch
            && !touch.Pressed;

        released |=
            @event is InputEventMouseButton mouse
            && !mouse.Pressed
            && mouse.ButtonIndex == MouseButton.Left;

        if (!released)
            return;

        _overlay.GetViewport().SetInputAsHandled();

        Callable
            .From(Close)
            .CallDeferred();
    }

    private void PlayOpenAnimation()
    {
        _openTween?.Kill();

        _panel.PivotOffset = _panel.Size / 2.0f;
        _panel.Scale = new Vector2(0.94f, 0.94f);
        _panel.Modulate = new Color(1, 1, 1, 0.72f);

        _openTween = _root.CreateTween();
        _openTween.SetParallel(true);

        _openTween
            .TweenProperty(
                _panel,
                "scale",
                new Vector2(1.012f, 1.012f),
                0.18
            )
            .SetTrans(Tween.TransitionType.Cubic)
            .SetEase(Tween.EaseType.Out);

        _openTween.TweenProperty(
            _panel,
            "modulate:a",
            1.0f,
            0.18
        );

        _openTween
            .Chain()
            .TweenProperty(
                _panel,
                "scale",
                Vector2.One,
                0.10
            )
            .SetTrans(Tween.TransitionType.Sine)
            .SetEase(Tween.EaseType.Out);
    }

    private static StyleBoxFlat CreatePanelStyle()
    {
        return new StyleBoxFlat
        {
            BgColor = new Color(0.02f, 0.035f, 0.06f, 0.985f),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            BorderColor = new Color(0.0f, 0.65f, 1.0f, 0.82f),
            CornerRadiusTopLeft = 18,
            CornerRadiusTopRight = 18,
            CornerRadiusBottomLeft = 18,
            CornerRadiusBottomRight = 18,
            ShadowColor = new Color(0, 0, 0, 0.55f),
            ShadowSize = 18
        };
    }

    private static StyleBoxFlat CreateImageFrameStyle()
    {
        return new StyleBoxFlat
        {
            BgColor = new Color(0.015f, 0.025f, 0.045f, 0.96f),
            BorderWidthLeft = 1,
            BorderWidthTop = 1,
            BorderWidthRight = 1,
            BorderWidthBottom = 1,
            BorderColor = new Color(0.2f, 0.45f, 0.75f, 0.55f),
            CornerRadiusTopLeft = 14,
            CornerRadiusTopRight = 14,
            CornerRadiusBottomLeft = 14,
            CornerRadiusBottomRight = 14
        };
    }
}
