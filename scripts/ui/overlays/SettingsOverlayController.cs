using Godot;
using System;

namespace IdleAi;


public sealed class SettingsOverlayController
{
	private readonly Game _root;

	private readonly GameSettingsService _settings;


	private Control _overlay =
		null!;

	private PanelContainer _panel =
		null!;

	private CheckButton _vibrationToggle =
		null!;

	private CheckButton _soundToggle =
		null!;

	private Label _vibrationState =
		null!;

	private Label _soundState =
		null!;


	public bool Visible =>
		_overlay != null
		&& _overlay.Visible;


	public SettingsOverlayController(
		Game root,
		GameSettingsService settings)
	{
		_root =
			root;

		_settings =
			settings;
	}


	public void Initialize()
	{
		CreateUi();

		Refresh();

		Hide();
	}


	public void Open()
	{
		Refresh();

		_overlay.Show();

		_overlay.MoveToFront();
	}


	public void Hide()
	{
		_overlay?.Hide();
	}


	public void Refresh()
	{
		if (
			_vibrationToggle == null
			|| _soundToggle == null
		)
		{
			return;
		}

		_vibrationToggle.SetPressedNoSignal(
			_settings.VibrationEnabled
		);

		_soundToggle.SetPressedNoSignal(
			_settings.SoundEnabled
		);

		_vibrationState.Text =
			_settings.VibrationEnabled
				? "ON"
				: "OFF";

		_vibrationState.AddThemeColorOverride(
			"font_color",
			_settings.VibrationEnabled
				? ShopUi.Green
				: ShopUi.TextSecondary
		);

		_soundState.Text =
			_settings.SoundEnabled
				? "ON"
				: "OFF";

		_soundState.AddThemeColorOverride(
			"font_color",
			_settings.SoundEnabled
				? ShopUi.Green
				: ShopUi.TextSecondary
		);
	}


	// ==================================================
	// UI
	// ==================================================

	private void CreateUi()
	{
		_overlay =
			new Control
			{
				Name =
					"SettingsOverlay",

				MouseFilter =
					Control.MouseFilterEnum.Stop,

				ZIndex =
					1200
			};

		_overlay.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);

		_root.AddChild(
			_overlay
		);

		ColorRect dim =
			new()
			{
				Color =
					new Color(
						0,
						0,
						0,
						0.80f
					),

				MouseFilter =
					Control.MouseFilterEnum.Stop
			};

		dim.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);

		dim.GuiInput +=
			OnDimInput;

		_overlay.AddChild(
			dim
		);

		CenterContainer center =
			new()
			{
				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};

		center.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);

		center.OffsetLeft =
			24;

		center.OffsetTop =
			24;

		center.OffsetRight =
			-24;

		center.OffsetBottom =
			-24;

		_overlay.AddChild(
			center
		);

		_panel =
			new PanelContainer
			{
				CustomMinimumSize =
					new Vector2(
						580,
						520
					),

				MouseFilter =
					Control.MouseFilterEnum.Stop
			};

		_panel.AddThemeStyleboxOverride(
			"panel",
			CreatePanelStyle()
		);

		center.AddChild(
			_panel
		);

		MarginContainer margin =
			new();

		margin.AddThemeConstantOverride(
			"margin_left",
			30
		);

		margin.AddThemeConstantOverride(
			"margin_top",
			34
		);

		margin.AddThemeConstantOverride(
			"margin_right",
			30
		);

		margin.AddThemeConstantOverride(
			"margin_bottom",
			28
		);

		_panel.AddChild(
			margin
		);

		VBoxContainer box =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,

				Alignment =
					BoxContainer.AlignmentMode.Center
			};

		box.AddThemeConstantOverride(
			"separation",
			18
		);

		margin.AddChild(
			box
		);

		Label title =
			ShopUi.CreateLabel(
				30
			);

		title.Text =
			"SETTINGS";

		title.AddThemeColorOverride(
			"font_color",
			ShopUi.Accent
		);

		box.AddChild(
			title
		);

		Label subtitle =
			ShopUi.CreateMutedLabel(
				13
			);

		subtitle.Text =
			"Audio and haptic feedback";

		box.AddChild(
			subtitle
		);

		box.AddChild(
			new HSeparator()
		);

		(
			_vibrationToggle,
			_vibrationState
		) =
			CreateSettingRow(
				box,
				"VIBRATION",
				"Vibrate on buttons, rewards and actions.",
				_settings.VibrationEnabled
			);

		_vibrationToggle.Toggled +=
			OnVibrationToggled;

		(
			_soundToggle,
			_soundState
		) =
			CreateSettingRow(
				box,
				"SOUND",
				"Background music and game audio.",
				_settings.SoundEnabled
			);

		_soundToggle.Toggled +=
			OnSoundToggled;

		box.AddChild(
			new HSeparator()
		);

		Label musicInfo =
			ShopUi.CreateMutedLabel(
				12
			);

		musicInfo.Text =
			"BACKGROUND MUSIC  •  SCORE";

		box.AddChild(
			musicInfo
		);

		/*
		 * IMPORTANT:
		 * Create X last so its transparent close layer remains above all
		 * panel content and always receives the click/touch.
		 */
		TextureButton closeButton =
			OverlayCloseButton.Add(
				_panel,
				Hide
			);

		if (
			closeButton.GetParent()
				is Control closeLayer
		)
		{
			closeLayer.MoveToFront();
		}

		closeButton.MoveToFront();
	}


	private static (
		CheckButton Toggle,
		Label State
	) CreateSettingRow(
		VBoxContainer parent,
		string title,
		string description,
		bool enabled)
	{
		PanelContainer panel =
			new()
			{
				CustomMinimumSize =
					new Vector2(
						0,
						112
					),

				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};

		StyleBoxFlat style =
			ShopUi.CreatePanelStyle();

		style.BorderColor =
			new Color(
				ShopUi.Purple.R,
				ShopUi.Purple.G,
				ShopUi.Purple.B,
				0.56f
			);

		panel.AddThemeStyleboxOverride(
			"panel",
			style
		);

		parent.AddChild(
			panel
		);

		MarginContainer margin =
			new();

		margin.AddThemeConstantOverride(
			"margin_left",
			18
		);

		margin.AddThemeConstantOverride(
			"margin_top",
			12
		);

		margin.AddThemeConstantOverride(
			"margin_right",
			18
		);

		margin.AddThemeConstantOverride(
			"margin_bottom",
			12
		);

		panel.AddChild(
			margin
		);

		HBoxContainer row =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};

		row.AddThemeConstantOverride(
			"separation",
			16
		);

		margin.AddChild(
			row
		);

		VBoxContainer text =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};

		text.AddThemeConstantOverride(
			"separation",
			4
		);

		row.AddChild(
			text
		);

		Label titleLabel =
			ShopUi.CreateLabel(
				19
			);

		titleLabel.Text =
			title;

		titleLabel.HorizontalAlignment =
			HorizontalAlignment.Left;

		text.AddChild(
			titleLabel
		);

		Label descriptionLabel =
			ShopUi.CreateMutedLabel(
				12
			);

		descriptionLabel.Text =
			description;

		descriptionLabel.HorizontalAlignment =
			HorizontalAlignment.Left;

		text.AddChild(
			descriptionLabel
		);

		Label state =
			ShopUi.CreateLabel(
				14
			);

		state.CustomMinimumSize =
			new Vector2(
				54,
				0
			);

		state.HorizontalAlignment =
			HorizontalAlignment.Right;

		state.VerticalAlignment =
			VerticalAlignment.Center;

		row.AddChild(
			state
		);

		CheckButton toggle =
			new()
			{
				ButtonPressed =
					enabled,

				CustomMinimumSize =
					new Vector2(
						84,
						54
					),

				FocusMode =
					Control.FocusModeEnum.None,

				TooltipText =
					title
			};

		row.AddChild(
			toggle
		);

		return (
			toggle,
			state
		);
	}


	// ==================================================
	// EVENTS
	// ==================================================

	private void OnVibrationToggled(
		bool enabled)
	{
		_settings.SetVibrationEnabled(
			enabled
		);

		Refresh();

		/*
		 * When enabling vibration, confirm the setting with one short pulse.
		 * When disabling it, the central Input wrapper already suppresses it.
		 */
		if (enabled)
		{
			Input.VibrateHandheld(
				16,
				0.12f
			);
		}
	}


	private void OnSoundToggled(
		bool enabled)
	{
		_settings.SetSoundEnabled(
			enabled
		);

		Refresh();
	}


	private void OnDimInput(
		InputEvent @event)
	{
		bool released =
			@event
				is InputEventScreenTouch touch
			&& !touch.Pressed;

		released |=
			@event
				is InputEventMouseButton mouse
			&& !mouse.Pressed
			&& mouse.ButtonIndex
				== MouseButton.Left;

		if (!released)
			return;

		_overlay
			.GetViewport()
			.SetInputAsHandled();

		Callable
			.From(
				Hide
			)
			.CallDeferred();
	}


	private static StyleBoxFlat CreatePanelStyle()
	{
		return new StyleBoxFlat
		{
			BgColor =
				new Color(
					0.018f,
					0.030f,
					0.055f,
					0.99f
				),

			BorderColor =
				new Color(
					0.18f,
					0.65f,
					1.0f,
					0.88f
				),

			BorderWidthLeft = 2,
			BorderWidthTop = 2,
			BorderWidthRight = 2,
			BorderWidthBottom = 2,

			CornerRadiusTopLeft = 20,
			CornerRadiusTopRight = 20,
			CornerRadiusBottomLeft = 20,
			CornerRadiusBottomRight = 20,

			ShadowColor =
				new Color(
					0,
					0,
					0,
					0.55f
				),

			ShadowSize =
				18
		};
	}
}
