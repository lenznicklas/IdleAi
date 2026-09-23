using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace IdleAi;

public sealed class MachineSkinShopController
{
	private const float MainShopBottomPadding =
		180.0f;

	private const float CollectionBottomPadding =
		180.0f;


	private readonly Game _root;
	private readonly GameState _state;
	private readonly MachineSkinService _service;

	private Control _shopPage =
		null!;

	private MarginContainer _shopMargin =
		null!;

	private Control _mainLayout =
		null!;

	private VBoxContainer _content =
		null!;

	private Label _entryStatus =
		null!;

	private Button _entryButton =
		null!;

	private VBoxContainer _collectionRoot =
		null!;

	private ScrollContainer _collectionScroll =
		null!;

	private VBoxContainer _collectionContent =
		null!;

	private Label _collectionShardLabel =
		null!;

	private MobileScrollController _collectionMobileScroll =
		null!;

	private readonly Dictionary<string, MachineSkinCardView>
		_skinCards =
			new(
				StringComparer.OrdinalIgnoreCase
			);

	private readonly Dictionary<int, MachineSkinCardView>
		_defaultCards =
			new();


	public event Action<string>? MessageRequested;

	public event Action? StateChanged;


	public bool CollectionVisible =>
		_collectionRoot != null
		&& _collectionRoot.Visible;


	public MachineSkinShopController(
		Game root,
		GameState state,
		MachineSkinService service)
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


	// ==================================================
	// MAIN SHOP ENTRY
	// ==================================================

	private void CreateMainShopEntry()
	{
		CreateSectionHeader(
			_content,
			"MACHINE SKINS",
			"Each room can use its own machine appearance pack."
		);

		_entryStatus =
			ShopUi.CreateMutedLabel(
				13
			);

		_entryButton =
			CreateMainShopCard(
				ShopUi.Cosmetics,
				"MACHINE SKIN COLLECTION",
				"Buy and equip machine packs independently for Garage, Server Room, Data Center and Quantum Lab.",
				_entryStatus,
				ShopUi.Gold,
				OpenCollection
			);

		_entryButton.Text =
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

		StyleBoxFlat style =
			ShopUi.CreatePanelStyle();

		style.BorderColor =
			new Color(
				accent.R,
				accent.G,
				accent.B,
				0.66f
			);

		panel.AddThemeStyleboxOverride(
			"panel",
			style
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

		TextureRect icon =
			new()
			{
				Texture =
					iconTexture,

				CustomMinimumSize =
					new Vector2(
						102,
						102
					),

				ExpandMode =
					TextureRect.ExpandModeEnum.IgnoreSize,

				StretchMode =
					TextureRect.StretchModeEnum.KeepAspectCentered,

				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};

		row.AddChild(
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


	// ==================================================
	// COLLECTION
	// ==================================================

	private void CreateCollectionPage()
	{
		_collectionRoot =
			new VBoxContainer
			{
				Name =
					"MachineSkinCollection",

				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,

				SizeFlagsVertical =
					Control.SizeFlags.ExpandFill
			};

		_collectionRoot.AddThemeConstantOverride(
			"separation",
			8
		);

		_shopMargin.AddChild(
			_collectionRoot
		);

		AddSpacer(
			_collectionRoot,
			GetSafeTopInset()
				+ 8.0f
		);

		CreateCollectionHeader();

		_collectionScroll =
			new ScrollContainer
			{
				Name =
					"MachineSkinCollectionScroll",

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

		_collectionRoot.AddChild(
			_collectionScroll
		);

		MarginContainer scrollMargin =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};

		scrollMargin.AddThemeConstantOverride(
			"margin_top",
			8
		);

		scrollMargin.AddThemeConstantOverride(
			"margin_bottom",
			(int)CollectionBottomPadding
		);

		_collectionScroll.AddChild(
			scrollMargin
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

		scrollMargin.AddChild(
			_collectionContent
		);

		for (
			int roomIndex = 0;
			roomIndex < _state.Rooms.Count;
			roomIndex++
		)
		{
			CreateRoomSection(
				roomIndex
			);
		}

		_collectionMobileScroll =
			new MobileScrollController
			{
				Name =
					"MachineSkinCollectionMobileScroll"
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

		_collectionRoot.Hide();
	}


	private void CreateCollectionHeader()
	{
		PanelContainer panel =
			new()
			{
				CustomMinimumSize =
					new Vector2(
						0,
						122
					),

				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};

		panel.AddThemeStyleboxOverride(
			"panel",
			ShopUi.CreateShardPanelStyle()
		);

		_collectionRoot.AddChild(
			panel
		);

		MarginContainer margin =
			CreateCardMargin();

		panel.AddChild(
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

		HBoxContainer top =
			new();

		top.AddThemeConstantOverride(
			"separation",
			10
		);

		box.AddChild(
			top
		);

		Button back =
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
			back
		);

		back.Pressed +=
			CloseCollection;

		top.AddChild(
			back
		);

		Label title =
			ShopUi.CreateLabel(
				24
			);

		title.Text =
			"MACHINE SKIN COLLECTION";

		title.SizeFlagsHorizontal =
			Control.SizeFlags.ExpandFill;

		title.HorizontalAlignment =
			HorizontalAlignment.Left;

		top.AddChild(
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


	private void CreateRoomSection(
		int roomIndex)
	{
		RoomData room =
			_state.Rooms[
				roomIndex
			];

		CreateSectionHeader(
			_collectionContent,
			room.Name.ToUpperInvariant(),
			"Machine packs affect all four machine tiers plus the empty slot."
		);

		MachineSkinCardView defaultCard =
			CreateCollectionCard(
				roomIndex,
				MachineSkinCatalog.DefaultSkinId,
				"DEFAULT",
				"Original "
					+ room.Name
					+ " machines.",
				0.0,
				room.DefaultEmptyTexture,
				[
					room.Machines[0].DefaultTexture,
					room.Machines[1].DefaultTexture,
					room.Machines[2].DefaultTexture,
					room.Machines[3].DefaultTexture
				],
				true,
				() =>
					HandleResult(
						_service.UseDefault(
							roomIndex
						)
					)
			);

		_defaultCards[
			roomIndex
		] =
			defaultCard;

		bool anyPack =
			false;

		foreach (
			MachineSkinDefinition skin
				in MachineSkinCatalog.GetForRoom(
					roomIndex
				)
		)
		{
			anyPack =
				true;

			MachineSkinCardView card =
				CreateCollectionCard(
					roomIndex,
					skin.Id,
					skin.Name.ToUpperInvariant(),
					skin.Description,
					skin.Cost,
					skin.EmptyTexture,
					skin.MachineTextures,
					skin.IsComplete,
					() =>
						OnSkinPressed(
							skin
						)
				);

			_skinCards[
				skin.Id
			] =
				card;
		}

		if (!anyPack)
		{
			Label empty =
				ShopUi.CreateMutedLabel(
					13
				);

			empty.Text =
				"No machine skin packs available for this room yet.";

			empty.CustomMinimumSize =
				new Vector2(
					0,
						52
				);

			_collectionContent.AddChild(
				empty
			);
		}
	}


	private MachineSkinCardView CreateCollectionCard(
		int roomIndex,
		string skinId,
		string title,
		string description,
		double cost,
		Texture2D? emptyTexture,
		IReadOnlyList<Texture2D?> machineTextures,
		bool assetsComplete,
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

		Color accent =
			RoomThemePalette.GetAccentColor(
				roomIndex
			);

		panelStyle.BorderColor =
			new Color(
				accent.R,
				accent.G,
				accent.B,
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
			new();

		box.AddThemeConstantOverride(
			"separation",
			9
		);

		margin.AddChild(
			box
		);

		Label name =
			ShopUi.CreateLabel(
				20
			);

		name.Text =
			title;

		name.AddThemeColorOverride(
			"font_color",
			accent
		);

		box.AddChild(
			name
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

		HBoxContainer previews =
			new()
			{
				CustomMinimumSize =
					new Vector2(
						0,
						86
					),

				Alignment =
					BoxContainer.AlignmentMode.Center
			};

		previews.AddThemeConstantOverride(
			"separation",
			6
		);

		box.AddChild(
			previews
		);

		for (
			int i = 0;
			i < 4;
			i++
		)
		{
			Texture2D? texture =
				i < machineTextures.Count
					? machineTextures[i]
					: null;

			AddPreview(
				previews,
				texture,
				accent
			);
		}

		AddPreview(
			previews,
			emptyTexture,
			accent
		);

		Label previewLabel =
			ShopUi.CreateMutedLabel(
				10
			);

		previewLabel.Text =
			"M1       M2       M3       M4      EMPTY";

		box.AddChild(
			previewLabel
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

		return new MachineSkinCardView(
			roomIndex,
			skinId,
			status,
			button,
			cost,
			assetsComplete
		);
	}


	private static void AddPreview(
		HBoxContainer parent,
		Texture2D? texture,
		Color accent)
	{
		PanelContainer frame =
			new()
			{
				CustomMinimumSize =
					new Vector2(
						70,
						70
					),

				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};

		StyleBoxFlat style =
			new()
			{
				BgColor =
					new Color(
						accent.R,
						accent.G,
						accent.B,
						0.10f
					),

				BorderColor =
					new Color(
						accent.R,
						accent.G,
						accent.B,
						0.42f
					),

				BorderWidthLeft =
					1,

				BorderWidthTop =
					1,

				BorderWidthRight =
					1,

				BorderWidthBottom =
					1,

				CornerRadiusTopLeft =
					12,

				CornerRadiusTopRight =
					12,

				CornerRadiusBottomLeft =
					12,

				CornerRadiusBottomRight =
					12
			};

		frame.AddThemeStyleboxOverride(
			"panel",
			style
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
			4;

		image.OffsetTop =
			4;

		image.OffsetRight =
			-4;

		image.OffsetBottom =
			-4;

		frame.AddChild(
			image
		);
	}


	// ==================================================
	// ACTIONS
	// ==================================================

	public void OpenCollection()
	{
		Refresh();

		_collectionMobileScroll?.ResetMotion();

		_collectionScroll.ScrollVertical =
			0;

		_mainLayout.Hide();

		_collectionRoot.Show();

		_collectionRoot.MoveToFront();
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
		MachineSkinDefinition skin)
	{
		MachineSkinResult result =
			_service.IsOwned(
				skin.Id
			)
				? _service.Equip(
					skin.Id
				)
				: _service.BuyAndEquip(
					skin.Id
				);

		HandleResult(
			result
		);
	}


	private void HandleResult(
		MachineSkinResult result)
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


	// ==================================================
	// REFRESH
	// ==================================================

	public void Refresh()
	{
		if (
			_entryStatus == null
			|| _entryButton == null
		)
		{
			return;
		}

		int customRooms =
			0;

		for (
			int roomIndex = 0;
			roomIndex < _state.Rooms.Count;
			roomIndex++
		)
		{
			if (
				!_service.GetEquippedSkinId(
					roomIndex
				)
				.Equals(
					MachineSkinCatalog.DefaultSkinId,
					StringComparison.OrdinalIgnoreCase
				)
			)
			{
				customRooms++;
			}
		}

		_entryStatus.Text =
			customRooms == 0
				? "ALL ROOMS: DEFAULT"
				: customRooms
					+ " ROOM"
					+ (
						customRooms == 1
							? ""
							: "S"
					)
					+ " CUSTOMIZED";

		_entryStatus.AddThemeColorOverride(
			"font_color",
			customRooms > 0
				? ShopUi.Green
				: ShopUi.TextSecondary
		);

		_entryButton.Text =
			"OPEN COLLECTION";

		_entryButton.Disabled =
			false;

		if (_collectionShardLabel != null)
		{
			_collectionShardLabel.Text =
				"DATA SHARDS: "
					+ NumberFormatter.Format(
						_state.Shop.DataShards
					);
		}

		foreach (
			KeyValuePair<int, MachineSkinCardView> pair
				in _defaultCards
		)
		{
			MachineSkinCardView card =
				pair.Value;

			if (
				_service.IsEquipped(
					pair.Key,
					MachineSkinCatalog.DefaultSkinId
				)
			)
			{
				SetActive(
					card
				);
			}
			else
			{
				card.Status.Text =
					"OWNED  •  ORIGINAL APPEARANCE";

				card.Status.AddThemeColorOverride(
					"font_color",
					ShopUi.Gold
				);

				card.Button.Text =
					"EQUIP";

				card.Button.Disabled =
					false;
			}
		}

		foreach (
			MachineSkinDefinition skin
				in MachineSkinCatalog.GetAll()
		)
		{
			if (
				!_skinCards.TryGetValue(
					skin.Id,
					out MachineSkinCardView? card
				)
			)
			{
				continue;
			}

			if (!card.AssetsComplete)
			{
				card.Status.Text =
					"ASSETS INCOMPLETE";

				card.Status.AddThemeColorOverride(
					"font_color",
					ShopUi.TextSecondary
				);

				card.Button.Text =
					"ADD ALL 5 PNG FILES";

				card.Button.Disabled =
					true;

				continue;
			}

			if (
				_service.IsEquipped(
					skin.RoomIndex,
					skin.Id
				)
			)
			{
				SetActive(
					card
				);

				continue;
			}

			if (
				_service.IsOwned(
					skin.Id
				)
			)
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


	private static void SetActive(
		MachineSkinCardView card)
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


	// ==================================================
	// HELPERS
	// ==================================================

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
			ShopUi.Gold
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


	private sealed class MachineSkinCardView
	{
		public int RoomIndex { get; }

		public string SkinId { get; }

		public Label Status { get; }

		public Button Button { get; }

		public double Cost { get; }

		public bool AssetsComplete { get; }


		public MachineSkinCardView(
			int roomIndex,
			string skinId,
			Label status,
			Button button,
			double cost,
			bool assetsComplete)
		{
			RoomIndex =
				roomIndex;

			SkinId =
				skinId;

			Status =
				status;

			Button =
				button;

			Cost =
				cost;

			AssetsComplete =
				assetsComplete;
		}
	}
}
