using Godot;
using System;

namespace IdleAi;

public partial class MachineSlot
{
	public int DurabilitySlotIndex => _slotIndex;

	// Full room refresh, cycle refresh and durability refresh all use this decision.
	// Bot ownership alone does not imply automation: broken bots remain repairable.
	public void ApplyBotDurabilityVisual(SlotData slot)
	{
		if (!slot.Unlocked)
			return;

		bool automated = slot.HasWorkingBot;
		string time = Math.Max(0.0, slot.CycleRemaining).ToString("F1");
		string status;
		if (slot.BotBroken)
			status = slot.IsRunning ? $"BOT BROKEN • MANUAL {time}s" : "BOT BROKEN • TAP START";
		else if (automated)
			status = slot.IsRunning ? $"AUTO • {time}s" : "AUTO";
		else
			status = slot.IsRunning ? $"{time}s" : "READY";
		if (_statusLabel.Text != status)
			_statusLabel.Text = status;

		if (slot.BotBroken)
			_statusLabel.AddThemeColorOverride("font_color", new Color(1.0f, 0.34f, 0.28f));
		else
			_statusLabel.RemoveThemeColorOverride("font_color");

		_startButton.Disabled = automated;
		_startButton.Visible = !automated;
		_botVisual.Visible = automated;
		if (automated)
		{
			_startButtonTween?.Kill();
			_startButton.Scale = Vector2.One;
			_startButton.Modulate = Colors.White;
			_botVisual.Modulate = Colors.White;
			StartBotAnimation();
		}
		else
		{
			StopBotAnimation();
		}
	}
}
