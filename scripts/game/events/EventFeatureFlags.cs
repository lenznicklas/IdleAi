using Godot;
using System.Linq;

namespace IdleAi;

/// <summary>Export switch. Disabled Events never initialize or mutate saved Event progress.</summary>
public static class EventFeatureFlags
{
    // Controls normal player access; disable only for temporary exports.
    public const bool EventsEnabled = true;

    public static bool EnabledForSession => EventsEnabled
#if DEBUG
        // Explicit developer/test opt-in only; compiled out of export release builds.
        || (OS.IsDebugBuild() && OS.GetCmdlineUserArgs().Contains("--event-tools"))
#endif
        ;
}
