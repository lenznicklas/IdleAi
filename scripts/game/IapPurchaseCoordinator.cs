#nullable enable
using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IdleAi;

/// <summary>Billing facade and explicit grant/save/ledger/consume composition.</summary>
public sealed class IapPurchaseCoordinator
{
    private readonly GooglePlayBillingService _billing;
    private readonly IapGrantWorkflow _workflow;
    private readonly IapGrantJournal _journal;
    private readonly SaveManager _persistence;
    private bool _initialized;
    private bool _retrying;
    private readonly Dictionary<string, (string Product, int Quantity)> _readyPurchases = new(StringComparer.Ordinal);
    public bool RecoveryBlocked { get; private set; }
    public event Action? Changed;
    public event Action? RewardGranted;
    public event Action<string>? MessageRequested;
    public bool PurchaseFlowActive => _billing.PurchaseFlowActive;
    public string PurchaseFlowProductId => _billing.PurchaseFlowProductId;

    public IapPurchaseCoordinator(ShopService shop, Func<SaveGameData> snapshot,
        Func<bool> saveGame, SaveManager persistence)
    {
        _persistence = persistence;
        _billing = new GooglePlayBillingService();
        _journal = new IapGrantJournal(
            ProjectSettings.GlobalizePath("user://idle_ai_iap_grant.json"),
            ProjectSettings.GlobalizePath("user://idle_ai_save.json"));
        _workflow = new IapGrantWorkflow(shop.GrantDataShards, _billing.IsPurchaseProcessed,
            _ => saveGame(), Complete,
            (token, amount) =>
            {
                if (RecoveryBlocked || _persistence.LastLoadFailed) return false;
                var rewardSave = snapshot();
                rewardSave.DataShards += amount; // DTO only: gameplay grant still uses ShopService/Economy.
                return _journal.Prepare(token, rewardSave);
            },
            () => { if (!_journal.Clear()) RecoveryBlocked = true; });
        _workflow.RewardGranted += () => RewardGranted?.Invoke();
        _workflow.MessageRequested += message => MessageRequested?.Invoke(message);
        _billing.Changed += () => Changed?.Invoke();
        _billing.MessageRequested += message => MessageRequested?.Invoke(message);
        _billing.ConsumablePurchaseReady += (product, token, quantity) =>
        {
            _readyPurchases.TryAdd(token, (product, quantity));
            RetryPending();
        };
    }
    /// <summary>Must run before normal state hydration, offline settlement, or any gameplay saves.</summary>
    public void RecoverBeforeLoad()
    {
        // Establish and respect the existing corruption lock before any recovery write.
        if (_journal.Exists) _persistence.LoadData();
        RecoveryBlocked = _persistence.LastLoadFailed ||
            !_journal.Recover(_persistence.SaveData, _billing.IsPurchaseProcessed, _billing.CompleteConsumableGrant);
    }
    private bool Complete(string token)
    {
        if (!_journal.ConfirmSaved() || !_billing.CompleteConsumableGrant(token)) return false;
        if (!_journal.Clear()) RecoveryBlocked = true;
        return true; // Ledger is already durable; cleanup failure cannot cause another grant.
    }
    public void Initialize()
    {
        if (_initialized) return;
        _initialized = true;
        _billing.Initialize();
    }
    public bool BeforeGameSave(SaveGameData snapshot) => !RecoveryBlocked && _journal.BeforeGameSave(snapshot);
    public void GameSaved()
    {
        if (_journal.ConfirmSaved()) RetryPending();
    }
    public void RetryPending()
    {
        if (_retrying || RecoveryBlocked) return;
        _retrying = true;
        try
        {
            foreach (var purchase in _readyPurchases.ToArray())
            {
                _workflow.Handle(purchase.Value.Product, purchase.Key, purchase.Value.Quantity);
                if (_billing.IsPurchaseProcessed(purchase.Key)) _readyPurchases.Remove(purchase.Key);
            }
        }
        finally { _retrying = false; }
    }
    private bool CanGrant => !RecoveryBlocked && !_persistence.LastLoadFailed && !_journal.Exists;
    public void Purchase(string productId)
    { RetryPending(); if (CanGrant) _billing.Purchase(productId); }
    public bool CanPurchase(string productId) => CanGrant && _billing.CanPurchase(productId);
    public string GetStatusText(string productId) => _billing.GetStatusText(productId);
    public string GetButtonText(string productId) => _billing.GetButtonText(productId);
}
