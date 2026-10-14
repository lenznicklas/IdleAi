#if DEBUG
using System;

namespace IdleAi;

public sealed partial class EventManager
{
    internal EventActionResult DebugChallenges(string operation, string id = "", double value = 0)
    {
        if (_actionInProgress) return Busy();
        Tick();
        if (_active == null || !IsActive(_active)) return new(false, "No active event.");
        var d = _active;
        long beforeOffset = _debugChallengeDayOffset;
        var result = Transaction(d, _ =>
        {
            var p = _save.ProgressByEventId[d.Id];
            if (operation is "day" or "daily")
            {
                if (operation == "day") _debugChallengeDayOffset++;
                EnsureChallengeSchedule(d, p, NowUnix, forceDaily: true);
                return true;
            }
            bool found = false;
            foreach (var c in d.Challenges)
            {
                if (!c.Enabled || !p.Challenges.Progress.TryGetValue(c.Id, out var state) || operation == "set" && c.Id != id) continue;
                if (operation == "set" && (!double.IsFinite(value) || value < 0)) return false;
                state.Progress = operation == "set" ? Math.Min(value, c.Target) : c.Target;
                state.Complete = state.Progress >= c.Target;
                // Marking ready never reissues a previously claimed reward.
                if (state.Claimed) { state.Complete = true; state.Progress = c.Target; }
                found = true;
            }
            p.Challenges.Revision++;
            return found;
        }, "Event challenge debug state updated.");
        if (!result.Changed) _debugChallengeDayOffset = beforeOffset;
        else _challengeScheduleDirty = false;
        return result;
    }
}
#endif
