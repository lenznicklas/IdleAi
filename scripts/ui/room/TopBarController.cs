using Godot;
using System;

namespace IdleAi;

public sealed class TopBarController
{
	private const double PopupVisibleSeconds =
		2.5;

	private const double PopupFadeSeconds =
		1.0;

	private const int NavigationHapticDurationMs =
		12;

	private const float NavigationHapticStrength =
		0.12f;

	private readonly Game _root;
	private readonly GameState _state;
	private readonly ProgressionService _progression;
	private readonly LevelRewardService _levelRewards;

	private TextureRect _background = null!;
	private TextureButton _tokenCard = null!;
	private TextureButton _statsCard = null!;
	private TextureButton _levelCard = null!;
	private Label _tokenLabel = null!;
	private Label _levelLabel = null!;
	private Label _roomLabel = null!;
	private PanelContainer _tokenPopup = null!;
	private PanelContainer _levelPopup = null!;

	private Tween? _tokenFadeTween;
	private Tween? _levelGlowTween;

	private int _tokenPopupGeneration;

	public event Action? StatsRequested;
	public event Action? LevelRewardsRequested;

	public TopBarController(
		Game root,
		GameState state,
		ProgressionService progression,
		LevelRewardService levelRewards)
	{
		_root =
			root;

		_state =
			state;

		_progression =
			progression;

		_levelRewards =
			levelRewards;
	}

	public void Initialize()
	{
		CacheNodes();

		_tokenCard.Pressed +=
			OnTokenPressed;

		_statsCard.Pressed +=
			OnStatsPressed;

		_levelCard.Pressed +=
			OnLevelPressed;

		HidePopups();

		UpdateLevelRewardGlow();
	}

	private void OnTokenPressed()
	{
		PlayNavigationHaptic();

		ToggleTokenPopup();
	}

	private void OnStatsPressed()
	{
		PlayNavigationHaptic();

		HidePopups();

		StatsRequested?.Invoke();
	}

	private void OnLevelPressed()
	{
		PlayNavigationHaptic();

		HidePopups();

		LevelRewardsRequested?.Invoke();
	}

	private static void PlayNavigationHaptic()
	{
		Input.VibrateHandheld(
			NavigationHapticDurationMs,
			NavigationHapticStrength
		);
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

		/*
		 * Keep the old LevelPopup node for scene compatibility, but the level
		 * card now opens the dedicated reward-road overlay instead.
		 */
		_levelPopup =
			_root.GetNode<PanelContainer>(
				"LevelPopup"
			);
	}

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

		UpdateLevelRewardGlow();
	}

	private void UpdateLevelRewardGlow()
	{
		bool shouldGlow =
			_levelRewards
				.HasClaimableReward;

		if (shouldGlow)
		{
			if (_levelGlowTween != null)
				return;

			_levelCard.Modulate =
				Colors.White;

			_levelGlowTween =
				_root.CreateTween();

			_levelGlowTween.SetLoops();

			_levelGlowTween.TweenProperty(
				_levelCard,
				"modulate",
				new Color(
					1.30f,
					1.20f,
					0.55f,
					1.0f
				),
				0.55
			)
			.SetTrans(
				Tween.TransitionType.Sine
			)
			.SetEase(
				Tween.EaseType.InOut
			);

			_levelGlowTween.TweenProperty(
				_levelCard,
				"modulate",
				Colors.White,
				0.55
			)
			.SetTrans(
				Tween.TransitionType.Sine
			)
			.SetEase(
				Tween.EaseType.InOut
			);

			return;
		}

		_levelGlowTween?.Kill();

		_levelGlowTween =
			null;

		_levelCard.Modulate =
			Colors.White;
	}

	public void SetRoomName(
		string roomName)
	{
		_roomLabel.Text =
			roomName;
	}

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

	private void ToggleTokenPopup()
	{
		_levelPopup.Hide();

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

	public void HidePopups()
	{
		HideTokenPopup();

		_levelPopup.Hide();
	}

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
