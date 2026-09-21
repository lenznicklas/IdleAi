using Godot;
using System;

namespace IdleAi;

public sealed class MachineDetailsOverlay
{
	private readonly Game _root;

	private readonly GameState _state;

	private readonly EconomyService _economy;

	private readonly ProgressionService _progression;

	private readonly BotService _bots;


	public event Action<string>? StateChanged;


	private Control _overlay = null!;

	private PanelContainer _panel = null!;

	private Label _title = null!;

	private TextureRect _machineImage = null!;

	private Label _level = null!;

	private Label _production = null!;

	private Label _cycle = null!;

	private Label _upgradeInfo = null!;

	private Button _upgradeButton = null!;

	private TextureRect _botImage = null!;

	private Label _botName = null!;

	private Label _botMultiplier = null!;

	private Button _buyBotButton = null!;

	private Button _sellBotButton = null!;


	private Control _sellOverlay = null!;

	private Label _sellInfo = null!;


	private int _roomIndex;

	private int _slotIndex;


	public bool Visible =>
		_overlay != null
		&& _overlay.Visible;


	public MachineDetailsOverlay(
		Game root,
		GameState state,
		EconomyService economy,
		ProgressionService progression,
		BotService bots)
	{
		_root =
			root;


		_state =
			state;


		_economy =
			economy;


		_progression =
			progression;


		_bots =
			bots;
	}


	// ==================================================
	// INITIALIZE
	// ==================================================

	public void Initialize()
	{
		CreateUi();

		CreateSellOverlay();


		Close();
	}


	// ==================================================
	// OPEN / CLOSE
	// ==================================================

	public void Open(
		int roomIndex,
		int slotIndex)
	{
		_roomIndex =
			roomIndex;


		_slotIndex =
			slotIndex;


		Refresh();


		_overlay.Show();

		_overlay.MoveToFront();
	}


	public void Close()
	{
		_sellOverlay?.Hide();

		_overlay?.Hide();
	}


	// ==================================================
	// REFRESH
	// ==================================================

	public void Refresh()
	{
		if (
			_roomIndex < 0
			|| _roomIndex >= _state.Rooms.Count
		)
		{
			return;
		}


		RoomState roomState =
			_state.RoomStates[
				_roomIndex
			];


		if (
			_slotIndex < 0
			|| _slotIndex >= roomState.Slots.Count
		)
		{
			return;
		}


		SlotData slot =
			roomState.Slots[
				_slotIndex
			];


		if (!slot.Unlocked)
		{
			Close();

			return;
		}


		RoomData room =
			_state.Rooms[
				_roomIndex
			];


		MachineData machine =
			room.Machines[
				slot.MachineTier
			];


		double cycleDuration =
			_economy.GetCycleDuration(
				slot
			);


		double cycleReward =
			_economy.GetCycleReward(
				_roomIndex,
				slot
			);


		double perSecond =
			cycleReward
			/ cycleDuration;


		_title.Text =
			machine.MachineName;


		_machineImage.Texture =
			machine.Texture;


		_level.Text =
			$"Level {slot.MachineLevel} / {machine.MaxLevel}";


		_production.Text =
			"Production: "
			+ NumberFormatter.Format(
				cycleReward
			)
			+ " Tokens / cycle"
			+ "\nAverage: "
			+ NumberFormatter.Format(
				perSecond
			)
			+ " Tokens/s";


		if (slot.IsRunning)
		{
			_cycle.Text =
				$"Cycle: {slot.CycleRemaining:F1}s / {cycleDuration:F1}s";
		}
		else
		{
			_cycle.Text =
				$"Cycle time: {cycleDuration:F1}s";
		}


		UpdateUpgradeSection(
			room,
			machine,
			slot
		);


		UpdateBotSection(
			slot
		);
	}


	// ==================================================
	// MACHINE UPGRADES
	// ==================================================

	private void UpdateUpgradeSection(
		RoomData room,
		MachineData machine,
		SlotData slot)
	{
		if (
			slot.MachineLevel
			< machine.MaxLevel
		)
		{
			double cost =
				_economy.GetLevelUpgradeCost(
					_roomIndex,
					slot,
					_slotIndex
				);


			double current =
				_economy.GetCycleReward(
					_roomIndex,
					slot
				);


			int original =
				slot.MachineLevel;


			slot.MachineLevel++;


			double next =
				_economy.GetCycleReward(
					_roomIndex,
					slot
				);


			slot.MachineLevel =
				original;


			_upgradeInfo.Text =
				"Next level: "
				+ NumberFormatter.Format(
					current
				)
				+ " → "
				+ NumberFormatter.Format(
					next
				);


			_upgradeButton.Text =
				"UPGRADE • "
				+ NumberFormatter.Format(
					cost
				);


			_upgradeButton.Disabled =
				false;


			return;
		}


		if (
			slot.MachineTier
			< room.Machines.Count - 1
		)
		{
			MachineData nextMachine =
				room.Machines[
					slot.MachineTier + 1
				];


			double cost =
				_economy.GetTierUpgradeCost(
					_roomIndex,
					slot,
					_slotIndex
				);


			_upgradeInfo.Text =
				$"Next machine: {nextMachine.MachineName}";


			_upgradeButton.Text =
				"UPGRADE MACHINE • "
				+ NumberFormatter.Format(
					cost
				);


			_upgradeButton.Disabled =
				false;


			return;
		}


		_upgradeInfo.Text =
			"Maximum machine reached";


		_upgradeButton.Text =
			"MAX";


		_upgradeButton.Disabled =
			true;
	}


	private void OnUpgradePressed()
	{
		ProgressionResult result =
			_progression.HandleSlotAction(
				_slotIndex
			);


		StateChanged?.Invoke(
			result.Message
		);


		Refresh();
	}


	// ==================================================
	// BOT
	// ==================================================

	private void UpdateBotSection(
		SlotData slot)
	{
		if (!slot.HasBot)
		{
			UpdateNoBotSection(
				slot
			);

			return;
		}


		BotDefinition bot =
			BotCatalog.Get(
				slot.BotRarity!.Value
			);


		double baseMultiplier =
			bot.ProductionMultiplier;


		double effectiveMultiplier =
			_bots.GetEffectiveMultiplier(
				slot
			);


		double researchBonus =
			_state.Lab
				.GetBotPowerBonus();


		_botImage.Texture =
			bot.Texture;


		_botName.Text =
			bot.Name.ToUpperInvariant();


		_botMultiplier.Text =
			"Base Power: x"
			+ baseMultiplier.ToString(
				"F2"
			)
			+ "\nResearch: +"
			+ FormatPercent(
				researchBonus
			)
			+ "\nEffective Power: x"
			+ effectiveMultiplier.ToString(
				"F2"
			);


		_buyBotButton.Hide();

		_sellBotButton.Show();


		_sellBotButton.Text =
			"SELL BOT • "
			+ NumberFormatter.Format(
				_bots.GetSellPrice(
					slot
				)
			);
	}


	private void UpdateNoBotSection(
		SlotData slot)
	{
		_botImage.Texture =
			null;


		_botName.Text =
			"NO BOT";


		(
			double common,
			double rare,
			double epic,
			double legendary
		) =
			_bots.GetRarityChances();


		_botMultiplier.Text =
			"Manual production"
			+ "\n\nBot chances:"
			+ "\nCommon: "
			+ FormatPercent(
				common
			)
			+ "\nRare: "
			+ FormatPercent(
				rare
			)
			+ "\nEpic: "
			+ FormatPercent(
				epic
			)
			+ "\nLegendary: "
			+ FormatPercent(
				legendary
			);


		_buyBotButton.Text =
			"BUY RANDOM BOT • "
			+ NumberFormatter.Format(
				_bots.GetBotPrice(
					_roomIndex,
					_slotIndex
				)
			);


		_buyBotButton.Show();

		_sellBotButton.Hide();
	}


	// ==================================================
	// BUY BOT
	// ==================================================

	private void BuyBot()
	{
		BotActionResult result =
			_bots.BuyBot(
				_roomIndex,
				_slotIndex
			);


		StateChanged?.Invoke(
			result.Message
		);


		Refresh();
	}


	// ==================================================
	// SELL BOT
	// ==================================================

	private void OpenSellConfirmation()
	{
		SlotData slot =
			_state.RoomStates[
				_roomIndex
			].Slots[
				_slotIndex
			];


		if (!slot.HasBot)
			return;


		BotDefinition bot =
			BotCatalog.Get(
				slot.BotRarity!.Value
			);


		double baseMultiplier =
			bot.ProductionMultiplier;


		double effectiveMultiplier =
			_bots.GetEffectiveMultiplier(
				slot
			);


		double researchBonus =
			_state.Lab
				.GetBotPowerBonus();


		_sellInfo.Text =
			$"Sell {bot.Name}?\n\n"
			+ $"Base power: x{baseMultiplier:F2}\n"
			+ "Research bonus: +"
			+ FormatPercent(
				researchBonus
			)
			+ "\nEffective power: x"
			+ effectiveMultiplier.ToString(
				"F2"
			)
			+ "\nRefund: "
			+ NumberFormatter.Format(
				_bots.GetSellPrice(
					slot
				)
			)
			+ " Tokens\n\n"
			+ "You receive one third of the original purchase price.";


		_sellOverlay.Show();

		_sellOverlay.MoveToFront();
	}


	private void ConfirmSell()
	{
		_sellOverlay.Hide();


		BotActionResult result =
			_bots.SellBot(
				_roomIndex,
				_slotIndex
			);


		StateChanged?.Invoke(
			result.Message
		);


		Refresh();
	}


	// ==================================================
	// MAIN UI
	// ==================================================

	private void CreateUi()
	{
		_overlay =
			CreateFullScreenOverlay(
				Close
			);


		_root.AddChild(
			_overlay
		);


		_panel =
			CreatePanel(
				new Vector2(
					580,
					900
				)
			);


		CenterContainer center =
			new();


		center.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);


		center.MouseFilter =
			Control.MouseFilterEnum.Ignore;


		_overlay.AddChild(
			center
		);


		center.AddChild(
			_panel
		);


		MarginContainer margin =
			CreateMargin();


		_panel.AddChild(
			margin
		);


		VBoxContainer vbox =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,

				SizeFlagsVertical =
					Control.SizeFlags.ExpandFill
			};


		vbox.AddThemeConstantOverride(
			"separation",
			8
		);


		margin.AddChild(
			vbox
		);


		// ==================================================
		// MACHINE
		// ==================================================

		_title =
			CreateLabel(
				27
			);


		vbox.AddChild(
			_title
		);


		_machineImage =
			new TextureRect
			{
				CustomMinimumSize =
					new Vector2(
						0,
						170
					),

				ExpandMode =
					TextureRect.ExpandModeEnum.IgnoreSize,

				StretchMode =
					TextureRect.StretchModeEnum.KeepAspectCentered
			};


		vbox.AddChild(
			_machineImage
		);


		_level =
			CreateLabel(
				17
			);


		vbox.AddChild(
			_level
		);


		_production =
			CreateLabel(
				14
			);


		vbox.AddChild(
			_production
		);


		_cycle =
			CreateLabel(
				14
			);


		vbox.AddChild(
			_cycle
		);


		vbox.AddChild(
			new HSeparator()
		);


		_upgradeInfo =
			CreateLabel(
				14
			);


		vbox.AddChild(
			_upgradeInfo
		);


		_upgradeButton =
			new Button
			{
				CustomMinimumSize =
					new Vector2(
						0,
						54
					)
			};


		_upgradeButton.Pressed +=
			OnUpgradePressed;


		vbox.AddChild(
			_upgradeButton
		);


		vbox.AddChild(
			new HSeparator()
		);


		// ==================================================
		// BOT
		// ==================================================

		Label botTitle =
			CreateLabel(
				19
			);


		botTitle.Text =
			"BOT";


		vbox.AddChild(
			botTitle
		);


		_botImage =
			new TextureRect
			{
				CustomMinimumSize =
					new Vector2(
						0,
						90
					),

				ExpandMode =
					TextureRect.ExpandModeEnum.IgnoreSize,

				StretchMode =
					TextureRect.StretchModeEnum.KeepAspectCentered
			};


		vbox.AddChild(
			_botImage
		);


		_botName =
			CreateLabel(
				16
			);


		vbox.AddChild(
			_botName
		);


		_botMultiplier =
			CreateLabel(
				13
			);


		vbox.AddChild(
			_botMultiplier
		);


		_buyBotButton =
			new Button
			{
				CustomMinimumSize =
					new Vector2(
						0,
						52
					)
			};


		_buyBotButton.Pressed +=
			BuyBot;


		vbox.AddChild(
			_buyBotButton
		);


		_sellBotButton =
			new Button
			{
				CustomMinimumSize =
					new Vector2(
						0,
						52
					)
			};


		_sellBotButton.Pressed +=
			OpenSellConfirmation;


		vbox.AddChild(
			_sellBotButton
		);


		Button close =
			new()
			{
				CustomMinimumSize =
					new Vector2(
						0,
						50
					),

				Text =
					"CLOSE"
			};


		close.Pressed +=
			Close;


		vbox.AddChild(
			close
		);
	}


	// ==================================================
	// SELL UI
	// ==================================================

	private void CreateSellOverlay()
	{
		_sellOverlay =
			CreateFullScreenOverlay(
				() =>
					_sellOverlay.Hide()
			);


		_root.AddChild(
			_sellOverlay
		);


		CenterContainer center =
			new();


		center.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);


		center.MouseFilter =
			Control.MouseFilterEnum.Ignore;


		_sellOverlay.AddChild(
			center
		);


		PanelContainer panel =
			CreatePanel(
				new Vector2(
					540,
					450
				)
			);


		center.AddChild(
			panel
		);


		MarginContainer margin =
			CreateMargin();


		panel.AddChild(
			margin
		);


		VBoxContainer box =
			new();


		box.AddThemeConstantOverride(
			"separation",
			16
		);


		margin.AddChild(
			box
		);


		Label title =
			CreateLabel(
				26
			);


		title.Text =
			"SELL BOT";


		box.AddChild(
			title
		);


		box.AddChild(
			new HSeparator()
		);


		_sellInfo =
			CreateLabel(
				15
			);


		box.AddChild(
			_sellInfo
		);


		Button confirm =
			new()
			{
				CustomMinimumSize =
					new Vector2(
						0,
						58
					),

				Text =
					"SELL"
			};


		confirm.Pressed +=
			ConfirmSell;


		box.AddChild(
			confirm
		);


		Button cancel =
			new()
			{
				CustomMinimumSize =
					new Vector2(
						0,
						54
					),

				Text =
					"CANCEL"
			};


		cancel.Pressed +=
			() =>
				_sellOverlay.Hide();


		box.AddChild(
			cancel
		);


		_sellOverlay.Hide();
	}


	// ==================================================
	// HELPERS
	// ==================================================

	private static string FormatPercent(
		double value)
	{
		return (
			value
			* 100.0
		).ToString(
			"0.#"
		)
		+ "%";
	}


	private static Control CreateFullScreenOverlay(
		Action outsidePressed)
	{
		Control overlay =
			new();


		overlay.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);


		ColorRect dim =
			new()
			{
				Color =
					new Color(
						0,
						0,
						0,
						0.76f
					),

				MouseFilter =
					Control.MouseFilterEnum.Stop
			};


		dim.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);


		dim.GuiInput +=
			@event =>
			{
				if (
					@event is InputEventMouseButton mouse
					&& mouse.Pressed
					&& mouse.ButtonIndex
					== MouseButton.Left
				)
				{
					outsidePressed();
				}


				if (
					@event is InputEventScreenTouch touch
					&& touch.Pressed
				)
				{
					outsidePressed();
				}
			};


		overlay.AddChild(
			dim
		);


		return overlay;
	}


	private static PanelContainer CreatePanel(
		Vector2 size)
	{
		PanelContainer panel =
			new()
			{
				CustomMinimumSize =
					size,

				MouseFilter =
					Control.MouseFilterEnum.Stop
			};


		panel.AddThemeStyleboxOverride(
			"panel",
			CreatePanelStyle()
		);


		return panel;
	}


	private static MarginContainer CreateMargin()
	{
		MarginContainer margin =
			new();


		margin.AddThemeConstantOverride(
			"margin_left",
			28
		);

		margin.AddThemeConstantOverride(
			"margin_top",
			24
		);

		margin.AddThemeConstantOverride(
			"margin_right",
			28
		);

		margin.AddThemeConstantOverride(
			"margin_bottom",
			24
		);


		return margin;
	}


	private static Label CreateLabel(
		int fontSize)
	{
		Label label =
			new()
			{
				HorizontalAlignment =
					HorizontalAlignment.Center,

				AutowrapMode =
					TextServer.AutowrapMode.WordSmart
			};


		label.AddThemeFontSizeOverride(
			"font_size",
			fontSize
		);


		return label;
	}


	private static StyleBoxFlat CreatePanelStyle()
	{
		return new StyleBoxFlat
		{
			BgColor =
				new Color(
					0.02f,
					0.035f,
					0.06f,
					0.98f
				),

			BorderWidthLeft =
				2,

			BorderWidthTop =
				2,

			BorderWidthRight =
				2,

			BorderWidthBottom =
				2,

			BorderColor =
				new Color(
					0,
					0.65f,
					1,
					0.8f
				),

			CornerRadiusTopLeft =
				14,

			CornerRadiusTopRight =
				14,

			CornerRadiusBottomLeft =
				14,

			CornerRadiusBottomRight =
				14
		};
	}
}
