using Godot;
using System;

namespace IdleAi;

public sealed class LabController
{
	private const float LabButtonWidth =
		92.0f;


	private const float LabButtonHeight =
		78.0f;


	private const float LabHeaderHeight =
		184.0f;


	private const float LabHeaderGap =
		10.0f;


	private const float LabHeaderExtraTopPadding =
		6.0f;


	private const int NavigationHapticDurationMs =
		12;


	private const float NavigationHapticStrength =
		0.12f;


	private readonly Game _root;

	private readonly GameState _state;

	private readonly LabService _service;


	private TextureButton _labButton =
		null!;


	private Control _page =
		null!;


	private MarginContainer _pageMargin =
		null!;


	private Control _layoutRoot =
		null!;


	private Control _lockedContent =
		null!;


	private ScrollContainer _scroll =
		null!;


	private MarginContainer _scrollMargin =
		null!;


	private MobileScrollController _mobileScroll =
		null!;


	private Label _unlockCost =
		null!;


	private Button _unlockButton =
		null!;


	private LabHeaderView _header =
		null!;


	private LabStatusView _status =
		null!;


	private ResearchTreeView _tree =
		null!;


	public event Action<string>? MessageRequested;

	public event Action? StateChanged;

	public event Action? OpenRequested;


	public bool Visible =>
		_page != null
		&& _page.Visible;


	public LabController(
		Game root,
		GameState state,
		LabService service)
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
		CreateBottomBarButton();

		CreatePage();


		_root.GetViewport().SizeChanged +=
			ApplyOverlayMetrics;


		ApplyOverlayMetrics();

		ConnectExistingNavigation();


		Hide();

		Refresh();
	}


	// ==================================================
	// BUTTON
	// ==================================================

	private void CreateBottomBarButton()
	{
		HBoxContainer hbox =
			_root.GetNode<HBoxContainer>(
				"BottomBar/Margin/HBox"
			);


		_labButton =
			new TextureButton
			{
				Name =
					"LabButton",

				CustomMinimumSize =
					new Vector2(
						LabButtonWidth,
						LabButtonHeight
					),

				TextureNormal =
					LabUi.LabIcon,

				IgnoreTextureSize =
					true,

				StretchMode =
					TextureButton.StretchModeEnum
						.KeepAspectCentered
			};


		_labButton.Pressed +=
			OnLabPressed;


		hbox.AddChild(
			_labButton
		);


		hbox.MoveChild(
			_labButton,
			1
		);
	}


	private void OnLabPressed()
	{
		PlayNavigationHaptic();


		TogglePage();
	}


	private static void PlayNavigationHaptic()
	{
		Input.VibrateHandheld(
			NavigationHapticDurationMs,
			NavigationHapticStrength
		);
	}


	private void ConnectExistingNavigation()
	{
		TextureButton map =
			_root.GetNode<TextureButton>(
				"BottomBar/Margin/HBox/MapButton"
			);


		TextureButton shop =
			_root.GetNode<TextureButton>(
				"BottomBar/Margin/HBox/ShopButton"
			);


		/*
		 * IMPORTANT:
		 *
		 * RoomUiController.EnableOverlayLayout() reparents
		 * TopBar from:
		 *
		 * MarginContainer/VBoxContainer/TopBar
		 *
		 * to:
		 *
		 * RoomOverlayLayer/TopBar
		 *
		 * GameUiController.Initialize() runs before this
		 * LabController is created, so the overlay path is
		 * the normal runtime path here.
		 *
		 * Keep the old path as fallback so this controller
		 * also remains compatible if the overlay layout is
		 * ever disabled again.
		 */
		TextureButton? stats =
			_root.GetNodeOrNull<TextureButton>(
				"RoomOverlayLayer/TopBar/Margin/VBox/TopStats/StatsCard"
			);


		stats ??=
			_root.GetNodeOrNull<TextureButton>(
				"MarginContainer/VBoxContainer/TopBar/Margin/VBox/TopStats/StatsCard"
			);


		map.Pressed +=
			Hide;


		shop.Pressed +=
			Hide;


		if (stats != null)
		{
			stats.Pressed +=
				Hide;
		}
		else
		{
			GD.PushWarning(
				"LabController could not find StatsCard. "
					+ "Lab initialization continues without the Stats close hook."
			);
		}
	}


	// ==================================================
	// PAGE
	// ==================================================

	private void CreatePage()
	{
		_page =
			new Control
			{
				Name =
					"LabPage",

				Visible =
					false
			};


		_page.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);


		_root.AddChild(
			_page
		);


		CreateBackground();

		CreateContent();
	}


	private void CreateBackground()
	{
		TextureRect background =
			new()
			{
				Texture =
					LabUi.LabBackground,

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


	private void CreateContent()
	{
		_pageMargin =
			new MarginContainer
			{
				Name =
					"LabMargin",

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


		/*
		 * The ScrollContainer is created FIRST and fills the
		 * complete LabMargin from the physical top of the
		 * display down to the area above BottomBar.
		 *
		 * The Lab header is added afterwards and therefore
		 * behaves like the fixed SHOP / DATA SHARDS overlay.
		 */
		_scroll =
			new ScrollContainer
			{
				Name =
					"LabScroll",

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
			28
		);


		_scroll.AddChild(
			_scrollMargin
		);


		_scrollMargin.AddChild(
			CreateUnlockedContent()
		);


		/*
		 * Locked content is a normal full-page layer below
		 * the fixed Lab header. It is only visible until the
		 * Lab has been unlocked.
		 */
		_lockedContent =
			CreateLockedContent();


		_lockedContent.AnchorLeft =
			0.0f;


		_lockedContent.AnchorTop =
			0.0f;


		_lockedContent.AnchorRight =
			1.0f;


		_lockedContent.AnchorBottom =
			1.0f;


		_layoutRoot.AddChild(
			_lockedContent
		);


		_header =
			new LabHeaderView();


		_header.Root.Name =
			"FixedLabHeader";


		_header.Root.AnchorLeft =
			0.0f;


		_header.Root.AnchorTop =
			0.0f;


		_header.Root.AnchorRight =
			1.0f;


		_header.Root.AnchorBottom =
			0.0f;


		_header.Root.CustomMinimumSize =
			Vector2.Zero;


		_header.BuyRequested +=
			BuyResearchPoints;


		_layoutRoot.AddChild(
			_header.Root
		);


		CreateMobileScrolling();


		ApplyOverlayMetrics();


		_header.Root.MoveToFront();
	}


	// ==================================================
	// MOBILE SCROLLING
	// ==================================================

	private void CreateMobileScrolling()
	{
		_mobileScroll =
			new MobileScrollController
			{
				Name =
					"LabMobileScroll"
			};


		_page.AddChild(
			_mobileScroll
		);


		/*
		 * Use the same two-direction rubber-band behavior
		 * as the room pages.
		 *
		 * This fixes the Lab getting stuck after scrolling
		 * down and makes upward/downward touch scrolling
		 * symmetrical.
		 */
		_mobileScroll.Setup(
			_scroll,
			allowTopOverscroll: true,
			allowBottomOverscroll: true
		);
	}


	// ==================================================
	// OVERLAY METRICS / SAFE AREA
	// ==================================================

	private void ApplyOverlayMetrics()
	{
		if (
			_header == null
			|| _scrollMargin == null
			|| _lockedContent == null
		)
		{
			return;
		}


		float safeTop =
			GetSafeTopInset();


		float headerTop =
			safeTop
			+ LabHeaderExtraTopPadding;


		float headerBottom =
			headerTop
			+ LabHeaderHeight;


		/*
		 * Only the fixed Lab header respects the top safe
		 * inset. The ScrollContainer itself stays at y = 0,
		 * so research cards can scroll behind the notch and
		 * behind the header just like Shop content.
		 */
		_header.Root.OffsetLeft =
			0.0f;


		_header.Root.OffsetTop =
			headerTop;


		_header.Root.OffsetRight =
			0.0f;


		_header.Root.OffsetBottom =
			headerBottom;


		_scrollMargin.AddThemeConstantOverride(
			"margin_top",
			(int)MathF.Ceiling(
				headerBottom
				+ LabHeaderGap
				+ 10.0f
			)
		);


		_lockedContent.OffsetLeft =
			0.0f;


		_lockedContent.OffsetTop =
			headerBottom
			+ LabHeaderGap;


		_lockedContent.OffsetRight =
			0.0f;


		_lockedContent.OffsetBottom =
			0.0f;


		_header.Root.MoveToFront();
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


	private VBoxContainer CreateUnlockedContent()
	{
		VBoxContainer content =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,

				SizeFlagsVertical =
					Control.SizeFlags.ShrinkBegin
			};


		content.AddThemeConstantOverride(
			"separation",
			12
		);


		_status =
			new LabStatusView(
				_state,
				_service
			);


		content.AddChild(
			_status.Root
		);


		_tree =
			new ResearchTreeView(
				_state,
				_service,
				StartResearch,

				() =>
				{
					if (
						_mobileScroll
						!= null
					)
					{
						_mobileScroll
							.ScrollToTop();


						return;
					}


					if (_scroll != null)
					{
						_scroll.ScrollVertical =
							0;
					}
				}
			);


		content.AddChild(
			_tree.Root
		);


		return content;
	}


	// ==================================================
	// LOCKED
	// ==================================================

	private Control CreateLockedContent()
	{
		PanelContainer panel =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,

				SizeFlagsVertical =
					Control.SizeFlags.ExpandFill
			};


		panel.AddThemeStyleboxOverride(
			"panel",
			LabUi.CreateSectionStyle(
				LabUi.LockedColor,
				0.12f
			)
		);


		MarginContainer margin =
			LabUi.CreateMargin(
				18
			);


		panel.AddChild(
			margin
		);


		VBoxContainer content =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,

				SizeFlagsVertical =
					Control.SizeFlags.ExpandFill
			};


		content.AddThemeConstantOverride(
			"separation",
			14
		);


		margin.AddChild(
			content
		);


		TextureRect image =
			new()
			{
				Texture =
					LabUi.LabLockedIcon,

				CustomMinimumSize =
					new Vector2(
						0,
						200
					),

				ExpandMode =
					TextureRect.ExpandModeEnum.IgnoreSize,

				StretchMode =
					TextureRect.StretchModeEnum.KeepAspectCentered
			};


		content.AddChild(
			image
		);


		Label title =
			LabUi.CreateLabel(
				24
			);


		title.Text =
			"LABORATORY LOCKED";


		content.AddChild(
			title
		);


		Label description =
			LabUi.CreateLabel(
				15
			);


		description.Text =
			"Unlock the laboratory to research permanent upgrades.";


		description.AutowrapMode =
			TextServer.AutowrapMode.WordSmart;


		content.AddChild(
			description
		);


		_unlockCost =
			LabUi.CreateLabel(
				18
			);


		content.AddChild(
			_unlockCost
		);


		_unlockButton =
			new Button
			{
				CustomMinimumSize =
					new Vector2(
						0,
						60
					)
			};


		_unlockButton.Pressed +=
			UnlockLab;


		content.AddChild(
			_unlockButton
		);


		return panel;
	}


	// ==================================================
	// ACTIONS
	// ==================================================

	private void UnlockLab()
	{
		LabResult result =
			_service.UnlockLab();


		HandleResult(
			result
		);
	}


	private void BuyResearchPoints()
	{
		LabResult result =
			_service.BuyResearchPoints(
				GameConfig
					.ResearchPointPurchaseAmount
			);


		HandleResult(
			result
		);
	}


	private void StartResearch(
		string researchId)
	{
		LabResult result =
			_service.Research(
				researchId
			);


		HandleResult(
			result
		);
	}


	private void HandleResult(
		LabResult result)
	{
		MessageRequested?.Invoke(
			result.Message
		);


		Refresh();


		if (result.Changed)
		{
			StateChanged?.Invoke();
		}
	}


	// ==================================================
	// NAVIGATION
	// ==================================================

	private void TogglePage()
	{
		if (_page.Visible)
		{
			Hide();


			return;
		}


		OpenRequested?.Invoke();


		HideOtherPages();


		ApplyOverlayMetrics();

		Refresh();


		_page.Show();

		_page.MoveToFront();


		SetRegularRoomChromeVisible(
			false
		);


		_header.Root.MoveToFront();


		_root.GetNode<Control>(
			"BottomBar"
		).MoveToFront();
	}


	private void HideOtherPages()
	{
		_root.GetNode<Control>(
			"MapPage"
		).Hide();


		_root.GetNode<Control>(
			"ShopPage"
		).Hide();


		_root.GetNode<Control>(
			"StatsOverlay"
		).Hide();


		_root.GetNode<Control>(
			"PrestigeConfirmOverlay"
		).Hide();


		_root.GetNode<PanelContainer>(
			"TokenPopup"
		).Hide();


		_root.GetNode<PanelContainer>(
			"LevelPopup"
		).Hide();
	}


	public void Hide()
	{
		_mobileScroll?.ResetMotion();


		_page?.Hide();


		bool anotherPageVisible =
			_root.GetNodeOrNull<Control>(
				"MapPage"
			)?.Visible
			== true
			|| _root.GetNodeOrNull<Control>(
				"ShopPage"
			)?.Visible
			== true;


		if (!anotherPageVisible)
		{
			SetRegularRoomChromeVisible(
				true
			);
		}
	}


	private void SetRegularRoomChromeVisible(
		bool visible)
	{
		Control? roomChrome =
			_root.GetNodeOrNull<Control>(
				"RoomOverlayLayer"
			);


		if (roomChrome == null)
			return;


		roomChrome.Visible =
			visible;
	}


	// ==================================================
	// REFRESH
	// ==================================================

	public void Refresh()
	{
		if (_page == null)
			return;


		bool unlocked =
			_state.Lab.Unlocked;


		_lockedContent.Visible =
			!unlocked;


		_scroll.Visible =
			unlocked;


		int amount =
			GameConfig
				.ResearchPointPurchaseAmount;


		double rpCost =
			_service
				.GetResearchPointPurchaseCost(
					amount
				);


		_header.Refresh(
			unlocked,
			_state.Lab.ResearchPoints,
			_state.Tokens,
			amount,
			rpCost
		);


		_unlockCost.Text =
			"Unlock cost: "
			+ NumberFormatter.Format(
				GameConfig.LabUnlockCost
			)
			+ " Tokens";


		_unlockButton.Text =
			"UNLOCK LAB • "
			+ NumberFormatter.Format(
				GameConfig.LabUnlockCost
			);


		_unlockButton.Disabled =
			_state.Tokens
			< GameConfig.LabUnlockCost;


		if (!unlocked)
			return;


		_status.Refresh();

		_tree.Refresh();
	}
}
