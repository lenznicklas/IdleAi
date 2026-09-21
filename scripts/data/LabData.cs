using System.Collections.Generic;

namespace IdleAi;

public sealed class LabData
{
	public bool Unlocked { get; set; }

	public double ResearchPoints { get; set; }


	// ==================================================
	// ACTIVE RESEARCH
	// ==================================================

	public string? ActiveResearchId { get; set; }

	public long ActiveResearchEndUnix { get; set; }


	public bool HasActiveResearch =>
		!string.IsNullOrWhiteSpace(
			ActiveResearchId
		);


	// ==================================================
	// COMPLETED RESEARCH
	// ==================================================

	private readonly HashSet<string> _completedResearch =
		[];


	public IReadOnlyCollection<string> CompletedResearch =>
		_completedResearch;


	public bool IsResearchCompleted(
		string researchId)
	{
		return _completedResearch.Contains(
			researchId
		);
	}


	public void CompleteResearch(
		string researchId)
	{
		_completedResearch.Add(
			researchId
		);
	}


	public void LoadCompletedResearch(
		IEnumerable<string>? researchIds)
	{
		_completedResearch.Clear();


		if (researchIds == null)
			return;


		foreach (
			string researchId
			in researchIds
		)
		{
			if (
				string.IsNullOrWhiteSpace(
					researchId
				)
			)
			{
				continue;
			}


			_completedResearch.Add(
				researchId
			);
		}
	}


	// ==================================================
	// ACTIVE RESEARCH RESET
	// ==================================================

	public void ClearActiveResearch()
	{
		ActiveResearchId =
			null;


		ActiveResearchEndUnix =
			0;
	}


	// ==================================================
	// TEST RESET
	// ==================================================

	public void ResetResearch()
	{
		_completedResearch.Clear();


		ClearActiveResearch();
	}


	// ==================================================
	// PRODUCTION
	// ==================================================

	public double GetProductionBonus()
	{
		return ResearchCatalog
			.GetProductionBonus(
				this
			);
	}


	public double GetProductionMultiplier()
	{
		return ResearchCatalog
			.GetProductionMultiplier(
				this
			);
	}


	// ==================================================
	// CYCLE
	// ==================================================

	public double GetCycleTimeReduction()
	{
		return ResearchCatalog
			.GetCycleTimeReduction(
				this
			);
	}


	public double GetCycleTimeMultiplier()
	{
		return ResearchCatalog
			.GetCycleTimeMultiplier(
				this
			);
	}


	// ==================================================
	// BOT
	// ==================================================

	public double GetBotPowerBonus()
	{
		return ResearchCatalog
			.GetBotPowerBonus(
				this
			);
	}


	public double GetBotPowerMultiplier()
	{
		return ResearchCatalog
			.GetBotPowerMultiplier(
				this
			);
	}


	public double GetRareBotChanceBonus()
	{
		return ResearchCatalog
			.GetRareBotChanceBonus(
				this
			);
	}


	public double GetEpicBotChanceBonus()
	{
		return ResearchCatalog
			.GetEpicBotChanceBonus(
				this
			);
	}


	public double GetLegendaryBotChanceBonus()
	{
		return ResearchCatalog
			.GetLegendaryBotChanceBonus(
				this
			);
	}


	// ==================================================
	// COSTS
	// ==================================================

	public double GetMachineUpgradeCostReduction()
	{
		return ResearchCatalog
			.GetMachineUpgradeCostReduction(
				this
			);
	}


	public double GetMachineUpgradeCostMultiplier()
	{
		return ResearchCatalog
			.GetMachineUpgradeCostMultiplier(
				this
			);
	}


	public double GetUnlockCostReduction()
	{
		return ResearchCatalog
			.GetUnlockCostReduction(
				this
			);
	}


	public double GetUnlockCostMultiplier()
	{
		return ResearchCatalog
			.GetUnlockCostMultiplier(
				this
			);
	}


	// ==================================================
	// OFFLINE
	// ==================================================

	public double GetOfflineIncomeBonus()
	{
		return ResearchCatalog
			.GetOfflineIncomeBonus(
				this
			);
	}
}
