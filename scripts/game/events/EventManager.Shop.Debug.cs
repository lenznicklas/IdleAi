#if DEBUG
using System;
using System.Linq;

namespace IdleAi;

public sealed partial class EventManager
{
    internal EventActionResult DebugShop(string operation, string id = "", int count = 0)
    {
        if (_actionInProgress) return Busy();
        Tick();
        if (_active == null || !IsActive(_active)) return new(false, "No active event.");
        var d = _active;
        if (operation == "print")
        {
            foreach (var item in GetShopItems(d.Id))
            {
                var state = GetShopItem(d.Id, item.Id);
                Godot.GD.Print($"[Event Shop] event={d.Id}; item={item.Id}; state={state.State}; cost={item.Cost}; currency={item.CostCurrency}; balance={state.Balance}; purchases={state.PurchasedCount}; limit={state.PurchaseLimit}");
            }
            return new(false, "Shop state printed to debug output.");
        }
        return Transaction(d, undo =>
        {
            var p = _save.ProgressByEventId[d.Id];
            switch (operation)
            {
                case "unlock": foreach (var item in d.Shop) if (item.Enabled) p.UnlockedShopItems.Add(item.Id); break;
                case "reset": p.ShopPurchases.Clear(); break;
                case "count":
                    if (count < 0 || !d.Shop.Any(i => i.Id == id)) return false;
                    p.ShopPurchases[id] = count; break;
                case "fund":
                    var offer = d.Shop.FirstOrDefault(i => i.Id == id && i.Enabled);
                    if (offer == null) return false;
                    double amount = Math.Max(0, offer.Cost - GetShopBalance(d.Id, offer.CostCurrency));
                    if (amount > 0)
                    {
                        var type = offer.CostCurrency switch
                        {
                            EventShopCurrency.EventCurrency => EventRewardType.EventCurrency,
                            EventShopCurrency.PrestigeResource => EventRewardType.PrestigeResource,
                            EventShopCurrency.DataShards => EventRewardType.DataShards,
                            _ => EventRewardType.Custom
                        };
                        if (!GrantReward(d, p, new(type, amount), undo)) return false;
                    }
                    break;
                default: return false;
            }
            RefreshChallenges(d, p, EventChallengeSignal.All, NowUnix);
            return true;
        }, "Event Shop debug state updated.");
    }
}
#endif
