namespace IdleAi;

/// <summary>Composes one game session in the original startup order.</summary>
public static class GameServiceFactory
{
	// Call during Game._Ready(): settings construction uses the Godot scene tree.
	public static GameServices Create(Game root)
	{
		GameState state = new(RoomCatalog.Create());
		EconomyService economy = new(state);
		PipelineService pipeline = new(state, economy);
		InfrastructureService infrastructure = new(state, economy);
		QuantumService quantum = new(state, economy);
		ProgressionService progression = new(state, economy);
		ProductionService production = new(state, economy, pipeline);
		BotService bots = new(state, economy);
		PrestigeService prestige = new(state, economy);
		LabService lab = new(state, economy);
		ShopService shop = new(state, economy, production);

		// Skin constructors load cosmetics before room views or the main save.
		// Settings then loads preferences and starts music at the original point.
		BotSkinService botSkins = new(state, economy);
		MachineSkinService machineSkins = new(state, economy);
		LevelRewardService levelRewards = new(state, progression, economy);
		GameSettingsService settings = new(root);

		return new GameServices(
			state, economy, progression, production, bots, prestige, lab,
			shop, pipeline, infrastructure, quantum, botSkins, machineSkins,
			levelRewards, settings);
	}
}
