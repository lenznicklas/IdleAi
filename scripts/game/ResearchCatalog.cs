using System.Collections.Generic;

namespace IdleAi;

public static class ResearchCatalog
{
	public const string EfficientHardware1Id =
		"efficient_hardware_1";


	private static readonly List<ResearchDefinition> Research =
	[
		new ResearchDefinition(
			EfficientHardware1Id,
			"Efficient Hardware I",
			"+5% production for all machines.",
			ResearchBranch.Hardware,
			25.0,
			0.05
		)
	];


	public static IReadOnlyList<ResearchDefinition> All =>
		Research;


	public static ResearchDefinition? Get(
		string id)
	{
		foreach (
			ResearchDefinition research
			in Research
		)
		{
			if (research.Id == id)
				return research;
		}


		return null;
	}


	public static double GetProductionMultiplier(
		LabData lab)
	{
		double multiplier =
			1.0;


		foreach (
			ResearchDefinition research
			in Research
		)
		{
			if (
				!lab.IsResearchCompleted(
					research.Id
				)
			)
			{
				continue;
			}


			multiplier +=
				research.ProductionBonus;
		}


		return multiplier;
	}
}
