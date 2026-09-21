using Godot;
using System;

namespace IdleAi;

public sealed class TopBarController
{
	private readonly Game _root;

	private readonly GameState _state;

	private readonly ProgressionService _progression;


	private TextureRect _background =
		null!;


	private TextureButton _tokenCard =
		null!;


	private TextureButton _statsCard =
		null!;


	private TextureButton _levelCard =
		null!;


	private Label _tokenLabel =
		null!;


	private Label _levelLabel =
		null!;


	private Label _roomLabel =
		null!;


	private PanelContainer _tokenPopup =
		null!;


	private PanelContainer _levelPopup =
		null!;


	public event Action? StatsRequested;


	public TopBarController(
		Game root,
		GameState state,
		ProgressionService progression)
	{
		_root =
			root;


		_state =
			state;


		_progression =
			progression;
	}


	public void Initialize()
	{
		CacheNodes();


		_tokenCard.Pressed +=
			ToggleTokenPopup;


		_statsCard.Pressed +=
			() =>
				StatsRequested?.Invoke();


		_levelCard.Pressed +=
			ToggleLevelPopup;


		HidePopups();
	}


	private void CacheNodes()
	{
		const string path =
			"MarginContainer/VBoxContainer/TopBar/";


		_background =
			_root.GetNode<TextureRect>(
				path
				+ "Background"
			);


		_tokenCard =
			_root.GetNode<TextureButton>(
				path
				+ "Margin/VBox/TopStats/TokenCard"
			);


		_statsCard =
			_root.GetNode<TextureButton>(
				path
				+ "Margin/VBox/TopStats/StatsCard"
			);


		_levelCard =
			_root.GetNode<TextureButton>(
				path
				+ "Margin/VBox/TopStats/LevelCard"
			);


		_tokenLabel =
			_root.GetNode<Label>(
				path
				+ "Margin/VBox/TopStats/TokenCard/TokenLabel"
			);


		_levelLabel =
			_root.GetNode<Label>(
				path
				+ "Margin/VBox/TopStats/LevelCard/TotalLevelLabel"
			);


		_roomLabel =
			_root.GetNode<Label>(
				path
				+ "Margin/VBox/RoomInTopBarLabel"
			);


		_tokenPopup =
			_root.GetNode<PanelContainer>(
				"TokenPopup"
			);


		_levelPopup =
			_root.GetNode<PanelContainer>(
				"LevelPopup"
			);
	}


	// ==================================================
	// UPDATE
	// ==================================================

	public void UpdateValues()
	{
		_tokenLabel.Text =
			NumberFormatter.Format(
				_state.Tokens
			);


		_levelLabel.Text =
			_progression
				.GetTotalLevel()
				.ToString();
	}


	public void SetRoomName(
		string roomName)
	{
		_roomLabel.Text =
			roomName;
	}


	// ==================================================
	// THEME
	// ==================================================

	public void ApplyTheme(
		RoomThemeTextures theme)
	{
		_background.Texture =
			theme.Bar;


		ApplyCardTexture(
			_tokenCard,
			theme.CardNormal,
			theme.CardHover,
			theme.CardPressed
		);


		ApplyCardTexture(
			_statsCard,
			theme.CardNormal,
			theme.CardHover,
			theme.CardPressed
		);


		ApplyCardTexture(
			_levelCard,
			theme.CardNormal,
			theme.CardHover,
			theme.CardPressed
		);
	}


	private static void ApplyCardTexture(
		TextureButton button,
		Texture2D normal,
		Texture2D hover,
		Texture2D pressed)
	{
		button.TextureNormal =
			normal;


		button.TextureHover =
			hover;


		button.TexturePressed =
			pressed;
	}


	// ==================================================
	// POPUPS
	// ==================================================

	private void ToggleTokenPopup()
	{
		_levelPopup.Hide();


		if (_tokenPopup.Visible)
		{
			_tokenPopup.Hide();

			return;
		}


		PositionPopup(
			_tokenPopup,
			_tokenCard
		);
	}


	private void ToggleLevelPopup()
	{
		_tokenPopup.Hide();


		if (_levelPopup.Visible)
		{
			_levelPopup.Hide();

			return;
		}


		PositionPopup(
			_levelPopup,
			_levelCard
		);
	}


	public void HidePopups()
	{
		_tokenPopup.Hide();

		_levelPopup.Hide();
	}


	private async void PositionPopup(
		Control popup,
		Control card)
	{
		popup.Show();


		await _root.ToSignal(
			_root.GetTree(),
			SceneTree.SignalName.ProcessFrame
		);


		popup.GlobalPosition =
			new Vector2(
				card.GlobalPosition.X
				+ card.Size.X / 2
				- popup.Size.X / 2,

				card.GlobalPosition.Y
				+ card.Size.Y
				+ 6
			);
	}
}
