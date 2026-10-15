using Godot;
using System;
using System.Collections.Generic;

namespace IdleAi;

internal sealed class EventPrestigePanel
{
    internal Control Root { get; }
    private readonly EventManager _manager;
    private readonly EventTextureCache _textures;
    private readonly EventImageView _coreIcon, _currencyIcon;
    private string? _visualEventId;
    private readonly Control _overlay;
    private readonly PanelContainer _confirmation;
    private readonly Label _resource, _bonus, _requirement, _preview, _confirmText;
    private readonly Label _afterBonus, _lifetime, _percentage, _availability, _resets, _keeps;
    private readonly ProgressBar _progress;
    private readonly List<PanelContainer> _cards = [];
    private bool? _lastReady;
    private Color _accent = EventUiStyle.Accent;
    private readonly Button _button, _confirm;
    private EventPrestigeQuote _quote;
    private string? _confirmEventId;
    private readonly Action<int, long> _prestige;
    private readonly Func<EventDefinition?> _currentEvent;
    private readonly Func<bool> _canInteract;
    internal bool ConfirmationVisible => _overlay.Visible;
    internal event Action? ConfirmationVisibilityChanged;
    internal EventPrestigePanel(EventManager manager, EventTextureCache textures, Control page, Action<int, long> prestige, Func<EventDefinition?> currentEvent, Func<bool>? canInteract = null)
    {
        _manager = manager; _textures = textures; _prestige = prestige; _currentEvent = currentEvent; _canInteract = canInteract ?? (() => true);
        var holder = new VBoxContainer { Name = "EventPrestigePanel", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        Root = holder;
        holder.AddThemeConstantOverride("separation", 10);
        holder.AddChild(EventUiStyle.Label("PrestigeTitle", "EVENT PRESTIGE", 20));
        var progressCard = Card(holder, "PrestigeProgressCard", "Prestige Progress");
        var currencyRow = new HBoxContainer(); currencyRow.AddThemeConstantOverride("separation", 8); progressCard.AddChild(currencyRow);
        _currencyIcon = new EventImageView(null, new(28, 28)) { Name = "PrestigeCurrencyIcon", Visible = false, CustomMinimumSize = new(28, 28) }; currencyRow.AddChild(_currencyIcon);
        _requirement = Text(currencyRow, "PrestigeRequirement");
        var progressRow = new HBoxContainer(); progressRow.AddThemeConstantOverride("separation", 10); progressCard.AddChild(progressRow);
        _progress = new ProgressBar { Name = "PrestigeProgressBar", ShowPercentage = false,
            CustomMinimumSize = new(0, 12), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
        _progress.AddThemeStyleboxOverride("background", MapController.CreateNodeStyle(new(.025f, .025f, .04f), EventUiStyle.Muted, 0, 4));
        progressRow.AddChild(_progress);
        _percentage = Text(progressRow, "PrestigeProgressPercent");
        _percentage.SizeFlagsHorizontal = Control.SizeFlags.ShrinkEnd;
        _percentage.CustomMinimumSize = new(56, 24); _percentage.HorizontalAlignment = HorizontalAlignment.Right;
        _lifetime = Text(progressCard, "PrestigeLifetime");
        _availability = Text(progressCard, "PrestigeAvailability");
        var rewardCard = Card(holder, "PrestigeRewardCard", "You will receive");
        var resourceRow = new HBoxContainer(); resourceRow.AddThemeConstantOverride("separation", 8); rewardCard.AddChild(resourceRow);
        _coreIcon = new EventImageView(null, new(28, 28)) { Name = "PrestigeCoreIcon", Visible = false, CustomMinimumSize = new(28, 28) }; resourceRow.AddChild(_coreIcon);
        _preview = Text(resourceRow, "PrestigePreview");
        _resource = Text(rewardCard, "PrestigeResource");
        var bonusCard = Card(holder, "PrestigeBonusCard", "Current Prestige Bonus");
        _bonus = Text(bonusCard, "PrestigeBonus"); _afterBonus = Text(bonusCard, "PrestigeAfterBonus");
        var rulesCard = Card(holder, "PrestigeRulesCard", "What changes?");
        _resets = Text(rulesCard, "PrestigeResetInfo"); _keeps = Text(rulesCard, "PrestigeKeepInfo");
        _button = EventUiStyle.Button("EventPrestigeButton", "Prestige Event", Request);
        _button.AddThemeFontSizeOverride("font_size", 14);
        _button.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        holder.AddChild(_button);
        _overlay = new Control { Name = "EventPrestigeConfirmation", ZIndex = 10, Visible = false, MouseFilter = Control.MouseFilterEnum.Stop };
        _overlay.VisibilityChanged += () => ConfirmationVisibilityChanged?.Invoke();
        _overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect); page.AddChild(_overlay);
        var dim = new ColorRect { Color = new(0, 0, 0, .82f), MouseFilter = Control.MouseFilterEnum.Stop };
        dim.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect); _overlay.AddChild(dim);
        var center = new CenterContainer(); center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect); _overlay.AddChild(center);
        var confirmationBox = EventUiStyle.Panel(center, "PrestigeConfirmationPanel");
        _confirmation = (PanelContainer)confirmationBox.GetParent().GetParent();
        var confirmTitle = EventUiStyle.Label("ConfirmPrestigeTitle", "CONFIRM EVENT PRESTIGE", 18);
        confirmTitle.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        confirmTitle.CustomMinimumSize = new(0, 28); confirmationBox.AddChild(confirmTitle);
        _confirmText = Text(confirmationBox, "ConfirmPrestigeText");
        _confirm = EventUiStyle.Button("ConfirmEventPrestige", "Confirm Prestige", Confirm);
        confirmationBox.AddChild(_confirm);
        confirmationBox.AddChild(EventUiStyle.Button("CancelEventPrestige", "Cancel", Dismiss));
        page.Resized += () => _confirmation.CustomMinimumSize = new(Math.Max(200, Math.Min(600, page.Size.X - 44)), 0);
    }
    private VBoxContainer Card(Control parent, string name, string title)
    {
        var box = EventUiStyle.Panel(parent, name);
        var margin = (MarginContainer)box.GetParent();
        foreach (string side in new[] { "left", "right", "top", "bottom" }) margin.AddThemeConstantOverride("margin_" + side, 12);
        box.AddThemeConstantOverride("separation", 8);
        _cards.Add((PanelContainer)margin.GetParent());
        box.AddChild(EventUiStyle.Label(name + "Title", title, 14));
        return box;
    }
    private static Label Text(Control parent, string name)
    {
        var label = EventUiStyle.Label(name, "", 13);
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        // Clipped auto-wrapped labels can collapse to a 1px minimum height in Godot.
        label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill; label.ClipText = false;
        label.CustomMinimumSize = new(0, 24);
        parent.AddChild(label); return label;
    }
    private void Request()
    {
        if (!_canInteract()) return;
        var d = _currentEvent();
        if (d == null) return;
        _quote = _manager.GetPrestigeQuote(d.Id);
        if (!_quote.Available) return;
        _confirmEventId = d.Id;
        EventUiStyle.SetText(_confirmText, "You will receive: +" + NumberFormatter.Format(_quote.ResourceGained) + " " + d.Prestige.ResourceName
            + "\nNew permanent bonus: +" + ((_quote.NewMultiplier - 1) * 100).ToString("0.#") + "% production"
            + "\n\nReset: " + d.CurrencyName + ", generator levels, run unlocks and partial cycles."
            + "\n\nKeep: " + d.Prestige.ResourceName + ", lifetime earnings, claimed rewards, bots, cosmetics, cumulative challenge progress, shop limits and event history.");
        EventUiStyle.SetDisabled(_confirm, false);
        _overlay.Show(); _overlay.MoveToFront();
    }
    private void Confirm()
    {
        if (!_overlay.Visible || _currentEvent()?.Id != _confirmEventId) return;
        EventUiStyle.SetDisabled(_confirm, true);
        Dismiss();
        _prestige(_quote.PrestigeCount, _quote.ResourceGained);
    }
    internal void Dismiss() => _overlay.Hide();
    internal bool DismissOnBack()
    {
        if (!_overlay.Visible) return false;
        Dismiss();
        return true;
    }
    internal void Refresh(EventDefinition? d)
    {
        bool enabled = d?.Prestige.Enabled == true;
        if (_visualEventId != d?.Id)
        {
            _visualEventId = d?.Id;
            _coreIcon.SetTexture(_textures.Get(d?.Prestige.IconPath ?? ""));
            _currencyIcon.SetTexture(_textures.Get(d?.CurrencyIconPath ?? ""));
            _coreIcon.Visible = enabled; _currencyIcon.Visible = enabled && d?.CurrencyIconPath.Length > 0;
            _accent = Color.FromHtml(d?.Theme.AccentHex ?? "#33b3f2");
            foreach (var card in _cards)
                card.AddThemeStyleboxOverride("panel", MapController.CreateNodeStyle(new(.025f, .02f, .04f, .97f), _accent.Darkened(.25f), 1, 8));
            _progress.AddThemeStyleboxOverride("fill", MapController.CreateNodeStyle(_accent, _accent, 0, 4));
            _lastReady = null;
        }
        var quote = d == null ? default : _manager.GetPrestigeQuote(d.Id);
        for (int i = 1; i < _cards.Count; i++) _cards[i].Visible = enabled;
        double percent = enabled && quote.Requirement > 0 ? Math.Clamp(quote.RunEarned / quote.Requirement * 100, 0, 100) : 0;
        _progress.Value = percent;
        EventUiStyle.SetText(_percentage, percent.ToString("0.#") + "%");
        EventUiStyle.SetText(_requirement, !enabled ? "Prestige is disabled for this event." : NumberFormatter.Format(quote.RunEarned) + " / " + NumberFormatter.Format(quote.Requirement) + " " + d!.CurrencyName);
        EventUiStyle.SetText(_lifetime, !enabled ? "" : "Lifetime earned: " + NumberFormatter.Format(_manager.GetProgress(d!.Id).TotalEarned) + " " + d.CurrencyName
            + "\nProgress counts earnings since your last Prestige.");
        string state = !enabled ? "Unavailable" : quote.Available ? "PRESTIGE READY"
            : !_manager.IsActive(d!) ? "EVENT INACTIVE" : "Requires " + NumberFormatter.Format(quote.Requirement) + " " + d!.CurrencyName;
        EventUiStyle.SetText(_availability, state);
        _availability.Modulate = quote.Available ? _accent.Lightened(.5f) : new(.8f, .84f, .90f);
        EventUiStyle.SetText(_resource, !enabled ? "" : "Current: " + NumberFormatter.Format(quote.CurrentResource) + " " + d!.Prestige.ResourceName
            + " • " + NumberFormatter.Format(_manager.GetTotalPrestigeResourceEarned(d.Id)) + " earned in total");
        EventUiStyle.SetText(_bonus, !enabled ? "" : "+" + ((quote.CurrentMultiplier - 1) * 100).ToString("0.#") + "% Event Production");
        EventUiStyle.SetText(_afterBonus, !enabled ? "" : "After Prestige: +" + ((quote.NewMultiplier - 1) * 100).ToString("0.#") + "% Event Production");
        EventUiStyle.SetText(_preview, !enabled ? "" : "+" + NumberFormatter.Format(quote.ResourceGained) + " " + d!.Prestige.ResourceName);
        EventUiStyle.SetText(_resets, !enabled ? "" : "Resets\n" + d!.CurrencyName + " • Generator levels • Run-based generator unlocks");
        EventUiStyle.SetText(_keeps, !enabled ? "" : "Keeps\n" + d!.Prestige.ResourceName + " • Lifetime earnings • Reward claims • Bots • Cosmetics • Cumulative challenge progress • Shop purchase limits"
            + "\nCurrent-state challenge goals are re-evaluated.");
        EventUiStyle.SetText(_button, quote.Available ? "Prestige for +" + NumberFormatter.Format(quote.ResourceGained) + " " + d!.Prestige.ResourceName : "Prestige Event");
        _button.TooltipText = quote.Available ? _button.Text : state;
        if (_lastReady != quote.Available)
        {
            EventUiStyle.StyleButton(_button, quote.Available ? _accent : EventUiStyle.Muted);
            _lastReady = quote.Available;
        }
        EventUiStyle.SetDisabled(_button, !quote.Available);
        if (_overlay.Visible && !quote.Available) Dismiss();
    }
}
