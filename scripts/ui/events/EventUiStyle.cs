using Godot;
using System;
using System.Collections.Generic;

namespace IdleAi;

internal static class EventUiStyle
{
    internal static readonly Color Accent = new(.20f, .70f, .95f);
    internal static readonly Color Muted = new(.36f, .43f, .53f);
    internal static readonly Color Ending = new(.95f, .64f, .22f);
    internal static Label Label(string name, string text, int size = 16)
    {
        var label = MapController.CreateLabel(size, text);
        label.Name = name;
        label.HorizontalAlignment = HorizontalAlignment.Left;
        return label;
    }
    internal static void SetDisabled(Button button, bool disabled)
    {
        if (button.Disabled != disabled) button.Disabled = disabled;
    }
    internal static void SetText(Label label, string text)
    {
        if (label.Text != text) label.Text = text;
    }
    internal static void SetText(Button button, string text)
    {
        if (button.Text != text) button.Text = text;
    }
    internal static Button Button(string name, string text, Action pressed)
    {
        var button = new Button { Name = name, Text = text, FocusMode = Control.FocusModeEnum.None,
            CustomMinimumSize = new Vector2(0, 56) };
        StyleButton(button, Accent);
        button.Pressed += () => { MapController.PlayNavigationHaptic(); pressed(); };
        return button;
    }
    internal static void StyleButton(Button button, Color accent)
    {
        var normal = new Color(accent.R * .24f, accent.G * .24f, accent.B * .24f, .97f);
        button.AddThemeStyleboxOverride("normal", MapController.CreateNodeStyle(normal, accent, 2, 8));
        button.AddThemeStyleboxOverride("hover", MapController.CreateNodeStyle(normal.Lightened(.08f), accent, 2, 8));
        var pressed = MapController.CreateNodeStyle(normal.Darkened(.08f), accent, 2, 6);
        button.AddThemeStyleboxOverride("pressed", pressed);
        button.AddThemeStyleboxOverride("hover_pressed", pressed);
        button.AddThemeStyleboxOverride("disabled", MapController.CreateNodeStyle(new(.035f, .045f, .060f, .96f), Muted, 2, 0));
        button.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
        // The global readability pass has already adjusted existing controls. Styling must not undo it.
        if (!button.HasThemeFontSizeOverride("font_size")) button.AddThemeFontSizeOverride("font_size", 16);
    }
    internal static void StyleTabButton(Button button, Color accent, bool active)
    {
        Color border = active ? accent : Muted;
        var fill = active ? new Color(accent.R * .32f, accent.G * .32f, accent.B * .32f, .98f)
            : new Color(.035f, .045f, .060f, .98f);
        button.SetPressedNoSignal(active);
        button.AddThemeStyleboxOverride("normal", MapController.CreateNodeStyle(fill, border, 2, active ? 10 : 3));
        button.AddThemeStyleboxOverride("hover", MapController.CreateNodeStyle(fill.Lightened(.06f), border, 2, 6));
        var pressed = MapController.CreateNodeStyle(fill, border, 2, active ? 10 : 3);
        button.AddThemeStyleboxOverride("pressed", pressed);
        button.AddThemeStyleboxOverride("hover_pressed", pressed);
        button.AddThemeStyleboxOverride("focus", MapController.CreateNodeStyle(new(0, 0, 0, 0), accent.Lightened(.3f), 2, 0));
        button.AddThemeStyleboxOverride("disabled", MapController.CreateNodeStyle(fill, Muted, 2, 0));
        foreach (string state in new[] { "font_color", "font_hover_color", "font_pressed_color", "font_hover_pressed_color", "font_focus_color" })
            button.AddThemeColorOverride(state, active ? accent.Lightened(.65f) : new Color(.82f, .87f, .93f));
    }
    internal static VBoxContainer Panel(Control parent, string name)
    {
        var panel = new PanelContainer { Name = name, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        panel.AddThemeStyleboxOverride("panel", MapController.CreateHeaderStyle());
        parent.AddChild(panel);
        var margin = new MarginContainer();
        foreach (var side in new[] { "left", "right", "top", "bottom" }) margin.AddThemeConstantOverride("margin_" + side, 18);
        panel.AddChild(margin);
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 14);
        margin.AddChild(box);
        return box;
    }
    internal static string Countdown(long seconds)
    {
        if (seconds <= 0) return "EVENT ENDED";
        if (seconds >= 86400) return $"Ends in {seconds / 86400}d {seconds % 86400 / 3600}h";
        if (seconds >= 3600) return $"Ends in {seconds / 3600}h {seconds % 3600 / 60}m";
        return $"Ends in {seconds / 60}m {seconds % 60}s";
    }
}

// Cache successful and missing optional assets. Changing balances never reloads textures.
internal sealed class EventTextureCache
{
    private readonly Dictionary<string, Texture2D?> _textures = new(StringComparer.Ordinal);
    internal Texture2D? Get(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;
        if (!_textures.TryGetValue(path, out var texture))
        {
            texture = ResourceLoader.Exists(path) ? GD.Load<Texture2D>(path) : null;
            _textures.Add(path, texture);
        }
        return texture;
    }
}
