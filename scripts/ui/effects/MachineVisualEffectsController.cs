using Godot;

namespace IdleAi;

/// <summary>Compact sprite-surface lights and soft glows; never modifies the art or skin.</summary>
public partial class MachineVisualEffectsController : Control
{
    private int _stage;
    private bool _running;
    private Color _accent;
    private double _period = 2.4;
    private float _phase;
    private double _redraw;
    private float _flash;
    private Tween? _completionTween;
    private Control? _clip;
    private Texture2D? _texture;
    private readonly Vector2[] _lightZones = new Vector2[5];
    private int _zoneCount;
    private static readonly Texture2D SoftGlow = CreateSoftGlow();

    private static Texture2D CreateSoftGlow()
    {
        var gradient = new Gradient();
        gradient.SetColor(0, Colors.White);
        gradient.AddPoint(0.3f, new Color(1, 1, 1, 0.35f));
        gradient.SetColor(1, new Color(1, 1, 1, 0));
        return new GradientTexture2D
        {
            Width = 32, Height = 32, Gradient = gradient,
            Fill = GradientTexture2D.FillEnum.Radial,
            FillFrom = new Vector2(0.5f, 0.5f), FillTo = new Vector2(1, 0.5f)
        };
    }

    // Resolve light positions only when the skin changes. Sampling opaque interior
    // pixels keeps accents on the sprite rather than in its transparent margins.
    private void UpdateLightZones(Texture2D? texture)
    {
        _zoneCount = 0;
        if (texture == null) return;
        using Image image = texture.GetImage();
        if (image == null || image.IsEmpty()) return;
        if (image.IsCompressed() && image.Decompress() != Error.Ok) return;
        int width = image.GetWidth();
        int height = image.GetHeight();
        if (width < 5 || height < 5) return;
        for (int zone = 0; zone < _lightZones.Length; zone++)
        {
            Vector2 target = zone switch
            {
                0 => new Vector2(0.60f, 0.55f),
                1 => new Vector2(0.40f, 0.43f),
                2 => new Vector2(0.57f, 0.34f),
                3 => new Vector2(0.37f, 0.64f),
                _ => new Vector2(0.66f, 0.70f)
            };
            float nearest = float.MaxValue;
            Vector2 chosen = Vector2.Zero;
            int stepX = System.Math.Max(1, width / 32);
            int stepY = System.Math.Max(1, height / 32);
            for (int y = 2; y < height - 2; y += stepY)
            for (int x = 2; x < width - 2; x += stepX)
            {
                if (image.GetPixel(x, y).A < 0.8f
                    || image.GetPixel(x - 2, y).A < 0.8f || image.GetPixel(x + 2, y).A < 0.8f
                    || image.GetPixel(x, y - 2).A < 0.8f || image.GetPixel(x, y + 2).A < 0.8f) continue;
                Vector2 candidate = new((float)x / width, (float)y / height);
                bool overlaps = false;
                for (int previous = 0; previous < _zoneCount; previous++)
                    if (candidate.DistanceSquaredTo(_lightZones[previous]) < 0.005f) overlaps = true;
                float distance = candidate.DistanceSquaredTo(target);
                if (overlaps || distance >= nearest) continue;
                nearest = distance;
                chosen = candidate;
            }
            if (nearest < float.MaxValue) _lightZones[_zoneCount++] = chosen;
        }
    }

    public float CompletionFlash { get => _flash; set { _flash = value; QueueRedraw(); } }

    public void Configure(Control? clip = null)
    {
        Name = "MachineVisualEffects";
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _clip = clip;
    }

    public override void _Ready() => _clip ??= FindParent("ScrollContainer") as Control;

    public void SetTheme(int room)
    {
        Color accent = RoomThemePalette.GetAccentColor(room).Lightened(0.35f);
        if (_accent == accent) return;
        _accent = accent;
        QueueRedraw();
    }

    public void Refresh(SlotData slot, double duration, Texture2D? texture = null)
    {
        int stage = slot.Unlocked ? VisualProgressionRules.MachineStage(slot.MachineTier) : 0;
        double period = VisualProgressionRules.PulseSeconds(duration);
        if (_stage != stage || _running != slot.IsRunning || _period != period || _texture != texture)
        {
            _stage = stage;
            _running = slot.Unlocked && slot.IsRunning;
            _period = period;
            if (_texture != texture) UpdateLightZones(texture);
            _texture = texture;
            QueueRedraw();
        }
        Visible = slot.Unlocked;
    }

    public void Clear()
    {
        _completionTween?.Kill();
        _flash = 0;
        _stage = 0;
        _running = false;
        Hide();
    }

    private bool Onscreen() => IsVisibleInTree() && GetGlobalRect().Intersects(
        _clip != null ? _clip.GetGlobalRect() : GetViewport().GetVisibleRect());

    public void CycleCompleted()
    {
        if (_stage == 0 || !Onscreen() || _flash > 0.2f) return; // coalesce fast/multiple completions
        _completionTween?.Kill();
        CompletionFlash = 0.65f;
        _completionTween = CreateTween();
        _completionTween.TweenMethod(Callable.From<float>(value => CompletionFlash = value), 0.65f, 0f, 0.35).SetTrans(Tween.TransitionType.Sine);
    }

    public override void _Process(double delta)
    {
        if (_stage == 0 || !Onscreen()) return;
        _redraw += delta;
        if (_redraw < 1.0 / 30) return;
        _phase = Mathf.PosMod(_phase + (float)(_redraw / (_running ? _period : 5.0)), 1f);
        _redraw = 0;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_stage == 0 || _texture == null || _zoneCount == 0) return;
        Vector2 art = _texture.GetSize();
        float fit = Mathf.Min(Size.X / Mathf.Max(art.X, 1), Size.Y / Mathf.Max(art.Y, 1));
        Vector2 extent = art * fit;
        Vector2 origin = (Size - extent) * 0.5f;
        int lights = System.Math.Min(_zoneCount, _stage == 1 ? 1 : _stage == 2 ? 3 : _stage);
        for (int i = 0; i < lights; i++)
        {
            Vector2 position = origin + extent * _lightZones[i];
            float pulse = 0.5f + 0.5f * Mathf.Sin((_phase + i * 0.17f) * Mathf.Tau);
            float intensity = _running ? 0.20f + pulse * 0.18f : 0.10f + pulse * 0.04f;
            float completion = i == 0 ? _flash : 0;
            float radius = Mathf.Clamp(extent.Y * 0.035f, 2, 6);
            DrawTextureRect(SoftGlow, new Rect2(position - Vector2.One * radius,
                Vector2.One * radius * 2), false, new Color(_accent, intensity + completion * 0.35f));
            DrawCircle(position, 0.85f, new Color(_accent.Lightened(0.25f), intensity + completion * 0.5f));
        }

        if (_stage < 3) return;
        Vector2 energy = origin + extent * _lightZones[0];
        // Tiny drifting specks stay within a few pixels of one localized light zone.
        if (_running)
        {
            int motes = _stage >= 4 ? 3 : 1;
            for (int i = 0; i < motes; i++)
            {
                float age = Mathf.PosMod(_phase + i * 0.31f, 1);
                if (age > 0.55f) continue;
                Vector2 position = energy + new Vector2(Mathf.Sin(i * 2.4f + age * 3) * 3, -age * 8);
                DrawCircle(position, 0.65f, new Color(_accent, (1 - age / 0.55f) * 0.24f));
            }
        }
        if (_flash <= 0) return;
        float travel = (1 - _flash / 0.65f) * 5;
        for (int i = 0; i < 3; i++)
        {
            Vector2 offset = Vector2.FromAngle(-2.6f + i * 0.9f) * travel;
            DrawCircle(energy + offset, 0.75f, new Color(_accent.Lightened(0.3f), _flash * 0.65f));
        }
    }

    public override void _ExitTree() => _completionTween?.Kill();
}
