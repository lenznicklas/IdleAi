namespace IdleAi;

public readonly record struct PrestigeResult(
	bool Success,
	string Message
);


public sealed class PrestigeService
{
	private const double RequiredRunTokens =
		1_000_000_000_000_000_000.0;

	private readonly GameState _state;
	private readonly NormalRunResetService _runReset;

	public PrestigeService(GameState state)
		: this(state, new NormalRunResetService(state, new EconomyService(state))) { }

	public PrestigeService(
		GameState state,
		EconomyService economy)
		: this(state, new NormalRunResetService(state, economy)) { }

	public PrestigeService(GameState state, NormalRunResetService runReset)
	{
		_runReset = runReset;
		_state =
			state;
	}

	public double GetRequiredRunTokens()
	{
		return RequiredRunTokens;
	}

	public bool CanPrestige()
	{
		return _state.RunEarnedTokens
			>= RequiredRunTokens;
	}

	public double GetProductionMultiplier()
	{
		return _state.Prestige
			.GetProductionMultiplier();
	}

	public double GetProductionBonusPercent()
	{
		return _state.Prestige
			.GetProductionBonusPercent();
	}

	public double GetProductionMultiplierAfterNextPrestige()
	{
		int nextCount =
			_state.Prestige.PrestigeCount
				== int.MaxValue
					? int.MaxValue
					: _state.Prestige.PrestigeCount
						+ 1;

		return PrestigeData
			.GetProductionMultiplierForPrestigeCount(
				nextCount
			);
	}

	public PrestigeResult Prestige()
	{
		if (!CanPrestige())
		{
			double remaining =
				System.Math.Max(
					0.0,
					RequiredRunTokens
						- _state.RunEarnedTokens
				);

			return new PrestigeResult(
				false,
				"Earn "
					+ NumberFormatter.Format(
						remaining
					)
					+ " more Tokens this run to prestige."
			);
		}

		int totalLevelBeforePrestige =
			ProgressionService
				.CalculateTotalLevel(
					_state
				);

		if (
			_state.Prestige.PrestigeCount
				< int.MaxValue
		)
		{
			_state.Prestige.PrestigeCount++;
		}

		_runReset.Reset();

		int resetRunLevel =
			ProgressionService
				.CalculateCurrentRunLevel(
					_state
				);

		_state.LifetimeLevelBase =
			System.Math.Max(
				0,
				totalLevelBeforePrestige
					- resetRunLevel
			);

		return new PrestigeResult(
			true,
			"Prestige complete! Total Level remains "
				+ ProgressionService
					.CalculateTotalLevel(
						_state
					)
				+ ". Permanent production x"
				+ GetProductionMultiplier()
					.ToString(
						"F2"
					)
		);
	}

}
