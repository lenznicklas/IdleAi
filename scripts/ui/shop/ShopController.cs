using Godot;
using System;

namespace IdleAi;

public sealed class ShopController
{
	private readonly Game _root;

	private readonly GameState _state;

	private readonly ShopService _service;


	private Control _page =
		null!;


	private MarginContainer _pageMargin =
		null!;


	private ScrollContainer _scroll =
		null!;


	private VBoxContainer _content =
		null!;


	private MobileScrollController _mobileScroll =
		null!;


	private Label _shardLabel =
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

		Hide();

		Refresh();
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
					Control.MouseFilterEnum.Pass
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


		VBoxContainer main =
			new()
			{
				Name =
					"Main",

				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,

				SizeFlagsVertical =
					Control.SizeFlags.ExpandFill
			};


		main.AddThemeConstantOverride(
			"separation",
			8
		);


		_pageMargin.AddChild(
			main
		);


		CreateHeader(
			main
		);


		_scroll =
			new ScrollContainer
			{
				Name =
					"Scroll",

				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,

				SizeFlagsVertical =
					Control.SizeFlags.ExpandFill,

				HorizontalScrollMode =
					ScrollContainer.ScrollMode.Disabled,

				VerticalScrollMode =
					ScrollContainer.ScrollMode.ShowNever,

				ClipContents =
					true,

				MouseFilter =
					Control.MouseFilterEnum.Pass
			};


		main.AddChild(
			_scroll
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


		_scroll.AddChild(
			_content
		);


		CreateBoostSection();

		CreatePermanentSection();

		CreateCosmeticSection();


		Control bottomSpace =
			new()
			{
				CustomMinimumSize =
					new Vector2(
						0,
						28
					),

				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};


		_content.AddChild(
			bottomSpace
		);


		CreateMobileScrolling();
	}


	// ==================================================
	// BACKGROUND
	// ==================================================

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

	private void CreateHeader(
		VBoxContainer parent)
	{
		VBoxContainer header =
			new()
			{
				Name =
					"Header",

				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};


		header.AddThemeConstantOverride(
			"separation",
			7
		);


		parent.AddChild(
			header
		);


		Label title =
			ShopUi.CreateLabel(
				34
			);


		title.Name =
			"Title";


		title.Text =
			"SHOP";


		title.CustomMinimumSize =
			new Vector2(
				0,
				48
			);


		title.AddThemeColorOverride(
			"font_color",
			Colors.White
		);


		title.AddThemeColorOverride(
			"font_shadow_color",
			new Color(
				0.05f,
				0.55f,
				1.0f,
				0.9f
			)
		);


		title.AddThemeConstantOverride(
			"shadow_offset_x",
			2
		);


		title.AddThemeConstantOverride(
			"shadow_offset_y",
			2
		);


		header.AddChild(
			title
		);


		Label subtitle =
			ShopUi.CreateMutedLabel(
				14
			);


		subtitle.Text =
			"Upgrade your AI empire";


		subtitle.CustomMinimumSize =
			new Vector2(
				0,
				24
			);


		header.AddChild(
			subtitle
		);


		PanelContainer shardPanel =
			new()
			{
				Name =
					"ShardPanel",

				CustomMinimumSize =
					new Vector2(
						0,
						86
					),

				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,

				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};


		shardPanel.AddThemeStyleboxOverride(
			"panel",
			ShopUi.CreateShardPanelStyle()
		);


		header.AddChild(
			shardPanel
		);


		MarginContainer shardMargin =
			new();


		shardMargin.AddThemeConstantOverride(
			"margin_left",
			18
		);


		shardMargin.AddThemeConstantOverride(
			"margin_right",
			18
		);


		shardMargin.AddThemeConstantOverride(
			"margin_top",
			10
		);


		shardMargin.AddThemeConstantOverride(
			"margin_bottom",
			10
		);


		shardPanel.AddChild(
			shardMargin
		);


		HBoxContainer row =
			new()
			{
				Alignment =
					BoxContainer.AlignmentMode.Center,

				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,

				SizeFlagsVertical =
					Control.SizeFlags.ExpandFill
			};


		row.AddThemeConstantOverride(
			"separation",
			14
		);


		shardMargin.AddChild(
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


		VBoxContainer shardText =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ShrinkCenter
			};


		shardText.AddThemeConstantOverride(
			"separation",
			0
		);


		row.AddChild(
			shardText
		);


		Label shardTitle =
			ShopUi.CreateMutedLabel(
				12
			);


		shardTitle.Text =
			"DATA SHARDS";


		shardTitle.HorizontalAlignment =
			HorizontalAlignment.Left;


		shardText.AddChild(
			shardTitle
		);


		_shardLabel =
			ShopUi.CreateLabel(
				26
			);


		_shardLabel.AutowrapMode =
			TextServer.AutowrapMode.Off;


		_shardLabel.CustomMinimumSize =
			new Vector2(
				220,
				42
			);


		_shardLabel.SizeFlagsHorizontal =
			Control.SizeFlags.ShrinkCenter;


		_shardLabel.HorizontalAlignment =
			HorizontalAlignment.Left;


		_shardLabel.AddThemeColorOverride(
			"font_color",
			ShopUi.Accent
		);


		shardText.AddChild(
			_shardLabel
		);
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


	// ==================================================
	// PERMANENT
	// ==================================================

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
	// COSMETICS
	// ==================================================

	private void CreateCosmeticSection()
	{
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


		CenterContainer iconCenter =
			new();


		iconPanel.AddChild(
			iconCenter
		);


		TextureRect icon =
			CreateIcon(
				iconTexture,
				82
			);


		iconCenter.AddChild(
			icon
		);


		VBoxContainer information =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,

				MouseFilter =
					Control.MouseFilterEnum.Pass
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


		descriptionLabel.VerticalAlignment =
			VerticalAlignment.Top;


		information.AddChild(
			descriptionLabel
		);


		status.HorizontalAlignment =
			HorizontalAlignment.Left;


		status.VerticalAlignment =
			VerticalAlignment.Center;


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


	// ==================================================
	// SECTION
	// ==================================================

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


		box.AddThemeConstantOverride(
			"separation",
			1
		);


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


	// ==================================================
	// ICON
	// ==================================================

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


	// ==================================================
	// MOBILE SCROLL
	// ==================================================

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
			_service
				.GetProductionUpgradeCost();


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
			_service
				.GetOfflineUpgradeCost();


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


	// ==================================================
	// TIME
	// ==================================================

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
