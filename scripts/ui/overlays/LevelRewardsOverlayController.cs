using Godot;
using System;

namespace IdleAi;

public sealed class LevelRewardsOverlayController
{
	private readonly Game _root;
	private readonly LevelRewardService _service;

	private Control _overlay = null!;
	private PanelContainer _panel = null!;
	private Label _levelLabel = null!;
	private Label _nextLabel = null!;
	private ProgressBar _progress = null!;
	private PanelContainer _milestone = null!;
	private Label _milestoneTitle = null!;
	private Label _progressLabel = null!;
	private TextureRect _shard = null!;
	private Button _claimButton = null!;
	private Tween? _progressTween;
	private Tween? _pulseTween;
	private Tween? _feedbackTween;
	private Tween? _milestoneTween;
	private int _lastLevel = -1;
	private int _lastClaimed = -1;
	private bool _wasReady;
	private VBoxContainer _road = null!;
	private ScrollContainer _scroll = null!;
	private MobileScrollController _mobileScroll = null!;

	public event Action? StateChanged;
	public event Action<string>? MessageRequested;

	public bool Visible =>
		_overlay != null
		&& _overlay.Visible;

	public LevelRewardsOverlayController(
		Game root,
		LevelRewardService service)
	{
		_root =
			root;

		_service =
			service;
	}

	public void Initialize()
	{
		CreateUi();

		Hide();
	}

	private void CreateUi()
	{
		_overlay =
			new Control
			{
				Name =
					"LevelRewardsOverlay",

				MouseFilter =
					Control.MouseFilterEnum.Stop,

				ZIndex =
					900
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

		_panel =
			new PanelContainer
			{
				MouseFilter =
					Control.MouseFilterEnum.Stop
			};

		_panel.SetAnchorsPreset(
			Control.LayoutPreset.Center
		);

		_panel.SetOffsetsPreset(
			Control.LayoutPreset.Center
		);

		// The road scrolls; the milestone card keeps its content-driven height.
		_overlay.Resized += FitPanelToViewport;
		FitPanelToViewport();

		_panel.AddThemeStyleboxOverride(
			"panel",
			CreatePanelStyle()
		);

		_overlay.AddChild(
			_panel
		);

		MarginContainer margin =
			new();

		margin.AddThemeConstantOverride(
			"margin_left",
			20
		);

		margin.AddThemeConstantOverride(
			"margin_top",
			26
		);

		margin.AddThemeConstantOverride(
			"margin_right",
			20
		);

		margin.AddThemeConstantOverride(
			"margin_bottom",
			26
		);

		_panel.AddChild(
			margin
		);

		VBoxContainer layout =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,

				SizeFlagsVertical =
					Control.SizeFlags.ExpandFill
			};

		layout.AddThemeConstantOverride(
			"separation",
			12
		);

		margin.AddChild(
			layout
		);

		Label title =
			CreateLabel(
				"LEVEL REWARDS",
				30
			);

		title.AddThemeColorOverride(
			"font_color",
			new Color(
				1.0f,
				0.82f,
				0.30f,
				1.0f
			)
		);

		layout.AddChild(
			title
		);

		Label subtitle =
			CreateLabel(
				 $"Every {LevelRewardService.RewardInterval} Total Levels earns {LevelRewardService.RewardDataShards} Data Shards.",
				14
			);

		subtitle.AddThemeColorOverride(
			"font_color",
			new Color(
				0.65f,
				0.74f,
				0.84f,
				1.0f
			)
		);

		layout.AddChild(
			subtitle
		);

		layout.AddChild(CreateLabel("TOTAL LEVEL", 18));
		_levelLabel = CreateLabel("", 48);
		layout.AddChild(_levelLabel);

		_milestone = new PanelContainer {
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			SizeFlagsVertical = Control.SizeFlags.Fill
		};
		layout.AddChild(_milestone);
		VBoxContainer milestoneLayout = new() {
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			SizeFlagsVertical = Control.SizeFlags.Fill
		};
		milestoneLayout.AddThemeConstantOverride("separation", 10);
		_milestone.AddChild(milestoneLayout);
		_milestoneTitle = CreateLabel("", 22);
		milestoneLayout.AddChild(_milestoneTitle);
		_progress = new ProgressBar {
			MaxValue = LevelRewardService.RewardInterval, ShowPercentage = false,
			CustomMinimumSize = new Vector2(0, 18), MouseFilter = Control.MouseFilterEnum.Ignore
		};
		milestoneLayout.AddChild(_progress);
		_progressLabel = CreateLabel("", 16);
		milestoneLayout.AddChild(_progressLabel);
		_nextLabel = CreateLabel("", 17);
		milestoneLayout.AddChild(_nextLabel);
		milestoneLayout.AddChild(new HSeparator());
		Label rewardHeading = CreateLabel("NEXT REWARD", 14);
		rewardHeading.AddThemeColorOverride("font_color", new Color(.65f, .74f, .84f));
		milestoneLayout.AddChild(rewardHeading);
		HBoxContainer preview = new() {
			Alignment = BoxContainer.AlignmentMode.Center,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			SizeFlagsVertical = Control.SizeFlags.Fill
		};
		preview.AddThemeConstantOverride("separation", 10);
		_shard = new TextureRect {
			SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
			SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
			Texture = ShopUi.DataShard, CustomMinimumSize = new Vector2(32, 32),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		preview.AddChild(_shard);
		Label rewardPreview = CreateLabel($"+{LevelRewardService.RewardDataShards} DATA SHARDS", 18);
		// Autowrapped labels have no useful horizontal minimum in a centered HBox.
		// Let the text's measured single-line width size the centered icon/text group.
		rewardPreview.AutowrapMode = TextServer.AutowrapMode.Off;
		rewardPreview.SizeFlagsHorizontal = Control.SizeFlags.Fill;
		rewardPreview.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
		rewardPreview.HorizontalAlignment = HorizontalAlignment.Left;
		rewardPreview.AddThemeColorOverride("font_color", new Color(1f, .82f, .3f));
		preview.AddChild(rewardPreview);
		milestoneLayout.AddChild(preview);
		_claimButton = new Button { Text = "CLAIM REWARD", CustomMinimumSize = new Vector2(0, 58) };
		_claimButton.AddThemeFontSizeOverride("font_size", 22);
		_claimButton.Pressed += () => Claim(_service.NextRewardLevel);
		milestoneLayout.AddChild(_claimButton);

		HSeparator separator =
			new();

		layout.AddChild(
			separator
		);

		_scroll =
			new ScrollContainer
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,

				SizeFlagsVertical =
					Control.SizeFlags.ExpandFill,

				HorizontalScrollMode =
					ScrollContainer.ScrollMode.Disabled,

				VerticalScrollMode =
					ScrollContainer.ScrollMode.Auto
			};

		layout.AddChild(
			_scroll
		);

		_road =
			new VBoxContainer
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};

		_road.AddThemeConstantOverride(
			"separation",
			12
		);

		_scroll.AddChild(
			_road
		);

		_mobileScroll =
			new MobileScrollController
			{
				Name =
					"LevelRewardsMobileScroll"
			};

		_overlay.AddChild(
			_mobileScroll
		);

		_mobileScroll.Setup(
			_scroll
		);

		/*
		 * IMPORTANT:
		 *
		 * Add the close layer LAST.
		 *
		 * PanelContainer is a Container. If the X is added before the
		 * Margin/Scroll content, that later content becomes the topmost
		 * input target. The X can still be visible, but taps are swallowed
		 * by the content sitting above its layer.
		 */
		TextureButton closeButton =
			OverlayCloseButton.Add(
				_panel,
				Hide
			);

		/*
		 * Defensive: keep both the transparent close layer and the actual
		 * button above every panel child.
		 */
		if (
			closeButton.GetParent()
				is Control closeLayer
		)
		{
			closeLayer.MoveToFront();
		}

		closeButton.MoveToFront();
	}

	private void FitPanelToViewport()
	{
		Vector2 available = _overlay.Size;
		float width = Math.Min(620, Math.Max(0, available.X - 32));
		float height = Math.Min(920, Math.Max(0, available.Y - 48));
		_panel.OffsetLeft = -width / 2;
		_panel.OffsetRight = width / 2;
		_panel.OffsetTop = -height / 2;
		_panel.OffsetBottom = height / 2;
	}

	public void Open()
	{
		FitPanelToViewport();
		Refresh();

		_mobileScroll.ResetMotion();

		_overlay.Show();

		_overlay.MoveToFront();
		UpdatePulse(_service.HasClaimableReward);

		_scroll.ScrollVertical =
			0;
	}

	public void Hide()
	{
		_mobileScroll?.ResetMotion();

		_pulseTween?.Kill();
		_claimButton.Scale = Vector2.One;
		_overlay?.Hide();
	}

	public void Refresh()
	{
		if (_overlay == null)
			return;

		int current = _service.CurrentLevel;
		int claimed = _service.HighestClaimedLevel;
		if (current == _lastLevel && claimed == _lastClaimed) return;
		bool ready = _service.HasClaimableReward;
		bool near = !ready && _service.LevelsUntilNextReward <= 15;
		Color accent = ready ? new Color(1f, .78f, .22f) : near ? new Color(.38f, .86f, 1f) : new Color(.24f, .72f, 1f);
		_levelLabel.Text = current.ToString();
		_milestoneTitle.Text = (ready ? "MILESTONE REACHED" : "NEXT MILESTONE")
			+ $" • LEVEL {_service.NextRewardLevel}";
		_milestoneTitle.AddThemeColorOverride("font_color", accent);
		_progressLabel.Text = $"{current} / {_service.NextRewardLevel}";
		_nextLabel.Text = ready
			? $"{_service.GetClaimableCount()} " + (_service.GetClaimableCount() == 1 ? "REWARD READY TO CLAIM" : "REWARDS READY TO CLAIM")
			: $"{_service.LevelsUntilNextReward} LEVELS UNTIL REWARD";
		StyleBoxFlat cardStyle = CreateMilestoneStyle(accent, ready ? .16f : .06f);
		cardStyle.ContentMarginLeft = cardStyle.ContentMarginRight = 14;
		cardStyle.ContentMarginTop = cardStyle.ContentMarginBottom = 12;
		cardStyle.ShadowColor = new Color(accent, ready ? .24f : near ? .16f : .07f);
		cardStyle.ShadowSize = ready ? 10 : 5;
		_milestone.AddThemeStyleboxOverride("panel", cardStyle);
		_progress.AddThemeStyleboxOverride("background", CreateMilestoneStyle(new Color(.06f, .12f, .2f), 1));
		StyleBoxFlat fill = CreateMilestoneStyle(accent, 1);
		fill.ShadowColor = new Color(accent, near || ready ? .35f : .15f);
		fill.ShadowSize = 4;
		_progress.AddThemeStyleboxOverride("fill", fill);
		_progressTween?.Kill();
		if (_lastLevel < 0 || !Visible) _progress.Value = _service.MilestoneProgress;
		else {
			_progressTween = _progress.CreateTween();
			_progressTween.TweenProperty(_progress, "value", _service.MilestoneProgress, .4)
				.SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
		}
		_claimButton.Visible = ready;
		_claimButton.Disabled = !ready;
		_claimButton.AddThemeStyleboxOverride("normal", CreateMilestoneStyle(accent, .25f));
		_claimButton.AddThemeStyleboxOverride("hover", CreateMilestoneStyle(accent, .4f));
		_claimButton.AddThemeStyleboxOverride("pressed", CreateMilestoneStyle(accent, .15f));
		if (ready != _wasReady || _lastLevel < 0) UpdatePulse(ready);
		_lastLevel = current;
		_lastClaimed = claimed;
		_wasReady = ready;
		RebuildRoad();
	}

	private void UpdatePulse(bool ready)
	{
		_pulseTween?.Kill();
		_claimButton.Scale = Vector2.One;
		_milestone.Modulate = Colors.White;
		if (!ready || !Visible) return;
		_claimButton.PivotOffset = _claimButton.Size / 2;
		_pulseTween = _claimButton.CreateTween().SetLoops();
		_pulseTween.TweenProperty(_claimButton, "scale", Vector2.One * 1.025f, .9).SetTrans(Tween.TransitionType.Sine);
		_pulseTween.Parallel().TweenProperty(_milestone, "modulate", new Color(1.08f, 1.04f, 1f), .9);
		_pulseTween.TweenProperty(_claimButton, "scale", Vector2.One, .9).SetTrans(Tween.TransitionType.Sine);
		_pulseTween.Parallel().TweenProperty(_milestone, "modulate", Colors.White, .9);
	}

	public void CelebrateMilestone()
	{
		if (!Visible) return;
		Refresh();
		_milestoneTween?.Kill();
		_levelLabel.Scale = Vector2.One;
		_levelLabel.PivotOffset = _levelLabel.Size / 2;
		_milestoneTween = _levelLabel.CreateTween();
		_milestoneTween.TweenProperty(_levelLabel, "scale", Vector2.One * 1.12f, .2);
		_milestoneTween.Parallel().TweenProperty(_levelLabel, "modulate", new Color(1f, .82f, .3f), .2);
		_milestoneTween.TweenProperty(_levelLabel, "scale", Vector2.One, .8).SetTrans(Tween.TransitionType.Back);
		_milestoneTween.Parallel().TweenProperty(_levelLabel, "modulate", Colors.White, .8);
	}

	private void PlayClaimFeedback()
	{
		_feedbackTween?.Kill();
		_shard.Scale = Vector2.One;
		_shard.PivotOffset = _shard.Size / 2;
		_feedbackTween = _shard.CreateTween();
		_feedbackTween.TweenProperty(_shard, "scale", Vector2.One * 1.25f, .16);
		_feedbackTween.TweenProperty(_shard, "scale", Vector2.One, .35).SetTrans(Tween.TransitionType.Back);
		Label floating = CreateLabel($"+{LevelRewardService.RewardDataShards} DATA SHARDS", 24);
		floating.AddThemeColorOverride("font_color", new Color(1f, .82f, .3f));
		_overlay.AddChild(floating);
		floating.GlobalPosition = _shard.GlobalPosition + new Vector2(-70, -30);
		Tween rise = floating.CreateTween();
		rise.TweenProperty(floating, "position:y", floating.Position.Y - 45, .9);
		rise.Parallel().TweenProperty(floating, "modulate:a", 0f, .9);
		rise.TweenCallback(Callable.From(floating.QueueFree));
	}

	private void RebuildRoad()
	{
		foreach (
			Node child
				in _road.GetChildren()
		)
		{
			_road.RemoveChild(
				child
			);

			child.QueueFree();
		}

		int current =
			_service.CurrentLevel;

		int highestClaimed =
			_service.HighestClaimedLevel;

		int first =
			Math.Max(
				LevelRewardService.RewardInterval,
				highestClaimed
					- LevelRewardService.RewardInterval
						* 2
			);

		first =
			(
				first
					/ LevelRewardService.RewardInterval
			)
			* LevelRewardService.RewardInterval;

		if (
			first
			< LevelRewardService.RewardInterval
		)
		{
			first =
				LevelRewardService.RewardInterval;
		}

		int last =
			Math.Max(
				_service.NextRewardLevel
					+ LevelRewardService.RewardInterval
						* 3,

				(
					current
						/ LevelRewardService.RewardInterval
					+ 3
				)
				* LevelRewardService.RewardInterval
			);

		for (
			int level = first;
			level <= last;
			level +=
				LevelRewardService.RewardInterval
		)
		{
			CreateMilestoneCard(
				level
			);
		}

		_road.AddChild(
			new Control
			{
				CustomMinimumSize =
					new Vector2(
						0,
						80
					),

				MouseFilter =
					Control.MouseFilterEnum.Ignore
			}
		);
	}

	private void CreateMilestoneCard(
		int level)
	{
		bool claimed =
			_service.IsClaimed(
				level
			);

		bool canClaim =
			_service.CanClaim(
				level
			);

		bool reached =
			_service.IsReached(
				level
			);

		Color accent =
			claimed
				? new Color(
					0.28f,
					0.90f,
					0.56f,
					1.0f
				)
				: canClaim
					? new Color(
						1.0f,
						0.78f,
						0.22f,
						1.0f
					)
					: new Color(
						0.24f,
						0.62f,
						0.95f,
						1.0f
					);

		PanelContainer card =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};

		card.AddThemeStyleboxOverride(
			"panel",
			CreateMilestoneStyle(
				accent,
				canClaim
					? 0.18f
					: 0.08f
			)
		);

		_road.AddChild(
			card
		);

		MarginContainer margin =
			new();

		margin.AddThemeConstantOverride(
			"margin_left",
			18
		);

		margin.AddThemeConstantOverride(
			"margin_top",
			14
		);

		margin.AddThemeConstantOverride(
			"margin_right",
			18
		);

		margin.AddThemeConstantOverride(
			"margin_bottom",
			14
		);

		card.AddChild(
			margin
		);

		BoxContainer row = _panel.Size.X < 500
			? new VBoxContainer() : new HBoxContainer();
		row.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;

		row.AddThemeConstantOverride(
			"separation",
			16
		);

		margin.AddChild(
			row
		);

		VBoxContainer info =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};

		row.AddChild(
			info
		);

		Label levelLabel =
			CreateLabel(
				"LEVEL "
					+ level,
				20
			);

		levelLabel.HorizontalAlignment =
			HorizontalAlignment.Left;

		levelLabel.AddThemeColorOverride(
			"font_color",
			accent
		);

		info.AddChild(
			levelLabel
		);

		Label reward =
			CreateLabel(
				"+"
					+ LevelRewardService.RewardDataShards
					+ " DATA SHARDS",
				15
			);

		reward.HorizontalAlignment =
			HorizontalAlignment.Left;

		info.AddChild(
			reward
		);

		Button button =
			new()
			{
				CustomMinimumSize =
					new Vector2(
						190,
						58
					),

				FocusMode =
					Control.FocusModeEnum.None
			};

		if (claimed)
		{
			button.Text =
				"CLAIMED";

			button.Disabled =
				true;
		}
		else if (canClaim)
		{
			button.Text =
				"CLAIM +" + LevelRewardService.RewardDataShards;

			button.Disabled =
				false;

			button.Pressed +=
				() =>
					Claim(
						level
					);
		}
		else if (reached)
		{
			/*
			 * A later milestone may be reached while an earlier one is still
			 * unclaimed. The player claims the road in order.
			 */
			button.Text =
				"CLAIM PREVIOUS";

			button.Disabled =
				true;
		}
		else
		{
			button.Text =
				"LOCKED";

			button.Disabled =
				true;
		}

		row.AddChild(
			button
		);
	}

	private void Claim(
		int level)
	{
		LevelRewardResult result =
			_service.Claim(
				level
			);

		MessageRequested?.Invoke(
			result.Message
		);

		if (result.Changed)
		{
			Input.VibrateHandheld(
				18,
				0.16f
			);

			StateChanged?.Invoke();
			PlayClaimFeedback();
		}

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

		Hide();
	}

	private static Label CreateLabel(
		string text,
		int fontSize)
	{
		Label label =
			new()
			{
				Text =
					text,

				HorizontalAlignment =
					HorizontalAlignment.Center,

				VerticalAlignment =
					VerticalAlignment.Center,

				AutowrapMode =
					TextServer.AutowrapMode.WordSmart,

				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};

		label.AddThemeFontSizeOverride(
			"font_size",
			fontSize
		);

		label.AddThemeColorOverride(
			"font_color",
			Colors.White
		);

		return label;
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
					0.22f,
					0.70f,
					1.0f,
					0.85f
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
					0.50f
				),

			ShadowSize =
				16
		};
	}

	private static StyleBoxFlat CreateMilestoneStyle(
		Color accent,
		float tint)
	{
		return new StyleBoxFlat
		{
			BgColor =
				new Color(
					accent.R,
					accent.G,
					accent.B,
					tint
				),

			BorderColor =
				new Color(
					accent.R,
					accent.G,
					accent.B,
					0.62f
				),

			BorderWidthLeft = 2,
			BorderWidthTop = 2,
			BorderWidthRight = 2,
			BorderWidthBottom = 2,

			CornerRadiusTopLeft = 14,
			CornerRadiusTopRight = 14,
			CornerRadiusBottomLeft = 14,
			CornerRadiusBottomRight = 14
		};
	}
}
