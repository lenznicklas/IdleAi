using Godot;
using System;
using System.Linq;

namespace IdleAi;

public partial class Game
{
    private EventManager? _eventManager;
    private EventController? _eventController;
#if DEBUG
    private EventDeveloperTools? _eventTools;
#endif
    private void InitializeEvents()
    {
        _eventManager = new EventManager(_state.Events, EventCatalog.Create(), TrySaveGame,
            canWrite: () => !_saveManager.LastLoadFailed && _purchases?.RecoveryBlocked != true,
            validateCosmetic: reward => reward.Type == EventRewardType.MachineSkin
                ? MachineSkinCatalog.TryGet(reward.Id, out _) : BotSkinCatalog.TryGet(reward.Id, out _),
            grantExternalReward: reward =>
            {
                if (reward.Type != EventRewardType.DataShards) return null;
                _economy.GrantDataShards(reward.Amount);
                return () => _economy.RefundDataShards(reward.Amount);
            });
        _eventManager.Tick();
        SyncEventCosmetics();
        _eventManager.Changed += SyncEventCosmetics;
        _eventController = new EventController(this, _ui.Map, _eventManager, () => _singularityController?.Hide());
        _eventController.MessageRequested += _ui.SetMessage;
        AddChild(new EventRuntime(_eventManager, _eventController, TrySaveGame));
#if DEBUG
        if (OS.IsDebugBuild() && OS.GetCmdlineUserArgs().Contains("--event-tools"))
            _eventTools = new EventDeveloperTools(_eventManager);
#endif
    }
    private int _eventCosmeticCount = -1;
    private void SyncEventCosmetics()
    {
        int count = _state.Events.CosmeticEntitlements.Count;
        if (_eventCosmeticCount == count) return;
        _eventCosmeticCount = count;
        _services.BotSkins.SyncEventOwnership();
        _services.MachineSkins.SyncEventOwnership();
    }
    internal EventActionResult? TryEventDeveloperCode(string code)
    {
#if DEBUG
        return _eventTools?.Execute(code);
#else
        return null;
#endif
    }
}
