using Godot;
using System;

namespace IdleAi;

public sealed class LabController
{
	private const float LabButtonWidth =
		92.0f;


	private const float LabButtonHeight =
		78.0f;


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


	private Control _lockedContent =
		null!;


	private ScrollContainer _scroll =
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


		_page.SetAnchorsPreset(
			Control.LayoutPreset.FullRect
		);


		_page.OffsetBottom =
			-88;


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
		MarginContainer margin =
			new();


		margin.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);


		margin.AddThemeConstantOverride(
			"margin_left",
			18
		);


		margin.AddThemeConstantOverride(
			"margin_top",
			0
		);


		margin.AddThemeConstantOverride(
			"margin_right",
			18
		);


		margin.AddThemeConstantOverride(
			"margin_bottom",
			0
		);


		_page.AddChild(
			margin
		);


		VBoxContainer main =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,

				SizeFlagsVertical =
					Control.SizeFlags.ExpandFill
			};


		main.AddThemeConstantOverride(
			"separation",
			0
		);


		margin.AddChild(
			main
		);


		_header =
			new LabHeaderView();


		_header.BuyRequested +=
			BuyResearchPoints;


		main.AddChild(
			_header.Root
		);


		_lockedContent =
			CreateLockedContent();


		main.AddChild(
			_lockedContent
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
					ScrollContainer.ScrollMode.ShowNever,

				ClipContents =
					true,

				MouseFilter =
					Control.MouseFilterEnum.Pass
			};


		main.AddChild(
			_scroll
		);


		_scroll.AddChild(
			CreateUnlockedContent()
		);


		CreateMobileScrolling();
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


		Refresh();


		_page.Show();

		_page.MoveToFront();


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
