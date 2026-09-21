using Godot;
using System;

namespace IdleAi;

public sealed class BottomBarController
{
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
			() =>
				MapRequested?.Invoke();


		_shopButton.Pressed +=
			() =>
				ShopRequested?.Invoke();
	}


	public void ApplyTheme(
		RoomThemeTextures theme)
	{
		_background.Texture =
			theme.Bar;
	}


	public void SetMessage(
		string message)
	{
		_message.Text =
			message;
	}


	public void MoveToFront()
	{
		_bar.MoveToFront();
	}
}
