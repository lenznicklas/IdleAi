using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace IdleAi;

public sealed class MachineSkinShopController
{
	private const float MainShopBottomPadding = 240.0f;
	private const float CollectionHeaderHeight = 122.0f;
	private const float CollectionHeaderTopPadding = 8.0f;
	private const float CollectionHeaderGap = 12.0f;
	private const float CollectionBottomPadding = 260.0f;
	private const int GridColumns = 2;

	private const double PreviewCycleSeconds = 5.0;
	private const double PreviewFadeOutSeconds = 0.20;
	private const double PreviewFadeInSeconds = 0.34;

	private static readonly Vector2 PreviewTransitionScale =
		new(0.94f, 0.94f);

	private readonly Game _root;
	private readonly GameState _state;
	private readonly MachineSkinService _service;

	private Control _shopPage = null!;
	private MarginContainer _shopMargin = null!;
	private Control _mainLayout = null!;
	private VBoxContainer _content = null!;

	private Label _entryStatus = null!;
	private Button _entryButton = null!;

	private Control _collectionRoot = null!;
	private ScrollContainer _collectionScroll = null!;
	private MarginContainer _collectionScrollMargin = null!;
	private PanelContainer _collectionHeader = null!;
	private VBoxContainer _collectionContent = null!;
	private GridContainer _ownedSkinGrid = null!;
	private GridContainer _availableSkinGrid = null!;
	private Label _collectionShardLabel = null!;
	private MobileScrollController _collectionMobileScroll = null!;
	private SkinPreviewOverlay _previewOverlay = null!;
	private Timer _previewCycleTimer = null!;

	private readonly List<RotatingPreviewState> _rotatingPreviews = new();

	private readonly Dictionary<string, MachineSkinCardView> _skinCards =
		new(StringComparer.OrdinalIgnoreCase);

	private readonly Dictionary<int, MachineSkinCardView> _defaultCards =
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
		_root = root;
		_state = state;
		_service = service;
	}

	public void Initialize()
	{
		_shopPage = _root.GetNode<Control>("ShopPage");
		_shopMargin = _root.GetNode<MarginContainer>("ShopPage/ShopMargin");
		_mainLayout = _root.GetNode<Control>("ShopPage/ShopMargin/LayoutRoot");
		_content = _root.GetNode<VBoxContainer>(
            "ShopPage/ShopMargin/LayoutRoot/Scroll/ScrollMargin/Content"
		);

		Node? bottomSpace = DetachBottomSpacer();
		CreateMainShopEntry();

		if (bottomSpace != null)
		{
			if (bottomSpace is Control control)
				control.CustomMinimumSize = new Vector2(0, MainShopBottomPadding);

			_content.AddChild(bottomSpace);
		}
		else
		{
			AddSpacer(_content, MainShopBottomPadding);
		}

		_previewOverlay = new SkinPreviewOverlay(
			_root,
            "MachineSkinGalleryPreviewOverlay"
		);
		_previewOverlay.Initialize();

		CreateCollectionPage();
		Refresh();
	}

	private void CreateMainShopEntry()
	{
		CreateSectionHeader(
			_content,
			"MACHINE SKINS",
            "Each room can use its own machine appearance pack."
		);

		_entryStatus = ShopUi.CreateMutedLabel(13);

		_entryButton = CreateMainShopCard(
			ShopUi.Cosmetics,
			"MACHINE SKIN COLLECTION",
			"Buy and equip machine packs independently for each room.",
			_entryStatus,
			ShopUi.Gold,
			OpenCollection
		);

		_entryButton.Text = "OPEN COLLECTION";
	}

	private Button CreateMainShopCard(
		Texture2D iconTexture,
		string title,
		string description,
		Label status,
		Color accent,
		Action pressed)
	{
		PanelContainer panel = new()
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			MouseFilter = Control.MouseFilterEnum.Pass
		};

		StyleBoxFlat style = ShopUi.CreatePanelStyle();
		style.BorderColor = new Color(accent.R, accent.G, accent.B, 0.66f);
		panel.AddThemeStyleboxOverride("panel", style);
		_content.AddChild(panel);

		MarginContainer margin = CreateCardMargin();
		panel.AddChild(margin);

		HBoxContainer row = new()
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		row.AddThemeConstantOverride("separation", 14);
		margin.AddChild(row);

		TextureRect icon = new()
		{
			Texture = iconTexture,
			CustomMinimumSize = new Vector2(102, 102),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		row.AddChild(icon);

		VBoxContainer info = new()
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		info.AddThemeConstantOverride("separation", 5);
		row.AddChild(info);

		Label titleLabel = ShopUi.CreateLabel(19);
		titleLabel.Text = title;
		titleLabel.HorizontalAlignment = HorizontalAlignment.Left;
		titleLabel.AddThemeColorOverride("font_color", accent);
		info.AddChild(titleLabel);

		Label descriptionLabel = ShopUi.CreateMutedLabel(13);
		descriptionLabel.Text = description;
		descriptionLabel.HorizontalAlignment = HorizontalAlignment.Left;
		info.AddChild(descriptionLabel);

		status.HorizontalAlignment = HorizontalAlignment.Left;
		status.CustomMinimumSize = new Vector2(0, 28);
		info.AddChild(status);

		Button button = CreateActionButton(52);
		button.Pressed += pressed;
		info.AddChild(button);

		return button;
	}

	private void CreateCollectionPage()
	{
		_collectionRoot = new Control
		{
			Name = "MachineSkinCollection",
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			SizeFlagsVertical = Control.SizeFlags.ExpandFill,
			MouseFilter = Control.MouseFilterEnum.Pass
		};
		_collectionRoot.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
		_shopMargin.AddChild(_collectionRoot);

		_collectionScroll = new ScrollContainer
		{
			Name = "MachineSkinCollectionScroll",
			AnchorLeft = 0.0f,
			AnchorTop = 0.0f,
			AnchorRight = 1.0f,
			AnchorBottom = 1.0f,
			HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
			VerticalScrollMode = ScrollContainer.ScrollMode.ShowNever,
			ClipContents = true,
			MouseFilter = Control.MouseFilterEnum.Pass
		};
		_collectionRoot.AddChild(_collectionScroll);

		_collectionScrollMargin = new MarginContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		_collectionScrollMargin.AddThemeConstantOverride("margin_left", 2);
		_collectionScrollMargin.AddThemeConstantOverride("margin_right", 2);
		_collectionScroll.AddChild(_collectionScrollMargin);

		_collectionContent = new VBoxContainer
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		_collectionContent.AddThemeConstantOverride("separation", 14);
		_collectionScrollMargin.AddChild(_collectionContent);

		CreateSectionHeader(
			_collectionContent,
			"OWNED SKINS",
            "Default and purchased machine packs."
		);

		_ownedSkinGrid = CreateSkinGrid("MachineSkinOwnedGrid");
		_collectionContent.AddChild(_ownedSkinGrid);

		AddCollectionDivider(_collectionContent);

		CreateSectionHeader(
			_collectionContent,
			"AVAILABLE TO BUY",
            "Unlock more machine appearances with Data Shards."
		);

		_availableSkinGrid = CreateSkinGrid("MachineSkinAvailableGrid");
		_collectionContent.AddChild(_availableSkinGrid);

		for (int roomIndex = 0; roomIndex < _state.Rooms.Count; roomIndex++)
			CreateRoomCards(roomIndex);

		CreatePreviewCycleTimer();
		CreateCollectionHeader();

		_collectionMobileScroll = new MobileScrollController
		{
			Name = "MachineSkinCollectionMobileScroll"
		};
		_shopPage.AddChild(_collectionMobileScroll);

		_collectionMobileScroll.Setup(
			_collectionScroll,
			allowTopOverscroll: true,
			allowBottomOverscroll: true
		);

		ApplyCollectionOverlayMetrics();
		_root.GetViewport().SizeChanged += ApplyCollectionOverlayMetrics;
		_collectionRoot.Hide();
	}

	private void CreateCollectionHeader()
	{
		_collectionHeader = new PanelContainer
		{
			Name = "CollectionHeaderOverlay",
			AnchorLeft = 0.0f,
			AnchorTop = 0.0f,
			AnchorRight = 1.0f,
			AnchorBottom = 0.0f,
			MouseFilter = Control.MouseFilterEnum.Stop
		};

		_collectionHeader.AddThemeStyleboxOverride(
			"panel",
			ShopUi.CreateShardPanelStyle()
		);
		_collectionRoot.AddChild(_collectionHeader);

		MarginContainer margin = CreateCardMargin();
		_collectionHeader.AddChild(margin);

		VBoxContainer box = new();
		box.AddThemeConstantOverride("separation", 8);
		margin.AddChild(box);

		HBoxContainer topRow = new();
		topRow.AddThemeConstantOverride("separation", 10);
		box.AddChild(topRow);

		Button backButton = new()
		{
			Text = "‹ SHOP",
			CustomMinimumSize = new Vector2(112, 46),
			FocusMode = Control.FocusModeEnum.None
		};
		ShopUi.ApplyPrimaryButtonStyle(backButton);
		backButton.Pressed += CloseCollection;
		topRow.AddChild(backButton);

		Label title = ShopUi.CreateLabel(24);
		title.Text = "MACHINE SKIN COLLECTION";
		title.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		title.HorizontalAlignment = HorizontalAlignment.Left;
		topRow.AddChild(title);

		_collectionShardLabel = ShopUi.CreateLabel(16);
		_collectionShardLabel.HorizontalAlignment = HorizontalAlignment.Right;
		_collectionShardLabel.AddThemeColorOverride("font_color", ShopUi.Gold);
		box.AddChild(_collectionShardLabel);
	}

	private void ApplyCollectionOverlayMetrics()
	{
		if (_collectionHeader == null || _collectionScrollMargin == null)
			return;

		float safeTop = GetSafeTopInset();
		float headerTop = safeTop + CollectionHeaderTopPadding;

		_collectionHeader.OffsetLeft = 0.0f;
		_collectionHeader.OffsetTop = headerTop;
		_collectionHeader.OffsetRight = 0.0f;
		_collectionHeader.OffsetBottom = headerTop + CollectionHeaderHeight;

		_collectionScrollMargin.AddThemeConstantOverride(
			"margin_top",
			(int)MathF.Ceiling(
				headerTop
				+ CollectionHeaderHeight
				+ CollectionHeaderGap
			)
		);

		_collectionScrollMargin.AddThemeConstantOverride(
			"margin_bottom",
			(int)MathF.Ceiling(CollectionBottomPadding)
		);

		_collectionHeader.MoveToFront();
	}

	private void CreateRoomCards(
		int roomIndex)
	{
		RoomData room = _state.Rooms[roomIndex];

		Texture2D?[] defaultTextures =
		[
			room.Machines[0].DefaultTexture,
			room.Machines[1].DefaultTexture,
			room.Machines[2].DefaultTexture,
			room.Machines[3].DefaultTexture
		];

		MachineSkinCardView defaultCard = CreateCollectionCard(
			_ownedSkinGrid,
			roomIndex,
			MachineSkinCatalog.DefaultSkinId,
			room.Name.ToUpperInvariant() + " • DEFAULT",
			0.0,
			defaultTextures,
			true,
			() => HandleResult(_service.UseDefault(roomIndex))
		);

		_defaultCards[roomIndex] = defaultCard;

		foreach (MachineSkinDefinition skin in MachineSkinCatalog.GetForRoom(roomIndex))
		{
			Texture2D?[] machineTextures =
			[
				skin.MachineTextures.Count > 0 ? skin.MachineTextures[0] : null,
				skin.MachineTextures.Count > 1 ? skin.MachineTextures[1] : null,
				skin.MachineTextures.Count > 2 ? skin.MachineTextures[2] : null,
				skin.MachineTextures.Count > 3 ? skin.MachineTextures[3] : null
			];

			MachineSkinCardView card = CreateCollectionCard(
				_service.IsOwned(skin.Id)
					? _ownedSkinGrid
					: _availableSkinGrid,
				roomIndex,
				skin.Id,
				room.Name.ToUpperInvariant()
					+ " • "
					+ skin.Name.ToUpperInvariant(),
				skin.Cost,
				machineTextures,
				skin.IsComplete,
				() => OnSkinPressed(skin)
			);

			_skinCards[skin.Id] = card;
		}

	}

	private MachineSkinCardView CreateCollectionCard(
		GridContainer parent,
		int roomIndex,
		string skinId,
		string title,
		double cost,
		IReadOnlyList<Texture2D?> machineTextures,
		bool assetsComplete,
		Action pressed)
	{
		Color accent = RoomThemePalette.GetAccentColor(roomIndex);

		PanelContainer panel = new()
		{
			Name = "MachineSkinCard_" + skinId,
			CustomMinimumSize = new Vector2(0, 300),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			MouseFilter = Control.MouseFilterEnum.Pass
		};

		StyleBoxFlat panelStyle = ShopUi.CreatePanelStyle();
		panelStyle.BorderColor = new Color(accent.R, accent.G, accent.B, 0.66f);
		panel.AddThemeStyleboxOverride("panel", panelStyle);
		parent.AddChild(panel);

		MarginContainer margin = new();
		margin.AddThemeConstantOverride("margin_left", 10);
		margin.AddThemeConstantOverride("margin_top", 10);
		margin.AddThemeConstantOverride("margin_right", 10);
		margin.AddThemeConstantOverride("margin_bottom", 10);
		panel.AddChild(margin);

		VBoxContainer box = new()
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		box.AddThemeConstantOverride("separation", 7);
		margin.AddChild(box);

		Label name = ShopUi.CreateLabel(16);
		name.Text = title;
		name.CustomMinimumSize = new Vector2(0, 44);
		name.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		name.AddThemeColorOverride("font_color", accent);
		box.AddChild(name);

		List<Texture2D> availableTextures = CollectAvailableTextures(machineTextures);
		int previewIndex = availableTextures.Count > 0
			? Random.Shared.Next(availableTextures.Count)
			: -1;

		Texture2D? previewTexture = previewIndex >= 0
			? availableTextures[previewIndex]
			: null;

		TextureButton preview = new()
		{
			TextureNormal = previewTexture,
			IgnoreTextureSize = true,
			StretchMode = TextureButton.StretchModeEnum.KeepAspectCentered,
			CustomMinimumSize = new Vector2(0, 150),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			FocusMode = Control.FocusModeEnum.None,
			Disabled = previewTexture == null,
			TooltipText = previewTexture != null
				? "Tap to view all four machine tiers"
				: ""
		};

		preview.Pressed += () =>
		{
			if (CollectionActionBlocked())
				return;

			_previewOverlay.OpenGallery(
				_state.Rooms[roomIndex].Name.ToUpperInvariant()
					+ " • "
					+ title,
				machineTextures,
				new string[]
				{
					"MACHINE 1",
					"MACHINE 2",
					"MACHINE 3",
                    "MACHINE 4"
				}
			);
		};

		box.AddChild(preview);

		if (availableTextures.Count > 1)
		{
			_rotatingPreviews.Add(
				new RotatingPreviewState(
					preview,
					availableTextures,
					previewIndex
				)
			);
		}

		Label status = ShopUi.CreateMutedLabel(11);
		status.CustomMinimumSize = new Vector2(0, 26);
		status.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		box.AddChild(status);

		Button button = CreateActionButton(44);
		button.AddThemeFontSizeOverride("font_size", 13);
		button.Pressed += () =>
		{
			if (CollectionActionBlocked())
				return;

			pressed();
		};
		box.AddChild(button);

		return new MachineSkinCardView(
			panel,
			roomIndex,
			skinId,
			status,
			button,
			cost,
			assetsComplete
		);
	}

	public void OpenCollection()
	{
		_previewOverlay?.Close();
		ApplyCollectionOverlayMetrics();
		Refresh();

		_collectionMobileScroll?.ResetMotion();
		_collectionScroll.ScrollVertical = 0;

		_mainLayout.Hide();
		_collectionRoot.Show();
		_collectionRoot.MoveToFront();
		_collectionHeader.MoveToFront();

		StartPreviewCycling();
	}

	public void CloseCollection()
	{
		if (_collectionRoot == null)
			return;

		_previewOverlay?.Close();
		_collectionMobileScroll?.ResetMotion();
		StopPreviewCycling();
		_collectionRoot.Hide();
		_mainLayout?.Show();
	}

	private void OnSkinPressed(
		MachineSkinDefinition skin)
	{
		MachineSkinResult result =
			_service.IsOwned(skin.Id)
				? _service.Equip(skin.Id)
				: _service.BuyAndEquip(skin.Id);

		HandleResult(result);
	}

	private void HandleResult(
		MachineSkinResult result)
	{
		MessageRequested?.Invoke(result.Message);

		if (result.Changed)
		{
			Input.VibrateHandheld(14, 0.12f);
			StateChanged?.Invoke();
		}

		Refresh();
	}

	public void Refresh()
	{
		if (_entryStatus == null || _entryButton == null)
			return;

		int customRooms = 0;

		for (int roomIndex = 0; roomIndex < _state.Rooms.Count; roomIndex++)
		{
			if (!_service.GetEquippedSkinId(roomIndex).Equals(
				MachineSkinCatalog.DefaultSkinId,
				StringComparison.OrdinalIgnoreCase
			))
			{
				customRooms++;
			}
		}

		_entryStatus.Text =
			customRooms == 0
				? "ALL ROOMS: DEFAULT"
				: customRooms
					+ " ROOM"
					+ (customRooms == 1 ? "" : "S")
					+ " CUSTOMIZED";

		_entryStatus.AddThemeColorOverride(
			"font_color",
			customRooms > 0
				? ShopUi.Green
				: ShopUi.TextSecondary
		);

		_entryButton.Text = "OPEN COLLECTION";
		_entryButton.Disabled = false;

		// Keep the collection header stable. The old NumberFormatter value
		// could be 5.00 while the global Shop normalizer changed it to 5 on
		// the next frame, which caused the visible "zucken".
		if (_collectionShardLabel != null)
		{
			_collectionShardLabel.Text =
                "DATA SHARDS: "
				+ FormatShardAmount(_state.Shop.DataShards);
		}

		RefreshCollectionGrouping();

		foreach (KeyValuePair<int, MachineSkinCardView> pair in _defaultCards)
		{
			MachineSkinCardView card = pair.Value;

			if (_service.IsEquipped(pair.Key, MachineSkinCatalog.DefaultSkinId))
			{
				SetActive(card);
			}
			else
			{
				card.Status.Text = "OWNED";
				card.Status.AddThemeColorOverride("font_color", ShopUi.Gold);
				card.Button.Text = "EQUIP";
				card.Button.Disabled = false;
			}
		}

		foreach (MachineSkinDefinition skin in MachineSkinCatalog.GetAll())
		{
			if (!_skinCards.TryGetValue(skin.Id, out MachineSkinCardView? card))
				continue;

			if (!card.AssetsComplete)
			{
				card.Status.Text = "ASSETS INCOMPLETE";
				card.Status.AddThemeColorOverride(
					"font_color",
					ShopUi.TextSecondary
				);
				card.Button.Text = "UNAVAILABLE";
				card.Button.Disabled = true;
				continue;
			}

			if (_service.IsEquipped(skin.RoomIndex, skin.Id))
			{
				SetActive(card);
				continue;
			}

			if (_service.IsOwned(skin.Id))
			{
				card.Status.Text = "OWNED";
				card.Status.AddThemeColorOverride("font_color", ShopUi.Gold);
				card.Button.Text = "EQUIP";
				card.Button.Disabled = false;
				continue;
			}

			card.Status.Text = "NOT OWNED";
			card.Status.AddThemeColorOverride(
				"font_color",
				ShopUi.TextSecondary
			);

			card.Button.Text =
                "BUY • "
				+ FormatShardAmount(skin.Cost)
				+ " SHARDS";

			card.Button.Disabled = _state.Shop.DataShards < skin.Cost;
		}
	}

	private void RefreshCollectionGrouping()
	{
		int ownedIndex = 0;
		int availableIndex = 0;

		for (int roomIndex = 0; roomIndex < _state.Rooms.Count; roomIndex++)
		{
			if (_defaultCards.TryGetValue(roomIndex, out MachineSkinCardView? defaultCard))
			{
				PlaceCard(
					defaultCard,
					_ownedSkinGrid,
					ownedIndex++
				);
			}
		}

		foreach (MachineSkinDefinition skin in MachineSkinCatalog.GetAll())
		{
			if (!_skinCards.TryGetValue(skin.Id, out MachineSkinCardView? card))
				continue;

			if (_service.IsOwned(skin.Id))
			{
				PlaceCard(
					card,
					_ownedSkinGrid,
					ownedIndex++
				);
			}
			else
			{
				PlaceCard(
					card,
					_availableSkinGrid,
					availableIndex++
				);
			}
		}
	}

	private static void PlaceCard(
		MachineSkinCardView card,
		GridContainer target,
		int index)
	{
		if (!GodotObject.IsInstanceValid(card.Panel))
			return;

		Node? currentParent = card.Panel.GetParent();

		if (currentParent != target)
		{
			currentParent?.RemoveChild(card.Panel);
			target.AddChild(card.Panel);
		}

		int maxIndex = Math.Max(
			0,
			target.GetChildCount() - 1
		);

		target.MoveChild(
			card.Panel,
			Math.Min(index, maxIndex)
		);
	}

	private static void SetActive(
		MachineSkinCardView card)
	{
		card.Status.Text = "● ACTIVE";
		card.Status.AddThemeColorOverride("font_color", ShopUi.Green);
		card.Button.Text = "ACTIVE";
		card.Button.Disabled = true;
	}

	private void CreatePreviewCycleTimer()
	{
		_previewCycleTimer = new Timer
		{
			Name = "MachineSkinPreviewCycleTimer",
			WaitTime = PreviewCycleSeconds,
			OneShot = false,
			Autostart = false
		};

		_previewCycleTimer.Timeout += CyclePreviewImages;
		_collectionRoot.AddChild(_previewCycleTimer);
	}

	private void StartPreviewCycling()
	{
		if (
			_previewCycleTimer == null
			|| _rotatingPreviews.Count == 0
		)
		{
			return;
		}

		_previewCycleTimer.Stop();
		_previewCycleTimer.Start(PreviewCycleSeconds);
	}

	private void StopPreviewCycling()
	{
		_previewCycleTimer?.Stop();

		foreach (RotatingPreviewState state in _rotatingPreviews)
		{
			state.Transition?.Kill();
			state.Transition = null;

			if (!GodotObject.IsInstanceValid(state.Preview))
				continue;

			state.Preview.Modulate = Colors.White;
			state.Preview.Scale = Vector2.One;
		}
	}

	private void CyclePreviewImages()
	{
		if (!CollectionVisible)
			return;

		foreach (RotatingPreviewState state in _rotatingPreviews)
			AnimatePreviewToNextTexture(state);
	}

	private void AnimatePreviewToNextTexture(
		RotatingPreviewState state)
	{
		if (
			!GodotObject.IsInstanceValid(state.Preview)
			|| state.Textures.Count < 2
		)
		{
			return;
		}

		int nextIndex = state.CurrentIndex;

		while (nextIndex == state.CurrentIndex)
			nextIndex = Random.Shared.Next(state.Textures.Count);

		state.Transition?.Kill();

		state.Preview.PivotOffset = state.Preview.Size / 2.0f;
		state.Preview.Modulate = Colors.White;
		state.Preview.Scale = Vector2.One;

		Tween tween = _root.CreateTween();
		state.Transition = tween;

		tween.SetTrans(Tween.TransitionType.Cubic);
		tween.SetEase(Tween.EaseType.InOut);

		tween.TweenProperty(
			state.Preview,
			"modulate:a",
			0.0f,
			PreviewFadeOutSeconds
		);

		tween.Parallel().TweenProperty(
			state.Preview,
			"scale",
			PreviewTransitionScale,
			PreviewFadeOutSeconds
		);

		tween.TweenCallback(
			Callable.From(() =>
			{
				if (!GodotObject.IsInstanceValid(state.Preview))
					return;

				state.CurrentIndex = nextIndex;
				state.Preview.TextureNormal = state.Textures[nextIndex];
			})
		);

		tween.TweenProperty(
			state.Preview,
			"modulate:a",
			1.0f,
			PreviewFadeInSeconds
		);

		tween.Parallel().TweenProperty(
			state.Preview,
			"scale",
			Vector2.One,
			PreviewFadeInSeconds
		);
	}

	private static List<Texture2D> CollectAvailableTextures(
		IReadOnlyList<Texture2D?> textures)
	{
		List<Texture2D> available = new();

		foreach (Texture2D? texture in textures)
		{
			if (texture != null)
				available.Add(texture);
		}

		return available;
	}

	private static string FormatShardAmount(
		double value)
	{
		double whole = Math.Max(
			0.0,
			Math.Truncate(value)
		);

		return whole.ToString(
			"0",
			CultureInfo.InvariantCulture
		);
	}

	private Node? DetachBottomSpacer()
	{
		if (_content.GetChildCount() == 0)
			return null;

		Node candidate = _content.GetChild(_content.GetChildCount() - 1);

		if (candidate is PanelContainer || candidate.GetChildCount() > 0)
			return null;

		_content.RemoveChild(candidate);
		return candidate;
	}

	private static MarginContainer CreateCardMargin()
	{
		MarginContainer margin = new();
		margin.AddThemeConstantOverride("margin_left", 14);
		margin.AddThemeConstantOverride("margin_top", 14);
		margin.AddThemeConstantOverride("margin_right", 14);
		margin.AddThemeConstantOverride("margin_bottom", 14);
		return margin;
	}

	private static Button CreateActionButton(
		float height)
	{
		Button button = new()
		{
			CustomMinimumSize = new Vector2(0, height),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			FocusMode = Control.FocusModeEnum.None
		};

		ShopUi.ApplyPrimaryButtonStyle(button);
		return button;
	}

	private static GridContainer CreateSkinGrid(
		string name)
	{
		GridContainer grid = new()
		{
			Name = name,
			Columns = GridColumns,
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};

		grid.AddThemeConstantOverride("h_separation", 12);
		grid.AddThemeConstantOverride("v_separation", 12);

		return grid;
	}

	private static void AddCollectionDivider(
		VBoxContainer parent)
	{
		AddSpacer(parent, 4);

		HSeparator divider = new()
		{
			CustomMinimumSize = new Vector2(0, 22),
			MouseFilter = Control.MouseFilterEnum.Ignore
		};

		parent.AddChild(divider);
		AddSpacer(parent, 4);
	}

	private static void CreateSectionHeader(
		VBoxContainer parent,
		string title,
		string subtitle)
	{
		PanelContainer panel = new()
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		panel.AddThemeStyleboxOverride("panel", ShopUi.CreateSectionStyle());
		parent.AddChild(panel);

		MarginContainer margin = new();
		margin.AddThemeConstantOverride("margin_left", 14);
		margin.AddThemeConstantOverride("margin_right", 14);
		margin.AddThemeConstantOverride("margin_top", 8);
		margin.AddThemeConstantOverride("margin_bottom", 8);
		panel.AddChild(margin);

		VBoxContainer box = new();
		box.AddThemeConstantOverride("separation", 1);
		margin.AddChild(box);

		Label titleLabel = ShopUi.CreateLabel(19);
		titleLabel.Text = title;
		titleLabel.HorizontalAlignment = HorizontalAlignment.Left;
		titleLabel.AddThemeColorOverride("font_color", ShopUi.Gold);
		box.AddChild(titleLabel);

		Label subtitleLabel = ShopUi.CreateMutedLabel(12);
		subtitleLabel.Text = subtitle;
		subtitleLabel.HorizontalAlignment = HorizontalAlignment.Left;
		box.AddChild(subtitleLabel);
	}

	private static void AddSpacer(
		Container parent,
		float height)
	{
		parent.AddChild(
			new Control
			{
				CustomMinimumSize = new Vector2(0, height),
				MouseFilter = Control.MouseFilterEnum.Ignore
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
		string os = OS.GetName();

		if (os != "Android" && os != "iOS")
			return 0.0f;

		Rect2I safeArea = DisplayServer.GetDisplaySafeArea();
		Vector2I windowSize = DisplayServer.WindowGetSize();
		Rect2 viewportRect = _root.GetViewport().GetVisibleRect();

		if (
			windowSize.X <= 0
			|| windowSize.Y <= 0
			|| safeArea.Size.X <= 0
			|| safeArea.Size.Y <= 0
		)
		{
			return 34.0f;
		}

		float scaleY = viewportRect.Size.Y / windowSize.Y;
		float top = safeArea.Position.Y * scaleY;

		return MathF.Max(top, 26.0f);
	}

	private sealed class RotatingPreviewState
	{
		public TextureButton Preview { get; }
		public IReadOnlyList<Texture2D> Textures { get; }
		public int CurrentIndex { get; set; }
		public Tween? Transition { get; set; }

		public RotatingPreviewState(
			TextureButton preview,
			IReadOnlyList<Texture2D> textures,
			int currentIndex)
		{
			Preview = preview;
			Textures = textures;
			CurrentIndex = currentIndex;
		}
	}

	private sealed class MachineSkinCardView
	{
		public PanelContainer Panel { get; }
		public int RoomIndex { get; }
		public string SkinId { get; }
		public Label Status { get; }
		public Button Button { get; }
		public double Cost { get; }
		public bool AssetsComplete { get; }

		public MachineSkinCardView(
			PanelContainer panel,
			int roomIndex,
			string skinId,
			Label status,
			Button button,
			double cost,
			bool assetsComplete)
		{
			Panel = panel;
			RoomIndex = roomIndex;
			SkinId = skinId;
			Status = status;
			Button = button;
			Cost = cost;
			AssetsComplete = assetsComplete;
		}
	}
}
