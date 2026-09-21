namespace IdleAi;


public readonly record struct LabResult(
	bool Changed,
	string Message
);


public sealed class LabService
{
	private readonly GameState _state;


	public LabService(
		GameState state)
	{
		_state =
			state;
	}


	public LabResult UnlockLab()
	{
		if (_state.Lab.Unlocked)
		{
			return new LabResult(
				false,
				"Laboratory already unlocked."
			);
		}


		double cost =
			GameConfig.LabUnlockCost;


		if (_state.Tokens < cost)
		{
			return new LabResult(
				false,
				"Not enough Tokens to unlock the Laboratory."
			);
		}


		_state.Tokens -=
			cost;


		_state.Stats.AddMachineSpending(
			"Laboratory",
			cost
		);


		_state.Lab.Unlocked =
			true;


		return new LabResult(
			true,
			"Laboratory unlocked!"
		);
	}
}
