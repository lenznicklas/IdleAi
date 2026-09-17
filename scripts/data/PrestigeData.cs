namespace IdleAi;

public sealed class PrestigeData
{
	public long AiCores { get; set; }

	public int PrestigeCount { get; set; }


	public double GetProductionMultiplier()
	{
		return 1.0
			   + AiCores
			   * GameConfig.ProductionBoostPerAiCore;
	}


	public double GetProductionBonusPercent()
	{
		return AiCores
			   * GameConfig.ProductionBoostPerAiCore
			   * 100.0;
	}
}
