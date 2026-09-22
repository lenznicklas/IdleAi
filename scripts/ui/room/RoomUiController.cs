using Godot;
using System;
using System.Collections.Generic;

namespace IdleAi;

public sealed class RoomUiController
{
	private const float TopSlotPadding =
		36.0f;


	private const int MachineHapticDurationMs =
		18;


	private const float MachineHapticStrength =
		0.18f;


	private readonly Game _root;

	private readonly GameState _state;

	private readonly ProgressionService _progression;

	private readonly ProductionService _production;


	private TextureRect _background =
		null!;


	private VBoxContainer _mainLayout =
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


	private readonly List<Control> _topSpacers =
		[];


	private bool _pipelineButtonsHooked;


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
			_scroll
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
		for (
			int i = 0;
			i < 2;
			i++
		)
		{
			Control spacer =
				new()
				{
					CustomMinimumSize =
						new Vector2(
							0,
							TopSlotPadding
						),

					MouseFilter =
						Control.MouseFilterEnum.Ignore
				};


			_topSpacers.Add(
				spacer
			);


			_slotGrid.AddChild(
				spacer
			);
		}
	}


	private void CreateBottomPadding()
	{
		for (
			int i = 0;
			i < 2;
			i++
		)
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


			_slotGrid.AddChild(
				spacer
			);
		}
	}


	// ==================================================
	// SPECIAL ROOM NAVIGATION
	// ==================================================

	private void UpdateSpecialRoomLayout()
	{
		float machineTopPadding =
			_state.CurrentRoomIndex == 0
				? TopSlotPadding
				: 0.0f;


		foreach (
			Control spacer
			in _topSpacers
		)
		{
			spacer.CustomMinimumSize =
				new Vector2(
					0,
					machineTopPadding
				);
		}


		SetNavigationHeight(
			"PipelineNavigationCenter",
			56.0f
		);


		SetNavigationHeight(
			"InfrastructureNavigationCenter",
			56.0f
		);


		SetNavigationHeight(
			"QuantumNavigationCenter",
			56.0f
		);


		RemoveSpecialViewTopMargin(
			"PipelineScroll"
		);


		RemoveSpecialViewTopMargin(
			"InfrastructureScroll"
		);


		RemoveSpecialViewTopMargin(
			"QuantumScroll"
		);


		HookPipelineButtons();


		Callable
			.From(
				ApplyPipelineTabTheme
			)
			.CallDeferred();
	}


	private void SetNavigationHeight(
		string nodeName,
		float height)
	{
		CenterContainer? navigation =
			_roomVBox.GetNodeOrNull<CenterContainer>(
				nodeName
			);


		if (navigation == null)
			return;


		navigation.CustomMinimumSize =
			new Vector2(
				0,
				height
			);
	}


	private void RemoveSpecialViewTopMargin(
		string scrollName)
	{
		ScrollContainer? specialScroll =
			_roomVBox.GetNodeOrNull<ScrollContainer>(
				scrollName
			);


		if (
			specialScroll == null
			|| specialScroll.GetChildCount() == 0
		)
		{
			return;
		}


		Control? first =
			specialScroll.GetChild(
				0
			)
			as Control;


		if (
			first == null
			|| first.GetChildCount() == 0
		)
		{
			return;
		}


		MarginContainer? margin =
			first.GetChild(
				0
			)
			as MarginContainer;


		if (margin == null)
			return;


		margin.AddThemeConstantOverride(
			"margin_top",
			0
		);
	}


	private void HookPipelineButtons()
	{
		if (_pipelineButtonsHooked)
			return;


		Control? navigation =
			_roomVBox.GetNodeOrNull<Control>(
				"PipelineNavigationCenter"
			);


		if (navigation == null)
			return;


		foreach (
			Node node
			in navigation.FindChildren(
				"*",
				"Button",
				true,
				false
			)
		)
		{
			if (node is not Button button)
				continue;


			button.Pressed +=
				() =>
					Callable
						.From(
							ApplyPipelineTabTheme
						)
						.CallDeferred();
		}


		_pipelineButtonsHooked =
			true;
	}


	private void ApplyPipelineTabTheme()
	{
		Control? navigation =
			_roomVBox.GetNodeOrNull<Control>(
				"PipelineNavigationCenter"
			);


		ScrollContainer? pipelineScroll =
			_roomVBox.GetNodeOrNull<ScrollContainer>(
				"PipelineScroll"
			);


		if (
			navigation == null
			|| pipelineScroll == null
		)
		{
			return;
		}


		Color accent =
			RoomThemePalette.GetAccentColor(
				GameConfig.PipelineRoomIndex
			);


		bool pipelineActive =
			pipelineScroll.Visible;


		foreach (
			Node node
			in navigation.FindChildren(
				"*",
				"Button",
				true,
				false
			)
		)
		{
			if (node is not Button button)
				continue;


			bool active =
				button.Text == "PIPELINE"
					? pipelineActive
					: !pipelineActive;


			ApplyRedNavigationStyle(
				button,
				accent,
				active
			);
		}


		PanelContainer? panel =
			navigation.GetNodeOrNull<PanelContainer>(
				"PipelineNavigation"
			);


		if (panel != null)
		{
			panel.AddThemeStyleboxOverride(
				"panel",
				CreateNavigationPanelStyle(
					accent
				)
			);
		}
	}


	private static void ApplyRedNavigationStyle(
		Button button,
		Color accent,
		bool active)
	{
		Color background =
			active
				? accent.Darkened(
					0.18f
				)
				: accent.Darkened(
					0.62f
				);


		Color border =
			active
				? accent.Lightened(
					0.10f
				)
				: new Color(
					accent.R,
					accent.G,
					accent.B,
					0.56f
				);


		button.AddThemeStyleboxOverride(
			"normal",
			CreateNavigationButtonStyle(
				background,
				border
			)
		);


		button.AddThemeStyleboxOverride(
			"hover",
			CreateNavigationButtonStyle(
				active
					? accent.Darkened(
						0.08f
					)
					: accent.Darkened(
						0.44f
					),
				accent
			)
		);


		button.AddThemeStyleboxOverride(
			"pressed",
			CreateNavigationButtonStyle(
				accent.Darkened(
					0.28f
				),
				accent
			)
		);


		button.AddThemeColorOverride(
			"font_color",
			active
				? Colors.White
				: new Color(
					0.86f,
					0.82f,
					0.83f,
					1.0f
				)
		);


		button.AddThemeColorOverride(
			"font_hover_color",
			Colors.White
		);
	}


	private static StyleBoxFlat CreateNavigationButtonStyle(
		Color background,
		Color border)
	{
		return new StyleBoxFlat
		{
			BgColor =
				background,

			BorderColor =
				border,

			BorderWidthLeft =
				1,

			BorderWidthTop =
				1,

			BorderWidthRight =
				1,

			BorderWidthBottom =
				1,

			CornerRadiusTopLeft =
				15,

			CornerRadiusTopRight =
				15,

			CornerRadiusBottomLeft =
				15,

			CornerRadiusBottomRight =
				15
		};
	}


	private static StyleBoxFlat CreateNavigationPanelStyle(
		Color accent)
	{
		return new StyleBoxFlat
		{
			BgColor =
				new Color(
					accent.R * 0.10f,
					accent.G * 0.10f,
					accent.B * 0.10f,
					0.97f
				),

			BorderColor =
				new Color(
					accent.R,
					accent.G,
					accent.B,
					0.62f
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
				19,

			CornerRadiusTopRight =
				19,

			CornerRadiusBottomLeft =
				19,

			CornerRadiusBottomRight =
				19
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

	public void UpdateAll()
	{
		_background.Texture =
			_state.CurrentRoom.Background;


		_ambient.SetRoom(
			_state.CurrentRoomIndex
		);


		UpdateSpecialRoomLayout();


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
