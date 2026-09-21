namespace IdleAi;

public enum ResearchBranch
{
	Hardware,
	Robotics,
	Infrastructure
}


public sealed record ResearchDefinition(
	string Id,
	string Name,
	string Description,
	ResearchBranch Branch,
	double Cost,
	double ProductionBonus = 0.0,
	double CycleTimeReduction = 0.0,
	double BotPowerBonus = 0.0,
	double OfflineIncomeBonus = 0.0,
	string? PrerequisiteId = null
);
