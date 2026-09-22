using Godot;
using System;

namespace IdleAi;

public sealed class RoomUiController
{
	private const int MachineHapticDurationMs =
		18;


	private const float MachineHapticStrength =
		0.18f;


	private const float MainHorizontalPadding =
		16.0f;


	private const float TopBarTopPadding =
		10.0f;


	private const float TopBarHeight =
		96.0f;


	private const float ModeTabsGap =
		8.0f;


	private const float ModeTabsHeight =
		56.0f;


	private const float ContentGap =
		12.0f;


	private const float BottomBarHeight =
		174.0f;


	private const float BottomContentGap =
		24.0f;


	private readonly Game _root;

	private readonly GameState _state;

	private readonly ProgressionService _progression;

	private readonly ProductionService _production;


	private TextureRect _background =
		null!;


	private VBoxContainer _mainLayout =
		null!;


	private MarginContainer _roomPanel =
		null!;


	private VBoxContainer _roomVBox =
		null!;


	private ScrollContainer _scroll =
		null!;


	private GridContainer _slotGrid =
		null!;


	private AmbientBackgroundController _ambient =
		null!;


	private MobileScrollController _mobileScroll =
		null!;


	private Control _contentLayer =
		null!;


	private Control _overlayLayer =
		null!;


	private Control _topBar =
		null!;


	private Control _bottomBar =
		null!;


	private CenterContainer? _pipelineNavigation;

	private CenterContainer? _infrastructureNavigation;

	private CenterContainer? _quantumNavigation;


	private ScrollContainer? _pipelineScroll;

	private ScrollContainer? _infrastructureScroll;

	private ScrollContainer? _quantumScroll;


	private Control _topSpacerLeft =
		null!;


	private Control _topSpacerRight =
		null!;


	private Control _bottomSpacerLeft =
		null!;


	private Control _bottomSpacerRight =
		null!;


	private bool _overlayLayoutEnabled;


	public event Action<int>? SlotActionRequested;

	public event Action<int>? DetailsRequested;

	public event Action<string>? MessageRequested;


	public RoomUiController(
		Game root,
		GameState state,
		ProgressionService progression,
		ProductionService production)
	{
		_root =
			root;


		_state =
			state;


		_progression =
			progression;


		_production =
			production;
	}


	// ==================================================
	// INITIALIZE
	// ==================================================

	public void Initialize()
	{
		CacheNodes();

		ConfigureLayout();

		ConfigureBackground();

		CreateAmbientBackground();

		CreateSlotViews();

		CreateMobileScrolling();


		_ambient.SetRoom(
			_state.CurrentRoomIndex
		);
	}


	// ==================================================
	// NODES
	// ==================================================

	private void CacheNodes()
	{
		_background =
			_root.GetNode<TextureRect>(
				"Background"
			);


		_mainLayout =
			_root.GetNode<VBoxContainer>(
				"MarginContainer/VBoxContainer"
			);


		_roomPanel =
			_root.GetNode<MarginContainer>(
				"MarginContainer/VBoxContainer/RoomPanel"
			);


		_roomVBox =
			_root.GetNode<VBoxContainer>(
				"MarginContainer/VBoxContainer/RoomPanel/RoomVBox"
			);


		_scroll =
			_root.GetNode<ScrollContainer>(
				"MarginContainer/VBoxContainer/RoomPanel/RoomVBox/ScrollContainer"
			);


		_slotGrid =
			_root.GetNode<GridContainer>(
				"MarginContainer/VBoxContainer/RoomPanel/RoomVBox/ScrollContainer/SlotGrid"
			);
	}


	// ==================================================
	// LAYOUT
	// ==================================================

	private void ConfigureLayout()
	{
		_mainLayout.AddThemeConstantOverride(
			"separation",
			0
		);


		_scroll.HorizontalScrollMode =
			ScrollContainer.ScrollMode.Disabled;


		_scroll.VerticalScrollMode =
			ScrollContainer.ScrollMode.ShowNever;


		_scroll.ClipContents =
			true;
	}


	private void ConfigureBackground()
	{
		_background.ExpandMode =
			TextureRect.ExpandModeEnum.IgnoreSize;


		_background.StretchMode =
			TextureRect.StretchModeEnum.KeepAspectCovered;


		_background.MouseFilter =
			Control.MouseFilterEnum.Ignore;
	}


	// ==================================================
	// OVERLAY LAYOUT
	// ==================================================

	public void EnableOverlayLayout()
	{
		if (_overlayLayoutEnabled)
			return;


		_overlayLayoutEnabled =
			true;


		_topBar =
			_root.GetNode<Control>(
				"MarginContainer/VBoxContainer/TopBar"
			);


		_bottomBar =
			_root.GetNode<Control>(
				"BottomBar"
			);


		_pipelineNavigation =
			_roomVBox.GetNodeOrNull<CenterContainer>(
				"PipelineNavigationCenter"
			);


		_infrastructureNavigation =
			_roomVBox.GetNodeOrNull<CenterContainer>(
				"InfrastructureNavigationCenter"
			);


		_quantumNavigation =
			_roomVBox.GetNodeOrNull<CenterContainer>(
				"QuantumNavigationCenter"
			);


		_pipelineScroll =
			_roomVBox.GetNodeOrNull<ScrollContainer>(
				"PipelineScroll"
			);


		_infrastructureScroll =
			_roomVBox.GetNodeOrNull<ScrollContainer>(
				"InfrastructureScroll"
			);


		_quantumScroll =
			_roomVBox.GetNodeOrNull<ScrollContainer>(
				"QuantumScroll"
			);


		CreateOverlayLayers();

		MoveScrollableViewsToContentLayer();

		MoveChromeToOverlayLayer();

		_roomPanel.Hide();


		_root.GetViewport().SizeChanged +=
			ApplyOverlayLayoutMetrics;


		ApplyOverlayLayoutMetrics();

		_overlayLayer.MoveToFront();

		_bottomBar.MoveToFront();
	}


	private void CreateOverlayLayers()
	{
		_contentLayer =
			new Control
			{
				Name =
					"RoomContentLayer",

				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};


		_contentLayer.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);


		_root.AddChild(
			_contentLayer
		);


		_overlayLayer =
			new Control
			{
				Name =
					"RoomOverlayLayer",

				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};


		_overlayLayer.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);


		_root.AddChild(
			_overlayLayer
		);
	}


	private void MoveScrollableViewsToContentLayer()
	{
		ReparentScrollToContentLayer(
			_scroll
		);


		if (_pipelineScroll != null)
		{
			ReparentScrollToContentLayer(
				_pipelineScroll
			);
		}


		if (_infrastructureScroll != null)
		{
			ReparentScrollToContentLayer(
				_infrastructureScroll
			);
		}


		if (_quantumScroll != null)
		{
			ReparentScrollToContentLayer(
				_quantumScroll
			);
		}
	}


	private void ReparentScrollToContentLayer(
		ScrollContainer scroll)
	{
		scroll.Reparent(
			_contentLayer,
			false
		);


		scroll.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);


		scroll.SizeFlagsHorizontal =
			Control.SizeFlags.ExpandFill;


		scroll.SizeFlagsVertical =
			Control.SizeFlags.ExpandFill;


		scroll.HorizontalScrollMode =
			ScrollContainer.ScrollMode.Disabled;


		scroll.VerticalScrollMode =
			ScrollContainer.ScrollMode.ShowNever;


		scroll.ClipContents =
			true;
	}


	private void MoveChromeToOverlayLayer()
	{
		_topBar.Reparent(
			_overlayLayer,
			false
		);


		PrepareTopOverlayControl(
			_topBar
		);


		if (_pipelineNavigation != null)
		{
			_pipelineNavigation.Reparent(
				_overlayLayer,
				false
			);


			PrepareTopOverlayControl(
				_pipelineNavigation
			);
		}


		if (_infrastructureNavigation != null)
		{
			_infrastructureNavigation.Reparent(
				_overlayLayer,
				false
			);


			PrepareTopOverlayControl(
				_infrastructureNavigation
			);
		}


		if (_quantumNavigation != null)
		{
			_quantumNavigation.Reparent(
				_overlayLayer,
				false
			);


			PrepareTopOverlayControl(
				_quantumNavigation
			);
		}
	}


	private static void PrepareTopOverlayControl(
		Control control)
	{
		control.AnchorLeft =
			0.0f;


		control.AnchorTop =
			0.0f;


		control.AnchorRight =
			1.0f;


		control.AnchorBottom =
			0.0f;

	}


	private void ApplyOverlayLayoutMetrics()
	{
		if (!_overlayLayoutEnabled)
			return;


		SafeInsets safe =
			GetSafeInsets();


		float horizontalLeft =
			MainHorizontalPadding
			+ safe.Left;


		float horizontalRight =
			MainHorizontalPadding
			+ safe.Right;


		float topBarTop =
			TopBarTopPadding
			+ safe.Top;


		float topBarBottom =
			topBarTop
			+ TopBarHeight;


		_topBar.OffsetLeft =
			horizontalLeft;


		_topBar.OffsetTop =
			topBarTop;


		_topBar.OffsetRight =
			-horizontalRight;


		_topBar.OffsetBottom =
			topBarBottom;


		float tabsTop =
			topBarBottom
			+ ModeTabsGap;


		float tabsBottom =
			tabsTop
			+ ModeTabsHeight;


		PositionNavigationOverlay(
			_pipelineNavigation,
			horizontalLeft,
			horizontalRight,
			tabsTop,
			tabsBottom
		);


		PositionNavigationOverlay(
			_infrastructureNavigation,
			horizontalLeft,
			horizontalRight,
			tabsTop,
			tabsBottom
		);


		PositionNavigationOverlay(
			_quantumNavigation,
			horizontalLeft,
			horizontalRight,
			tabsTop,
			tabsBottom
		);


		PositionScrollableView(
			_scroll,
			horizontalLeft,
			horizontalRight
		);


		if (_pipelineScroll != null)
		{
			PositionScrollableView(
				_pipelineScroll,
				horizontalLeft,
				horizontalRight
			);
		}


		if (_infrastructureScroll != null)
		{
			PositionScrollableView(
				_infrastructureScroll,
				horizontalLeft,
				horizontalRight
			);
		}


		if (_quantumScroll != null)
		{
			PositionScrollableView(
				_quantumScroll,
				horizontalLeft,
				horizontalRight
			);
		}


		float machineTopPadding =
			GetMachineContentTopPadding(
				safe
			);


		float bottomPadding =
			BottomBarHeight
			+ safe.Bottom
			+ BottomContentGap;


		SetMachineContentPadding(
			machineTopPadding,
			bottomPadding
		);


		float specialTopPadding =
			tabsBottom
			+ ContentGap;


		SetSpecialScrollContentPadding(
			_pipelineScroll,
			specialTopPadding,
			bottomPadding
		);


		SetSpecialScrollContentPadding(
			_infrastructureScroll,
			specialTopPadding,
			bottomPadding
		);


		SetSpecialScrollContentPadding(
			_quantumScroll,
			specialTopPadding,
			bottomPadding
		);
	}


	private static void PositionNavigationOverlay(
		Control? navigation,
		float left,
		float right,
		float top,
		float bottom)
	{
		if (navigation == null)
			return;


		navigation.OffsetLeft =
			left;


		navigation.OffsetTop =
			top;


		navigation.OffsetRight =
			-right;


		navigation.OffsetBottom =
			bottom;


		navigation.CustomMinimumSize =
			Vector2.Zero;
	}


	private static void PositionScrollableView(
		ScrollContainer scroll,
		float left,
		float right)
	{
		scroll.OffsetLeft =
			left;


		scroll.OffsetTop =
			0.0f;


		scroll.OffsetRight =
			-right;


		scroll.OffsetBottom =
			0.0f;
	}


	private float GetMachineContentTopPadding(
		SafeInsets safe)
	{
		float top =
			TopBarTopPadding
			+ safe.Top
			+ TopBarHeight
			+ ContentGap;


		if (_state.CurrentRoomIndex == 0)
		{
			return top;
		}


		return top
			+ ModeTabsGap
			+ ModeTabsHeight;
	}


	private void SetMachineContentPadding(
		float top,
		float bottom)
	{
		_topSpacerLeft.CustomMinimumSize =
			new Vector2(
				0,
				top
			);


		_topSpacerRight.CustomMinimumSize =
			new Vector2(
				0,
				top
			);


		_bottomSpacerLeft.CustomMinimumSize =
			new Vector2(
				0,
				bottom
			);


		_bottomSpacerRight.CustomMinimumSize =
			new Vector2(
				0,
				bottom
			);
	}


	private static void SetSpecialScrollContentPadding(
		ScrollContainer? scroll,
		float top,
		float bottom)
	{
		if (
			scroll == null
			|| scroll.GetChildCount() == 0
		)
		{
			return;
		}


		Control? center =
			scroll.GetChild(
				0
			)
			as Control;


		if (
			center == null
			|| center.GetChildCount() == 0
		)
		{
			return;
		}


		MarginContainer? margin =
			center.GetChild(
				0
			)
			as MarginContainer;


		if (margin == null)
			return;


		margin.AddThemeConstantOverride(
			"margin_top",
			(int)MathF.Ceiling(
				top
			)
		);


		margin.AddThemeConstantOverride(
			"margin_bottom",
			(int)MathF.Ceiling(
				bottom
			)
		);
	}


	// ==================================================
	// SAFE AREA
	// ==================================================

	private readonly record struct SafeInsets(
		float Left,
		float Top,
		float Right,
		float Bottom
	);


	private SafeInsets GetSafeInsets()
	{
		string os =
			OS.GetName();


		if (
			os != "Android"
			&& os != "iOS"
		)
		{
			return new SafeInsets(
				0,
				0,
				0,
				0
			);
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
			return new SafeInsets(
				0,
				34,
				0,
				24
			);
		}


		float scaleX =
			viewportRect.Size.X
			/ windowSize.X;


		float scaleY =
			viewportRect.Size.Y
			/ windowSize.Y;


		float left =
			safeArea.Position.X
			* scaleX;


		float top =
			safeArea.Position.Y
			* scaleY;


		float rightPhysical =
			windowSize.X
			- (
				safeArea.Position.X
				+ safeArea.Size.X
			);


		float bottomPhysical =
			windowSize.Y
			- (
				safeArea.Position.Y
				+ safeArea.Size.Y
			);


		float right =
			rightPhysical
			* scaleX;


		float bottom =
			bottomPhysical
			* scaleY;


		top =
			MathF.Max(
				top,
				26.0f
			);


		bottom =
			MathF.Max(
				bottom,
				12.0f
			);


		return new SafeInsets(
			MathF.Max(
				0,
				left
			),

			MathF.Max(
				0,
				top
			),

			MathF.Max(
				0,
				right
			),

			MathF.Max(
				0,
				bottom
			)
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
					"RoomMobileScroll"
			};


		_root.AddChild(
			_mobileScroll
		);


		_mobileScroll.Setup(
			_scroll,
			allowTopOverscroll:
				true,
			allowBottomOverscroll:
				true
		);
	}


	private bool SlotActionBlocked()
	{
		return _mobileScroll != null
			&& _mobileScroll.ShouldSuppressTap;
	}


	public void ScrollToTop()
	{
		if (
			_mobileScroll != null
			&& GodotObject.IsInstanceValid(
				_mobileScroll
			)
		)
		{
			_mobileScroll.ScrollToTop();

			return;
		}


		if (
			_scroll != null
			&& GodotObject.IsInstanceValid(
				_scroll
			)
		)
		{
			_scroll.ScrollVertical =
				0;
		}
	}


	// ==================================================
	// AMBIENT
	// ==================================================

	private void CreateAmbientBackground()
	{
		_ambient =
			new AmbientBackgroundController(
				_root
			);


		_ambient.Initialize();
	}


	// ==================================================
	// SLOTS
	// ==================================================

	private void CreateSlotViews()
	{
		CreateTopPadding();


		for (
			int i = 0;
			i < 8;
			i++
		)
		{
			MachineSlot slot =
				new();


			slot.Setup(
				i
			);


			slot.UnlockPressed +=
				OnUnlockPressed;


			slot.ManualStartPressed +=
				OnManualStart;


			slot.DetailsPressed +=
				OnDetailsPressed;


			slot.EmptyPressed +=
				OnEmptyPressed;


			_slotGrid.AddChild(
				slot
			);
		}


		CreateBottomPadding();
	}


	private void CreateTopPadding()
	{
		_topSpacerLeft =
			CreatePaddingSpacer(
				"MachineTopSpacerLeft"
			);


		_topSpacerRight =
			CreatePaddingSpacer(
				"MachineTopSpacerRight"
			);


		_slotGrid.AddChild(
			_topSpacerLeft
		);


		_slotGrid.AddChild(
			_topSpacerRight
		);
	}


	private void CreateBottomPadding()
	{
		_bottomSpacerLeft =
			CreatePaddingSpacer(
				"MachineBottomSpacerLeft"
			);


		_bottomSpacerRight =
			CreatePaddingSpacer(
				"MachineBottomSpacerRight"
			);


		_slotGrid.AddChild(
			_bottomSpacerLeft
		);


		_slotGrid.AddChild(
			_bottomSpacerRight
		);
	}


	private static Control CreatePaddingSpacer(
		string name)
	{
		return new Control
		{
			Name =
				name,

			CustomMinimumSize =
				new Vector2(
					0,
					0
				),

			MouseFilter =
				Control.MouseFilterEnum.Ignore
		};
	}


	// ==================================================
	// SLOT ACTIONS
	// ==================================================

	private void OnUnlockPressed(
		int slotIndex)
	{
		if (SlotActionBlocked())
			return;


		SlotActionRequested?.Invoke(
			slotIndex
		);
	}


	private void OnEmptyPressed(
		int slotIndex)
	{
		if (SlotActionBlocked())
			return;


		MachineSlot view =
			GetMachineSlot(
				slotIndex
			);


		view.PlayEmptyPulse();
	}


	private void OnDetailsPressed(
		int slotIndex)
	{
		if (SlotActionBlocked())
			return;


		PlayMachineHaptic();


		DetailsRequested?.Invoke(
			slotIndex
		);
	}


	private void OnManualStart(
		int slotIndex)
	{
		if (SlotActionBlocked())
			return;


		ManualStartResult result =
			_production.TryStartManual(
				_state.CurrentRoomIndex,
				slotIndex
			);


		MessageRequested?.Invoke(
			result.Message
		);
	}


	// ==================================================
	// HAPTICS
	// ==================================================

	private static void PlayMachineHaptic()
	{
		Input.VibrateHandheld(
			MachineHapticDurationMs,
			MachineHapticStrength
		);
	}


	// ==================================================
	// SLOT ACCESS
	// ==================================================

	private MachineSlot GetMachineSlot(
		int slotIndex)
	{
		return (MachineSlot)
			_slotGrid.GetChild(
				slotIndex + 2
			);
	}


	// ==================================================
	// UPDATE
	// ==================================================

	public void UpdateAll(
		bool bringChromeToFront = true)
	{
		_background.Texture =
			_state.CurrentRoom.Background;


		_ambient.SetRoom(
			_state.CurrentRoomIndex
		);


		if (_overlayLayoutEnabled)
		{
			ApplyOverlayLayoutMetrics();


			/*
			 * Modal detail overlays must remain above the
			 * fixed room chrome. During normal gameplay the
			 * chrome stays on top of the scrolling content.
			 */
			if (bringChromeToFront)
			{
				_overlayLayer.MoveToFront();

				_bottomBar.MoveToFront();
			}
		}


		if (_state.CurrentRoomIndex == 0)
		{
			_scroll.Show();
		}


		int count =
			Math.Min(
				8,
				_state.CurrentRoomState.Slots.Count
			);


		for (
			int i = 0;
			i < count;
			i++
		)
		{
			MachineSlot view =
				GetMachineSlot(
					i
				);


			view.ApplyRoomTheme(
				_state.CurrentRoomIndex
			);


			UpdateSlot(
				i
			);
		}
	}


	private void UpdateSlot(
		int index)
	{
		SlotData slot =
			_state.CurrentRoomState
				.Slots[
					index
				];


		MachineSlot view =
			GetMachineSlot(
				index
			);


		if (!slot.Unlocked)
		{
			double cost =
				_progression
					.GetSlotUnlockCost(
						_state.CurrentRoomIndex,
						index
					);


			view.ShowLocked(
				NumberFormatter.Format(
					cost
				),

				_state.CurrentRoom
					.EmptyTexture
			);


			return;
		}


		MachineData machine =
			_state.CurrentRoom
				.Machines[
					slot.MachineTier
				];


		BotDefinition? bot =
			slot.HasBot
				? BotCatalog.Get(
					slot.BotRarity!.Value
				)
				: null;


		view.ShowMachine(
			machine,
			slot,
			bot
		);
	}


	public void UpdateRuntime()
	{
		int count =
			Math.Min(
				8,
				_state.CurrentRoomState.Slots.Count
			);


		for (
			int i = 0;
			i < count;
			i++
		)
		{
			SlotData slot =
				_state.CurrentRoomState
					.Slots[
						i
					];


			if (!slot.Unlocked)
				continue;


			MachineSlot view =
				GetMachineSlot(
					i
				);


			view.UpdateRuntime(
				slot
			);
		}
	}
}
