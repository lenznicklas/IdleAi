using System;

namespace IdleAi;

// Runtime Matter transactions only. Persistence hydrates the shared DTO directly.
internal sealed class SingularityEconomy
{
    private readonly SingularitySaveData _data;
    internal SingularityEconomy(SingularitySaveData data) => _data = data;
    internal bool CanAfford(double cost) => !(_data.Matter < cost);
    internal bool TrySpend(double cost)
    {
        if (!CanAfford(cost)) return false;
        _data.Matter -= cost;
        return true;
    }
    internal void GrantProduction(double amount) => _data.Matter += amount;
    internal void Refund(double amount) => _data.Matter += amount;
    internal static SingularityActionResult NotEnoughMatter(double cost) =>
        new(false, "Need " + NumberFormatter.Format(cost) + " Singularity Matter.");
}
