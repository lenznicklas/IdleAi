using Godot;
using System;
using System.Collections.Generic;

namespace IdleAi;

public sealed class BotSkinShopController
{
	private const float MainShopBottomPadding =
		240.0f;

	private const float CollectionHeaderHeight =
		122.0f;

	private const float CollectionHeaderTopPadding =
		8.0f;

	private const float CollectionHeaderGap =
		12.0f;

	private const float CollectionBottomPadding =
		260.0f;


	private readonly Game _root;

	private readonly GameState _state;

	private readonly BotSkinService _service;


	private Control _shopPage =
		null!;

	private MarginContainer _shopMargin =
		null!;

	private Control _mainLayout =
		null!;

	private VBoxContainer _content =
		null!;


	private Label _collectionEntryStatus =
		null!;

	private Button _collectionEntryButton =
		null!;


	private Control _collectionRoot =
		null!;

	private ScrollContainer _collectionScroll =
		null!;

	private MarginContainer _collectionScrollMargin =
		null!;

	private PanelContainer _collectionHeader =
		null!;

	private VBoxContainer _collectionContent =
		null!;

	private Label _collectionShardLabel =
		null!;

	private MobileScrollController _collectionMobileScroll =
		null!;


	private SkinCardView? _defaultCard;

	private readonly Dictionary<string, SkinCardView>
		_cards =
			new(
				StringComparer.OrdinalIgnoreCase
			);


	public event Action<string>? MessageRequested;

	public event Action? StateChanged;


	public bool CollectionVisible =>
		_collectionRoot != null
		&& _collectionRoot.Visible;


	public BotSkinShopController(
		Game root,
		GameState state,
		BotSkinService service)
	{
		_root =
			root;

		_state =
			state;

		_service =
			service;
	}


	public void Initialize()
	{
		_shopPage =
			_root.GetNode<Control>(
				"ShopPage"
			);

		_shopMargin =
			_root.GetNode<MarginContainer>(
				"ShopPage/ShopMargin"
			);

		_mainLayout =
			_root.GetNode<Control>(
				"ShopPage/ShopMargin/LayoutRoot"
			);

		_content =
			_root.GetNode<VBoxContainer>(
				"ShopPage/ShopMargin/LayoutRoot/Scroll/ScrollMargin/Content"
			);

		RemoveComingSoonCosmeticPlaceholder();

		Node? bottomSpace =
			DetachBottomSpacer();

		CreateMainShopEntry();

		if (bottomSpace != null)
		{
			if (bottomSpace is Control control)
			{
				control.CustomMinimumSize =
					new Vector2(
						0,
						MainShopBottomPadding
					);
			}

			_content.AddChild(
				bottomSpace
			);
		}
		else
		{
			AddSpacer(
				_content,
				MainShopBottomPadding
			);
		}

		CreateCollectionPage();

		Refresh();
	}


	private void CreateMainShopEntry()
	{
		CreateSectionHeader(
			_content,
			"BOT SKINS",
			"Choose which visual bot pack is active."
		);

		_collectionEntryStatus =
			ShopUi.CreateMutedLabel(
				13
			);

		_collectionEntryButton =
			CreateMainShopCard(
				ShopUi.Cosmetics,
				"BOT SKIN COLLECTION",
				"Browse owned packs, buy new skins and choose the active bot appearance.",
				_collectionEntryStatus,
				ShopUi.Purple,
				OpenCollection
			);

		_collectionEntryButton.Text =
			"OPEN COLLECTION";
	}


	private Button CreateMainShopCard(
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

		StyleBoxFlat panelStyle =
			ShopUi.CreatePanelStyle();

		panelStyle.BorderColor =
			new Color(
				accent.R,
				accent.G,
				accent.B,
				0.62f
			);

		panel.AddThemeStyleboxOverride(
			"panel",
			panelStyle
		);

		_content.AddChild(
			panel
		);

		MarginContainer margin =
			CreateCardMargin();

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

		iconPanel.AddThemeStyleboxOverride(
			"panel",
			CreatePreviewFrameStyle(
				accent,
				0.12f,
				0.56f
			)
		);

		row.AddChild(
			iconPanel
		);

		TextureRect icon =
			new()
			{
				Texture =
					iconTexture,

				ExpandMode =
					TextureRect.ExpandModeEnum.IgnoreSize,

				StretchMode =
					TextureRect.StretchModeEnum.KeepAspectCentered,

				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};

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

		iconPanel.AddChild(
			icon
		);

		VBoxContainer info =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};

		info.AddThemeConstantOverride(
			"separation",
			5
		);

		row.AddChild(
			info
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

		info.AddChild(
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

		info.AddChild(
			descriptionLabel
		);

		status.HorizontalAlignment =
			HorizontalAlignment.Left;

		status.CustomMinimumSize =
			new Vector2(
				0,
				28
			);

		info.AddChild(
			status
		);

		Button button =
			CreateActionButton();

		button.Pressed +=
			pressed;

		info.AddChild(
			button
		);

		return button;
	}


	private void CreateCollectionPage()
	{
		/*
		 * The collection uses a true overlay layout:
		 *
		 * - ScrollContainer fills the complete ShopMargin from
		 *   the physical top edge down to the usable bottom.
		 * - The collection header floats above that scroll view.
		 * - Scroll content receives top padding equal to the
		 *   safe area + fixed header height.
		 *
		 * This matches the main Shop/Lab behavior and prevents
		 * the header from consuming ScrollContainer height.
		 */
		_collectionRoot =
			new Control
			{
				Name =
					"BotSkinCollection",

				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,

				SizeFlagsVertical =
					Control.SizeFlags.ExpandFill,

				MouseFilter =
					Control.MouseFilterEnum.Pass
			};

		_collectionRoot.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);

		_shopMargin.AddChild(
			_collectionRoot
		);

		_collectionScroll =
			new ScrollContainer
			{
				Name =
					"BotSkinCollectionScroll",

				AnchorLeft =
					0.0f,

				AnchorTop =
					0.0f,

				AnchorRight =
					1.0f,

				AnchorBottom =
					1.0f,

				OffsetLeft =
					0.0f,

				OffsetTop =
					0.0f,

				OffsetRight =
					0.0f,

				OffsetBottom =
					0.0f,

				HorizontalScrollMode =
					ScrollContainer.ScrollMode.Disabled,

				VerticalScrollMode =
					ScrollContainer.ScrollMode.ShowNever,

				ClipContents =
					true,

				MouseFilter =
					Control.MouseFilterEnum.Pass
			};

		_collectionRoot.AddChild(
			_collectionScroll
		);

		_collectionScrollMargin =
			new MarginContainer
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};

		_collectionScrollMargin.AddThemeConstantOverride(
			"margin_left",
			2
		);

		_collectionScrollMargin.AddThemeConstantOverride(
			"margin_right",
			2
		);

		_collectionScroll.AddChild(
			_collectionScrollMargin
		);

		_collectionContent =
			new VBoxContainer
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};

		_collectionContent.AddThemeConstantOverride(
			"separation",
			16
		);

		_collectionScrollMargin.AddChild(
			_collectionContent
		);

		CreateDefaultSkinCard();

		foreach (
			SkinDefinition skin
				in BotSkinCatalog.GetAll()
		)
		{
			if (
				skin.Target
					!= SkinTarget.Bots
			)
			{
				continue;
			}

			CreateSkinCard(
				skin
			);
		}

		/*
		 * Add the fixed header AFTER the ScrollContainer so it
		 * renders above the scrolling cards.
		 */
		CreateCollectionHeader();

		_collectionMobileScroll =
			new MobileScrollController
			{
				Name =
					"BotSkinCollectionMobileScroll"
			};

		_shopPage.AddChild(
			_collectionMobileScroll
		);

		_collectionMobileScroll.Setup(
			_collectionScroll,
			allowTopOverscroll:
				true,
			allowBottomOverscroll:
				true
		);

		ApplyCollectionOverlayMetrics();

		_root.GetViewport().SizeChanged +=
			ApplyCollectionOverlayMetrics;

		_collectionRoot.Hide();
	}


	private void CreateCollectionHeader()
	{
		_collectionHeader =
			new PanelContainer
			{
				Name =
					"CollectionHeaderOverlay",

				AnchorLeft =
					0.0f,

				AnchorTop =
					0.0f,

				AnchorRight =
					1.0f,

				AnchorBottom =
					0.0f,

				MouseFilter =
					Control.MouseFilterEnum.Stop
			};

		_collectionHeader.AddThemeStyleboxOverride(
			"panel",
			ShopUi.CreateShardPanelStyle()
		);

		_collectionRoot.AddChild(
			_collectionHeader
		);

		MarginContainer margin =
			new();

		margin.AddThemeConstantOverride(
			"margin_left",
			16
		);

		margin.AddThemeConstantOverride(
			"margin_top",
			12
		);

		margin.AddThemeConstantOverride(
			"margin_right",
			16
		);

		margin.AddThemeConstantOverride(
			"margin_bottom",
			12
		);

		_collectionHeader.AddChild(
			margin
		);

		VBoxContainer box =
			new();

		box.AddThemeConstantOverride(
			"separation",
			8
		);

		margin.AddChild(
			box
		);

		HBoxContainer topRow =
			new();

		topRow.AddThemeConstantOverride(
			"separation",
			10
		);

		box.AddChild(
			topRow
		);

		Button backButton =
			new()
			{
				Text =
					"‹ SHOP",

				CustomMinimumSize =
					new Vector2(
						112,
						46
					),

				FocusMode =
					Control.FocusModeEnum.None
			};

		ShopUi.ApplyPrimaryButtonStyle(
			backButton
		);

		backButton.Pressed +=
			CloseCollection;

		topRow.AddChild(
			backButton
		);

		Label title =
			ShopUi.CreateLabel(
				24
			);

		title.Text =
			"BOT SKIN COLLECTION";

		title.SizeFlagsHorizontal =
			Control.SizeFlags.ExpandFill;

		title.HorizontalAlignment =
			HorizontalAlignment.Left;

		topRow.AddChild(
			title
		);

		_collectionShardLabel =
			ShopUi.CreateLabel(
				16
			);

		_collectionShardLabel.HorizontalAlignment =
			HorizontalAlignment.Right;

		_collectionShardLabel.AddThemeColorOverride(
			"font_color",
			ShopUi.Gold
		);

		box.AddChild(
			_collectionShardLabel
		);
	}


	private void ApplyCollectionOverlayMetrics()
	{
		if (
			_collectionHeader == null
			|| _collectionScrollMargin == null
		)
		{
			return;
		}

		float safeTop =
			GetSafeTopInset();

		float headerTop =
			safeTop
				+ CollectionHeaderTopPadding;

		_collectionHeader.OffsetLeft =
			0.0f;

		_collectionHeader.OffsetTop =
			headerTop;

		_collectionHeader.OffsetRight =
			0.0f;

		_collectionHeader.OffsetBottom =
			headerTop
				+ CollectionHeaderHeight;

		/*
		 * The ScrollContainer itself starts at y=0. Only its
		 * CONTENT receives padding, so swiping/overscroll can
		 * extend all the way behind the fixed header/notch.
		 */
		_collectionScrollMargin.AddThemeConstantOverride(
			"margin_top",
			(int)MathF.Ceiling(
				headerTop
					+ CollectionHeaderHeight
					+ CollectionHeaderGap
			)
		);

		/*
		 * Extra end room lets the final skin card move clearly
		 * above the navigation bar instead of stopping exactly
		 * on the lower edge of the usable viewport.
		 */
		_collectionScrollMargin.AddThemeConstantOverride(
			"margin_bottom",
			(int)MathF.Ceiling(
				CollectionBottomPadding
			)
		);

		_collectionHeader.MoveToFront();
	}


	private void CreateDefaultSkinCard()
	{
		_defaultCard =
			CreateCollectionCard(
				"default",
				"DEFAULT",
				"Original bot appearance.",
				0.0,
				new Dictionary<BotRarity, Texture2D?>
				{
					{
						BotRarity.Common,
						BotCatalog.Get(
							BotRarity.Common
						).DefaultTexture
					},
					{
						BotRarity.Rare,
						BotCatalog.Get(
							BotRarity.Rare
						).DefaultTexture
					},
					{
						BotRarity.Epic,
						BotCatalog.Get(
							BotRarity.Epic
						).DefaultTexture
					},
					{
						BotRarity.Legendary,
						BotCatalog.Get(
							BotRarity.Legendary
						).DefaultTexture
					}
				},
				() =>
					HandleSkinResult(
						_service.UseDefault()
					)
			);
	}


	private void CreateSkinCard(
		SkinDefinition skin)
	{
		Dictionary<BotRarity, Texture2D?> textures =
			new()
			{
				{
					BotRarity.Common,
						skin.GetBotTexture(
							BotRarity.Common
						)
				},
				{
					BotRarity.Rare,
						skin.GetBotTexture(
							BotRarity.Rare
						)
				},
				{
					BotRarity.Epic,
						skin.GetBotTexture(
							BotRarity.Epic
						)
				},
				{
					BotRarity.Legendary,
						skin.GetBotTexture(
							BotRarity.Legendary
						)
				}
			};

		SkinCardView card =
			CreateCollectionCard(
				skin.Id,
				skin.Name.ToUpperInvariant(),
				skin.Description,
				skin.Cost,
				textures,
				() =>
					OnSkinPressed(
						skin
					)
			);

		_cards[
			skin.Id
		] =
			card;
	}


	private SkinCardView CreateCollectionCard(
		string id,
		string name,
		string description,
		double cost,
		IReadOnlyDictionary<BotRarity, Texture2D?> textures,
		Action pressed)
	{
		PanelContainer panel =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};

		StyleBoxFlat panelStyle =
			ShopUi.CreatePanelStyle();

		panelStyle.BorderColor =
			new Color(
				ShopUi.Purple.R,
				ShopUi.Purple.G,
				ShopUi.Purple.B,
				0.66f
			);

		panel.AddThemeStyleboxOverride(
			"panel",
			panelStyle
		);

		_collectionContent.AddChild(
			panel
		);

		MarginContainer margin =
			CreateCardMargin();

		panel.AddChild(
			margin
		);

		VBoxContainer box =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};

		box.AddThemeConstantOverride(
			"separation",
			9
		);

		margin.AddChild(
			box
		);

		Label title =
			ShopUi.CreateLabel(
				20
			);

		title.Text =
			name;

		title.AddThemeColorOverride(
			"font_color",
			ShopUi.Purple
		);

		box.AddChild(
			title
		);

		Label descriptionLabel =
			ShopUi.CreateMutedLabel(
				13
			);

		descriptionLabel.Text =
			description;

		box.AddChild(
			descriptionLabel
		);

		HBoxContainer previewRow =
			new()
			{
				CustomMinimumSize =
					new Vector2(
						0,
						92
					),

				Alignment =
					BoxContainer.AlignmentMode.Center
			};

		previewRow.AddThemeConstantOverride(
			"separation",
			8
		);

		box.AddChild(
			previewRow
		);

		AddPreview(
			previewRow,
			textures[
				BotRarity.Common
			]
		);

		AddPreview(
			previewRow,
			textures[
				BotRarity.Rare
			]
		);

		AddPreview(
			previewRow,
			textures[
				BotRarity.Epic
			]
		);

		AddPreview(
			previewRow,
			textures[
				BotRarity.Legendary
			]
		);

		Label rarityLabel =
			ShopUi.CreateMutedLabel(
				11
			);

		rarityLabel.Text =
			"COMMON   •   RARE   •   EPIC   •   LEGENDARY";

		box.AddChild(
			rarityLabel
		);

		Label status =
			ShopUi.CreateMutedLabel(
				13
			);

		status.CustomMinimumSize =
			new Vector2(
				0,
				28
			);

		box.AddChild(
			status
		);

		Button button =
			CreateActionButton();

		button.Pressed +=
			() =>
			{
				if (CollectionActionBlocked())
					return;

				pressed();
			};

		box.AddChild(
			button
		);

		return new SkinCardView(
			status,
			button,
			cost
		);
	}


	private static void AddPreview(
		HBoxContainer parent,
		Texture2D? texture)
	{
		PanelContainer frame =
			new()
			{
				CustomMinimumSize =
					new Vector2(
						78,
						78
					),

				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};

		frame.AddThemeStyleboxOverride(
			"panel",
			CreatePreviewFrameStyle(
				ShopUi.Purple,
				0.10f,
				0.38f
			)
		);

		parent.AddChild(
			frame
		);

		TextureRect image =
			new()
			{
				Texture =
					texture,

				ExpandMode =
					TextureRect.ExpandModeEnum.IgnoreSize,

				StretchMode =
					TextureRect.StretchModeEnum.KeepAspectCentered,

				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};

		image.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);

		image.OffsetLeft =
			5;

		image.OffsetTop =
			5;

		image.OffsetRight =
			-5;

		image.OffsetBottom =
			-5;

		frame.AddChild(
			image
		);
	}


	public void OpenCollection()
	{
		ApplyCollectionOverlayMetrics();

		Refresh();

		_collectionMobileScroll.ResetMotion();

		_collectionScroll.ScrollVertical =
			0;

		_mainLayout.Hide();

		_collectionRoot.Show();

		_collectionRoot.MoveToFront();

		_collectionHeader.MoveToFront();
	}


	public void CloseCollection()
	{
		if (_collectionRoot == null)
			return;

		_collectionMobileScroll?.ResetMotion();

		_collectionRoot.Hide();

		_mainLayout?.Show();
	}


	private void OnSkinPressed(
		SkinDefinition skin)
	{
		BotSkinResult result;

		if (
			!_service.IsOwned(
				skin.Id
			)
		)
		{
			result =
				_service.BuyAndEquip(
					skin.Id
				);
		}
		else
		{
			result =
				_service.Equip(
					skin.Id
				);
		}

		HandleSkinResult(
			result
		);
	}


	private void HandleSkinResult(
		BotSkinResult result)
	{
		MessageRequested?.Invoke(
			result.Message
		);

		if (result.Changed)
		{
			Input.VibrateHandheld(
				14,
				0.12f
			);

			StateChanged?.Invoke();
		}

		Refresh();
	}


	public void Refresh()
	{
		if (
			_collectionEntryStatus == null
			|| _collectionEntryButton == null
		)
		{
			return;
		}

		string activeName =
			GetActiveSkinName();

		int ownedPacks =
			1;

		foreach (
			SkinDefinition skin
				in BotSkinCatalog.GetAll()
		)
		{
			if (
				_service.IsOwned(
					skin.Id
				)
			)
			{
				ownedPacks++;
			}
		}

		_collectionEntryStatus.Text =
			"ACTIVE: "
			+ activeName.ToUpperInvariant()
			+ "   •   "
			+ ownedPacks
			+ " OWNED";

		_collectionEntryStatus.AddThemeColorOverride(
			"font_color",
			ShopUi.Green
		);

		_collectionEntryButton.Text =
			"OPEN COLLECTION";

		_collectionEntryButton.Disabled =
			false;

		if (_collectionShardLabel != null)
		{
			_collectionShardLabel.Text =
				"DATA SHARDS: "
				+ NumberFormatter.Format(
					_state.Shop.DataShards
				);
		}

		RefreshDefaultCard();

		foreach (
			SkinDefinition skin
				in BotSkinCatalog.GetAll()
		)
		{
			if (
				!_cards.TryGetValue(
					skin.Id,
					out SkinCardView? card
				)
			)
			{
				continue;
			}

			bool owned =
				_service.IsOwned(
					skin.Id
				);

			bool equipped =
				_service.IsEquipped(
					skin.Id
				);

			if (equipped)
			{
				SetCardActive(
					card
				);

				continue;
			}

			if (owned)
			{
				card.Status.Text =
					"OWNED  •  READY TO EQUIP";

				card.Status.AddThemeColorOverride(
					"font_color",
					ShopUi.Gold
				);

				card.Button.Text =
					"EQUIP";

				card.Button.Disabled =
					false;

				continue;
			}

			card.Status.Text =
				"NOT OWNED";

			card.Status.AddThemeColorOverride(
				"font_color",
				ShopUi.TextSecondary
			);

			card.Button.Text =
				"BUY  •  "
				+ NumberFormatter.Format(
					skin.Cost
				)
				+ " SHARDS";

			card.Button.Disabled =
				_state.Shop.DataShards
				< skin.Cost;
		}
	}


	private void RefreshDefaultCard()
	{
		if (_defaultCard == null)
			return;

		if (
			_service.IsEquipped(
				BotSkinCatalog.DefaultSkinId
			)
		)
		{
			SetCardActive(
				_defaultCard
			);

			return;
		}

		_defaultCard.Status.Text =
			"OWNED  •  ORIGINAL APPEARANCE";

		_defaultCard.Status.AddThemeColorOverride(
			"font_color",
			ShopUi.Gold
		);

		_defaultCard.Button.Text =
			"EQUIP";

		_defaultCard.Button.Disabled =
			false;
	}


	private static void SetCardActive(
		SkinCardView card)
	{
		card.Status.Text =
			"● ACTIVE";

		card.Status.AddThemeColorOverride(
			"font_color",
			ShopUi.Green
		);

		card.Button.Text =
			"ACTIVE";

		card.Button.Disabled =
			true;
	}


	private string GetActiveSkinName()
	{
		if (
			_service.IsEquipped(
				BotSkinCatalog.DefaultSkinId
			)
		)
		{
			return "Default";
		}

		if (
			BotSkinCatalog.TryGet(
				_service.EquippedBotSkinId,
				out SkinDefinition skin
			)
		)
		{
			return skin.Name;
		}

		return "Default";
	}


	private void RemoveComingSoonCosmeticPlaceholder()
	{
		Node? card =
			null;

		Node? header =
			null;

		for (
			int i = 0;
			i < _content.GetChildCount();
			i++
		)
		{
			Node child =
				_content.GetChild(
					i
				);

			if (
				!ContainsButtonText(
					child,
					"COMING SOON"
				)
			)
			{
				continue;
			}

			card =
				child;

			if (i > 0)
			{
				Node previous =
					_content.GetChild(
						i - 1
					);

				if (
					ContainsLabelText(
						previous,
						"COSMETICS"
					)
				)
				{
					header =
						previous;
				}
			}

			break;
		}

		RemoveAndFree(
			card
		);

		RemoveAndFree(
			header
		);
	}


	private Node? DetachBottomSpacer()
	{
		if (_content.GetChildCount() == 0)
			return null;

		Node candidate =
			_content.GetChild(
				_content.GetChildCount() - 1
			);

		if (
			candidate is PanelContainer
			|| candidate.GetChildCount() > 0
		)
		{
			return null;
		}

		_content.RemoveChild(
			candidate
		);

		return candidate;
	}


	private static MarginContainer CreateCardMargin()
	{
		MarginContainer margin =
			new();

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

		return margin;
	}


	private static Button CreateActionButton()
	{
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

		return button;
	}


	private static void CreateSectionHeader(
		VBoxContainer parent,
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

		parent.AddChild(
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
			ShopUi.Purple
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


	private static StyleBoxFlat CreatePreviewFrameStyle(
		Color accent,
		float backgroundAlpha,
		float borderAlpha)
	{
		return new StyleBoxFlat
		{
			BgColor =
				new Color(
					accent.R,
					accent.G,
					accent.B,
					backgroundAlpha
				),

			BorderColor =
				new Color(
					accent.R,
					accent.G,
					accent.B,
					borderAlpha
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
				16,

			CornerRadiusTopRight =
				16,

			CornerRadiusBottomLeft =
				16,

			CornerRadiusBottomRight =
				16
		};
	}


	private static void AddSpacer(
		Container parent,
		float height)
	{
		parent.AddChild(
			new Control
			{
				CustomMinimumSize =
					new Vector2(
						0,
						height
					),

				MouseFilter =
					Control.MouseFilterEnum.Ignore
			}
		);
	}


	private bool CollectionActionBlocked()
	{
		return _collectionMobileScroll != null
			&& _collectionMobileScroll.ShouldSuppressTap;
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


	private static bool ContainsButtonText(
		Node node,
		string text)
	{
		if (
			node is Button button
			&& button.Text == text
		)
		{
			return true;
		}

		foreach (
			Node child
				in node.GetChildren()
		)
		{
			if (
				ContainsButtonText(
					child,
					text
				)
			)
			{
				return true;
			}
		}

		return false;
	}


	private static bool ContainsLabelText(
		Node node,
		string text)
	{
		if (
			node is Label label
			&& label.Text == text
		)
		{
			return true;
		}

		foreach (
			Node child
				in node.GetChildren()
		)
		{
			if (
				ContainsLabelText(
					child,
					text
				)
			)
			{
				return true;
			}
		}

		return false;
	}


	private static void RemoveAndFree(
		Node? node)
	{
		if (node == null)
			return;

		Node? parent =
			node.GetParent();

		parent?.RemoveChild(
			node
		);

		node.QueueFree();
	}


	private sealed class SkinCardView
	{
		public Label Status { get; }

		public Button Button { get; }

		public double Cost { get; }


		public SkinCardView(
			Label status,
			Button button,
			double cost)
		{
			Status =
				status;

			Button =
				button;

			Cost =
				cost;
		}
	}
}
