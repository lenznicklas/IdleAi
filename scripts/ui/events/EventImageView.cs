using Godot;

namespace IdleAi;

/// <summary>A bounded image slot. Godot fits the aspect ratio; sizing only caps the
/// texture's layout rectangle to the slot and native resolution, without per-frame work.</summary>
internal partial class EventImageView : Control
{
    private readonly TextureRect _image;
    private readonly Vector2 _maximum;

    internal EventImageView(Texture2D? texture, Vector2 maximum)
    {
        Name = "EventImageArea";
        MouseFilter = MouseFilterEnum.Ignore;
        ClipContents = true; // Defensive; the normal fitted image never needs clipping.
        _maximum = maximum;
        var center = new CenterContainer { MouseFilter = MouseFilterEnum.Ignore };
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(center);
        // Set IgnoreSize BEFORE Texture so native PNG dimensions cannot enlarge the control.
        _image = new TextureRect
        {
            Name = "EventImage", ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter, SizeFlagsVertical = SizeFlags.ShrinkCenter,
            MouseFilter = MouseFilterEnum.Ignore, Texture = texture
        };
        center.AddChild(_image);
        Resized += Fit;
        Fit();
    }

    // Fixed-size slots can receive Size before entering the tree, when Resized
    // is not delivered. Refit after attachment so their textures cannot stay 0x0.
    public override void _Ready() => Fit();

    internal void SetTexture(Texture2D? texture) { if (_image.Texture == texture) return; _image.Texture = texture; Fit(); }

    private void Fit()
    {
        var native = _image.Texture?.GetSize() ?? Vector2.Zero;
        var cap = new Vector2(Mathf.Max(0, Mathf.Min(Size.X, Mathf.Min(_maximum.X, native.X))),
            Mathf.Max(0, Mathf.Min(Size.Y, Mathf.Min(_maximum.Y, native.Y))));
        if (_image.CustomMinimumSize != cap) _image.CustomMinimumSize = cap;
    }
}
