using Godot;
using System;

namespace IdleAi;

/// <summary>Event data binding for the same enlarged bot preview used by room details.</summary>
internal sealed class EventBotDetailPanel
{
    private readonly SkinPreviewOverlay _preview;
    private readonly EventManager _manager;
    private readonly EventTextureCache _textures;
    private EventDefinition? _definition;
    private EventBotDefinition? _bot;
    internal bool Visible => _preview.Visible;
    internal event Action? VisibilityChanged;

    internal EventBotDetailPanel(Control page, EventManager manager, EventTextureCache textures)
    {
        _manager = manager;
        _textures = textures;
        _preview = new SkinPreviewOverlay(page, "EventBotDetail");
        _preview.Initialize();
        _preview.VisibilityChanged += () => VisibilityChanged?.Invoke();
    }

    internal void Open(EventDefinition definition, EventBotDefinition bot)
    {
        _definition = definition;
        _bot = bot;
        _preview.OpenDetail(_textures.Get(bot.IconPath), bot.Name, bot.Rarity.ToUpperInvariant(), Details());
    }

    private string Details()
    {
        if (_definition == null || _bot == null) return "";
        var snapshot = _manager.GetBot(_definition.Id, _bot.Id);
        return _bot.Description + "\n" + EventBotPresentation.Bonus(_bot)
            + "\n" + EventBotPresentation.Requirement(_definition, _bot)
            + "\n" + EventBotPresentation.Status(snapshot, _manager.IsActive(_definition));
    }

    internal void Refresh()
    {
        if (Visible) _preview.SetDetails(Details());
    }

    internal void Dismiss() => _preview.Close();
    internal bool DismissOnBack() { if (!Visible) return false; Dismiss(); return true; }
}
