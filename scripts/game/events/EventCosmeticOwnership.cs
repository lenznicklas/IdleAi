namespace IdleAi;

// The atomic main save is the durable event-entitlement source. Import into the
// existing global skin ownership stores idempotently, without buying or auto-equipping.
// Failed cosmetic-file writes can be replayed from the main save on next launch.
public sealed partial class BotSkinService
{
    public void SyncEventOwnership()
    {
        bool changed = false;
        foreach (string entitlement in _state.Events.CosmeticEntitlements)
            if (entitlement.StartsWith("bot:", System.StringComparison.Ordinal)
                && BotSkinCatalog.TryGet(entitlement[4..], out _)) changed |= _ownedSkinIds.Add(entitlement[4..]);
        if (changed) Save();
    }
}
public sealed partial class MachineSkinService
{
    public void SyncEventOwnership()
    {
        bool changed = false;
        foreach (string entitlement in _state.Events.CosmeticEntitlements)
            if (entitlement.StartsWith("machine:", System.StringComparison.Ordinal)
                && MachineSkinCatalog.TryGet(entitlement[8..], out _)) changed |= _ownedSkinIds.Add(entitlement[8..]);
        if (changed) Save();
    }
}
