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

	private ConfirmationDialog _sellConfirmation =
		null!;


	private int _roomIndex;
	private int _slotIndex;


	public bool Visible =>
		_overlay != null
		&& _overlay.Visible;


	// ==================================================
	// CONSTRUCTOR
	// ==================================================

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

		CreateSellConfirmation();

		Close();
	}


	// ==================================================
	// OPEN
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
		if (_overlay != null)
		{
			_overlay.Hide();
		}
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


		double cycleReward =
			_economy.GetCycleReward(
				_roomIndex,
				slot
			);


		double theoreticalPerSecond =
			cycleReward
			/ GameConfig.ProductionCycleSeconds;


		_title.Text =
			machine.MachineName;


		_machineImage.Texture =
			machine.Texture;


		_level.Text =
			$"Level {slot.MachineLevel} / {machine.MaxLevel}";


		_production.Text =
            "Production / cycle: "
			+ NumberFormatter.Format(
				cycleReward
			)
			+ " Tokens\nEquivalent: "
			+ NumberFormatter.Format(
				theoreticalPerSecond
			)
			+ " Tokens/s";


		if (slot.IsRunning)
		{
			_cycle.Text =
				$"Cycle: {slot.CycleRemaining:F1}s remaining";
		}
		else if (slot.HasBot)
		{
			_cycle.Text =
				"Cycle: AUTO";
		}
		else
		{
			_cycle.Text =
				"Cycle: READY - press START";
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
	// UPGRADE
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


			double currentProduction =
				_economy.GetCycleReward(
					_roomIndex,
					slot
				);


			int oldLevel =
				slot.MachineLevel;


			slot.MachineLevel++;


			double nextProduction =
				_economy.GetCycleReward(
					_roomIndex,
					slot
				);


			slot.MachineLevel =
				oldLevel;


			_upgradeInfo.Text =
                "Next level\n"
				+ NumberFormatter.Format(
					currentProduction
				)
				+ " → "
				+ NumberFormatter.Format(
					nextProduction
				)
				+ " / cycle";


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
			MachineData next =
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
				$"Next machine: {next.MachineName}";


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
			_botImage.Texture =
				null;


			_botName.Text =
				"NO BOT";


			_botMultiplier.Text =
				"Manual production";


			double botPrice =
				_bots.GetBotPrice(
					_roomIndex,
					_slotIndex
				);


			_buyBotButton.Text =
                "BUY RANDOM BOT • "
				+ NumberFormatter.Format(
					botPrice
				);


			_buyBotButton.Show();

			_sellBotButton.Hide();


			return;
		}


		BotDefinition bot =
			BotCatalog.Get(
				slot.BotRarity!.Value
			);


		_botImage.Texture =
			bot.Texture;


		_botName.Text =
			bot.Name.ToUpperInvariant();


		_botMultiplier.Text =
			$"Production x{bot.ProductionMultiplier:F1}";


		_buyBotButton.Hide();

		_sellBotButton.Show();


		double sellPrice =
			_bots.GetSellPrice(
				slot
			);


		_sellBotButton.Text =
            "SELL BOT • "
			+ NumberFormatter.Format(
				sellPrice
			);
	}


	// ==================================================
	// BUY
	// ==================================================

	private void OnBuyBotPressed()
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
	// SELL
	// ==================================================

	private void OnSellBotPressed()
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


		double sellPrice =
			_bots.GetSellPrice(
				slot
			);


		_sellConfirmation.DialogText =
			$"Sell {bot.Name}?\n\n"
			+ $"Multiplier: x{bot.ProductionMultiplier:F1}\n"
			+ $"Refund: {NumberFormatter.Format(sellPrice)} Tokens\n\n"
			+ "You only receive one third of the original purchase price.";


		_sellConfirmation.PopupCentered(
			new Vector2I(
				500,
				320
			)
		);
	}


	private void ConfirmSellBot()
	{
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


	private void CreateSellConfirmation()
	{
		_sellConfirmation =
			new ConfirmationDialog
			{
				Title =
					"Sell Bot",

				OkButtonText =
                    "SELL"
			};


		_sellConfirmation.Confirmed +=
			ConfirmSellBot;


		_root.AddChild(
			_sellConfirmation
		);
	}


	// ==================================================
	// CREATE UI
	// ==================================================

	private void CreateUi()
	{
		_overlay =
			new Control();


		_overlay.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);


		_root.AddChild(
			_overlay
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
					)
			};


		dim.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);


		_overlay.AddChild(
			dim
		);


		CenterContainer center =
			new();


		center.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);


		_overlay.AddChild(
			center
		);


		PanelContainer panel =
			new()
			{
				CustomMinimumSize =
					new Vector2(
						580,
						900
					)
			};


		center.AddChild(
			panel
		);


		StyleBoxFlat panelStyle =
			CreatePanelStyle();


		panel.AddThemeStyleboxOverride(
			"panel",
			panelStyle
		);


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


		panel.AddChild(
			margin
		);


		ScrollContainer scroll =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,

				SizeFlagsVertical =
					Control.SizeFlags.ExpandFill,

				HorizontalScrollMode =
					ScrollContainer.ScrollMode.Disabled
			};


		margin.AddChild(
			scroll
		);


		VBoxContainer vbox =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};


		vbox.AddThemeConstantOverride(
			"separation",
			12
		);


		scroll.AddChild(
			vbox
		);


		_title =
			CreateLabel(
				28
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
						200
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
				18
			);


		vbox.AddChild(
			_level
		);


		_production =
			CreateLabel(
				15
			);


		vbox.AddChild(
			_production
		);


		_cycle =
			CreateLabel(
				15
			);


		vbox.AddChild(
			_cycle
		);


		vbox.AddChild(
			new HSeparator()
		);


		_upgradeInfo =
			CreateLabel(
				15
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
						60
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


		Label botTitle =
			CreateLabel(
				21
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
						125
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
				17
			);


		vbox.AddChild(
			_botName
		);


		_botMultiplier =
			CreateLabel(
				14
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
						60
					)
			};


		_buyBotButton.Pressed +=
			OnBuyBotPressed;


		vbox.AddChild(
			_buyBotButton
		);


		_sellBotButton =
			new Button
			{
				CustomMinimumSize =
					new Vector2(
						0,
						55
					)
			};


		_sellBotButton.Pressed +=
			OnSellBotPressed;


		vbox.AddChild(
			_sellBotButton
		);


		Button close =
			new()
			{
				CustomMinimumSize =
					new Vector2(
						0,
						55
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
	// HELPERS
	// ==================================================

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
					0.0f,
					0.65f,
					1.0f,
					0.8f
				),

			CornerRadiusTopLeft =
				18,

			CornerRadiusTopRight =
				18,

			CornerRadiusBottomLeft =
				18,

			CornerRadiusBottomRight =
				18
		};
	}
}
