using System.Collections.Generic;

namespace IdleAi;

public sealed class LabData
{
	public bool Unlocked { get; set; }


	public double ResearchPoints { get; set; }


	private readonly HashSet<string> _completedResearch =
		[];


	public IReadOnlyCollection<string> CompletedResearch =>
		_completedResearch;


	// ==================================================
	// RESEARCH STATE
	// ==================================================

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
	// RESEARCH EFFECTS
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


	public double GetBotPowerBonus()
	{
		return ResearchCatalog
			.GetBotPowerBonus(
				this
			);
	}


	public double GetOfflineIncomeBonus()
	{
		return ResearchCatalog
			.GetOfflineIncomeBonus(
				this
			);
	}
}
