using Godot;
using System;

namespace IdleAi;

public sealed class BottomBarController
{
	private const int NavigationHapticDurationMs =
		12;


	private const float NavigationHapticStrength =
		0.12f;


	private readonly Game _root;


	private Control _bar =
		null!;


	private TextureRect _background =
		null!;


	private TextureButton _mapButton =
		null!;


	private TextureButton _shopButton =
		null!;


	private Label _message =
		null!;


	public event Action? MapRequested;

	public event Action? ShopRequested;


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


		_mapButton.Pressed +=
			OnMapPressed;


		_shopButton.Pressed +=
			OnShopPressed;
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
