using Godot;
using System;
using System.Collections.Generic;

namespace IdleAi;

public sealed class StatsOverlayController
{
	private const float OpenStartScale = 0.94f;
	private const float OpenOvershootScale = 1.015f;
	private const double OpenGrowDuration = 0.22;
	private const double OpenSettleDuration = 0.14;
	private const float OpenStartAlpha = 0.82f;

	private readonly Game _root;
	private readonly GameState _state;
	private readonly EconomyService _economy;
	private readonly ProgressionService _progression;
	private readonly PrestigeService _prestige;

	private Control _overlay = null!;
	private PanelContainer _panel = null!;
	private ScrollContainer _scroll = null!;
	private MobileScrollController _mobileScroll = null!;

	private Label _income = null!;
	private Label _earned = null!;
	private Label _spent = null!;
	private Label _slots = null!;
	private Label _level = null!;
	private Label _unlockSpend = null!;
	private Label _machineSpend = null!;
	private Label _prestigeCount = null!;
	private Label _prestigeBoost = null!;
	private Label _prestigeProgress = null!;

	private Button _prestigeButton = null!;
	private Button _oldCloseButton = null!;
	private Tween? _openTween;

	public event Action? PrestigeRequested;

	public bool Visible => _overlay.Visible;

	public StatsOverlayController(
		Game root,
		GameState state,
		EconomyService economy,
		ProgressionService progression,
		PrestigeService prestige)
	{
		_root = root;
		_state = state;
		_economy = economy;
		_progression = progression;
		_prestige = prestige;
	}

	public void Initialize()
	{
		_overlay =
			_root.GetNode<Control>("StatsOverlay");

		_panel =
			_root.GetNode<PanelContainer>(
				"StatsOverlay/StatsPanel"
			);

		_scroll =
			_root.GetNode<ScrollContainer>(
				"StatsOverlay/StatsPanel/Margin/Scroll"
			);

		const string path =
			"StatsOverlay/StatsPanel/Margin/Scroll/VBox/";

		_income = _root.GetNode<Label>(path + "IncomeLabel");
		_earned = _root.GetNode<Label>(path + "EarnedLabel");
		_spent = _root.GetNode<Label>(path + "SpentLabel");
		_slots = _root.GetNode<Label>(path + "SlotsLabel");
		_level = _root.GetNode<Label>(path + "LevelLabel");
		_unlockSpend = _root.GetNode<Label>(path + "UnlockSpendLabel");
		_machineSpend = _root.GetNode<Label>(path + "MachineSpendLabel");
		_prestigeCount = _root.GetNode<Label>(path + "AiCoresLabel");
		_prestigeBoost = _root.GetNode<Label>(path + "PrestigeBoostLabel");
		_prestigeProgress = _root.GetNode<Label>(path + "PrestigeProgressLabel");
		_prestigeButton = _root.GetNode<Button>(path + "PrestigeButton");
		_oldCloseButton = _root.GetNode<Button>(path + "CloseButton");

		ApplyVisualStyle(path);

		_oldCloseButton.Hide();
		_oldCloseButton.MouseFilter = Control.MouseFilterEnum.Ignore;

		OverlayCloseButton.Add(
			_panel,
			Hide
		);

		_prestigeButton.Pressed +=
			() => PrestigeRequested?.Invoke();

		ConfigureOutsideClose();
		CreateMobileScrolling();
		Hide();
	}

	private void ApplyVisualStyle(string path)
	{
		_panel.AddThemeStyleboxOverride(
			"panel",
			CreateStatsPanelStyle()
		);

		Label? title =
			_root.GetNodeOrNull<Label>(path + "Title");
		Label? spendingTitle =
			_root.GetNodeOrNull<Label>(path + "SpendTitle");
		Label? prestigeTitle =
			_root.GetNodeOrNull<Label>(path + "PrestigeTitle");

		if (title != null)
		{
			title.AddThemeColorOverride(
				"font_color",
				new Color(0.44f, 0.82f, 1.0f, 1.0f)
			);
			title.AddThemeFontSizeOverride("font_size", 30);
		}

		if (spendingTitle != null)
		{
			spendingTitle.AddThemeColorOverride(
				"font_color",
				new Color(0.64f, 0.80f, 0.94f, 1.0f)
			);
		}

		if (prestigeTitle != null)
		{
			prestigeTitle.AddThemeColorOverride(
				"font_color",
				new Color(1.0f, 0.78f, 0.24f, 1.0f)
			);
			prestigeTitle.AddThemeFontSizeOverride("font_size", 24);
		}

		DecorateStatLabel(
			_income,
			new Color(0.32f, 0.82f, 1.0f, 1.0f),
			17
		);
		DecorateStatLabel(
			_earned,
			new Color(0.36f, 0.92f, 0.62f, 1.0f),
			16
		);
		DecorateStatLabel(
			_spent,
			new Color(0.93f, 0.76f, 0.42f, 1.0f),
			16
		);
		DecorateStatLabel(
			_slots,
			new Color(0.72f, 0.82f, 0.94f, 1.0f),
			16
		);
		DecorateStatLabel(
			_level,
			new Color(0.76f, 0.60f, 1.0f, 1.0f),
			17
		);
		DecorateStatLabel(
			_unlockSpend,
			new Color(0.68f, 0.78f, 0.90f, 1.0f),
			15
		);
		DecorateStatLabel(
			_machineSpend,
			new Color(0.70f, 0.78f, 0.88f, 1.0f),
			14
		);

		_prestigeCount.AddThemeColorOverride(
			"font_color",
			new Color(1.0f, 0.84f, 0.42f, 1.0f)
		);
		_prestigeCount.AddThemeFontSizeOverride("font_size", 18);

		_prestigeBoost.AddThemeColorOverride(
			"font_color",
			new Color(0.46f, 0.94f, 0.68f, 1.0f)
		);
		_prestigeBoost.AddThemeFontSizeOverride("font_size", 16);

		_prestigeProgress.AddThemeColorOverride(
			"font_color",
			new Color(0.72f, 0.82f, 0.92f, 1.0f)
		);
		_prestigeProgress.AddThemeFontSizeOverride("font_size", 15);
		_prestigeProgress.CustomMinimumSize = new Vector2(0, 56);

		ApplyPrestigeButtonStyle(_prestigeButton);
	}

	private static void DecorateStatLabel(
		Label label,
		Color color,
		int fontSize)
	{
		label.AddThemeColorOverride("font_color", color);
		label.AddThemeFontSizeOverride("font_size", fontSize);
		label.CustomMinimumSize =
			new Vector2(
				label.CustomMinimumSize.X,
				MathF.Max(label.CustomMinimumSize.Y, 34.0f)
			);
	}

	private static StyleBoxFlat CreateStatsPanelStyle()
	{
		return new StyleBoxFlat
		{
			BgColor = new Color(0.012f, 0.026f, 0.052f, 0.995f),
			BorderColor = new Color(0.26f, 0.72f, 1.0f, 0.88f),
			BorderWidthLeft = 2,
			BorderWidthTop = 2,
			BorderWidthRight = 2,
			BorderWidthBottom = 2,
			CornerRadiusTopLeft = 22,
			CornerRadiusTopRight = 22,
			CornerRadiusBottomLeft = 22,
			CornerRadiusBottomRight = 22,
			ShadowColor = new Color(0.0f, 0.24f, 0.54f, 0.22f),
			ShadowSize = 14
		};
	}

	private static void ApplyPrestigeButtonStyle(Button button)
	{
		button.FocusMode = Control.FocusModeEnum.None;
		button.AddThemeFontSizeOverride("font_size", 17);
		button.AddThemeColorOverride("font_color", Colors.White);
		button.AddThemeColorOverride("font_hover_color", Colors.White);
		button.AddThemeColorOverride("font_pressed_color", Colors.White);
		button.AddThemeColorOverride(
			"font_disabled_color",
			new Color(0.48f, 0.46f, 0.40f, 1.0f)
		);

		button.AddThemeStyleboxOverride(
			"normal",
			CreatePrestigeButtonBox(
				new Color(0.34f, 0.20f, 0.035f, 1.0f),
				new Color(1.0f, 0.72f, 0.20f, 1.0f)
			)
		);
		button.AddThemeStyleboxOverride(
			"hover",
			CreatePrestigeButtonBox(
				new Color(0.48f, 0.29f, 0.045f, 1.0f),
				new Color(1.0f, 0.84f, 0.36f, 1.0f)
			)
		);
		button.AddThemeStyleboxOverride(
			"pressed",
			CreatePrestigeButtonBox(
				new Color(0.25f, 0.14f, 0.025f, 1.0f),
				new Color(0.90f, 0.60f, 0.14f, 1.0f)
			)
		);
		button.AddThemeStyleboxOverride(
			"disabled",
			CreatePrestigeButtonBox(
				new Color(0.055f, 0.060f, 0.072f, 0.96f),
				new Color(0.26f, 0.25f, 0.22f, 0.78f)
			)
		);
	}

	private static StyleBoxFlat CreatePrestigeButtonBox(
		Color background,
		Color border)
	{
		return new StyleBoxFlat
		{
			BgColor = background,
			BorderColor = border,
			BorderWidthLeft = 2,
			BorderWidthTop = 2,
			BorderWidthRight = 2,
			BorderWidthBottom = 2,
			CornerRadiusTopLeft = 14,
			CornerRadiusTopRight = 14,
			CornerRadiusBottomLeft = 14,
			CornerRadiusBottomRight = 14,
			ShadowColor = new Color(0.76f, 0.45f, 0.06f, 0.20f),
			ShadowSize = 7
		};
	}

	private void CreateMobileScrolling()
	{
		_mobileScroll =
			new MobileScrollController
			{
				Name = "StatsMobileScroll"
			};

		_overlay.AddChild(_mobileScroll);
		_mobileScroll.Setup(_scroll);
	}

	private void ConfigureOutsideClose()
	{
		ColorRect dim =
			_root.GetNode<ColorRect>("StatsOverlay/Dim");

		dim.GuiInput +=
			@event =>
			{
				bool released =
					@event is InputEventScreenTouch touch
					&& !touch.Pressed;

				released |=
					@event is InputEventMouseButton mouse
					&& !mouse.Pressed
					&& mouse.ButtonIndex == MouseButton.Left;

				if (!released)
					return;

				dim.GetViewport().SetInputAsHandled();
				Callable.From(Hide).CallDeferred();
			};
	}

	public void Open()
	{
		Refresh();
		_overlay.Show();
		_overlay.MoveToFront();
		PlayOpenAnimation();
	}

	public void Hide()
	{
		_openTween?.Kill();
		_openTween = null;
		_mobileScroll?.ResetMotion();
		ResetPanelTransform();
		_overlay.Hide();
	}

	private void PlayOpenAnimation()
	{
		_openTween?.Kill();

		_panel.PivotOffset = _panel.Size / 2.0f;
		_panel.Scale = new Vector2(OpenStartScale, OpenStartScale);
		_panel.Modulate = new Color(1.0f, 1.0f, 1.0f, OpenStartAlpha);

		_openTween = _root.CreateTween();
		_openTween.SetParallel(true);

		_openTween.TweenProperty(
			_panel,
			"scale",
			new Vector2(OpenOvershootScale, OpenOvershootScale),
			OpenGrowDuration
		)
		.SetTrans(Tween.TransitionType.Cubic)
		.SetEase(Tween.EaseType.Out);

		_openTween.TweenProperty(
			_panel,
			"modulate:a",
			1.0f,
			OpenGrowDuration
		)
		.SetTrans(Tween.TransitionType.Sine)
		.SetEase(Tween.EaseType.Out);

		_openTween
			.Chain()
			.TweenProperty(
				_panel,
				"scale",
				Vector2.One,
				OpenSettleDuration
			)
			.SetTrans(Tween.TransitionType.Sine)
			.SetEase(Tween.EaseType.Out);
	}

	private void ResetPanelTransform()
	{
		if (
			_panel == null
			|| !GodotObject.IsInstanceValid(_panel)
		)
		{
			return;
		}

		_panel.Scale = Vector2.One;
		_panel.Modulate = Colors.White;
	}

	public void Refresh()
	{
		_income.Text =
			"Automated Tokens / sec: "
			+ NumberFormatter.Format(_economy.GetTotalIncome());

		_earned.Text =
			"Total earned: "
			+ NumberFormatter.Format(_state.Stats.TotalEarned)
			+ "\nOffline earned: "
			+ NumberFormatter.Format(_state.Stats.OfflineEarned);

		_spent.Text =
			"Total spent: "
			+ NumberFormatter.Format(_state.Stats.TotalSpent);

		_slots.Text =
			"Unlocked slots: "
			+ _progression.GetUnlockedSlotCount(_state.CurrentRoomIndex)
			+ " / "
			+ _state.CurrentRoomState.Slots.Count;

		_level.Text =
			"Total level: "
			+ _progression.GetTotalLevel();

		_unlockSpend.Text =
			"Slot unlocks: "
			+ NumberFormatter.Format(_state.Stats.SlotUnlockSpent);

		RefreshMachineSpending();
		RefreshPrestige();
	}

	private void RefreshMachineSpending()
	{
		List<string> lines = [];

		foreach (RoomData room in _state.Rooms)
		{
			lines.Add(room.Name.ToUpperInvariant());

			foreach (MachineData machine in room.Machines)
			{
				lines.Add(
					machine.MachineName
					+ ": "
					+ NumberFormatter.Format(
						_state.Stats.GetMachineSpending(
							machine.MachineName
						)
					)
				);
			}

			lines.Add("");
		}

		lines.Add(
			"BOTS: "
			+ NumberFormatter.Format(
				_state.Stats.GetMachineSpending("Bots")
			)
		);

		_machineSpend.Text = string.Join("\n", lines);
	}

	public void RefreshRuntime()
	{
		RefreshPrestige();
	}

	private void RefreshPrestige()
	{
		bool canPrestige = _prestige.CanPrestige();

		_prestigeCount.Text =
			"Prestiges: "
			+ _state.Prestige.PrestigeCount;

		_prestigeBoost.Text =
			"Permanent production: x"
			+ _prestige
				.GetProductionMultiplier()
				.ToString("F2");

		if (canPrestige)
		{
			double nextMultiplier =
				_prestige.GetProductionMultiplierAfterNextPrestige();

			_prestigeProgress.Text =
				"Current run: "
				+ NumberFormatter.Format(_state.RunEarnedTokens)
				+ "\nAfter prestige: x"
				+ nextMultiplier.ToString("F2")
				+ " permanent production";

			_prestigeButton.Text = "PRESTIGE";
			_prestigeButton.Disabled = false;
			return;
		}

		double remaining =
			Math.Max(
				0,
				_prestige.GetRequiredRunTokens()
				- _state.RunEarnedTokens
			);

		_prestigeProgress.Text =
			"Prestige available in: "
			+ NumberFormatter.Format(remaining)
			+ " Tokens";

		_prestigeButton.Text = "PRESTIGE";
		_prestigeButton.Disabled = true;
	}
}
