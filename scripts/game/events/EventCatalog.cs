using System.Collections.Generic;

namespace IdleAi;

public static class EventCatalog
{
    public const string TestEventId = "event_test_001";
    // Fixed UTC dates: never extend the event on restart. Disable/remove before a live event release.
    public static IReadOnlyList<EventDefinition> Create(long? halloweenStartUnix = null, long? halloweenEndUnix = null) =>
    [
        TestProtocolEventDefinition.Create(),
        HalloweenEventDefinition.Create(halloweenStartUnix, halloweenEndUnix)
    ];
}
