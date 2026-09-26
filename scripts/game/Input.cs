namespace IdleAi;


/*
 * Central vibration gate.
 *
 * All existing game files are inside namespace IdleAi and currently use only
 * Input.VibrateHandheld(...). Because this class lives in the same namespace,
 * those calls resolve here instead of directly to Godot.Input.
 *
 * Result: one setting disables vibration across the entire game without
 * touching every individual controller.
 */
public static class Input
{
	public static void VibrateHandheld(
		int durationMs = 500,
		float amplitude = -1.0f)
	{
		if (
			!GameSettingsService
				.IsVibrationEnabled
		)
		{
			return;
		}

		Godot.Input.VibrateHandheld(
			durationMs,
			amplitude
		);
	}
}
