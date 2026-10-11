#nullable enable
using System;
using System.Collections.Generic;

namespace IdleAi;

/// <summary>First-party purchase policy. SDK state and token persistence stay in billing.</summary>
public sealed class IapGrantWorkflow
{
    private readonly Func<int, ShopResult> _grant;
    private readonly Func<string, bool> _processed;
    private readonly Func<string, bool> _save;
    private readonly Func<string, bool> _complete;
    private readonly HashSet<string> _granted = new(StringComparer.Ordinal);
    private readonly HashSet<string> _completed = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ShopResult> _results = new(StringComparer.Ordinal);
    private bool _handling;
    private readonly Func<string, int, bool> _prepare;
    private readonly Action _abort;
    private (string Product, string Token, int Quantity)? _pending;
    public event Action? RewardGranted;
    public event Action<string>? MessageRequested;

    public IapGrantWorkflow(Func<int, ShopResult> grant, Func<string, bool> processed,
        Func<string, bool> save, Func<string, bool> complete,
        Func<string, int, bool>? prepare = null, Action? abort = null)
    { _grant = grant; _processed = processed; _save = save; _complete = complete;
      _prepare = prepare ?? ((_, _) => true); _abort = abort ?? (() => { }); }

    public void RetryPending()
    { if (_pending is { } pending) Handle(pending.Product, pending.Token, pending.Quantity); }

    public void Handle(string productId, string token, int quantity)
    {
        if (_handling || string.IsNullOrWhiteSpace(token) || _completed.Contains(token) || _processed(token)) return;
        if (!IapCatalog.TryGet(productId, out var product)) return;
        long amount = (long)product.ShardAmount * Math.Max(1, quantity);
        if (amount > int.MaxValue) { MessageRequested?.Invoke("Purchase completed, but the reward could not be added."); return; }
        _handling = true;
        try
        {
            if (!_granted.Contains(token))
            {
                if (!_prepare(token, (int)amount))
                { MessageRequested?.Invoke("Purchase reward could not be saved. Please try again."); return; }
                ShopResult result = _grant((int)amount);
                if (!result.Changed)
                { _abort(); MessageRequested?.Invoke("Purchase completed, but the reward could not be added."); return; }
                _granted.Add(token);
                _results[token] = result;
                _pending = (productId, token, quantity);
            }
            // A failed save/finalization is retryable without granting twice in this process.
            if (!_save(token))
            { MessageRequested?.Invoke("Purchase reward is waiting for a successful game save."); return; }
            if (!_complete(token))
            { MessageRequested?.Invoke("Purchase reward saved. Finalization will be retried."); return; }
            _completed.Add(token);
            _pending = null;
            RewardGranted?.Invoke();
            MessageRequested?.Invoke(_results[token].Message);
        }
        finally { _handling = false; }
    }
}
