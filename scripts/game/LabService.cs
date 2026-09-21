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


	// ==================================================
	// LAB UNLOCK
	// ==================================================

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


	// ==================================================
	// RESEARCH POINTS
	// ==================================================

	public double GetResearchPointPurchaseCost(
		int amount)
	{
		if (amount <= 0)
			return 0.0;


		return amount
			* GameConfig.TokensPerResearchPoint;
	}


	public LabResult BuyResearchPoints(
		int amount)
	{
		if (!_state.Lab.Unlocked)
		{
			return new LabResult(
				false,
				"Unlock the Laboratory first."
			);
		}


		if (amount <= 0)
		{
			return new LabResult(
				false,
				"Invalid Research Point amount."
			);
		}


		double cost =
			GetResearchPointPurchaseCost(
				amount
			);


		if (_state.Tokens < cost)
		{
			return new LabResult(
				false,
				"Not enough Tokens to buy Research Points."
			);
		}


		_state.Tokens -=
			cost;


		_state.Lab.ResearchPoints +=
			amount;


		_state.Stats.AddMachineSpending(
			"Research Points",
			cost
		);


		return new LabResult(
			true,
			$"+{amount} Research Points"
		);
	}


	// ==================================================
	// RESEARCH
	// ==================================================

	public bool IsResearchAvailable(
		ResearchDefinition research)
	{
		if (!_state.Lab.Unlocked)
			return false;


		if (
			_state.Lab.IsResearchCompleted(
				research.Id
			)
		)
		{
			return false;
		}


		if (
			research.PrerequisiteId != null
			&& !_state.Lab.IsResearchCompleted(
				research.PrerequisiteId
			)
		)
		{
			return false;
		}


		return true;
	}


	public LabResult Research(
		string researchId)
	{
		if (!_state.Lab.Unlocked)
		{
			return new LabResult(
				false,
				"Unlock the Laboratory first."
			);
		}


		ResearchDefinition? research =
			ResearchCatalog.Get(
				researchId
			);


		if (research == null)
		{
			return new LabResult(
				false,
				"Unknown research."
			);
		}


		if (
			_state.Lab.IsResearchCompleted(
				research.Id
			)
		)
		{
			return new LabResult(
				false,
				"Research already completed."
			);
		}


		if (
			research.PrerequisiteId != null
			&& !_state.Lab.IsResearchCompleted(
				research.PrerequisiteId
			)
		)
		{
			return new LabResult(
				false,
				"Complete the previous research first."
			);
		}


		if (
			_state.Lab.ResearchPoints
			< research.Cost
		)
		{
			return new LabResult(
				false,
				"Not enough Research Points."
			);
		}


		_state.Lab.ResearchPoints -=
			research.Cost;


		_state.Lab.CompleteResearch(
			research.Id
		);


		return new LabResult(
			true,
			$"{research.Name} completed!"
		);
	}


	// ==================================================
	// EFFECTS
	// ==================================================

	public double GetProductionMultiplier()
	{
		return _state.Lab
			.GetProductionMultiplier();
	}
}
