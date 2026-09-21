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
					"ShopMargin"
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
			18
		);


		_pageMargin.AddThemeConstantOverride(
			"margin_right",
			18
		);


		_pageMargin.AddThemeConstantOverride(
			"margin_bottom",
			12
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
			12
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
					true
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
			14
		);


		_scroll.AddChild(
			_content
		);


		CreateBoostSection();

		CreatePermanentSection();

		CreateCosmeticSection();

		CreateBottomPadding();


		CreateMobileScrolling();
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
					Control.MouseFilterEnum.Ignore
			};


		background.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);


		_page.AddChild(
			background
		);
	}


	// ==================================================
	// HEADER
	// ==================================================

	private void CreateHeader(
		VBoxContainer parent)
	{
		Label title =
			ShopUi.CreateLabel(
				30
			);


		title.Name =
			"Title";


		title.Text =
			"SHOP";


		parent.AddChild(
			title
		);


		PanelContainer shardPanel =
			new()
			{
				Name =
					"ShardPanel",

				CustomMinimumSize =
					new Vector2(
						0,
						76
					),

				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};


		shardPanel.AddThemeStyleboxOverride(
			"panel",
			ShopUi.CreatePanelStyle()
		);


		parent.AddChild(
			shardPanel
		);


		MarginContainer shardMargin =
			new();


		shardMargin.AddThemeConstantOverride(
			"margin_left",
			16
		);


		shardMargin.AddThemeConstantOverride(
			"margin_right",
			16
		);


		shardMargin.AddThemeConstantOverride(
			"margin_top",
			8
		);


		shardMargin.AddThemeConstantOverride(
			"margin_bottom",
			8
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
			12
		);


		shardMargin.AddChild(
			row
		);


		TextureRect icon =
			CreateIcon(
				ShopUi.DataShard,
				52
			);


		row.AddChild(
			icon
		);


		_shardLabel =
			ShopUi.CreateLabel(
				22
			);


		/*
		 * IMPORTANT:
		 *
		 * Data Shards must always stay on one line.
		 * ShopUi.CreateLabel() normally enables
		 * WordSmart wrapping, which caused the text
		 * to be displayed vertically on narrow layouts.
		 */
		_shardLabel.AutowrapMode =
			TextServer.AutowrapMode.Off;


		_shardLabel.CustomMinimumSize =
			new Vector2(
				260,
				52
			);


		_shardLabel.SizeFlagsHorizontal =
			Control.SizeFlags.ShrinkCenter;


		_shardLabel.SizeFlagsVertical =
			Control.SizeFlags.ShrinkCenter;


		_shardLabel.HorizontalAlignment =
			HorizontalAlignment.Left;


		_shardLabel.VerticalAlignment =
			VerticalAlignment.Center;


		_shardLabel.ClipText =
			false;


		row.AddChild(
			_shardLabel
		);
	}


	// ==================================================
	// BOOSTS
	// ==================================================

	private void CreateBoostSection()
	{
		CreateSectionTitle(
			"BOOSTS"
		);


		_productionBoostStatus =
			ShopUi.CreateLabel(
				13
			);


		_productionBoostButton =
			CreateShopCard(
				ShopUi.Boost,
				"2x PRODUCTION",
				"Double all production for 15 minutes.",
				_productionBoostStatus,
				BuyProductionBoost
			);


		_luckStatus =
			ShopUi.CreateLabel(
				13
			);


		_luckButton =
			CreateShopCard(
				ShopUi.Luck,
				"BOT LUCK",
				"Improves Rare, Epic and Legendary chances for 10 minutes.",
				_luckStatus,
				BuyLuck
			);


		Label instantStatus =
			ShopUi.CreateLabel(
				13
			);


		instantStatus.Text =
			"Complete every currently running cycle.";


		_instantButton =
			CreateShopCard(
				ShopUi.Instant,
				"INSTANT PRODUCTION",
				"Finish all active production cycles immediately.",
				instantStatus,
				BuyInstant
			);
	}


	// ==================================================
	// PERMANENT
	// ==================================================

	private void CreatePermanentSection()
	{
		CreateSectionTitle(
			"PERMANENT UPGRADES"
		);


		_productionUpgradeStatus =
			ShopUi.CreateLabel(
				13
			);


		_productionUpgradeButton =
			CreateShopCard(
				ShopUi.Production,
				"GLOBAL PRODUCTION",
				"+2% permanent production per level.",
				_productionUpgradeStatus,
				BuyProductionUpgrade
			);


		_offlineUpgradeStatus =
			ShopUi.CreateLabel(
				13
			);


		_offlineUpgradeButton =
			CreateShopCard(
				ShopUi.Offline,
				"OFFLINE INCOME",
				"+5% offline income per level.",
				_offlineUpgradeStatus,
				BuyOfflineUpgrade
			);
	}


	// ==================================================
	// COSMETICS
	// ==================================================

	private void CreateCosmeticSection()
	{
		CreateSectionTitle(
			"COSMETICS"
		);


		Label status =
			ShopUi.CreateLabel(
				13
			);


		status.Text =
			"More themes and particle styles will be added later.";


		Button button =
			CreateShopCard(
				ShopUi.Cosmetics,
				"COSMETICS",
				"Machine skins, themes and particles.",
				status,
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
		Action pressed)
	{
		PanelContainer panel =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};


		panel.AddThemeStyleboxOverride(
			"panel",
			ShopUi.CreatePanelStyle()
		);


		_content.AddChild(
			panel
		);


		MarginContainer margin =
			new();


		margin.AddThemeConstantOverride(
			"margin_left",
			16
		);


		margin.AddThemeConstantOverride(
			"margin_top",
			14
		);


		margin.AddThemeConstantOverride(
			"margin_right",
			16
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
					Control.SizeFlags.ExpandFill
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
				iconTexture,
				92
			);


		row.AddChild(
			icon
		);


		VBoxContainer information =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};


		information.AddThemeConstantOverride(
			"separation",
			6
		);


		row.AddChild(
			information
		);


		Label titleLabel =
			ShopUi.CreateLabel(
				20
			);


		titleLabel.Text =
			title;


		information.AddChild(
			titleLabel
		);


		Label descriptionLabel =
			ShopUi.CreateLabel(
				14
			);


		descriptionLabel.Text =
			description;


		information.AddChild(
			descriptionLabel
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
					Control.SizeFlags.ExpandFill
			};


		button.Pressed +=
			pressed;


		information.AddChild(
			button
		);


		return button;
	}


	private void CreateSectionTitle(
		string text)
	{
		Label label =
			ShopUi.CreateLabel(
				21
			);


		label.Text =
			text;


		label.CustomMinimumSize =
			new Vector2(
				0,
				38
			);


		_content.AddChild(
			label
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


	private void CreateBottomPadding()
	{
		Control spacer =
			new()
			{
				CustomMinimumSize =
					new Vector2(
						0,
						70
					),

				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};


		_content.AddChild(
			spacer
		);
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
			)
			+ " DATA SHARDS";


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


		_productionBoostStatus.Text =
			remaining > 0
				? "ACTIVE • "
					+ FormatTime(
						remaining
					)
					+ " remaining"
				: "Currently inactive";


		_productionBoostButton.Text =
			"BUY • "
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


		_luckStatus.Text =
			remaining > 0
				? "ACTIVE • "
					+ FormatTime(
						remaining
					)
					+ " remaining"
				: "Currently inactive";


		_luckButton.Text =
			"BUY • "
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
			"USE • "
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
			$"Level {level} / {GameConfig.ShopProductionUpgradeMaxLevel}"
			+ "\nPermanent bonus: +"
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
			"UPGRADE • "
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
			$"Level {level} / {GameConfig.ShopOfflineUpgradeMaxLevel}"
			+ "\nAdditional offline income: +"
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
			"UPGRADE • "
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
