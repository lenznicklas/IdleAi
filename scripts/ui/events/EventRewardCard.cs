using Godot;
using System;

namespace IdleAi;

/// <summary>Fixed slots keep long values and optional art inside the portrait track.</summary>
internal sealed class EventRewardCard
{
    internal Control Root { get; }
    private readonly PanelContainer _panel;
    private readonly Label _progress, _status;
    private readonly ProgressBar _bar;
    private readonly Button _claim;
    private readonly EventMilestone _milestone;
    private readonly Color _accent;
    private string _lastState = "";

    internal EventRewardCard(EventDefinition d, EventMilestone milestone, EventTextureCache textures, Action claim)
    {
        _milestone = milestone;
        _accent = Color.FromHtml(d.Theme.AccentHex);
        Root = new Control { Name = "EventRewardCard_" + milestone.Id, CustomMinimumSize = new(0, 270),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _panel = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        _panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        Root.AddChild(_panel);
        Slot("RewardRequirement", NumberFormatter.Format(milestone.TargetAmount) + " " + d.CurrencyName, 14, 16, 12, 40);
        var icon = new EventImageView(textures.Get(string.IsNullOrEmpty(milestone.Reward.IconPath) ? d.IconPath : milestone.Reward.IconPath), new(64, 72))
        { Position = new(16, 48), Size = new(64, 72) };
        Root.AddChild(icon);
        string title = string.IsNullOrEmpty(milestone.Title) ? EventPresentation.RewardName(milestone.Reward, d) : milestone.Title;
        var name = Slot("RewardName", title, 16, 92, 48, 108, 2);
        name.TooltipText = title + (milestone.Description.Length > 0 ? "\n" + milestone.Description : "");
        _progress = Slot("RewardProgress", "", 12, 16, 125, 151);
        _bar = new ProgressBar { Name = "RewardProgressBar", ShowPercentage = false, MouseFilter = Control.MouseFilterEnum.Ignore };
        _bar.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopWide);
        _bar.OffsetLeft = 16; _bar.OffsetRight = -16; _bar.OffsetTop = 156; _bar.OffsetBottom = 164;
        _bar.AddThemeStyleboxOverride("background", MapController.CreateNodeStyle(new(.04f, .05f, .07f), EventUiStyle.Muted, 0, 0));
        _bar.AddThemeStyleboxOverride("fill", MapController.CreateNodeStyle(_accent, _accent, 0, 0));
        Root.AddChild(_bar);
        _status = Slot("RewardStatus", "", 12, 16, 173, 198);
        _claim = EventUiStyle.Button("EventReward_" + milestone.Id, "CLAIM", claim);
        _claim.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopWide);
        _claim.OffsetLeft = 16; _claim.OffsetRight = -16; _claim.OffsetTop = 208; _claim.OffsetBottom = 256;
        _claim.CustomMinimumSize = new(0, 48);
        Root.AddChild(_claim);
    }
    private Label Slot(string name, string text, int size, float left, float top, float bottom, int lines = 1)
    {
        var label = EventUiStyle.Label(name, text, size);
        label.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopWide);
        label.OffsetLeft = left; label.OffsetRight = -16; label.OffsetTop = top; label.OffsetBottom = bottom;
        label.ClipText = true; label.MaxLinesVisible = lines;
        label.AutowrapMode = lines == 1 ? TextServer.AutowrapMode.Off : TextServer.AutowrapMode.WordSmart;
        label.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        Root.AddChild(label);
        return label;
    }
    internal void Refresh(double earned, bool unlocked, bool claimed, bool canClaim, bool ended)
    {
        double ratio = _milestone.TargetAmount <= 0 ? 1 : Math.Clamp(earned / _milestone.TargetAmount, 0, 1);
        _bar.Value = unlocked || claimed ? 100 : ratio * 100;
        EventUiStyle.SetText(_progress, unlocked || claimed ? "Requirement reached" :
            NumberFormatter.Format(earned) + " / " + NumberFormatter.Format(_milestone.TargetAmount) + " lifetime earned");
        string state = claimed ? "CLAIMED" : canClaim ? "READY TO CLAIM" : unlocked && ended ? "EXPIRED" : "LOCKED";
        EventUiStyle.SetText(_status, (_milestone.FinalReward ? "FINAL REWARD • " : _milestone.Featured ? "MAJOR REWARD • " : "") + state);
        EventUiStyle.SetText(_claim, claimed ? "CLAIMED" : canClaim ? "CLAIM" : state);
        EventUiStyle.SetDisabled(_claim, !canClaim);
        if (_lastState == state) return;
        _lastState = state;
        Color border = claimed ? new(.3f, .7f, .5f) : canClaim ? _accent : EventUiStyle.Muted;
        _status.Modulate = border.Lightened(.35f);
        _panel.AddThemeStyleboxOverride("panel", MapController.CreateNodeStyle(new(.035f, .025f, .05f, .97f),
            border, _milestone.Featured || _milestone.FinalReward ? 3 : 2, canClaim ? 8 : 2));
        EventUiStyle.StyleButton(_claim, border);
    }
}
