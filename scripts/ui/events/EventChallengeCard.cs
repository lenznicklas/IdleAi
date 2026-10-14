using Godot;
using System;
using System.Globalization;

namespace IdleAi;

internal sealed class EventChallengeCard
{
    internal Control Root { get; }
    private readonly Label _progress, _status;
    private readonly ProgressBar _bar;
    private readonly Button _claim;
    private readonly PanelContainer _panel;
    private readonly Color _accent;
    private readonly bool _major;
    private string _lastStyle = "";
    internal EventChallengeCard(EventDefinition d, EventChallengeDefinition c, EventTextureCache textures, Action claim)
    {
        _accent = Color.FromHtml(d.Theme.AccentHex); _major = c.Category == EventChallengeCategory.Major;
        Root = new Control { Name = "EventChallengeCard_" + c.Id, CustomMinimumSize = new(0, 328), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _panel = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        _panel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect); Root.AddChild(_panel);
        Slot("ChallengeCategory", c.Category.ToString().ToUpperInvariant() + (c.Repeatable ? " • REPEATABLE" : ""), 12, 16, 12, 32);
        Slot("ChallengeTitle", c.Title, 16, 16, 36, 80, 2);
        Slot("ChallengeDescription", c.Description, 12, 16, 84, 126, 2);
        var icon = new EventImageView(textures.Get(c.Reward.IconPath.Length == 0 ? d.IconPath : c.Reward.IconPath), new(48, 48))
            { Position = new(16, 134), Size = new(48, 48) };
        Root.AddChild(icon);
        Slot("ChallengeReward", "Reward: " + EventPresentation.RewardName(c.Reward, d), 14, 76, 134, 182, 2);
        _progress = Slot("ChallengeProgress", "", 13, 16, 186, 210);
        _bar = new ProgressBar { Name = "ChallengeProgressBar", ShowPercentage = false, MouseFilter = Control.MouseFilterEnum.Ignore };
        _bar.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopWide);
        _bar.OffsetLeft = 16; _bar.OffsetRight = -16; _bar.OffsetTop = 218; _bar.OffsetBottom = 226;
        _bar.AddThemeStyleboxOverride("background", MapController.CreateNodeStyle(new(.04f, .05f, .07f), EventUiStyle.Muted, 0, 0));
        _bar.AddThemeStyleboxOverride("fill", MapController.CreateNodeStyle(_accent, _accent, 0, 0)); Root.AddChild(_bar);
        _status = Slot("ChallengeStatus", "", 12, 16, 234, 258);
        _claim = EventUiStyle.Button("EventChallengeClaim_" + c.Id, "CLAIM", claim);
        _claim.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopWide);
        _claim.OffsetLeft = 16; _claim.OffsetRight = -16; _claim.OffsetTop = 264; _claim.OffsetBottom = 312;
        _claim.CustomMinimumSize = new(0, 48); Root.AddChild(_claim);
    }
    private Label Slot(string name, string text, int size, float left, float top, float bottom, int lines = 1)
    {
        var label = EventUiStyle.Label(name, text, size);
        label.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopWide);
        label.OffsetLeft = left; label.OffsetRight = -16; label.OffsetTop = top; label.OffsetBottom = bottom;
        label.ClipText = true; label.MaxLinesVisible = lines;
        label.AutowrapMode = lines == 1 ? TextServer.AutowrapMode.Off : TextServer.AutowrapMode.WordSmart;
        label.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis; label.TooltipText = text;
        Root.AddChild(label); return label;
    }
    private static string FormatProgress(double value) => value < 1000 ? value.ToString("0.##", CultureInfo.InvariantCulture) : NumberFormatter.Format(value);
    internal void Refresh(EventChallengeSnapshot s, bool ended)
    {
        EventUiStyle.SetText(_progress, FormatProgress(s.Progress) + " / " + FormatProgress(s.Definition.Target));
        _bar.Value = s.Progress / s.Definition.Target * 100;
        string state = s.Claimed ? "CLAIMED" : s.Complete ? "COMPLETE" : "ACTIVE";
        string status = state + (s.Claimed && s.Definition.Repeatable ? " • " + s.TimesClaimed + " earned" : ended && !s.Claimed ? " • EVENT ENDED" : "");
        EventUiStyle.SetText(_status, status);
        EventUiStyle.SetText(_claim, s.CanClaim ? "CLAIM" : s.Complete && !s.Claimed && ended ? "EXPIRED" : state);
        EventUiStyle.SetDisabled(_claim, !s.CanClaim);
        string style = state + ":" + s.CanClaim;
        if (_lastStyle == style) return;
        _lastStyle = style;
        Color border = s.CanClaim ? _accent : EventUiStyle.Muted;
        _panel.AddThemeStyleboxOverride("panel", MapController.CreateNodeStyle(new(.035f, .025f, .05f, .97f), border, _major ? 3 : 2, s.CanClaim ? 8 : 2));
        Root.Modulate = s.Claimed ? new(.7f, .74f, .8f) : Colors.White;
        EventUiStyle.StyleButton(_claim, border);
    }
}
