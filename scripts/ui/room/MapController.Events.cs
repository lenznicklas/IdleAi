using Godot;

namespace IdleAi;

public sealed partial class MapController
{
    // The right side of the frontier row is unused by the room network.
    internal void AddEventMapCard(Button card)
    {
        card.Position = new Vector2(398, 12);
        card.Size = card.CustomMinimumSize = new Vector2(220, 190);
        _canvas.AddChild(card);
        _root.GetNodeOrNull<UiReadabilityController>("UiReadabilityController")?.ApplyToSubtree(card);
    }
}
