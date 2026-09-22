using Godot;
using System;

namespace IdleAi;

public sealed class MachineDetailsOverlay
{
	private const ulong OutsideCloseReopenBlockMs =
		180;


	private enum UpgradeAmount
	{
		One,
		Five,
		Ten,
		Max
	}


	public event Action<string>? StateChanged;


	private readonly Game _root;

	private readonly GameState _state;

	private readonly EconomyService _economy;

	private readonly ProgressionService _progression;

	private readonly BotService _bots;


	private Control _overlay =
		null!;


	private PanelContainer _panel =
		null!;


	private Label _title =
		null!;


	private TextureRect _machineImage =
		null!;


	private Label _level =
		null!;


	private Label _production =
		null!;


	private Label _cycle =
		null!;


	private Label _upgradeInfo =
		null!;


	private HBoxContainer _upgradeAmountRow =
		null!;


	private Button _upgrade1Button =
		null!;


	private Button _upgrade5Button =
		null!;


	private Button _upgrade10Button =
		null!;


	private Button _upgradeMaxButton =
		null!;


	private Button _upgradeButton =
		null!;


	private TextureRect _botImage =
		null!;


	private Label _botName =
		null!;


	private Label _botMultiplier =
		null!;


	private Button _buyBotButton =
		null!;


	private Button _sellBotButton =
		null!;


	private Control _sellOverlay =
		null!;


	private Label _sellInfo =
		null!;


	private Tween? _openTween;


	private int _roomIndex;

	private int _slotIndex;


	private ulong _blockOpenUntil;


	private UpgradeAmount _upgradeAmount =
		UpgradeAmount.One;


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
		if (
			Time.GetTicksMsec()
			< _blockOpenUntil
		)
		{
			return;
		}


		_roomIndex =
			roomIndex;


		_slotIndex =
			slotIndex;


		_upgradeAmount =
			UpgradeAmount.One;


		Refresh();


		_overlay.Show();

		_overlay.MoveToFront();


		PlayOpenAnimation();
	}


	public void Close()
	{
		_openTween?.Kill();


		if (_panel != null)
		{
			_panel.Scale =
				Vector2.One;


			_panel.Modulate =
				Colors.White;
		}


		_sellOverlay?.Hide();

		_overlay?.Hide();
	}


	private void CloseFromOutside()
	{
		_blockOpenUntil =
			Time.GetTicksMsec()
			+ OutsideCloseReopenBlockMs;


		Close();
	}


	private void PlayOpenAnimation()
	{
		_openTween?.Kill();


		_panel.PivotOffset =
			_panel.Size
			/ 2.0f;


		_panel.Scale =
			new Vector2(
				0.94f,
				0.94f
			);


		_panel.Modulate =
			new Color(
				1,
				1,
				1,
				0.82f
			);


		_openTween =
			_root.CreateTween();


		_openTween.SetParallel(
			true
		);


		_openTween.TweenProperty(
			_panel,
			"scale",
			new Vector2(
				1.015f,
				1.015f
			),
			0.22
		)
		.SetTrans(
			Tween.TransitionType.Cubic
		)
		.SetEase(
			Tween.EaseType.Out
		);


		_openTween.TweenProperty(
			_panel,
			"modulate:a",
			1.0f,
			0.22
		);


		_openTween
			.Chain()
			.TweenProperty(
				_panel,
				"scale",
				Vector2.One,
				0.14
			)
			.SetTrans(
				Tween.TransitionType.Sine
			)
			.SetEase(
				Tween.EaseType.Out
			);
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


		bool pipelineMachine =
			_roomIndex
			== GameConfig.PipelineRoomIndex;


		double cycleProduction =
			pipelineMachine
				? _economy
					.GetPipelineInputCycleReward(
						_roomIndex,
						slot
					)
				: _economy
					.GetCycleReward(
						_roomIndex,
						slot
					);


		double perSecond =
			cycleDuration > 0.0
				? cycleProduction
					/ cycleDuration
				: 0.0;


		_title.Text =
			machine.MachineName;


		_machineImage.Texture =
			machine.Texture;


		_level.Text =
			$"Level {slot.MachineLevel} / {machine.MaxLevel}";


		if (pipelineMachine)
		{
			_production.Text =
				"Production: "
				+ NumberFormatter.Format(
					cycleProduction
				)
				+ " Pipeline Material / cycle"
				+ "\nAverage: "
				+ NumberFormatter.Format(
					perSecond
				)
				+ " Material/s";
		}
		else
		{
			_production.Text =
				"Production: "
				+ NumberFormatter.Format(
					cycleProduction
				)
				+ " Tokens / cycle"
				+ "\nAverage: "
				+ NumberFormatter.Format(
					perSecond
				)
				+ " Tokens/s";
		}


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
	// UPGRADE AMOUNT
	// ==================================================

	private void SetUpgradeAmount(
		UpgradeAmount amount)
	{
		_upgradeAmount =
			amount;


		UpdateUpgradeAmountButtons();

		Refresh();
	}


	private void UpdateUpgradeAmountButtons()
	{
		_upgrade1Button.Text =
			_upgradeAmount == UpgradeAmount.One
				? "✓ 1x"
				: "1x";


		_upgrade5Button.Text =
			_upgradeAmount == UpgradeAmount.Five
				? "✓ 5x"
				: "5x";


		_upgrade10Button.Text =
			_upgradeAmount == UpgradeAmount.Ten
				? "✓ 10x"
				: "10x";


		_upgradeMaxButton.Text =
			_upgradeAmount == UpgradeAmount.Max
				? "✓ MAX"
				: "MAX";
	}


	private int GetRequestedUpgradeLevels()
	{
		return _upgradeAmount switch
		{
			UpgradeAmount.One =>
				1,

			UpgradeAmount.Five =>
				5,

			UpgradeAmount.Ten =>
				10,

			UpgradeAmount.Max =>
				0,

			_ =>
				1
		};
	}


	// ==================================================
	// MACHINE UPGRADE
	// ==================================================

	private void UpdateUpgradeSection(
		RoomData room,
		MachineData machine,
		SlotData slot)
	{
		UpdateUpgradeAmountButtons();


		if (
			slot.MachineLevel
			< machine.MaxLevel
		)
		{
			_upgradeAmountRow.Show();


			int requested =
				GetRequestedUpgradeLevels();


			MachineUpgradeQuote quote =
				_progression
					.GetMachineUpgradeQuote(
						_roomIndex,
						_slotIndex,
						requested
					);


			bool pipelineMachine =
				_roomIndex
				== GameConfig.PipelineRoomIndex;


			double currentProduction =
				pipelineMachine
					? _economy
						.GetPipelineInputCycleReward(
							_roomIndex,
							slot
						)
					: _economy
						.GetCycleReward(
							_roomIndex,
							slot
						);


			int originalLevel =
				slot.MachineLevel;


			if (quote.Levels > 0)
			{
				slot.MachineLevel =
					quote.TargetLevel;
			}


			double targetProduction =
				pipelineMachine
					? _economy
						.GetPipelineInputCycleReward(
							_roomIndex,
							slot
						)
					: _economy
						.GetCycleReward(
							_roomIndex,
							slot
						);


			slot.MachineLevel =
				originalLevel;


			if (quote.Levels <= 0)
			{
				_upgradeInfo.Text =
					_upgradeAmount
						== UpgradeAmount.Max
						? "Not enough Tokens for another level."
						: "No levels available.";


				_upgradeButton.Text =
					"UPGRADE";


				_upgradeButton.Disabled =
					true;


				return;
			}


			string productionName =
				pipelineMachine
					? "Material / cycle"
					: "Production";


			_upgradeInfo.Text =
				"Level "
				+ originalLevel
				+ " → "
				+ quote.TargetLevel
				+ "\n"
				+ productionName
				+ ": "
				+ NumberFormatter.Format(
					currentProduction
				)
				+ " → "
				+ NumberFormatter.Format(
					targetProduction
				);


			string amountText =
				quote.Levels == 1
					? "1 LEVEL"
					: quote.Levels
						+ " LEVELS";


			_upgradeButton.Text =
				"UPGRADE "
				+ amountText
				+ " • "
				+ NumberFormatter.Format(
					quote.Cost
				);


			_upgradeButton.Disabled =
				!quote.CanAfford;


			return;
		}


		_upgradeAmountRow.Hide();


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
				_state.Tokens
				< cost;


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
		SlotData slot =
			_state.RoomStates[
				_roomIndex
			].Slots[
				_slotIndex
			];


		RoomData room =
			_state.Rooms[
				_roomIndex
			];


		MachineData machine =
			room.Machines[
				slot.MachineTier
			];


		ProgressionResult result;


		if (
			slot.MachineLevel
			< machine.MaxLevel
		)
		{
			result =
				_progression
					.UpgradeMachineLevels(
						_roomIndex,
						_slotIndex,
						GetRequestedUpgradeLevels()
					);
		}
		else
		{
			result =
				_progression
					.HandleSlotAction(
						_slotIndex
					);
		}


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
			+ bot.ProductionMultiplier.ToString(
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


		double effectiveMultiplier =
			_bots.GetEffectiveMultiplier(
				slot
			);


		double researchBonus =
			_state.Lab
				.GetBotPowerBonus();


		_sellInfo.Text =
			$"Sell {bot.Name}?\n\n"
			+ $"Base power: x{bot.ProductionMultiplier:F2}\n"
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
				CloseFromOutside
			);


		_root.AddChild(
			_overlay
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


		_panel =
			CreatePanel(
				new Vector2(
					580,
					900
				)
			);


		center.AddChild(
			_panel
		);


		MarginContainer margin =
			CreateMargin();


		margin.AddThemeConstantOverride(
			"margin_top",
			36
		);


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
						150
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


		_upgradeAmountRow =
			new HBoxContainer
			{
				CustomMinimumSize =
					new Vector2(
						0,
						48
					),

				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};


		_upgradeAmountRow.AddThemeConstantOverride(
			"separation",
			6
		);


		vbox.AddChild(
			_upgradeAmountRow
		);


		_upgrade1Button =
			CreateUpgradeAmountButton(
				"1x"
			);


		_upgrade5Button =
			CreateUpgradeAmountButton(
				"5x"
			);


		_upgrade10Button =
			CreateUpgradeAmountButton(
				"10x"
			);


		_upgradeMaxButton =
			CreateUpgradeAmountButton(
				"MAX"
			);


		_upgrade1Button.Pressed +=
			() =>
				SetUpgradeAmount(
					UpgradeAmount.One
				);


		_upgrade5Button.Pressed +=
			() =>
				SetUpgradeAmount(
					UpgradeAmount.Five
				);


		_upgrade10Button.Pressed +=
			() =>
				SetUpgradeAmount(
					UpgradeAmount.Ten
				);


		_upgradeMaxButton.Pressed +=
			() =>
				SetUpgradeAmount(
					UpgradeAmount.Max
				);


		_upgradeAmountRow.AddChild(
			_upgrade1Button
		);


		_upgradeAmountRow.AddChild(
			_upgrade5Button
		);


		_upgradeAmountRow.AddChild(
			_upgrade10Button
		);


		_upgradeAmountRow.AddChild(
			_upgradeMaxButton
		);


		_upgradeButton =
			new Button
			{
				CustomMinimumSize =
					new Vector2(
						0,
						56
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
						80
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


		UpdateUpgradeAmountButtons();


		OverlayCloseButton.Add(
			_panel,
			Close
		);
	}


	private static Button CreateUpgradeAmountButton(
		string text)
	{
		return new Button
		{
			Text =
				text,

			CustomMinimumSize =
				new Vector2(
					0,
					46
				),

			SizeFlagsHorizontal =
				Control.SizeFlags.ExpandFill
		};
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


		margin.AddThemeConstantOverride(
			"margin_top",
			36
		);


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


		OverlayCloseButton.Add(
			panel,
			() =>
				_sellOverlay.Hide()
		);


		_sellOverlay.Hide();
	}


	// ==================================================
	// HELPERS
	// ==================================================

	private static Control CreateFullScreenOverlay(
		Action outsideReleased)
	{
		Control overlay =
			new()
			{
				MouseFilter =
					Control.MouseFilterEnum.Stop
			};


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
				bool released =
					false;


				if (
					@event
						is InputEventScreenTouch touch
					&& !touch.Pressed
				)
				{
					released =
						true;
				}


				if (
					@event
						is InputEventMouseButton mouse
					&& !mouse.Pressed
					&& mouse.ButtonIndex
						== MouseButton.Left
				)
				{
					released =
						true;
				}


				if (!released)
					return;


				overlay
					.GetViewport()
					.SetInputAsHandled();


				Callable
					.From(
						outsideReleased
					)
					.CallDeferred();
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
				14,

			ShadowColor =
				new Color(
					0,
					0,
					0,
					0.45f
				),

			ShadowSize =
				14
		};
	}


	private static string FormatPercent(
		double value)
	{
		return (
			value
			* 100
		)
		.ToString(
			"0.#"
		)
		+ "%";
	}
}
