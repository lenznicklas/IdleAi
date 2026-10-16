using Godot;
using System;

namespace IdleAi;

/// <summary>Lazy, static collection pages layered over Stats; Stats retains its scroll position.</summary>
public sealed class AiArchiveOverlayController
{
    private static readonly Color Cyan = new("72dafa"), Gold = new("e8bd61");
    private readonly Game _root;
    private readonly AiArchiveProgressService _progress;
    private Control? _overlay;
    private VBoxContainer _content = null!;
    private ScrollContainer _scroll = null!;
    private Button _back = null!;
    private MobileScrollController _motion = null!;
    private AiArchiveSnapshot _snapshot = null!;
    private int _pageGeneration;

    public AiArchiveOverlayController(Game root, AiArchiveProgressService progress)
    { _root = root; _progress = progress; }

    public void Open()
    {
        if (_overlay == null) Create();
        _snapshot = _progress.Capture();
        _overlay!.Show();
        Overview();
        _root.MoveChild(_overlay, _root.GetChildCount() - 1);
    }

    private void Create()
    {
        _overlay = new Control { Name = "AiArchiveOverlay", Visible = false, MouseFilter = Control.MouseFilterEnum.Stop };
        _root.AddChild(_overlay); _overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var panel = new PanelContainer(); _overlay.AddChild(panel);
        panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        var panelStyle = Style(new Color("091323"), Cyan);
        panelStyle.SetBorderWidthAll(0);
        panel.AddThemeStyleboxOverride("panel", panelStyle);
        var margin = new MarginContainer(); panel.AddChild(margin);
        foreach (string edge in new[] { "left", "right", "top", "bottom" }) margin.AddThemeConstantOverride("margin_" + edge, 28);
        var body = new VBoxContainer(); margin.AddChild(body); body.AddThemeConstantOverride("separation", 14);
        var header = new Label { Text = "AI ARCHIVE", CustomMinimumSize = new Vector2(0, 56), VerticalAlignment = VerticalAlignment.Center };
        header.AddThemeFontSizeOverride("font_size", 30); header.AddThemeColorOverride("font_color", Cyan); body.AddChild(header);
        _back = NavigationButton("‹  ALL CATEGORIES", Overview); body.AddChild(_back);
        _scroll = new ScrollContainer { SizeFlagsVertical = Control.SizeFlags.ExpandFill, HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        body.AddChild(_scroll);
        _content = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _content.AddThemeConstantOverride("separation", 12); _scroll.AddChild(_content);
        _motion = new MobileScrollController(); _overlay.AddChild(_motion); _motion.Setup(_scroll);
        var close = OverlayCloseButton.Add(panel, Close);
        // Browsing gets only a small brightness response, with no tween or haptic.
        close.ButtonDown += () => close.SelfModulate = new Color(0.92f, 0.92f, 0.92f);
        close.ButtonUp += () => close.SelfModulate = Colors.White;
        close.MouseExited += () => close.SelfModulate = Colors.White;
        close.VisibilityChanged += () => close.SelfModulate = Colors.White;
    }

    private void Clear()
    {
        _pageGeneration++;
        // Keep native containers visible for layout, but never draw a partial page.
        RenderingServer.CanvasItemSetVisible(_overlay!.GetCanvasItem(), false);
        _motion.ResetMotion(); _scroll.ScrollVertical = 0;
        foreach (Node child in _content.GetChildren()) { _content.RemoveChild(child); child.QueueFree(); }
    }
    private void Overview()
    {
        Clear(); _back.Hide();
        Text(_content, $"DISCOVERED  ·  {_snapshot.Percent}%", 28, Gold);
        Text(_content, $"{_snapshot.Completed} / {_snapshot.Total} ENTRIES", 18, Cyan);
        Text(_content, "Track everything you've discovered.", 18);
        foreach (var category in _snapshot.Categories)
        {
            var button = NavigationButton($"{category.Name}  {category.Completed}/{category.Total}  ›", () => Category(category));
            if (category.Total > 0 && category.Completed == category.Total) button.AddThemeColorOverride("font_color", Gold);
            _content.AddChild(button);
        }
        FinishPage();
    }
    private void Category(AiArchiveCategory category)
    {
        Clear(); _back.Show();
        Text(_content, $"{category.Name}  ·  {category.Completed} / {category.Total}", 25, Cyan);
        Text(_content, category.Description, 17);
        foreach (var entry in category.Entries)
        {
            var card = new PanelContainer(); card.AddThemeStyleboxOverride("panel", Style(new Color("102039"), entry.Accent ?? (entry.Complete ? Gold : new Color("254d6b"))));
            _content.AddChild(card);
            var row = new HBoxContainer(); row.AddThemeConstantOverride("separation", 14); card.AddChild(row);
            if (entry.BadgeNumber > 0 && entry.Icon != null)
                row.AddChild(new NumberedBadgeView(entry.Icon, entry.BadgeNumber) { CustomMinimumSize = new Vector2(144, 144) });
            else if (entry.Icon != null)
            {
                var icon = new TextureRect { Texture = entry.Icon, CustomMinimumSize = new Vector2(68, 68),
                    ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                    TextureFilter = category.Name == "EVENT BADGES" ? CanvasItem.TextureFilterEnum.LinearWithMipmaps : CanvasItem.TextureFilterEnum.ParentNode,
                    Modulate = entry.Concealed ? new Color(0.03f, 0.05f, 0.08f, 1) : entry.Complete ? Colors.White : new Color(0.45f, 0.5f, 0.6f),
                    MouseFilter = Control.MouseFilterEnum.Ignore };
                row.AddChild(icon);
            }
            var labels = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill }; row.AddChild(labels);
            Text(labels, entry.Concealed ? "???" : entry.Name, 21, entry.Complete ? Gold : Cyan);
            Text(labels, entry.Concealed ? "Undiscovered" : entry.Detail, 16);
            if (category.Name == "ACHIEVEMENTS") Text(labels, entry.Complete ? "Unlocked locally" : "Locked", 15, entry.Complete ? Gold : null);
        }
        FinishPage();
    }

    private void Close()
    {
        _pageGeneration++;
        _motion.ResetMotion(); _overlay!.Hide();
    }

    private void FinishPage()
    {
        // Newly built labels otherwise receive larger/bolder fonts on the next
        // global readability scan, changing card bounds after they are on screen.
        _root.GetNodeOrNull<UiReadabilityController>("UiReadabilityController")?.ApplyToSubtree(_overlay!);
        int generation = _pageGeneration;
        // Text shaping and wrapped minimum sizes use native deferred queues.
        // Enable drawing only after those queues settle, without any tween,
        // temporary scale, opacity change, or alteration to the final layout.
        Callable.From(() => Callable.From(() =>
        {
            if (generation != _pageGeneration || !_overlay!.IsVisibleInTree()) return;
            RenderingServer.CanvasItemSetVisible(_overlay.GetCanvasItem(), true);
        }).CallDeferred()).CallDeferred();
    }

    internal static Label Text(VBoxContainer parent, string text, int size, Color? color = null)
    {
        var label = new Label { Text = text, AutowrapMode = TextServer.AutowrapMode.WordSmart, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        label.AddThemeFontSizeOverride("font_size", size); label.AddThemeColorOverride("font_color", color ?? new Color("a4b9cf")); parent.AddChild(label); return label;
    }
    internal static Button NavigationButton(string text, Action action)
    {
        var button = new Button { Text = text, ClipText = true, TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis, CustomMinimumSize = new Vector2(0, 68), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        button.AddThemeFontSizeOverride("font_size", 20); button.AddThemeColorOverride("font_color", Cyan);
        // Navigation stays in the normal palette; gold is reserved for collection status.
        // Define every interactive state so touch/keyboard input cannot fall back to a loud theme.
        var background = new Color("102039");
        var border = new Color("295672");
        var pressed = Style(background.Darkened(0.08f), border);
        button.AddThemeStyleboxOverride("normal", Style(background, border));
        button.AddThemeStyleboxOverride("hover", Style(background.Lightened(0.035f), border));
        button.AddThemeStyleboxOverride("pressed", pressed);
        button.AddThemeStyleboxOverride("hover_pressed", pressed);
        button.AddThemeStyleboxOverride("focus", Style(Colors.Transparent, border.Lightened(0.08f)));
        button.Pressed += action; return button;
    }
    internal static StyleBoxFlat Style(Color background, Color border) => new() {
        BgColor = background, BorderColor = border, BorderWidthLeft = 1, BorderWidthRight = 1, BorderWidthTop = 1, BorderWidthBottom = 1,
        CornerRadiusTopLeft = 12, CornerRadiusTopRight = 12, CornerRadiusBottomLeft = 12, CornerRadiusBottomRight = 12,
        ContentMarginLeft = 16, ContentMarginRight = 16, ContentMarginTop = 14, ContentMarginBottom = 14
    };
}
