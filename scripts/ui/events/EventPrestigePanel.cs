using Godot;
using System;

namespace IdleAi;

internal sealed class EventPrestigePanel
{
    internal Control Root { get; }
    private readonly EventManager _manager;
    private readonly Control _overlay;
    private readonly PanelContainer _confirmation;
    private readonly Label _resource, _bonus, _requirement, _preview, _confirmText;
    private readonly Button _button, _confirm;
    private EventPrestigeQuote _quote;
    private string? _confirmEventId;
    private readonly Action<int, long> _prestige;
    private readonly Func<EventDefinition?> _currentEvent;
    internal EventPrestigePanel(EventManager manager, Control page, Action<int, long> prestige, Func<EventDefinition?> currentEvent)
    {
        _manager = manager; _prestige = prestige; _currentEvent = currentEvent;
        var holder = new VBoxContainer { Name = "EventPrestigePanel", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        Root = holder;
        var box = EventUiStyle.Panel(holder, "PrestigeDetails");
        box.AddChild(EventUiStyle.Label("PrestigeTitle", "EVENT PRESTIGE", 20));
        _resource = Text(box, "PrestigeResource"); _bonus = Text(box, "PrestigeBonus");
        _requirement = Text(box, "PrestigeRequirement"); _preview = Text(box, "PrestigePreview");
        _button = EventUiStyle.Button("EventPrestigeButton", "Prestige Event", Request);
        box.AddChild(_button);
        _overlay = new Control { Name = "EventPrestigeConfirmation", ZIndex = 10, Visible = false, MouseFilter = Control.MouseFilterEnum.Stop };
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
        parent.AddChild(label); return label;
    }
    private void Request()
    {
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
    internal void Refresh(EventDefinition? d)
    {
        bool enabled = d?.Prestige.Enabled == true;
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
