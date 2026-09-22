using System;

namespace IdleAi;

public sealed class PrestigeData
{
	public long AiCores { get; set; }

	public int PrestigeCount { get; set; }


	public double GetProductionMultiplier()
	{
		return GetProductionMultiplierForCores(
			AiCores
		);
	}


	public static double GetProductionMultiplierForCores(
		long cores)
	{
		if (cores <= 0)
			return 1.0;


		double progress =
			1.0
			- Math.Exp(
				-cores
				/ GameConfig.PrestigeCoreSoftcap
			);


		return 1.0
			+ progress
			* (
				GameConfig.PrestigeMaximumProductionMultiplier
				- 1.0
			);
	}


	public double GetProductionBonusPercent()
	{
		return (
			GetProductionMultiplier()
			- 1.0
		)
		* 100.0;
	}
}
