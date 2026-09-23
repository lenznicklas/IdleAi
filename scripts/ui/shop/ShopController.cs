using Godot;
using System;

using AdMobMobileAds =
	PoingStudios.AdMob.Api.MobileAds;

using AdMobRewardedAd =
	PoingStudios.AdMob.Api.RewardedAd;

using AdMobRewardedAdLoader =
	PoingStudios.AdMob.Api.RewardedAdLoader;

using AdMobAdRequest =
	PoingStudios.AdMob.Api.Core.AdRequest;

using AdMobRewardedAdLoadCallback =
	PoingStudios.AdMob.Api.Listeners.RewardedAdLoadCallback;

using AdMobFullScreenContentCallback =
	PoingStudios.AdMob.Api.Listeners.FullScreenContentCallback;

using AdMobRewardListener =
	PoingStudios.AdMob.Api.Listeners.OnUserEarnedRewardListener;

namespace IdleAi;

public sealed class ShopController
{
	private const float HeaderHeight =
		190.0f;

	private const int HeaderGap =
		10;

	private const float HeaderExtraTopPadding =
		6.0f;

	private const int BottomScrollPadding =
		180;

	private const int RewardedAdShardReward =
		5;

	private const string AndroidRewardedAdUnitId =
		"ca-app-pub-3940256099942544/5224354917";

	private const string IosRewardedAdUnitId =
		"ca-app-pub-3940256099942544/1712485313";

	private static readonly Texture2D FiveShardsIcon =
		GD.Load<Texture2D>(
			"res://assets/shop/five_shards.png"
		);

	private readonly Game _root;
	private readonly GameState _state;
	private readonly ShopService _service;

	private Control _page =
		null!;

	private MarginContainer _pageMargin =
		null!;

	private Control _layoutRoot =
		null!;

	private VBoxContainer _header =
		null!;

	private ScrollContainer _scroll =
		null!;

	private MarginContainer _scrollMargin =
		null!;

	private VBoxContainer _content =
		null!;

	private MobileScrollController _mobileScroll =
		null!;

	private Label _shardLabel =
		null!;

	private Label _rewardedAdStatus =
		null!;

	private Button _rewardedAdButton =
		null!;

	private Label _productionBoostStatus =
		null!;

	private Label _luckStatus =
		null!;

	private Label _productionUpgradeStatus =
		null!;

	private Label _offlineUpgradeStatus =
		null!;

	private Button _productionBoostButton =
		null!;

	private Button _luckButton =
		null!;

	private Button _instantButton =
		null!;

	private Button _productionUpgradeButton =
		null!;

	private Button _offlineUpgradeButton =
		null!;

	private AdMobRewardedAd _rewardedAd =
		null!;

	private AdMobFullScreenContentCallback _rewardedFullScreenCallback =
		null!;

	private AdMobRewardListener _rewardListener =
		null!;

	private bool _adMobInitialized;
	private bool _rewardedAdLoading;
	private bool _rewardedAdShowing;
	private bool _rewardGrantedForCurrentAd;

	public event Action<string>? MessageRequested;
	public event Action? StateChanged;

	public bool Visible =>
		_page != null
		&& _page.Visible;

	public ShopController(
		Game root,
		GameState state,
		ShopService service)
	{
		_root =
			root;

		_state =
			state;

		_service =
			service;
	}

	// ==================================================
	// INITIALIZE
	// ==================================================

	public void Initialize()
	{
		_page =
			_root.GetNode<Control>(
				"ShopPage"
			);

		ClearOldContent();
		CreateUi();

		_root.GetViewport().SizeChanged +=
			ApplyOverlayMetrics;

		ApplyOverlayMetrics();

		Hide();
		Refresh();

		/*
		 * IMPORTANT:
		 *
		 * Do NOT initialize AdMob here.
		 *
		 * ShopController.Initialize() is called the first time
		 * the user taps SHOP. The previous implementation called
		 * MobileAds.Initialize() right here, which means a native
		 * AdMob/configuration problem can terminate Android before
		 * the shop even becomes visible.
		 *
		 * AdMob is now initialized only when the user actually
		 * presses the rewarded-video button.
		 */
	}

	private void ClearOldContent()
	{
		foreach (
			Node child
				in _page.GetChildren()
		)
		{
			_page.RemoveChild(
				child
			);

			child.QueueFree();
		}
	}

	// ==================================================
	// BUILD
	// ==================================================

	private void CreateUi()
	{
		CreateBackground();

		_pageMargin =
			new MarginContainer
			{
				Name =
					"ShopMargin",

				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};

		_pageMargin.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);

		_pageMargin.AddThemeConstantOverride(
			"margin_left",
			18
		);

		_pageMargin.AddThemeConstantOverride(
			"margin_top",
			0
		);

		_pageMargin.AddThemeConstantOverride(
			"margin_right",
			18
		);

		_pageMargin.AddThemeConstantOverride(
			"margin_bottom",
			0
		);

		_page.AddChild(
			_pageMargin
		);

		_layoutRoot =
			new Control
			{
				Name =
					"LayoutRoot",

				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,

				SizeFlagsVertical =
					Control.SizeFlags.ExpandFill,

				MouseFilter =
					Control.MouseFilterEnum.Pass
			};

		_pageMargin.AddChild(
			_layoutRoot
		);

		CreateScrollArea();
		CreateFixedHeader();
		CreateRewardedAdSection();
		CreateBoostSection();
		CreatePermanentSection();
		CreateCosmeticSection();
		CreateBottomSpace();
		CreateMobileScrolling();

		_header.MoveToFront();
	}

	private void CreateBackground()
	{
		TextureRect background =
			new()
			{
				Name =
					"ShopBackground",

				Texture =
					ShopUi.Background,

				ExpandMode =
					TextureRect.ExpandModeEnum.IgnoreSize,

				StretchMode =
					TextureRect.StretchModeEnum.KeepAspectCovered,

				MouseFilter =
					Control.MouseFilterEnum.Ignore,

				Modulate =
					new Color(
						0.70f,
						0.78f,
						0.90f,
						1.0f
					)
			};

		background.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);

		_page.AddChild(
			background
		);

		ColorRect tint =
			new()
			{
				Name =
					"ShopTint",

				Color =
					new Color(
						0.01f,
						0.025f,
						0.06f,
						0.42f
					),

				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};

		tint.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);

		_page.AddChild(
			tint
		);
	}

	// ==================================================
	// HEADER
	// ==================================================

	private void CreateFixedHeader()
	{
		_header =
			new VBoxContainer
			{
				Name =
					"FixedHeader",

				AnchorLeft =
					0.0f,

				AnchorTop =
					0.0f,

				AnchorRight =
					1.0f,

				AnchorBottom =
					0.0f,

				OffsetBottom =
					HeaderHeight,

				MouseFilter =
					Control.MouseFilterEnum.Pass
			};

		_header.AddThemeConstantOverride(
			"separation",
			HeaderGap
		);

		_layoutRoot.AddChild(
			_header
		);

		CreateShopTitlePanel();
		CreateShardPanel();
	}

	private void CreateShopTitlePanel()
	{
		PanelContainer panel =
			new()
			{
				CustomMinimumSize =
					new Vector2(
						0,
						84
					),

				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,

				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};

		panel.AddThemeStyleboxOverride(
			"panel",
			CreateShopTitleStyle()
		);

		_header.AddChild(
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
			8
		);

		margin.AddThemeConstantOverride(
			"margin_right",
			18
		);

		margin.AddThemeConstantOverride(
			"margin_bottom",
			8
		);

		panel.AddChild(
			margin
		);

		VBoxContainer box =
			new()
			{
				Alignment =
					BoxContainer.AlignmentMode.Center
			};

		margin.AddChild(
			box
		);

		Label title =
			ShopUi.CreateLabel(
				32
			);

		title.Text =
			"SHOP";

		title.AddThemeColorOverride(
			"font_color",
			Colors.White
		);

		box.AddChild(
			title
		);

		Label subtitle =
			ShopUi.CreateMutedLabel(
				13
			);

		subtitle.Text =
			"Upgrade your AI empire";

		box.AddChild(
			subtitle
		);
	}

	private void CreateShardPanel()
	{
		PanelContainer panel =
			new()
			{
				CustomMinimumSize =
					new Vector2(
						0,
						96
					),

				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,

				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};

		panel.AddThemeStyleboxOverride(
			"panel",
			ShopUi.CreateShardPanelStyle()
		);

		_header.AddChild(
			panel
		);

		MarginContainer margin =
			new();

		margin.AddThemeConstantOverride(
			"margin_left",
			18
		);

		margin.AddThemeConstantOverride(
			"margin_right",
			18
		);

		margin.AddThemeConstantOverride(
			"margin_top",
			10
		);

		margin.AddThemeConstantOverride(
			"margin_bottom",
			10
		);

		panel.AddChild(
			margin
		);

		HBoxContainer row =
			new()
			{
				Alignment =
					BoxContainer.AlignmentMode.Center
			};

		row.AddThemeConstantOverride(
			"separation",
			14
		);

		margin.AddChild(
			row
		);

		TextureRect icon =
			CreateIcon(
				ShopUi.DataShard,
				58
			);

		row.AddChild(
			icon
		);

		VBoxContainer text =
			new();

		row.AddChild(
			text
		);

		Label title =
			ShopUi.CreateMutedLabel(
				12
			);

		title.Text =
			"DATA SHARDS";

		title.HorizontalAlignment =
			HorizontalAlignment.Left;

		text.AddChild(
			title
		);

		_shardLabel =
			ShopUi.CreateLabel(
				26
			);

		_shardLabel.CustomMinimumSize =
			new Vector2(
				220,
				42
			);

		_shardLabel.HorizontalAlignment =
			HorizontalAlignment.Left;

		_shardLabel.AddThemeColorOverride(
			"font_color",
			ShopUi.Accent
		);

		text.AddChild(
			_shardLabel
		);
	}

	private static StyleBoxFlat CreateShopTitleStyle()
	{
		return new StyleBoxFlat
		{
			BgColor =
				new Color(
					0.018f,
					0.045f,
					0.085f,
					0.98f
				),

			BorderColor =
				new Color(
					ShopUi.Accent.R,
					ShopUi.Accent.G,
					ShopUi.Accent.B,
					0.70f
				),

			BorderWidthLeft =
				2,

			BorderWidthTop =
				2,

			BorderWidthRight =
				2,

			BorderWidthBottom =
				2,

			CornerRadiusTopLeft =
				22,

			CornerRadiusTopRight =
				22,

			CornerRadiusBottomLeft =
				22,

			CornerRadiusBottomRight =
				22
		};
	}

	// ==================================================
	// SCROLL
	// ==================================================

	private void CreateScrollArea()
	{
		_scroll =
			new ScrollContainer
			{
				Name =
					"Scroll",

				AnchorLeft =
					0.0f,

				AnchorTop =
					0.0f,

				AnchorRight =
					1.0f,

				AnchorBottom =
					1.0f,

				HorizontalScrollMode =
					ScrollContainer.ScrollMode.Disabled,

				VerticalScrollMode =
					ScrollContainer.ScrollMode.ShowNever,

				ClipContents =
					true,

				MouseFilter =
					Control.MouseFilterEnum.Pass
			};

		_layoutRoot.AddChild(
			_scroll
		);

		_scrollMargin =
			new MarginContainer
			{
				Name =
					"ScrollMargin",

				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};

		_scrollMargin.AddThemeConstantOverride(
			"margin_top",
			0
		);

		_scrollMargin.AddThemeConstantOverride(
			"margin_bottom",
			24
		);

		_scroll.AddChild(
			_scrollMargin
		);

		_content =
			new VBoxContainer
			{
				Name =
					"Content",

				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};

		_content.AddThemeConstantOverride(
			"separation",
			18
		);

		_scrollMargin.AddChild(
			_content
		);
	}

	private void ApplyOverlayMetrics()
	{
		if (
			_header == null
			|| _scrollMargin == null
		)
		{
			return;
		}

		float safeTop =
			GetSafeTopInset();

		float headerTop =
			safeTop
			+ HeaderExtraTopPadding;

		_header.OffsetTop =
			headerTop;

		_header.OffsetBottom =
			headerTop
			+ HeaderHeight;

		_scrollMargin.AddThemeConstantOverride(
			"margin_top",
			(int)MathF.Ceiling(
				headerTop
				+ HeaderHeight
				+ HeaderGap
				+ 10.0f
			)
		);

		_header.MoveToFront();
	}

	private float GetSafeTopInset()
	{
		string os =
			OS.GetName();

		if (
			os != "Android"
			&& os != "iOS"
		)
		{
			return 0.0f;
		}

		Rect2I safeArea =
			DisplayServer.GetDisplaySafeArea();

		Vector2I windowSize =
			DisplayServer.WindowGetSize();

		Rect2 viewportRect =
			_root.GetViewport()
				.GetVisibleRect();

		if (
			windowSize.X <= 0
			|| windowSize.Y <= 0
			|| safeArea.Size.X <= 0
			|| safeArea.Size.Y <= 0
		)
		{
			return 34.0f;
		}

		float scaleY =
			viewportRect.Size.Y
			/ windowSize.Y;

		float top =
			safeArea.Position.Y
			* scaleY;

		return MathF.Max(
			top,
			26.0f
		);
	}

	// ==================================================
	// REWARDED ADS
	// ==================================================

	private void CreateRewardedAdSection()
	{
		CreateSectionTitle(
			"FREE SHARDS",
			"Watch a rewarded video and collect Data Shards."
		);

		_rewardedAdStatus =
			ShopUi.CreateMutedLabel(
				13
			);

		_rewardedAdButton =
			CreateShopCard(
				FiveShardsIcon,
				"5 DATA SHARDS",
				"Watch one video and receive 5 Data Shards.",
				_rewardedAdStatus,
				ShopUi.Gold,
				ShowRewardedVideo
			);

		RefreshRewardedAdOffer();
	}

	private void EnsureAdMobInitialized()
	{
		if (_adMobInitialized)
			return;

		/*
		 * The project.godot included with this fix contains
		 * Poing v5's required test App IDs.
		 *
		 * This method is deliberately reached only after the
		 * user presses the rewarded-video button. Opening SHOP
		 * itself never calls into the native ads plugin.
		 */
		AdMobMobileAds.Initialize();

		_adMobInitialized =
			true;
	}

	private void LoadRewardedAd()
	{
		if (
			_rewardedAdLoading
			|| _rewardedAdShowing
		)
		{
			return;
		}

		EnsureAdMobInitialized();

		DestroyRewardedAd();

		_rewardedAdLoading =
			true;

		RefreshRewardedAdOffer();

		AdMobRewardedAdLoadCallback loadCallback =
			new()
			{
				OnAdLoaded =
					ad =>
					{
						_rewardedAdLoading =
							false;

						_rewardedAd =
							ad;

						ConfigureRewardedAdCallbacks();
						RefreshRewardedAdOffer();
					},

				OnAdFailedToLoad =
					error =>
					{
						_rewardedAdLoading =
							false;

						_rewardedAd =
							null!;

						GD.PushWarning(
							"Rewarded Ad failed to load: "
							+ error.Message
						);

						RefreshRewardedAdOffer();
					}
			};

		new AdMobRewardedAdLoader()
			.Load(
				GetRewardedAdUnitId(),
				new AdMobAdRequest(),
				loadCallback
			);
	}

	private void ConfigureRewardedAdCallbacks()
	{
		if (_rewardedAd == null)
			return;

		_rewardedFullScreenCallback =
			new AdMobFullScreenContentCallback
			{
				OnAdDismissedFullScreenContent =
					() =>
					{
						_rewardedAdShowing =
							false;

						DestroyRewardedAd();
						LoadRewardedAd();
					},

				OnAdFailedToShowFullScreenContent =
					error =>
					{
						_rewardedAdShowing =
							false;

						MessageRequested?.Invoke(
							"Video could not be shown. Please try again."
						);

						GD.PushWarning(
							"Rewarded Ad failed to show: "
							+ error.Message
						);

						DestroyRewardedAd();
					}
			};

		_rewardedAd.FullScreenContentCallback =
			_rewardedFullScreenCallback;
	}

	private void ShowRewardedVideo()
	{
		if (_rewardedAdShowing)
			return;

		if (_rewardedAd == null)
		{
			if (!_rewardedAdLoading)
			{
				LoadRewardedAd();
			}

			MessageRequested?.Invoke(
				"Reward video is loading. Tap again when it is ready."
			);

			return;
		}

		_rewardGrantedForCurrentAd =
			false;

		_rewardedAdShowing =
			true;

		RefreshRewardedAdOffer();

		_rewardListener =
			new AdMobRewardListener
			{
				OnUserEarnedReward =
					_reward =>
						GrantRewardedAdShards()
			};

		_rewardedAd.Show(
			_rewardListener
		);
	}

	private void GrantRewardedAdShards()
	{
		if (_rewardGrantedForCurrentAd)
			return;

		_rewardGrantedForCurrentAd =
			true;

		HandleResult(
			_service.GrantDataShards(
				RewardedAdShardReward
			)
		);
	}

	private void DestroyRewardedAd()
	{
		if (_rewardedAd == null)
			return;

		_rewardedAd.Destroy();

		_rewardedAd =
			null!;
	}

	private string GetRewardedAdUnitId()
	{
		return OS.GetName() == "iOS"
			? IosRewardedAdUnitId
			: AndroidRewardedAdUnitId;
	}

	private void RefreshRewardedAdOffer()
	{
		if (
			_rewardedAdStatus == null
			|| _rewardedAdButton == null
		)
		{
			return;
		}

		if (_rewardedAdShowing)
		{
			_rewardedAdStatus.Text =
				"COMPLETE THE VIDEO TO CLAIM YOUR REWARD";

			_rewardedAdStatus.AddThemeColorOverride(
				"font_color",
				ShopUi.Gold
			);

			_rewardedAdButton.Text =
				"VIDEO PLAYING...";

			_rewardedAdButton.Disabled =
				true;

			return;
		}

		if (_rewardedAd != null)
		{
			_rewardedAdStatus.Text =
				"VIDEO READY";

			_rewardedAdStatus.AddThemeColorOverride(
				"font_color",
				ShopUi.Green
			);

			_rewardedAdButton.Text =
				"WATCH VIDEO  •  +5 SHARDS";

			_rewardedAdButton.Disabled =
				false;

			return;
		}

		if (_rewardedAdLoading)
		{
			_rewardedAdStatus.Text =
				"LOADING REWARDED VIDEO...";

			_rewardedAdStatus.AddThemeColorOverride(
				"font_color",
				ShopUi.TextSecondary
			);

			_rewardedAdButton.Text =
				"LOADING VIDEO...";

			_rewardedAdButton.Disabled =
				true;

			return;
		}

		_rewardedAdStatus.Text =
			"VIDEO LOADS ONLY WHEN REQUESTED";

		_rewardedAdStatus.AddThemeColorOverride(
			"font_color",
			ShopUi.TextSecondary
		);

		_rewardedAdButton.Text =
			"LOAD REWARDED VIDEO";

		_rewardedAdButton.Disabled =
			false;
	}

	// ==================================================
	// BOOSTS
	// ==================================================

	private void CreateBoostSection()
	{
		CreateSectionTitle(
			"BOOSTS",
			"Temporary upgrades for faster progress."
		);

		_productionBoostStatus =
			ShopUi.CreateMutedLabel(
				13
			);

		_productionBoostButton =
			CreateShopCard(
				ShopUi.Boost,
				"2x PRODUCTION",
				"Double all production for 15 minutes.",
				_productionBoostStatus,
				ShopUi.Accent,
				BuyProductionBoost
			);

		_luckStatus =
			ShopUi.CreateMutedLabel(
				13
			);

		_luckButton =
			CreateShopCard(
				ShopUi.Luck,
				"BOT LUCK",
				"Better chances for Rare, Epic and Legendary bots.",
				_luckStatus,
				ShopUi.Purple,
				BuyLuck
			);

		Label instantStatus =
			ShopUi.CreateMutedLabel(
				13
			);

		instantStatus.Text =
			"Finishes all currently running cycles.";

		_instantButton =
			CreateShopCard(
				ShopUi.Instant,
				"INSTANT PRODUCTION",
				"Complete all active production cycles immediately.",
				instantStatus,
				ShopUi.Gold,
				BuyInstant
			);
	}

	private void CreatePermanentSection()
	{
		CreateSectionTitle(
			"PERMANENT UPGRADES",
			"These upgrades remain forever."
		);

		_productionUpgradeStatus =
			ShopUi.CreateMutedLabel(
				13
			);

		_productionUpgradeButton =
			CreateShopCard(
				ShopUi.Production,
				"GLOBAL PRODUCTION",
				"+2% permanent production per level.",
				_productionUpgradeStatus,
				ShopUi.Green,
				BuyProductionUpgrade
			);

		_offlineUpgradeStatus =
			ShopUi.CreateMutedLabel(
				13
			);

		_offlineUpgradeButton =
			CreateShopCard(
				ShopUi.Offline,
				"OFFLINE INCOME",
				"+5% offline income per level.",
				_offlineUpgradeStatus,
				ShopUi.AccentSoft,
				BuyOfflineUpgrade
			);
	}

	// ==================================================
	// COSMETICS PLACEHOLDER
	// ==================================================

	private void CreateCosmeticSection()
	{
		/*
		 * BotSkinShopController intentionally looks for this
		 * placeholder and replaces it with the BOT SKINS
		 * collection entry.
		 */
		CreateSectionTitle(
			"COSMETICS",
			"Visual customization for your AI empire."
		);

		Label status =
			ShopUi.CreateMutedLabel(
				13
			);

		status.Text =
			"Machine skins, themes and particle effects.";

		Button button =
			CreateShopCard(
				ShopUi.Cosmetics,
				"COSMETICS",
				"Customize machines and environments.",
				status,
				ShopUi.Purple,
				() =>
				{
					MessageRequested?.Invoke(
						"Cosmetics coming soon."
					);
				}
			);

		button.Text =
			"COMING SOON";

		button.Disabled =
			true;
	}

	// ==================================================
	// CARD
	// ==================================================

	private Button CreateShopCard(
		Texture2D iconTexture,
		string title,
		string description,
		Label status,
		Color accent,
		Action pressed)
	{
		PanelContainer panel =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,

				MouseFilter =
					Control.MouseFilterEnum.Pass
			};

		panel.AddThemeStyleboxOverride(
			"panel",
			ShopUi.CreatePanelStyle()
		);

		_content.AddChild(
			panel
		);

		MarginContainer margin =
			new()
			{
				MouseFilter =
					Control.MouseFilterEnum.Pass
			};

		margin.AddThemeConstantOverride(
			"margin_left",
			14
		);

		margin.AddThemeConstantOverride(
			"margin_top",
			14
		);

		margin.AddThemeConstantOverride(
			"margin_right",
			14
		);

		margin.AddThemeConstantOverride(
			"margin_bottom",
			14
		);

		panel.AddChild(
			margin
		);

		HBoxContainer row =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,

				MouseFilter =
					Control.MouseFilterEnum.Pass
			};

		row.AddThemeConstantOverride(
			"separation",
			14
		);

		margin.AddChild(
			row
		);

		PanelContainer iconPanel =
			new()
			{
				CustomMinimumSize =
					new Vector2(
						102,
						102
					),

				SizeFlagsVertical =
					Control.SizeFlags.ShrinkCenter,

				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};

		StyleBoxFlat iconStyle =
			new()
			{
				BgColor =
					new Color(
						accent.R,
						accent.G,
						accent.B,
						0.12f
					),

				BorderColor =
					new Color(
						accent.R,
						accent.G,
						accent.B,
						0.56f
					),

				BorderWidthLeft =
					2,

				BorderWidthTop =
					2,

				BorderWidthRight =
					2,

				BorderWidthBottom =
					2,

				CornerRadiusTopLeft =
					18,

				CornerRadiusTopRight =
					18,

				CornerRadiusBottomLeft =
					18,

				CornerRadiusBottomRight =
					18
			};

		iconPanel.AddThemeStyleboxOverride(
			"panel",
			iconStyle
		);

		row.AddChild(
			iconPanel
		);

		TextureRect icon =
			CreateIcon(
				iconTexture,
				82
			);

		iconPanel.AddChild(
			icon
		);

		icon.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);

		icon.OffsetLeft =
			10;

		icon.OffsetTop =
			10;

		icon.OffsetRight =
			-10;

		icon.OffsetBottom =
			-10;

		VBoxContainer information =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};

		information.AddThemeConstantOverride(
			"separation",
			5
		);

		row.AddChild(
			information
		);

		Label titleLabel =
			ShopUi.CreateLabel(
				19
			);

		titleLabel.Text =
			title;

		titleLabel.HorizontalAlignment =
			HorizontalAlignment.Left;

		titleLabel.AddThemeColorOverride(
			"font_color",
			accent
		);

		information.AddChild(
			titleLabel
		);

		Label descriptionLabel =
			ShopUi.CreateMutedLabel(
				13
			);

		descriptionLabel.Text =
			description;

		descriptionLabel.HorizontalAlignment =
			HorizontalAlignment.Left;

		information.AddChild(
			descriptionLabel
		);

		status.HorizontalAlignment =
			HorizontalAlignment.Left;

		status.CustomMinimumSize =
			new Vector2(
				0,
				28
			);

		information.AddChild(
			status
		);

		Button button =
			new()
			{
				CustomMinimumSize =
					new Vector2(
						0,
						52
					),

				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,

				FocusMode =
					Control.FocusModeEnum.None
			};

		ShopUi.ApplyPrimaryButtonStyle(
			button
		);

		button.Pressed +=
			() =>
			{
				if (ShopActionBlocked())
					return;

				pressed();
			};

		information.AddChild(
			button
		);

		return button;
	}

	private void CreateSectionTitle(
		string title,
		string subtitle)
	{
		PanelContainer panel =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,

				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};

		panel.AddThemeStyleboxOverride(
			"panel",
			ShopUi.CreateSectionStyle()
		);

		_content.AddChild(
			panel
		);

		MarginContainer margin =
			new();

		margin.AddThemeConstantOverride(
			"margin_left",
			14
		);

		margin.AddThemeConstantOverride(
			"margin_right",
			14
		);

		margin.AddThemeConstantOverride(
			"margin_top",
			8
		);

		margin.AddThemeConstantOverride(
			"margin_bottom",
			8
		);

		panel.AddChild(
			margin
		);

		VBoxContainer box =
			new();

		margin.AddChild(
			box
		);

		Label titleLabel =
			ShopUi.CreateLabel(
				19
			);

		titleLabel.Text =
			title;

		titleLabel.HorizontalAlignment =
			HorizontalAlignment.Left;

		titleLabel.AddThemeColorOverride(
			"font_color",
			ShopUi.Accent
		);

		box.AddChild(
			titleLabel
		);

		Label subtitleLabel =
			ShopUi.CreateMutedLabel(
				12
			);

		subtitleLabel.Text =
			subtitle;

		subtitleLabel.HorizontalAlignment =
			HorizontalAlignment.Left;

		box.AddChild(
			subtitleLabel
		);
	}

	private static TextureRect CreateIcon(
		Texture2D texture,
		float size)
	{
		return new TextureRect
		{
			Texture =
				texture,

			CustomMinimumSize =
				new Vector2(
					size,
					size
				),

			ExpandMode =
				TextureRect.ExpandModeEnum.IgnoreSize,

			StretchMode =
				TextureRect.StretchModeEnum.KeepAspectCentered,

			MouseFilter =
				Control.MouseFilterEnum.Ignore
		};
	}

	private void CreateBottomSpace()
	{
		_content.AddChild(
			new Control
			{
				CustomMinimumSize =
					new Vector2(
						0,
						BottomScrollPadding
					),

				MouseFilter =
					Control.MouseFilterEnum.Ignore
			}
		);
	}

	private void CreateMobileScrolling()
	{
		_mobileScroll =
			new MobileScrollController
			{
				Name =
					"ShopMobileScroll"
			};

		_page.AddChild(
			_mobileScroll
		);

		_mobileScroll.Setup(
			_scroll
		);
	}

	private bool ShopActionBlocked()
	{
		return _mobileScroll != null
			&& _mobileScroll.ShouldSuppressTap;
	}

	// ==================================================
	// ACTIONS
	// ==================================================

	private void BuyProductionBoost()
	{
		HandleResult(
			_service.BuyProductionBoost()
		);
	}

	private void BuyLuck()
	{
		HandleResult(
			_service.BuyBotLuck()
		);
	}

	private void BuyInstant()
	{
		HandleResult(
			_service.BuyInstantProduction()
		);
	}

	private void BuyProductionUpgrade()
	{
		HandleResult(
			_service.BuyProductionUpgrade()
		);
	}

	private void BuyOfflineUpgrade()
	{
		HandleResult(
			_service.BuyOfflineUpgrade()
		);
	}

	private void HandleResult(
		ShopResult result)
	{
		MessageRequested?.Invoke(
			result.Message
		);

		Refresh();

		if (!result.Changed)
			return;

		Input.VibrateHandheld(
			14,
			0.12f
		);

		StateChanged?.Invoke();
	}

	// ==================================================
	// PAGE
	// ==================================================

	public void Open()
	{
		if (_page == null)
			return;

		ApplyOverlayMetrics();
		Refresh();

		_mobileScroll?.ScrollToTop();

		_page.Show();
		_page.MoveToFront();
	}

	public void Hide()
	{
		_mobileScroll?.ResetMotion();
		_page?.Hide();
	}

	// ==================================================
	// REFRESH
	// ==================================================

	public void Refresh()
	{
		if (
			_page == null
			|| _shardLabel == null
		)
		{
			return;
		}

		_shardLabel.Text =
			NumberFormatter.Format(
				_state.Shop.DataShards
			);

		RefreshRewardedAdOffer();
		RefreshProductionBoost();
		RefreshLuck();
		RefreshProductionUpgrade();
		RefreshOfflineUpgrade();
		RefreshInstantProduction();
	}

	private void RefreshProductionBoost()
	{
		double remaining =
			_state.Shop
				.GetProductionBoostRemainingSeconds();

		if (remaining > 0)
		{
			_productionBoostStatus.Text =
				"● ACTIVE  •  "
				+ FormatTime(
					remaining
				);

			_productionBoostStatus.AddThemeColorOverride(
				"font_color",
				ShopUi.Green
			);
		}
		else
		{
			_productionBoostStatus.Text =
				"INACTIVE";

			_productionBoostStatus.AddThemeColorOverride(
				"font_color",
				ShopUi.TextSecondary
			);
		}

		_productionBoostButton.Text =
			"BUY  •  "
			+ NumberFormatter.Format(
				GameConfig.ShopProductionBoostCost
			)
			+ " SHARDS";

		_productionBoostButton.Disabled =
			_state.Shop.DataShards
			< GameConfig.ShopProductionBoostCost;
	}

	private void RefreshLuck()
	{
		double remaining =
			_state.Shop
				.GetBotLuckRemainingSeconds();

		if (remaining > 0)
		{
			_luckStatus.Text =
				"● ACTIVE  •  "
				+ FormatTime(
					remaining
				);

			_luckStatus.AddThemeColorOverride(
				"font_color",
				ShopUi.Green
			);
		}
		else
		{
			_luckStatus.Text =
				"INACTIVE";

			_luckStatus.AddThemeColorOverride(
				"font_color",
				ShopUi.TextSecondary
			);
		}

		_luckButton.Text =
			"BUY  •  "
			+ NumberFormatter.Format(
				GameConfig.ShopBotLuckCost
			)
			+ " SHARDS";

		_luckButton.Disabled =
			_state.Shop.DataShards
			< GameConfig.ShopBotLuckCost;
	}

	private void RefreshInstantProduction()
	{
		_instantButton.Text =
			"USE  •  "
			+ NumberFormatter.Format(
				GameConfig.ShopInstantProductionCost
			)
			+ " SHARDS";

		_instantButton.Disabled =
			_state.Shop.DataShards
			< GameConfig.ShopInstantProductionCost;
	}

	private void RefreshProductionUpgrade()
	{
		int level =
			_state.Shop
				.ProductionUpgradeLevel;

		_productionUpgradeStatus.Text =
			"LEVEL "
			+ level
			+ " / "
			+ GameConfig.ShopProductionUpgradeMaxLevel
			+ "   •   +"
			+ (
				level
				* GameConfig.ShopProductionUpgradeBonus
				* 100.0
			).ToString(
				"0"
			)
			+ "%";

		if (
			level
			>= GameConfig.ShopProductionUpgradeMaxLevel
		)
		{
			_productionUpgradeButton.Text =
				"MAX LEVEL";

			_productionUpgradeButton.Disabled =
				true;

			return;
		}

		double cost =
			_service.GetProductionUpgradeCost();

		_productionUpgradeButton.Text =
			"UPGRADE  •  "
			+ NumberFormatter.Format(
				cost
			)
			+ " SHARDS";

		_productionUpgradeButton.Disabled =
			_state.Shop.DataShards
			< cost;
	}

	private void RefreshOfflineUpgrade()
	{
		int level =
			_state.Shop
				.OfflineUpgradeLevel;

		_offlineUpgradeStatus.Text =
			"LEVEL "
			+ level
			+ " / "
			+ GameConfig.ShopOfflineUpgradeMaxLevel
			+ "   •   +"
			+ (
				level
				* GameConfig.ShopOfflineUpgradeBonus
				* 100.0
			).ToString(
				"0"
			)
			+ "%";

		if (
			level
			>= GameConfig.ShopOfflineUpgradeMaxLevel
		)
		{
			_offlineUpgradeButton.Text =
				"MAX LEVEL";

			_offlineUpgradeButton.Disabled =
				true;

			return;
		}

		double cost =
			_service.GetOfflineUpgradeCost();

		_offlineUpgradeButton.Text =
			"UPGRADE  •  "
			+ NumberFormatter.Format(
				cost
			)
			+ " SHARDS";

		_offlineUpgradeButton.Disabled =
			_state.Shop.DataShards
			< cost;
	}

	private static string FormatTime(
		double seconds)
	{
		int total =
			Math.Max(
				0,
				(int)Math.Ceiling(
					seconds
				)
			);

		return
			$"{total / 60}:{total % 60:00}";
	}
}
