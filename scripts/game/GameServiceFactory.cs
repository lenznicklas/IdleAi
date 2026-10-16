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
		NormalRunResetService runReset = new(state, economy);
		PrestigeService prestige = new(state, runReset);
		LabService lab = new(state, economy);
		OfflineProgressService offline = new(state, economy, lab);
		ShopService shop = new(state, economy, production);
		GameSaveService save = new(state, economy);

		// Skin constructors load cosmetics before room views or the main save.
		// Settings then loads preferences and starts music at the original point.
		BotSkinService botSkins = new(state, economy);
		MachineSkinService machineSkins = new(state, economy);
		SettingsPersistenceService settingsPersistence = new(
            Godot.ProjectSettings.GlobalizePath(SettingsPersistenceService.SettingsSavePath),
            Godot.ProjectSettings.GlobalizePath(SettingsPersistenceService.TemporarySettingsSavePath),
            message => Godot.GD.PushWarning(message));
        AudioService audio = AudioService.ForRoot(root);
        HapticsService haptics = Input.CreateHaptics();
        Input.UseHaptics(haptics);
        GameSettingsService settings = new(settingsPersistence, audio.SetSoundEnabled, haptics);

		return new GameServices(
			state, economy, progression, production, bots, prestige, runReset, lab, offline, save,
			shop, pipeline, infrastructure, quantum, botSkins, machineSkins,
			settings, settingsPersistence, audio, haptics);
	}
}
