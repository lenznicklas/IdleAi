using Godot;
using System.Globalization;

namespace IdleAi;

/// <summary>Aspect-fitted artwork with one fitted UI text overlay; no numbered bitmaps.</summary>
public sealed partial class NumberedBadgeView : Control
{
    private readonly Texture2D _texture;
    public int Number { get; private set; }
    public int FittedFontSize { get; private set; }
    public Rect2 NumberBounds { get; private set; }

    public NumberedBadgeView(Texture2D texture, int number)
    {
        Name = "LevelBadgeImage";
        _texture = texture; Number = number;
        MouseFilter = MouseFilterEnum.Ignore;
        ClipContents = true;
        TextureFilter = TextureFilterEnum.LinearWithMipmaps;
        Resized += QueueRedraw;
    }

    public void SetNumber(int number)
    {
        if (Number == number) return;
        Number = number;
        QueueRedraw();
    }

    public override void _Draw()
    {
        Vector2 source = _texture.GetSize();
        float scale = Mathf.Min(Size.X / source.X, Size.Y / source.Y);
        Vector2 fitted = source * scale;
        Vector2 center = Size / 2;
        DrawTextureRect(_texture, new Rect2(center - fitted / 2, fitted), false);
        float side = Mathf.Min(fitted.X, fitted.Y);
        var font = GetThemeFont("font", "Label");
        string text = Number.ToString(CultureInfo.InvariantCulture);
        int size = Mathf.Max(1, (int)(side * .40f));
        Vector2 extent;
        while (true)
        {
            extent = font.GetStringSize(text, HorizontalAlignment.Left, -1, size);
            if (size <= 1 || extent.X <= side * .52f && font.GetHeight(size) <= side * .38f) break;
            size--;
        }
        FittedFontSize = size;
        Vector2 baseline = new(center.X - extent.X / 2, center.Y + (font.GetAscent(size) - font.GetDescent(size)) / 2);
        NumberBounds = new Rect2(baseline - new Vector2(0, font.GetAscent(size)), new(extent.X, font.GetHeight(size)));
        DrawStringOutline(font, baseline, text, HorizontalAlignment.Left, -1, size, 2, new Color("091323"));
        DrawString(font, baseline, text, HorizontalAlignment.Left, -1, size, new Color("e9faff"));
    }
}
