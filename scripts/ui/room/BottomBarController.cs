using Godot;
using System;

namespace IdleAi;

public sealed class BottomBarController
{
	private const int NavigationHapticDurationMs =
		12;


	private const float NavigationHapticStrength =
		0.12f;


	private const string SettingsIconPath =
		"res://assets/ui/settings.png";


	private readonly Game _root;


	private Control _bar =
		null!;


	private TextureRect _background =
		null!;


	private TextureButton _mapButton =
		null!;


	private TextureButton _shopButton =
		null!;


	private TextureButton _settingsButton =
		null!;


	private Label _message =
		null!;


	public event Action? MapRequested;

	public event Action? ShopRequested;

	public event Action? SettingsRequested;


	public BottomBarController(
		Game root)
	{
		_root =
			root;
	}


	// ==================================================
	// INITIALIZE
	// ==================================================

	public void Initialize()
	{
		_bar =
			_root.GetNode<Control>(
				"BottomBar"
			);


		_background =
			_root.GetNode<TextureRect>(
				"BottomBar/Background"
			);


		_mapButton =
			_root.GetNode<TextureButton>(
				"BottomBar/Margin/HBox/MapButton"
			);


		_shopButton =
			_root.GetNode<TextureButton>(
				"BottomBar/Margin/HBox/ShopButton"
			);


		_message =
			_root.GetNode<Label>(
				"BottomBar/Margin/HBox/MessageLabel"
			);


		CreateSettingsButton();


		_mapButton.Pressed +=
			OnMapPressed;


		_shopButton.Pressed +=
			OnShopPressed;


		_settingsButton.Pressed +=
			OnSettingsPressed;
	}


	private void CreateSettingsButton()
	{
		HBoxContainer row =
			_root.GetNode<HBoxContainer>(
				"BottomBar/Margin/HBox"
			);


		Texture2D? settingsTexture =
			ResourceLoader.Exists(
				SettingsIconPath
			)
				? GD.Load<Texture2D>(
					SettingsIconPath
				)
				: null;


		_settingsButton =
			new TextureButton
			{
				Name =
					"SettingsButton",

				CustomMinimumSize =
					new Vector2(
						72,
						0
					),

				SizeFlagsVertical =
					Control.SizeFlags.ExpandFill,

				TextureNormal =
					settingsTexture,

				IgnoreTextureSize =
					true,

				StretchMode =
					TextureButton.StretchModeEnum.KeepAspectCentered,

				FocusMode =
					Control.FocusModeEnum.None,

				TooltipText =
					"Settings"
			};


		row.AddChild(
			_settingsButton
		);


		/*
		 * settings.png is expected locally. Keep a readable fallback if the
		 * asset has not been copied/imported yet.
		 */
		if (settingsTexture == null)
		{
			Label fallback =
				new()
				{
					Text =
						"⚙",

					MouseFilter =
						Control.MouseFilterEnum.Ignore,

					HorizontalAlignment =
						HorizontalAlignment.Center,

					VerticalAlignment =
						VerticalAlignment.Center
				};


			fallback.AddThemeFontSizeOverride(
				"font_size",
				32
			);


			fallback.SetAnchorsAndOffsetsPreset(
				Control.LayoutPreset.FullRect
			);


			_settingsButton.AddChild(
				fallback
			);
		}
	}


	// ==================================================
	// BUTTONS
	// ==================================================

	private void OnMapPressed()
	{
		PlayNavigationHaptic();


		MapRequested?.Invoke();
	}


	private void OnShopPressed()
	{
		PlayNavigationHaptic();


		ShopRequested?.Invoke();
	}


	private void OnSettingsPressed()
	{
		PlayNavigationHaptic();


		SettingsRequested?.Invoke();
	}


	// ==================================================
	// HAPTICS
	// ==================================================

	private static void PlayNavigationHaptic()
	{
		Input.VibrateHandheld(
			NavigationHapticDurationMs,
			NavigationHapticStrength
		);
	}


	// ==================================================
	// THEME
	// ==================================================

	public void ApplyTheme(
		RoomThemeTextures theme)
	{
		_background.Texture =
			theme.Bar;
	}


	// ==================================================
	// MESSAGE
	// ==================================================

	public void SetMessage(
		string message)
	{
		_message.Text =
			message;
	}


	// ==================================================
	// LAYERING
	// ==================================================

	public void MoveToFront()
	{
		_bar.MoveToFront();
	}
}
