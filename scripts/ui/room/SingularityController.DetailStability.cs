using Godot;
using System;
using System.Collections.Generic;

namespace IdleAi;

public sealed partial class SingularityController
{
    // Detail views deliberately have no custom opening, pressing or refresh animation.
    private readonly List<Action> _detailValueUpdates = [];
    private string? _detailViewKey;
    private ScrollContainer _detailScroll = null!;

    private bool BeginDetailView(string key)
    {
        if (_detailViewKey == key)
        {
            UpdateDetailValues();
            return false;
        }
        ClearDetailContent();
        _detailViewKey = key;
        return true;
    }

    private void UpdateDetailValues()
    {
        foreach (var update in _detailValueUpdates) update();
        FinishDetailView();
    }

    private void FinishDetailView()
    {
        EnsureBotButtonInNodeOverlay();
        _root.GetNodeOrNull<UiReadabilityController>("UiReadabilityController")?.ApplyToSubtree(_detailOverlay);
    }

    private Label CreateDetailValueLabel(int size, Func<string> value, int lines = 1)
    {
        var label = CreateLabel(size, value());
        label.ClipText = true;
        label.AutowrapMode = lines > 1 ? TextServer.AutowrapMode.WordSmart : TextServer.AutowrapMode.Off;
        label.MaxLinesVisible = lines;
        label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        _root.GetNodeOrNull<UiReadabilityController>("UiReadabilityController")?.ApplyToSubtree(label);
        float lineHeight = Mathf.Ceil(label.GetThemeFont("font").GetHeight(label.GetThemeFontSize("font_size"))) + 2;
        label.CustomMinimumSize = new Vector2(0,
            lineHeight * lines + Math.Max(0, label.GetThemeConstant("line_spacing")) * (lines - 1));
        _detailValueUpdates.Add(() =>
        {
            string text = value();
            if (label.Text != text) label.Text = text;
        });
        return label;
    }

    private void AddDetailValueInfo(Control parent, Func<string> value, int lines = 1, Color? color = null)
    {
        var label = CreateDetailValueLabel(13, value, lines);
        if (color.HasValue) label.AddThemeColorOverride("font_color", color.Value);
        parent.AddChild(label);
    }

    private void BindDetailButton(Button button, Func<string> value, Func<bool>? disabled = null)
    {
        // Native theme states are retained. Text never changes the button's width.
        button.ClipText = true;
        void Update()
        {
            string text = value();
            if (button.Text != text) button.Text = text;
            if (disabled != null)
            {
                bool nextDisabled = disabled();
                if (button.Disabled != nextDisabled) button.Disabled = nextDisabled;
            }
        }
        _detailValueUpdates.Add(Update);
        Update();
    }

    private void SizeDetailPanel()
    {
        var viewport = _root.GetViewport().GetVisibleRect().Size;
        _detailPanel.CustomMinimumSize = new Vector2(
            Mathf.Min(590, Mathf.Max(1, viewport.X - 36)),
            Mathf.Min(960, Mathf.Max(1, viewport.Y - 68)));
    }
}
