using Godot;
using System;
using System.Collections.Generic;

namespace IdleAi;

/*
 * Adds The Singularity to the existing ROOM NETWORK and, importantly, keeps
 * The Singularity as the active room context while MAP / SHOP / LAB / SETTINGS
 * are opened from it.
 *
 * Normal rooms stay alive underneath those pages. The Singularity used to be
 * different: SingularityController hid it as soon as any bottom navigation
 * button was pressed, so closing the page exposed the last normal room.
 *
 * This extension now remembers the Singularity context on ButtonDown (before
 * the existing Pressed -> Hide callback runs), watches all four navigation
 * pages, and restores The Singularity only when the last external page closes.
 * Selecting a normal room from MAP explicitly cancels that return context.
 */
public sealed partial class SingularityMapExtension
{
    private const float NodeWidth = 330.0f;
    private const float NodeHeight = 190.0f;
    private const float InfoPanelWidth = 146.0f;
    private const float BadgeWidth = 276.0f;

    private static readonly Vector2 SingularityPosition = new(48.0f, 12.0f);
    private static readonly Vector2 QuantumCenter = new(330.0f + 135.0f, 220.0f + 89.0f);

    private static readonly Color Accent = new(0.92f, 0.66f, 0.20f, 1.0f);

    private static readonly Texture2D UnlockedIcon =
        GD.Load<Texture2D>("res://assets/map/room_singularity.png");

    private static readonly Texture2D LockedIcon =
        GD.Load<Texture2D>("res://assets/map/locked_singularity.png");

    private static readonly string[] BottomNavigationPaths =
    [
        "MarginContainer/VBoxContainer/BottomBar/BarPanel/BarMargin/Buttons/MapButton",
        "MarginContainer/VBoxContainer/BottomBar/BarPanel/BarMargin/Buttons/ShopButton",
        "MarginContainer/VBoxContainer/BottomBar/BarPanel/BarMargin/Buttons/LabButton",
        "MarginContainer/VBoxContainer/BottomBar/BarPanel/BarMargin/Buttons/SettingsButton"
    ];

    private readonly Game _root;
    private readonly GameState _state;
    private readonly SingularityService _service;
    private readonly SingularityController _controller;
    private readonly MapController _map;

    private Control _mapPage = null!;
    private Control _canvas = null!;
    private Button _roomButton = null!;
    private TextureRect _icon = null!;
    private Label _stateLabel = null!;
    private Label _badgeLabel = null!;
    private SingularityMapLine _line = null!;

    private Control? _shopPage;
    private Control? _labPage;
    private Control? _settingsOverlay;

    private bool _shopVisibilityHooked;
    private bool _labVisibilityHooked;
    private bool _settingsVisibilityHooked;

    /*
     * This is the actual room-context flag. GameState.CurrentRoomIndex cannot
     * represent The Singularity because it is intentionally implemented as a
     * separate end-game room/service. Keeping the context here makes it behave
     * like a normal current room while MAP / SHOP / LAB / SETTINGS are open.
     *
     * It stays true when Singularity itself is temporarily hidden for another
     * page and is cleared only after a normal room was successfully selected.
     */
    private bool _singularityIsCurrentRoom;
    private bool _restoreQueued;

    public event Action<string>? MessageRequested;
    public event Action? GameStateChanged;

    public SingularityMapExtension(
        Game root,
        GameState state,
        SingularityService service,
        SingularityController controller,
        MapController map)
    {
        _root = root;
        _state = state;
        _service = service;
        _controller = controller;
        _map = map;
    }

    public void Initialize()
    {
        _map.ExternalRoomIsCurrent = () =>
            _singularityIsCurrentRoom || _controller.Visible;
        _mapPage = _root.GetNode<Control>("MapPage");

        ScrollContainer? scroll = FindDescendant<ScrollContainer>(_mapPage);
        if (scroll == null)
        {
            GD.PushWarning("Singularity map: Map ScrollContainer not found.");
            return;
        }

        CenterContainer? center = FindDescendant<CenterContainer>(scroll);
        if (center == null)
        {
            GD.PushWarning("Singularity map: map center not found.");
            return;
        }

        foreach (Node child in center.GetChildren())
        {
            if (child is Control control && control.CustomMinimumSize.X >= 600.0f)
            {
                _canvas = control;
                break;
            }
        }

        if (_canvas == null)
        {
            GD.PushWarning("Singularity map: canvas not found.");
            return;
        }

        HideFutureCard();
        CreateConnectionLine();
        CreateRoomNode();
        HookNormalRoomButtons();
        HookBottomNavigationContext();
        EnsureExternalPageHooks();

        _mapPage.VisibilityChanged += OnMapVisibilityChanged;
        _service.Changed += Refresh;

        /*
         * Settings is created dynamically and Lab is initialized after the
         * main GameUiController. Re-discover once deferred as well. ButtonDown
         * also calls EnsureExternalPageHooks(), so late-created controls are
         * still covered even if they appear after this deferred call.
         */
        Callable.From(EnsureExternalPageHooks).CallDeferred();

        Refresh();
    }

    private void HookBottomNavigationContext()
    {
        foreach (string path in BottomNavigationPaths)
        {
            Button? button = _root.GetNodeOrNull<Button>(path);
            if (button == null)
                continue;

            /*
             * ButtonDown happens before Pressed. SingularityController already
             * has Pressed += Hide, so remembering the context here avoids the
             * ordering bug that made _controller.Visible false by the time the
             * destination page emitted VisibilityChanged.
             */
            button.ButtonDown += RememberSingularityBeforeBottomNavigation;
        }
    }

    private void RememberSingularityBeforeBottomNavigation()
    {
        EnsureExternalPageHooks();

        if (_controller.Visible)
        {
            _singularityIsCurrentRoom = true;
        }
    }

    private void EnsureExternalPageHooks()
    {
        if (!_shopVisibilityHooked)
        {
            _shopPage ??= _root.GetNodeOrNull<Control>("ShopPage");
            if (_shopPage != null)
            {
                _shopPage.VisibilityChanged += OnExternalPageVisibilityChanged;
                _shopVisibilityHooked = true;
            }
        }

        if (!_labVisibilityHooked)
        {
            _labPage ??= _root.GetNodeOrNull<Control>("LabPage");
            if (_labPage != null)
            {
                _labPage.VisibilityChanged += OnExternalPageVisibilityChanged;
                _labVisibilityHooked = true;
            }
        }

        if (!_settingsVisibilityHooked)
        {
            _settingsOverlay ??= _root.GetNodeOrNull<Control>("SettingsOverlay");
            if (_settingsOverlay != null)
            {
                _settingsOverlay.VisibilityChanged += OnExternalPageVisibilityChanged;
                _settingsVisibilityHooked = true;
            }
        }
    }

    private bool AnyExternalPageVisible()
    {
        return _mapPage.Visible
            || (_shopPage?.Visible ?? false)
            || (_labPage?.Visible ?? false)
            || (_settingsOverlay?.Visible ?? false);
    }

    private void OnMapVisibilityChanged()
    {
        OnExternalPageVisibilityChanged();

        Callable
            .From(Refresh)
            .CallDeferred();
    }

    private void OnExternalPageVisibilityChanged()
    {
        EnsureExternalPageHooks();

        if (AnyExternalPageVisible())
        {
            /*
             * Fallback for programmatic page opens. This also covers the
             * Singularity header's own "‹ MAP" button, whose existing callback
             * hides Singularity before MapPage becomes visible.
             */
            if (_controller.Visible)
            {
                _singularityIsCurrentRoom = true;

                // Settings is a modal over the current room, so keep its
                // background visible unless a full page also covers it.
                if (_mapPage.Visible
                    || (_shopPage?.Visible ?? false)
                    || (_labPage?.Visible ?? false))
                {
                    _controller.Hide();
                }
            }

            if (_singularityIsCurrentRoom)
            {
                /*
                 * Hide() restores the last normal-room BottomBar theme. Reapply
                 * Singularity chrome deferred so it wins regardless of signal
                 * subscription order and the active room still looks/behaves
                 * like Singularity while an external page is open.
                 */
                Callable
                    .From(_controller.PreserveRoomChromeForExternalNavigation)
                    .CallDeferred();
            }

            return;
        }

        if (_singularityIsCurrentRoom && !_controller.Visible)
        {
            QueueSingularityRestore();
        }
    }

    private void QueueSingularityRestore()
    {
        if (_restoreQueued)
            return;

        _restoreQueued = true;
        RestoreSingularityAfterExternalNavigation();
    }

    private async void RestoreSingularityAfterExternalNavigation()
    {
        /*
         * Wait one frame before restoring. This is essential for two cases:
         *
         * 1. MAP -> SHOP/LAB/SETTINGS: MAP can hide before the next page shows.
         *    Waiting prevents a one-frame Singularity flash between pages.
         *
         * 2. Selecting a normal room from MAP: the room button's deferred
         *    callback clears the Singularity context before this check runs.
         */
        await _root.ToSignal(
            _root.GetTree(),
            SceneTree.SignalName.ProcessFrame
        );

        _restoreQueued = false;
        EnsureExternalPageHooks();

        if (!_singularityIsCurrentRoom || AnyExternalPageVisible())
            return;

        _controller.Open();
    }

    private void HideFutureCard()
    {
        foreach (Node child in _canvas.GetChildren())
        {
            if (child is PanelContainer panel
                && panel.Position.Y < 120.0f
                && panel.Size.X <= 230.0f)
            {
                panel.Hide();
            }
        }
    }

    private void CreateConnectionLine()
    {
        _line = new SingularityMapLine
        {
            Name = "SingularityMapConnection",
            MouseFilter = Control.MouseFilterEnum.Ignore
        };

        _line.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _canvas.AddChild(_line);

        /*
         * Route layer is normally the first canvas child. Put our line directly
         * above it, still behind all room buttons.
         */
        _canvas.MoveChild(
            _line,
            Math.Min(1, _canvas.GetChildCount() - 1)
        );

        _line.Configure(
            QuantumCenter,
            SingularityPosition + new Vector2(NodeWidth / 2.0f, NodeHeight / 2.0f),
            _service.Unlocked
        );
    }

    private void CreateRoomNode()
    {
        _roomButton = new Button
        {
            Name = "SingularityRoomNode",
            Position = SingularityPosition,
            Size = new Vector2(NodeWidth, NodeHeight),
            CustomMinimumSize = new Vector2(NodeWidth, NodeHeight),
            FocusMode = Control.FocusModeEnum.None,
            ClipContents = true
        };

        _roomButton.Pressed += OnPressed;
        _canvas.AddChild(_roomButton);

        Control iconHolder = new()
        {
            Position = new Vector2(16, 35),
            Size = new Vector2(96, 96),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        _roomButton.AddChild(iconHolder);

        _icon = new TextureRect
        {
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        _icon.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        iconHolder.AddChild(_icon);

        PanelContainer info = new()
        {
            Position = new Vector2(136, 28),
            Size = new Vector2(InfoPanelWidth, 104),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };

        info.AddThemeStyleboxOverride(
            "panel",
            new StyleBoxFlat
            {
                BgColor = new Color(0.015f, 0.025f, 0.045f, 0.78f),
                CornerRadiusTopLeft = 14,
                CornerRadiusTopRight = 14,
                CornerRadiusBottomLeft = 14,
                CornerRadiusBottomRight = 14
            }
        );
        _roomButton.AddChild(info);

        MarginContainer infoMargin = new();
        infoMargin.AddThemeConstantOverride("margin_left", 10);
        infoMargin.AddThemeConstantOverride("margin_top", 9);
        infoMargin.AddThemeConstantOverride("margin_right", 10);
        infoMargin.AddThemeConstantOverride("margin_bottom", 9);
        info.AddChild(infoMargin);

        VBoxContainer box = new();
        box.AddThemeConstantOverride("separation", 3);
        infoMargin.AddChild(box);

        Label index = CreateLabel(10, "ROOM 5");
        index.HorizontalAlignment = HorizontalAlignment.Left;
        index.Modulate = Accent.Lightened(0.20f);
        box.AddChild(index);

        Label name = CreateLabel(16, "THE\nSINGULARITY");
        name.HorizontalAlignment = HorizontalAlignment.Left;
        box.AddChild(name);

        _stateLabel = CreateLabel(10, "");
        _stateLabel.HorizontalAlignment = HorizontalAlignment.Left;
        box.AddChild(_stateLabel);

        PanelContainer badge = new()
        {
            Position = new Vector2(27, 151),
            Size = new Vector2(BadgeWidth, 30),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        _roomButton.AddChild(badge);

        _badgeLabel = CreateLabel(11, "");
        badge.AddChild(_badgeLabel);
        RefreshBadgeStyle(badge);
    }

    private void OnPressed()
    {
        Input.VibrateHandheld(14, 0.14f);

        if (!_service.Unlocked)
        {
            bool room4Unlocked =
                _state.RoomStates.Count > 3
                && _state.RoomStates[3].Unlocked;

            if (!room4Unlocked)
            {
                MessageRequested?.Invoke("Unlock Room 4 first.");
                Refresh();
                return;
            }

            SingularityActionResult result = _service.Unlock(_state);
            MessageRequested?.Invoke(result.Message);

            if (!result.Changed)
            {
                Refresh();
                return;
            }

            GameStateChanged?.Invoke();
            Refresh();
        }

        /*
         * The player deliberately entered Singularity from MAP. Temporarily
         * clear the context while MapPage is hidden so its VisibilityChanged
         * callback does not schedule a redundant restore. Then make
         * Singularity the current room and open it normally.
         */
        _singularityIsCurrentRoom = false;
        _mapPage.Hide();
        _singularityIsCurrentRoom = true;
        _controller.Open();
    }

    private void HookNormalRoomButtons()
    {
        foreach (Button button in FindDescendants<Button>(_canvas))
        {
            if (button == _roomButton)
                continue;

            /*
             * Restore the pre-Singularity bar BEFORE MapController handles a
             * normal-room press. If the room changes, GameUiController can then
             * apply the newly selected room's theme without us overwriting it
             * afterwards. If the press fails (locked room), the deferred final
             * check below simply reapplies Singularity chrome.
             */
            button.ButtonDown += PrepareNormalRoomSelection;

            button.Pressed += () =>
                Callable
                    .From(FinalizeNormalRoomSelection)
                    .CallDeferred();
        }
    }

    private void PrepareNormalRoomSelection()
    {
        if (!_singularityIsCurrentRoom)
            return;

        _controller.PrepareNormalRoomSelectionFromExternalNavigation();
    }

    private void FinalizeNormalRoomSelection()
    {
        if (!_singularityIsCurrentRoom)
            return;

        if (!_mapPage.Visible)
        {
            _singularityIsCurrentRoom = false;
            _controller.FinishExternalNavigationToNormalRoom();
            Refresh();
            return;
        }

        /* Locked/failed room selection: MAP stayed open, so Singularity is
         * still the current room. Put its bar straight back. */
        Callable
            .From(_controller.PreserveRoomChromeForExternalNavigation)
            .CallDeferred();
    }

    public void Refresh()
    {
        if (_roomButton == null || !GodotObject.IsInstanceValid(_roomButton))
            return;

        bool unlocked = _service.Unlocked;
        _map.Refresh();
        bool current =
            _singularityIsCurrentRoom
            && _mapPage.Visible;

        _icon.Texture = unlocked ? UnlockedIcon : LockedIcon;

        if (current)
        {
            _stateLabel.Text = "TAP TO ENTER";
            _stateLabel.Modulate = Colors.White;
            _badgeLabel.Text = "● CURRENT";
        }
        else if (unlocked)
        {
            _stateLabel.Text = "TAP TO ENTER";
            _stateLabel.Modulate = Colors.White;
            _badgeLabel.Text = "ENTER";
        }
        else
        {
            _stateLabel.Text = "LOCKED";
            _stateLabel.Modulate = new Color(0.64f, 0.67f, 0.72f, 1.0f);

            bool room4Unlocked =
                _state.RoomStates.Count > 3
                && _state.RoomStates[3].Unlocked;

            _badgeLabel.Text = !room4Unlocked
                ? "UNLOCK ROOM 4 FIRST"
                : "UNLOCK • "
                    + NumberFormatter.Format(
                        SingularityService.TestUnlockTokenCost
                    );
        }

        ApplyNodeStyle(unlocked);

        if (_roomButton.GetChild(2) is PanelContainer badge)
        {
            RefreshBadgeStyle(badge);
        }

        _line?.Configure(
            QuantumCenter,
            SingularityPosition + new Vector2(NodeWidth / 2.0f, NodeHeight / 2.0f),
            unlocked
        );

        UpdateProgressLabel();
    }

    private void UpdateProgressLabel()
    {
        foreach (Label label in FindDescendants<Label>(_mapPage))
        {
            if (!label.Text.Contains("ROOMS DISCOVERED", StringComparison.Ordinal))
                continue;

            int normalUnlocked = 0;
            foreach (RoomState room in _state.RoomStates)
            {
                if (room.Unlocked)
                    normalUnlocked++;
            }

            int totalUnlocked = normalUnlocked + (_service.Unlocked ? 1 : 0);
            label.Text = totalUnlocked + " / 5 ROOMS DISCOVERED";
            break;
        }
    }

    private void ApplyNodeStyle(bool unlocked)
    {
        Color background = unlocked
            ? new Color(
                Accent.R * 0.22f,
                Accent.G * 0.22f,
                Accent.B * 0.22f,
                0.98f
            )
            : new Color(0.035f, 0.045f, 0.060f, 0.96f);

        Color border = unlocked
            ? Accent
            : new Color(0.24f, 0.28f, 0.34f, 0.72f);

        _roomButton.AddThemeStyleboxOverride(
            "normal",
            CreateButtonStyle(background, border, 2)
        );

        _roomButton.AddThemeStyleboxOverride(
            "hover",
            CreateButtonStyle(background.Lightened(0.08f), border, 2)
        );

        _roomButton.AddThemeStyleboxOverride(
            "pressed",
            CreateButtonStyle(background.Darkened(0.08f), border, 2)
        );

        _roomButton.Modulate = unlocked
            ? Colors.White
            : new Color(0.82f, 0.84f, 0.88f, 1.0f);
    }

    private void RefreshBadgeStyle(PanelContainer badge)
    {
        bool unlocked = _service.Unlocked;

        badge.AddThemeStyleboxOverride(
            "panel",
            new StyleBoxFlat
            {
                BgColor = unlocked
                    ? new Color(
                        Accent.R * 0.30f,
                        Accent.G * 0.30f,
                        Accent.B * 0.30f,
                        0.98f
                    )
                    : new Color(0.075f, 0.085f, 0.105f, 0.98f),

                BorderColor = unlocked
                    ? Accent
                    : new Color(0.28f, 0.32f, 0.38f, 0.90f),

                BorderWidthLeft = 2,
                BorderWidthTop = 2,
                BorderWidthRight = 2,
                BorderWidthBottom = 2,

                CornerRadiusTopLeft = 15,
                CornerRadiusTopRight = 15,
                CornerRadiusBottomLeft = 15,
                CornerRadiusBottomRight = 15
            }
        );
    }

    private static StyleBoxFlat CreateButtonStyle(
        Color background,
        Color border,
        int borderWidth)
    {
        return new StyleBoxFlat
        {
            BgColor = background,
            BorderColor = border,
            BorderWidthLeft = borderWidth,
            BorderWidthTop = borderWidth,
            BorderWidthRight = borderWidth,
            BorderWidthBottom = borderWidth,
            CornerRadiusTopLeft = 24,
            CornerRadiusTopRight = 24,
            CornerRadiusBottomLeft = 24,
            CornerRadiusBottomRight = 24,
            ShadowColor = new Color(border.R, border.G, border.B, 0.20f),
            ShadowSize = 8
        };
    }

    private static Label CreateLabel(int size, string text)
    {
        Label label = new()
        {
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };

        label.AddThemeFontSizeOverride("font_size", size);
        return label;
    }

    private static T? FindDescendant<T>(Node root)
        where T : Node
    {
        foreach (Node child in root.GetChildren())
        {
            if (child is T typed)
                return typed;

            T? nested = FindDescendant<T>(child);
            if (nested != null)
                return nested;
        }

        return null;
    }

    private static List<T> FindDescendants<T>(Node root)
        where T : Node
    {
        List<T> result = [];

        foreach (Node child in root.GetChildren())
        {
            if (child is T typed)
                result.Add(typed);

            result.AddRange(FindDescendants<T>(child));
        }

        return result;
    }

    private sealed partial class SingularityMapLine : Control
    {
        private Vector2 _from;
        private Vector2 _to;
        private bool _active;

        public void Configure(Vector2 from, Vector2 to, bool active)
        {
            _from = from;
            _to = to;
            _active = active;
            QueueRedraw();
        }

        public override void _Draw()
        {
            Color color = _active
                ? Accent
                : new Color(0.24f, 0.28f, 0.34f, 0.82f);

            DrawLine(
                _from,
                _to,
                new Color(color.R, color.G, color.B, 0.18f),
                _active ? 16.0f : 9.0f,
                true
            );

            DrawLine(
                _from,
                _to,
                color,
                _active ? 6.0f : 3.5f,
                true
            );
        }
    }
}
