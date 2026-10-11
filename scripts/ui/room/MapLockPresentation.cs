using Godot;

namespace IdleAi;

internal readonly record struct RoomMapCardState(
    bool IsUnlocked, bool IsCurrent, string StatusText, string RequirementText);

internal readonly record struct RoomMapCardInputs(
    bool IsUnlocked, bool IsCurrent, bool PreviousUnlocked, int MissingSlots, double UnlockCost);

/// <summary>Compact map copy derived from the existing sequential unlock rules.</summary>
internal static class MapLockPresentation
{
    public static RoomMapCardState GetRoomCardState(GameState state, int roomIndex, RoomMapCardInputs inputs)
    {
        if (inputs.IsCurrent)
            return new(inputs.IsUnlocked, true, "CURRENT LOCATION", "● CURRENT");
        if (inputs.IsUnlocked)
            return new(true, false, "TAP TO ENTER", "ENTER");
        return new(false, false, "LOCKED", GetRoomLockMessage(state, roomIndex, inputs.UnlockCost));
    }

    // This is the only source of prerequisite copy for all normal room cards.
    public static string GetRoomLockMessage(GameState state, int roomIndex, double cost) =>
        GetRequirementBadge(state, roomIndex, cost);

    public static string GetRequirementBadge(GameState state, int roomIndex, double cost, bool singularity = false)
    {
        bool eligible = singularity
            ? RoomUnlockRules.CanUnlockSingularity(state, out _)
            : RoomUnlockRules.CanUnlockRoom(state, roomIndex, out _);
        if (eligible) return "UNLOCK • " + NumberFormatter.Format(cost);

        int previousIndex = singularity ? GameConfig.QuantumRoomIndex : roomIndex - 1;
        if (previousIndex < 0 || previousIndex >= state.RoomStates.Count) return "LOCKED";
        var previous = state.RoomStates[previousIndex];
        if (!previous.Unlocked) return $"UNLOCK ROOM {previousIndex + 1} FIRST";
        int missing = RoomUnlockRules.CountLockedSlots(previous);
        return $"UNLOCK {missing} MORE SLOT{(missing == 1 ? "" : "S")}";
    }

    public static void SetText(Label label, string text)
    {
        if (label.Text != text) label.Text = text;
    }

    public static void ConfigureLabel(Label label)
    {
        // All map status/badge copy fits on one line at the existing font sizes.
        // Clipping prevents a future longer message from resizing its fixed card.
        label.AutowrapMode = TextServer.AutowrapMode.Off;
        label.ClipText = true;
        label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        label.CustomMinimumSize = new Vector2(0, 14);
    }
}
