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
	public NormalRunResetService RunReset { get; }
	public LabService Lab { get; }
	public OfflineProgressService Offline { get; }
	public GameSaveService Save { get; }
	public ShopService Shop { get; }
	public PipelineService Pipeline { get; }
	public InfrastructureService Infrastructure { get; }
	public QuantumService Quantum { get; }
	public BotSkinService BotSkins { get; }
	public MachineSkinService MachineSkins { get; }
	public LevelRewardService LevelRewards { get; }
	public GameSettingsService Settings { get; }
    public SettingsPersistenceService SettingsPersistence { get; }
    public AudioService Audio { get; }
    public HapticsService Haptics { get; }

	internal GameServices(
		GameState state,
		EconomyService economy,
		ProgressionService progression,
		ProductionService production,
		BotService bots,
		PrestigeService prestige,
		NormalRunResetService runReset,
		LabService lab,
		OfflineProgressService offline,
		GameSaveService save,
		ShopService shop,
		PipelineService pipeline,
		InfrastructureService infrastructure,
		QuantumService quantum,
		BotSkinService botSkins,
		MachineSkinService machineSkins,
		LevelRewardService levelRewards,
		GameSettingsService settings, SettingsPersistenceService settingsPersistence, AudioService audio, HapticsService haptics)
	{
		State = state;
		Economy = economy;
		Progression = progression;
		Production = production;
		Bots = bots;
		Prestige = prestige;
		RunReset = runReset;
		Lab = lab;
		Offline = offline;
		Save = save;
		Shop = shop;
		Pipeline = pipeline;
		Infrastructure = infrastructure;
		Quantum = quantum;
		BotSkins = botSkins;
		MachineSkins = machineSkins;
		LevelRewards = levelRewards;
		Settings = settings;
        SettingsPersistence = settingsPersistence;
        Audio = audio;
        Haptics = haptics;
	}
}
