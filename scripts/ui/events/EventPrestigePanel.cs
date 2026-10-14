using Godot;
using System;

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
        var box = EventUiStyle.Panel(holder, "PrestigeDetails");
        box.AddChild(EventUiStyle.Label("PrestigeTitle", "EVENT PRESTIGE", 20));
        var resourceRow = new HBoxContainer(); box.AddChild(resourceRow);
        _coreIcon = new EventImageView(null, new(28, 28)) { Name = "PrestigeCoreIcon", Visible = false, CustomMinimumSize = new(28, 28) }; resourceRow.AddChild(_coreIcon);
        _resource = Text(resourceRow, "PrestigeResource"); _bonus = Text(box, "PrestigeBonus");
        var currencyRow = new HBoxContainer(); box.AddChild(currencyRow);
        _currencyIcon = new EventImageView(null, new(28, 28)) { Name = "PrestigeCurrencyIcon", Visible = false, CustomMinimumSize = new(28, 28) }; currencyRow.AddChild(_currencyIcon);
        _requirement = Text(currencyRow, "PrestigeRequirement"); _preview = Text(box, "PrestigePreview");
        _button = EventUiStyle.Button("EventPrestigeButton", "Prestige Event", Request);
        box.AddChild(_button);
        _overlay = new Control { Name = "EventPrestigeConfirmation", ZIndex = 10, Visible = false, MouseFilter = Control.MouseFilterEnum.Stop };
        _overlay.VisibilityChanged += () => ConfirmationVisibilityChanged?.Invoke();
        _overlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect); page.AddChild(_overlay);
        var dim = new ColorRect { Color = new(0, 0, 0, .82f), MouseFilter = Control.MouseFilterEnum.Stop };
        dim.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect); _overlay.AddChild(dim);
        var center = new CenterContainer(); center.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect); _overlay.AddChild(center);
        var confirmationBox = EventUiStyle.Panel(center, "PrestigeConfirmationPanel");
        _confirmation = (PanelContainer)confirmationBox.GetParent().GetParent();
        confirmationBox.AddChild(EventUiStyle.Label("ConfirmPrestigeTitle", "CONFIRM EVENT PRESTIGE", 18));
        _confirmText = Text(confirmationBox, "ConfirmPrestigeText");
        _confirm = EventUiStyle.Button("ConfirmEventPrestige", "Confirm Prestige", Confirm);
        confirmationBox.AddChild(_confirm);
        confirmationBox.AddChild(EventUiStyle.Button("CancelEventPrestige", "Cancel", Dismiss));
        page.Resized += () => _confirmation.CustomMinimumSize = new(Math.Max(200, Math.Min(600, page.Size.X - 44)), 0);
    }
    private static Label Text(Control parent, string name)
    {
        var label = EventUiStyle.Label(name, "", 13);
        label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill; label.ClipText = true;
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
            + "\n\nKeep: " + d.Prestige.ResourceName + ", lifetime earnings, claimed rewards, bots, cosmetics, shop limits and event history.");
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
        }
        var quote = d == null ? default : _manager.GetPrestigeQuote(d.Id);
        EventUiStyle.SetText(_resource, !enabled ? "Prestige is disabled for this event." : NumberFormatter.Format(quote.CurrentResource) + " " + d!.Prestige.ResourceName
            + " • " + NumberFormatter.Format(_manager.GetTotalPrestigeResourceEarned(d.Id)) + " earned");
        EventUiStyle.SetText(_bonus, !enabled ? "" : "Permanent production bonus: +" + ((quote.CurrentMultiplier - 1) * 100).ToString("0.#") + "%");
        EventUiStyle.SetText(_requirement, !enabled ? "" : "Earned this run: " + NumberFormatter.Format(quote.RunEarned) + " / " + NumberFormatter.Format(quote.Requirement) + " " + d!.CurrencyName);
        EventUiStyle.SetText(_preview, !enabled ? "" : "Prestige grants: +" + NumberFormatter.Format(quote.ResourceGained) + " " + d!.Prestige.ResourceName);
        EventUiStyle.SetDisabled(_button, !quote.Available);
        if (_overlay.Visible && !quote.Available) Dismiss();
    }
}
