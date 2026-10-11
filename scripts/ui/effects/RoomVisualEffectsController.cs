using Godot;

namespace IdleAi;

/// <summary>Bounded background decorations below UI and above the existing room map.</summary>
public partial class RoomVisualEffectsController : Control
{
    private int _stage;
    private int _room;
    private Color _accent;
    private float _phase;
    private double _redraw;
    private Tween? _reveal;
    private Control? _singularityPage;

    public void Configure()
    {
        Name = "RoomVisualEffects";
        MouseFilter = MouseFilterEnum.Ignore;
        ZIndex = -7; // background -10, existing ambient particles -5, UI 0
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
    }

    public override void _Ready()
    {
        GetParent().ChildEnteredTree += OnPageEntered;
        if (GetParent().GetNodeOrNull<Control>("SingularityPage") is Control page) OnPageEntered(page);
    }

    private void OnPageEntered(Node node)
    {
        if (node is not Control page || page.Name != "SingularityPage") return;
        _singularityPage = page;
        page.VisibilityChanged += SyncVisibility;
        SyncVisibility();
    }

    private void SyncVisibility() => Visible = _stage > 0 && !(_singularityPage?.Visible ?? false);

    public void Refresh(int roomIndex, RoomState room)
    {
        int count = 0;
        double tiers = 0;
        foreach (SlotData slot in room.Slots)
        {
            if (!slot.Unlocked) continue;
            count++;
            tiers += VisualProgressionRules.MachineStage(slot.MachineTier);
        }
        int stage = roomIndex < 4 ? VisualProgressionRules.RoomStage(room.Unlocked,
            room.Slots.Count, count, count > 0 ? tiers / count : 0) : 0;
        if (stage == _stage && roomIndex == _room) return;
        _stage = stage;
        _room = roomIndex;
        _accent = RoomThemePalette.GetAccentColor(roomIndex).Lightened(0.35f);
        _reveal?.Kill();
        SyncVisibility();
        Modulate = new Color(1, 1, 1, 0);
        if (Visible)
        {
            _reveal = CreateTween();
            _reveal.TweenProperty(this, "modulate:a", 1f, 0.6);
        }
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        if (_stage == 0 || !IsVisibleInTree()) return;
        _redraw += delta;
        if (_redraw < 1.0 / 20) return;
        _phase = Mathf.PosMod(_phase + (float)_redraw * (0.08f + _stage * 0.025f), 1f);
        _redraw = 0;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_stage == 0) return;
        float pulse = 0.55f + 0.2f * Mathf.Sin(_phase * Mathf.Tau);
        // Edge-mounted industrial lights remain clear of machine labels and buttons.
        for (int side = 0; side < 2; side++)
        {
            float x = side == 0 ? 7 : Size.X - 13;
            for (int i = 0; i < _stage; i++)
            {
                float y = Size.Y * (0.22f + i * 0.17f);
                DrawRect(new Rect2(x - 3, y - 3, 12, 45), new Color(_accent, 0.06f));
                DrawRect(new Rect2(x, y, 6, 38), new Color(_accent, pulse * 0.55f));
            }
        }
        if (_stage < 2) return;
        for (int i = 0; i < _stage - 1; i++)
        {
            float x = Size.X * (0.12f + i * 0.28f);
            float y = Size.Y * 0.17f;
            DrawRect(new Rect2(x, y, 52, 24), new Color(0.025f, 0.035f, 0.045f, 0.7f));
            DrawRect(new Rect2(x, y, 52, 24), new Color(_accent, 0.3f), false, 1);
            for (int j = 0; j < 8; j++)
            {
                float height = 3 + 10 * (0.5f + 0.5f * Mathf.Sin(_phase * Mathf.Tau + j * 0.8f + i));
                DrawLine(new(x + 5 + j * 6, y + 19), new(x + 5 + j * 6, y + 19 - height),
                    new Color(_accent, 0.5f), 2);
            }
        }
        if (_stage < 3) return;
        float cableY = Size.Y * 0.86f;
        DrawLine(new(18, cableY), new(Size.X - 18, cableY), new Color(_accent, 0.13f), 3);
        DrawCircle(new(Mathf.Lerp(18, Size.X - 18, _phase), cableY), 3, new Color(_accent, 0.45f));
        if (_room == 3 || _stage == 4)
        {
            // Six reusable procedural motes, no particle systems or spawning nodes.
            for (int i = 0; i < 6; i++)
            {
                float p = Mathf.PosMod(_phase + i / 6f, 1);
                DrawCircle(new(Size.X * (0.1f + i * 0.16f), Size.Y * (0.85f - p * 0.6f)),
                    1.5f, new Color(_accent, Mathf.Sin(p * Mathf.Pi) * 0.25f));
            }
        }
    }

    public override void _ExitTree()
    {
        _reveal?.Kill();
        GetParent().ChildEnteredTree -= OnPageEntered;
        if (GodotObject.IsInstanceValid(_singularityPage)) _singularityPage!.VisibilityChanged -= SyncVisibility;
    }
}
