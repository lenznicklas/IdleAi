using Godot;
using System;

namespace IdleAi;

public sealed class RoomUiController
{
	private const float TopSlotPadding =
		36.0f;


	/*
	 * Very short/light haptic feedback.
	 */
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


	private ScrollContainer _scroll =
		null!;


	private GridContainer _slotGrid =
		null!;


	private AmbientBackgroundController _ambient =
		null!;


	private MobileScrollController _mobileScroll =
		null!;


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
		/*
		 * Child 0 + 1 are the top padding row.
		 */
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
