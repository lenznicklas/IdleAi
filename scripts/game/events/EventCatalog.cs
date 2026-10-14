using System.Collections.Generic;

namespace IdleAi;

public static class EventCatalog
{
    public const string TestEventId = "event_test_001";
    // Keep the sandbox definition/save IDs available, but never auto-select it.
    // DebugForce explicitly enables it for the current development session only.
    public static IReadOnlyList<EventDefinition> Create(long? halloweenStartUnix = null, long? halloweenEndUnix = null) =>
    [
        TestProtocolEventDefinition.Create() with { Enabled = false },
        HalloweenEventDefinition.Create(halloweenStartUnix, halloweenEndUnix)
    ];
}
