namespace IdleAi;

/// <summary>Owns the shared state and service instances for one game session.</summary>
public sealed class GameServices
{
	public GameState State { get; }
	public EconomyService Economy { get; }
	public ProgressionService Progression { get; }
	public ProductionService Production { get; }
	public BotService Bots { get; }
	public PrestigeService Prestige { get; }
	public LabService Lab { get; }
	public OfflineProgressService Offline { get; }
	public ShopService Shop { get; }
	public PipelineService Pipeline { get; }
	public InfrastructureService Infrastructure { get; }
	public QuantumService Quantum { get; }
	public BotSkinService BotSkins { get; }
	public MachineSkinService MachineSkins { get; }
	public LevelRewardService LevelRewards { get; }
	public GameSettingsService Settings { get; }

	internal GameServices(
		GameState state,
		EconomyService economy,
		ProgressionService progression,
		ProductionService production,
		BotService bots,
		PrestigeService prestige,
		LabService lab,
		OfflineProgressService offline,
		ShopService shop,
		PipelineService pipeline,
		InfrastructureService infrastructure,
		QuantumService quantum,
		BotSkinService botSkins,
		MachineSkinService machineSkins,
		LevelRewardService levelRewards,
		GameSettingsService settings)
	{
		State = state;
		Economy = economy;
		Progression = progression;
		Production = production;
		Bots = bots;
		Prestige = prestige;
		Lab = lab;
		Offline = offline;
		Shop = shop;
		Pipeline = pipeline;
		Infrastructure = infrastructure;
		Quantum = quantum;
		BotSkins = botSkins;
		MachineSkins = machineSkins;
		LevelRewards = levelRewards;
		Settings = settings;
	}
}
