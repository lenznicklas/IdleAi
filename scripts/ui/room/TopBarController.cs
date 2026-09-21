using Godot;
using System;

namespace IdleAi;

public sealed class TopBarController
{
	private const double PopupVisibleSeconds =
		2.5;


	private const double PopupFadeSeconds =
		1.0;


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


	private Tween? _tokenFadeTween;

	private Tween? _levelFadeTween;


	private int _tokenPopupGeneration;

	private int _levelPopupGeneration;


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


	// ==================================================
	// INITIALIZE
	// ==================================================

	public void Initialize()
	{
		CacheNodes();


		_tokenCard.Pressed +=
			ToggleTokenPopup;


		_statsCard.Pressed +=
			() =>
			{
				HidePopups();

				StatsRequested?.Invoke();
			};


		_levelCard.Pressed +=
			ToggleLevelPopup;


		HidePopups();
	}


	// ==================================================
	// CACHE
	// ==================================================

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
	// TOKEN POPUP
	// ==================================================

	private void ToggleTokenPopup()
	{
		HideLevelPopup();


		if (_tokenPopup.Visible)
		{
			HideTokenPopup();

			return;
		}


		OpenTokenPopup();
	}


	private async void OpenTokenPopup()
	{
		_tokenPopupGeneration++;


		int generation =
			_tokenPopupGeneration;


		_tokenFadeTween?.Kill();

		_tokenFadeTween =
			null;


		_tokenPopup.Modulate =
			Colors.White;


		await PositionPopup(
			_tokenPopup,
			_tokenCard
		);


		if (
			generation
			!= _tokenPopupGeneration
		)
		{
			return;
		}


		await _root.ToSignal(
			_root.GetTree()
				.CreateTimer(
					PopupVisibleSeconds
				),

			SceneTreeTimer.SignalName.Timeout
		);


		if (
			generation
			!= _tokenPopupGeneration
			|| !_tokenPopup.Visible
		)
		{
			return;
		}


		_tokenFadeTween =
			_root.CreateTween();


		_tokenFadeTween.TweenProperty(
			_tokenPopup,
			"modulate:a",
			0.0f,
			PopupFadeSeconds
		);


		await _root.ToSignal(
			_tokenFadeTween,
			Tween.SignalName.Finished
		);


		if (
			generation
			!= _tokenPopupGeneration
		)
		{
			return;
		}


		_tokenPopup.Hide();


		_tokenPopup.Modulate =
			Colors.White;


		_tokenFadeTween =
			null;
	}


	private void HideTokenPopup()
	{
		_tokenPopupGeneration++;


		_tokenFadeTween?.Kill();

		_tokenFadeTween =
			null;


		_tokenPopup.Hide();


		_tokenPopup.Modulate =
			Colors.White;
	}


	// ==================================================
	// LEVEL POPUP
	// ==================================================

	private void ToggleLevelPopup()
	{
		HideTokenPopup();


		if (_levelPopup.Visible)
		{
			HideLevelPopup();

			return;
		}


		OpenLevelPopup();
	}


	private async void OpenLevelPopup()
	{
		_levelPopupGeneration++;


		int generation =
			_levelPopupGeneration;


		_levelFadeTween?.Kill();

		_levelFadeTween =
			null;


		_levelPopup.Modulate =
			Colors.White;


		await PositionPopup(
			_levelPopup,
			_levelCard
		);


		if (
			generation
			!= _levelPopupGeneration
		)
		{
			return;
		}


		await _root.ToSignal(
			_root.GetTree()
				.CreateTimer(
					PopupVisibleSeconds
				),

			SceneTreeTimer.SignalName.Timeout
		);


		if (
			generation
			!= _levelPopupGeneration
			|| !_levelPopup.Visible
		)
		{
			return;
		}


		_levelFadeTween =
			_root.CreateTween();


		_levelFadeTween.TweenProperty(
			_levelPopup,
			"modulate:a",
			0.0f,
			PopupFadeSeconds
		);


		await _root.ToSignal(
			_levelFadeTween,
			Tween.SignalName.Finished
		);


		if (
			generation
			!= _levelPopupGeneration
		)
		{
			return;
		}


		_levelPopup.Hide();


		_levelPopup.Modulate =
			Colors.White;


		_levelFadeTween =
			null;
	}


	private void HideLevelPopup()
	{
		_levelPopupGeneration++;


		_levelFadeTween?.Kill();

		_levelFadeTween =
			null;


		_levelPopup.Hide();


		_levelPopup.Modulate =
			Colors.White;
	}


	// ==================================================
	// ALL POPUPS
	// ==================================================

	public void HidePopups()
	{
		HideTokenPopup();

		HideLevelPopup();
	}


	// ==================================================
	// POSITION
	// ==================================================

	private async System.Threading.Tasks.Task PositionPopup(
		Control popup,
		Control card)
	{
		popup.Modulate =
			Colors.White;


		popup.Show();


		await _root.ToSignal(
			_root.GetTree(),
			SceneTree.SignalName.ProcessFrame
		);


		if (!popup.Visible)
			return;


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
