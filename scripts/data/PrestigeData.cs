using System;

namespace IdleAi;

public sealed class PrestigeData
{
	/*
	 * LEGACY SAVE COMPATIBILITY ONLY
	 *
	 * Older Idle AI saves contain AiCores.
	 * Keep the property so old save files can still be loaded without
	 * migration problems, but it is no longer displayed, awarded or used
	 * for the prestige production multiplier.
	 */
	public long AiCores { get; set; }


	public int PrestigeCount { get; set; }


	private const double PrestigeCountSoftcap =
		20.0;


	public double GetProductionMultiplier()
	{
		return GetProductionMultiplierForPrestigeCount(
			PrestigeCount
		);
	}


	public static double GetProductionMultiplierForPrestigeCount(
		int prestigeCount)
	{
		if (prestigeCount <= 0)
			return 1.0;


		double progress =
			1.0
			- Math.Exp(
				-prestigeCount
					/ PrestigeCountSoftcap
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
