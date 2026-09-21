using System;

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
	// LAB
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
	// ACTIVE RESEARCH
	// ==================================================

	public bool HasActiveResearch()
	{
		return _state.Lab
			.HasActiveResearch;
	}


	public ResearchDefinition? GetActiveResearch()
	{
		if (
			!_state.Lab
				.HasActiveResearch
		)
		{
			return null;
		}


		return ResearchCatalog.Get(
			_state.Lab.ActiveResearchId!
		);
	}


	public double GetRemainingResearchSeconds()
	{
		if (
			!_state.Lab
				.HasActiveResearch
		)
		{
			return 0.0;
		}


		long now =
			GetCurrentUnixTime();


		return Math.Max(
			0.0,
			_state.Lab.ActiveResearchEndUnix
			- now
		);
	}


	public double GetActiveResearchProgress()
	{
		ResearchDefinition? research =
			GetActiveResearch();


		if (research == null)
			return 0.0;


		double duration =
			ResearchCatalog
				.GetDurationSeconds(
					research
				);


		if (duration <= 0.0)
			return 1.0;


		double remaining =
			GetRemainingResearchSeconds();


		return Math.Clamp(
			1.0
			- remaining / duration,
			0.0,
			1.0
		);
	}


	// ==================================================
	// AVAILABILITY
	// ==================================================

	public bool IsResearchAvailable(
		ResearchDefinition research)
	{
		if (!_state.Lab.Unlocked)
			return false;


		if (
			_state.Lab
				.HasActiveResearch
		)
		{
			return false;
		}


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


	// ==================================================
	// START RESEARCH
	// ==================================================

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


		if (
			_state.Lab
				.HasActiveResearch
		)
		{
			ResearchDefinition? active =
				GetActiveResearch();


			string activeName =
				active?.Name
				?? "another research";


			return new LabResult(
				false,
				$"{activeName} is already being researched."
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


		double duration =
			ResearchCatalog
				.GetDurationSeconds(
					research
				);


		_state.Lab.ResearchPoints -=
			research.Cost;


		_state.Lab.ActiveResearchId =
			research.Id;


		_state.Lab.ActiveResearchEndUnix =
			GetCurrentUnixTime()
			+ (long)Math.Ceiling(
				duration
			);


		return new LabResult(
			true,
			$"{research.Name} started!"
		);
	}


	// ==================================================
	// UPDATE
	// ==================================================

	public LabResult Update()
	{
		if (
			!_state.Lab
				.HasActiveResearch
		)
		{
			return new LabResult(
				false,
				""
			);
		}


		if (
			GetRemainingResearchSeconds()
			> 0.0
		)
		{
			return new LabResult(
				false,
				""
			);
		}


		return CompleteActiveResearch();
	}


	// ==================================================
	// COMPLETE
	// ==================================================

	private LabResult CompleteActiveResearch()
	{
		ResearchDefinition? research =
			GetActiveResearch();


		if (research == null)
		{
			_state.Lab
				.ClearActiveResearch();


			return new LabResult(
				true,
				"Invalid research was cleared."
			);
		}


		_state.Lab.CompleteResearch(
			research.Id
		);


		_state.Lab.ClearActiveResearch();


		/*
		 * Important for Overclocking / Cooling.
		 * Existing machine cycles are immediately
		 * adjusted when the research completes.
		 */

		RefreshCycleDurations();


		return new LabResult(
			true,
			$"{research.Name} completed!"
		);
	}


	// ==================================================
	// CYCLE REFRESH
	// ==================================================

	private void RefreshCycleDurations()
	{
		foreach (
			RoomState room
			in _state.RoomStates
		)
		{
			foreach (
				SlotData slot
				in room.Slots
			)
			{
				if (!slot.Unlocked)
					continue;


				double baseDuration =
					GameConfig
						.GetProductionCycleSeconds(
							slot.MachineTier
						);


				double oldDuration =
					slot.RuntimeCycleDuration > 0.0
						? slot.RuntimeCycleDuration
						: baseDuration;


				double newDuration =
					Math.Max(
						0.25,
						baseDuration
						* _state.Lab
							.GetCycleTimeMultiplier()
					);


				if (
					slot.IsRunning
					&& oldDuration > 0.0
				)
				{
					double remainingRatio =
						Math.Clamp(
							slot.CycleRemaining
							/ oldDuration,
							0.0,
							1.0
						);


					slot.CycleRemaining =
						newDuration
						* remainingRatio;
				}


				slot.RuntimeCycleDuration =
					newDuration;
			}
		}
	}


	// ==================================================
	// HELPERS
	// ==================================================

	private static long GetCurrentUnixTime()
	{
		return DateTimeOffset
			.UtcNow
			.ToUnixTimeSeconds();
	}


	public double GetProductionMultiplier()
	{
		return _state.Lab
			.GetProductionMultiplier();
	}
}
