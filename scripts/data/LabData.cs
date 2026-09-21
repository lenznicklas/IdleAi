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
	// STATE
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
	// BOTS
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
