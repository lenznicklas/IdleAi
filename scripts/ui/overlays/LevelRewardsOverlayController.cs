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
				CustomMinimumSize =
					new Vector2(
						620,
						920
					),

				MouseFilter =
					Control.MouseFilterEnum.Stop
			};

		_panel.SetAnchorsPreset(
			Control.LayoutPreset.Center
		);

		_panel.SetOffsetsPreset(
			Control.LayoutPreset.Center
		);

		_panel.OffsetLeft =
			-310;

		_panel.OffsetTop =
			-460;

		_panel.OffsetRight =
			310;

		_panel.OffsetBottom =
			460;

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
			28
		);

		margin.AddThemeConstantOverride(
			"margin_top",
			26
		);

		margin.AddThemeConstantOverride(
			"margin_right",
			28
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
				"Every 150 total levels unlocks 10 Data Shards.",
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

		_levelLabel =
			CreateLabel(
				"",
				22
			);

		layout.AddChild(
			_levelLabel
		);

		_progress =
			new ProgressBar
			{
				MinValue =
					0,

				MaxValue =
					LevelRewardService.RewardInterval,

				ShowPercentage =
					false,

				CustomMinimumSize =
					new Vector2(
						0,
						24
					)
			};

		layout.AddChild(
			_progress
		);

		_nextLabel =
			CreateLabel(
				"",
				13
			);

		_nextLabel.AddThemeColorOverride(
			"font_color",
			new Color(
				0.64f,
				0.76f,
				0.88f,
				1.0f
			)
		);

		layout.AddChild(
			_nextLabel
		);

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
		closeButton.GetParent()
			.MoveToFront();

		closeButton.MoveToFront();
	}

	public void Open()
	{
		Refresh();

		_mobileScroll.ResetMotion();

		_overlay.Show();

		_overlay.MoveToFront();

		_scroll.ScrollVertical =
			0;
	}

	public void Hide()
	{
		_mobileScroll?.ResetMotion();

		_overlay?.Hide();
	}

	public void Refresh()
	{
		if (_overlay == null)
			return;

		int current =
			_service.CurrentLevel;

		int next =
			_service.NextRewardLevel;

		_levelLabel.Text =
			"TOTAL LEVEL  "
				+ current;

		int progressFrom =
			Math.Max(
				0,
				next
					- LevelRewardService.RewardInterval
			);

		_progress.Value =
			Math.Clamp(
				current
					- progressFrom,
				0,
				LevelRewardService.RewardInterval
			);

		if (_service.HasClaimableReward)
		{
			int count =
				_service.GetClaimableCount();

			_nextLabel.Text =
				count
				+ (
					count == 1
						? " REWARD READY TO CLAIM"
						: " REWARDS READY TO CLAIM"
				);

			_nextLabel.AddThemeColorOverride(
				"font_color",
				new Color(
					1.0f,
					0.82f,
					0.30f,
					1.0f
				)
			);
		}
		else
		{
			_nextLabel.Text =
				"NEXT REWARD AT LEVEL "
					+ next
					+ "  •  "
					+ Math.Max(
						0,
						next
							- current
					)
					+ " LEVELS TO GO";

			_nextLabel.AddThemeColorOverride(
				"font_color",
				new Color(
					0.64f,
					0.76f,
					0.88f,
					1.0f
				)
			);
		}

		RebuildRoad();
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
				"CLAIM +10";

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
