#nullable enable
using System;

namespace IdleAi;

/// <summary>One tick fans out independently; unavailable native adapters are safe no-ops.</summary>
public sealed class PlayGamesSyncWorkflow
{
    private readonly Action _achievements;
    private readonly Action _official;
    private readonly Action _firebase;
    private readonly Action<Exception> _error;
    public PlayGamesSyncWorkflow(Action achievements, Action official, Action firebase, Action<Exception> error)
    { _achievements = achievements; _official = official; _firebase = firebase; _error = error; }
    public void Tick()
    {
        Invoke(_achievements);
        Invoke(_official);
        Invoke(_firebase);
    }
    private void Invoke(Action action)
    {
        try { action(); }
        catch (Exception exception) { _error(exception); }
    }
}
