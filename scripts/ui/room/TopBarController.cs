using Godot;
using System;

namespace IdleAi;

public sealed partial class TopBarController : Node
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

	private TextureRect _background = null!;
	private TextureButton _tokenCard = null!;
	private TextureButton _statsCard = null!;
	private TextureButton _levelCard = null!;
	private Label _tokenLabel = null!;
	private Label _levelLabel = null!;
	private Label _roomLabel = null!;
	private PanelContainer _tokenPopup = null!;
	private PanelContainer _levelPopup = null!;
	private StyleBoxFlat _popupStyle = null!;

	private Tween? _popupTween;
	private PanelContainer? _activePopup;
	private Control? _activeCard;

	private int _popupGeneration;
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
		// Share one private style across TopBar tooltips without recoloring other panels.
		_popupStyle = (StyleBoxFlat)_tokenPopup.GetThemeStylebox("panel").Duplicate();
		_tokenPopup.AddThemeStyleboxOverride("panel", _popupStyle);
		_levelPopup.AddThemeStyleboxOverride("panel", _popupStyle);
		UpdatePopupAccent();

		_tokenCard.Pressed +=
			OnTokenPressed;

		_statsCard.Pressed +=
			OnStatsPressed;

		_levelCard.Pressed +=
			OnLevelPressed;

		HidePopups();
	}

	private void OnTokenPressed()
	{
		PlayNavigationHaptic();

		TogglePopup(_tokenPopup, _tokenCard);
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

		TogglePopup(_levelPopup, _levelCard);
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

		_levelPopup =
			_root.GetNode<PanelContainer>(
				"LevelPopup"
			);
	}

	public void UpdateValues()
	{
		UpdatePopupAccent();
		_tokenLabel.Text =
			NumberFormatter.Format(
				_state.Tokens
			);

		_levelLabel.Text = "LV " + _progression.GetTotalLevel();
		LevelBadgeProgress.Update(_state);
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
		UpdatePopupAccent();
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

	private async void TogglePopup(PanelContainer popup, Control card)
	{
		bool close = _activePopup == popup && popup.Visible;
		HidePopups();
		if (close) return;
		int generation = _popupGeneration;
		_activePopup = popup; _activeCard = card;
		popup.Modulate = new Color(1, 1, 1, 0);
		popup.Show(); popup.MoveToFront();
		FitPopup();
		SetProcess(true); SetProcessInput(true);
		// Shape wrapped text before positioning/fading either tooltip.
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		if (!GodotObject.IsInstanceValid(this) || !IsInsideTree() || generation != _popupGeneration) return;
		FitPopup();
		_popupTween = CreateTween();
		_popupTween.TweenProperty(popup, "modulate:a", 1f, .12).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
		_popupTween.TweenInterval(PopupVisibleSeconds);
		_popupTween.TweenProperty(popup, "modulate:a", 0f, PopupFadeSeconds);
		_popupTween.TweenCallback(Callable.From(HidePopups));
	}

	private void UpdatePopupAccent()
	{
		_popupStyle.BorderColor = _root.ActiveAreaAccent;
	}

	private void FitPopup()
	{
		UpdatePopupAccent();
		if (_activePopup == null || _activeCard == null) return;
		Rect2 bounds = _root.GetGlobalRect().Intersection(GetViewport().GetVisibleRect());
		const float margin = 12, gap = 6;
		float width = Mathf.Min(230, Mathf.Max(1, bounds.Size.X - margin * 2));
		if (!Mathf.IsEqualApprox(_activePopup.CustomMinimumSize.X, width))
		{
			_activePopup.CustomMinimumSize = new(width, 70);
		}
		// Wrapped labels can initially measure at zero width. Let the panel shrink
		// back to its content height after the container has laid out the text.
		_activePopup.Size = new(width, 0);
		Rect2 card = _activeCard.GetGlobalRect();
		float x = Mathf.Clamp(card.GetCenter().X - _activePopup.Size.X / 2,
			bounds.Position.X + margin, Mathf.Max(bounds.Position.X + margin, bounds.End.X - margin - _activePopup.Size.X));
		// Keep the entire TopBar, including the room name, clear of both tooltips.
		float y = Mathf.Max(card.End.Y, _background.GetParent<Control>().GetGlobalRect().End.Y) + gap;
		if (y + _activePopup.Size.Y > bounds.End.Y - margin) y = card.Position.Y - _activePopup.Size.Y - gap;
		y = Mathf.Clamp(y, bounds.Position.Y + margin, Mathf.Max(bounds.Position.Y + margin, bounds.End.Y - margin - _activePopup.Size.Y));
		_activePopup.GlobalPosition = new(x, y);
	}

	public override void _Process(double delta)
	{
		if (_activePopup == null || !_activePopup.Visible || _activeCard == null || !_activeCard.IsVisibleInTree()) HidePopups();
		else FitPopup();
	}

	public override void _Input(InputEvent input)
	{
		Vector2 position;
		if (input is InputEventScreenTouch { Pressed: true } touch) position = touch.Position;
		else if (input is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } mouse) position = mouse.Position;
		else return;
		if (_activePopup != null && _activeCard != null
			&& !_activePopup.GetGlobalRect().HasPoint(position) && !_activeCard.GetGlobalRect().HasPoint(position)) HidePopups();
	}

	public void HidePopups()
	{
		_popupGeneration++;
		_popupTween?.Kill(); _popupTween = null;
		if (GodotObject.IsInstanceValid(_tokenPopup)) { _tokenPopup.Hide(); _tokenPopup.Modulate = Colors.White; }
		if (GodotObject.IsInstanceValid(_levelPopup)) { _levelPopup.Hide(); _levelPopup.Modulate = Colors.White; }
		_activePopup = null; _activeCard = null;
		SetProcess(false); SetProcessInput(false);
	}

	public override void _ExitTree() => HidePopups();
}
