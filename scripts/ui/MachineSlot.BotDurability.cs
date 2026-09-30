using Godot;
using System;

namespace IdleAi;


/*
 * Visual companion for Bot durability.
 *
 * The original MachineSlot remains untouched. When a Bot breaks we expose the
 * manual Start button again, stop the Bot animation, and clearly mark the slot
 * as broken instead of leaving the misleading "AUTO" state visible.
 */
public partial class MachineSlot
{
	public int DurabilitySlotIndex =>
		_slotIndex;


	public void ApplyBotDurabilityVisual(
		SlotData slot)
	{
		if (!slot.HasBot)
		{
			_statusLabel?.RemoveThemeColorOverride(
				"font_color"
			);

			return;
		}

		if (!slot.BotBroken)
		{
			if (_botVisual != null)
			{
				_botVisual.Modulate =
					Colors.White;
			}

			_statusLabel?.RemoveThemeColorOverride(
				"font_color"
			);

			return;
		}

		StopBotAnimation();

		/*
		 * The broken Bot still exists and is repairable from the details
		 * overlay, but the slot exposes manual operation again.
		 */
		_botVisual.Hide();

		_startButton.Show();

		_startButton.Disabled =
			false;

		_statusLabel.AddThemeColorOverride(
			"font_color",
			new Color(
				1.0f,
				0.34f,
				0.28f,
				1.0f
			)
		);

		if (slot.IsRunning)
		{
			double cycleDuration =
				slot.RuntimeCycleDuration > 0.0
					? slot.RuntimeCycleDuration
					: GameConfig
						.GetProductionCycleSeconds(
							slot.MachineTier
						);

			string time =
				Math.Max(
					0.0,
					slot.CycleRemaining
				)
				.ToString(
					"F1"
				);

			_statusLabel.Text =
				"BOT BROKEN • MANUAL "
				+ time
				+ "s";

			_progressBar.Value =
				Math.Clamp(
					(
						cycleDuration
						- slot.CycleRemaining
					)
					/ Math.Max(
						0.001,
						cycleDuration
					)
					* 100.0,
					0.0,
					100.0
				);
		}
		else
		{
			_statusLabel.Text =
				"BOT BROKEN • TAP START";

			_progressBar.Value =
				100.0;
		}
	}
}
