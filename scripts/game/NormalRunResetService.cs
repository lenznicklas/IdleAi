namespace IdleAi;

/// <summary>Coordinates normal run resets without changing permanent progression.</summary>
public sealed class NormalRunResetService
{
	private readonly GameState _state;
	private readonly EconomyService _economy;

	public NormalRunResetService(GameState state, EconomyService economy)
	{
		_state = state;
		_economy = economy;
	}

	public void Reset()
	{
		AiArchiveDiscovery.ObserveRun(_state);
		_economy.ResetRunTokens();
		_state.CurrentRoomIndex = 0;
		for (int roomIndex = 0; roomIndex < _state.RoomStates.Count; roomIndex++)
			_state.RoomStates[roomIndex].ResetForNewRun(roomIndex == 0);
	}
}
