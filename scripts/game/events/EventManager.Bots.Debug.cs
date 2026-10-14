#if DEBUG
namespace IdleAi;

public sealed partial class EventManager
{
    internal EventActionResult DebugBots(string operation, string id = "")
    {
        if (_actionInProgress) return Busy();
        Tick();
        if (_active == null || !IsActive(_active)) return new(false, "No active event.");
        var d = _active;
        if (operation == "equip") return EquipBot(d.Id, id);
        return Transaction(d, undo =>
        {
            var p = _save.ProgressByEventId[d.Id];
            if (operation == "reset") { p.OwnedBots.Clear(); p.ActiveBotId = null; }
            else
            {
                bool found = false;
                foreach (var bot in d.Bots)
                    if (bot.Enabled && (operation == "all" || bot.Id == id))
                    {
                        if (!GrantReward(d, p, new(EventRewardType.EventBot, 1, bot.Id), undo)) return false;
                        found = true;
                    }
                if (!found) return false;
            }
            _production[d.Id].RefreshHistory(p);
            RefreshChallenges(d, p, EventChallengeSignal.Bots | EventChallengeSignal.Production, NowUnix);
            return true;
        }, "Event bot debug state updated.");
    }
}
#endif
