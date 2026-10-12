using Godot;

namespace IdleAi;

/// <summary>Stable balance-label geometry and updates; formatting never changes currency state.</summary>
internal static class DataShardDisplay
{
    internal static void Configure(Label label, float minimumWidth = 220)
    {
        label.CustomMinimumSize = new Vector2(minimumWidth, label.CustomMinimumSize.Y);
        label.AutowrapMode = TextServer.AutowrapMode.Off;
        label.ClipText = true; // Text cannot expand a container's minimum width.
        label.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        label.HorizontalAlignment = HorizontalAlignment.Right;
        // Retain the game font; the reserved area and alignment keep neighbors fixed.
    }

    internal static bool Update(Label label, double value, string prefix = "")
    {
        string text = prefix + NumberFormatter.FormatDataShards(value);
        if (label.Text == text) return false;
        label.Text = text;
        return true;
    }
}
