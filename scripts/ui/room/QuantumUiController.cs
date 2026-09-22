using Godot;
using System;
using System.Collections.Generic;

namespace IdleAi;

public sealed class QuantumUiController
{
	private const float ContentWidth =
		560.0f;


	private const float UpgradeCardHeight =
		220.0f;


	private static readonly QuantumUpgrade[] UpgradeOrder =
	[
		QuantumUpgrade.Stabilizer,
		QuantumUpgrade.EnergyCore,
		QuantumUpgrade.Amplifier
	];


	private static readonly Texture2D StabilityIcon =
		GD.Load<Texture2D>(
			"res://assets/quantum/quantum_stability.png"
		);


	private static readonly Texture2D OverclockIcon =
		GD.Load<Texture2D>(
			"res://assets/quantum/quantum_overclock.png"
		);


	private static readonly Texture2D EnergyIcon =
		GD.Load<Texture2D>(
			"res://assets/quantum/quantum_energy.png"
		);


	private static readonly Texture2D StabilizerIcon =
		GD.Load<Texture2D>(
			"res://assets/quantum/quantum_stabilizer.png"
		);


	private static readonly Texture2D EnergyCoreIcon =
		GD.Load<Texture2D>(
			"res://assets/quantum/quantum_core.png"
		);


	private static readonly Texture2D AmplifierIcon =
		GD.Load<Texture2D>(
			"res://assets/quantum/quantum_amplifier.png"
		);


	private static readonly Texture2D UpgradeIcon =
		GD.Load<Texture2D>(
			"res://assets/pipeline/pipeline_upgrade.png"
		);


	private readonly Game _root;

	private readonly GameState _state;

	private readonly QuantumService _service;


	private VBoxContainer _roomVBox =
		null!;


	private ScrollContainer _machineScroll =
		null!;


	private CenterContainer _navigationCenter =
		null!;


	private Button _machinesButton =
		null!;


	private Button _quantumButton =
		null!;


	private ScrollContainer _quantumScroll =
		null!;


	private MobileScrollController _mobileScroll =
		null!;


	private Label _stabilityLabel =
		null!;


	private ProgressBar _stabilityBar =
		null!;


	private Label _energyLabel =
		null!;


	private ProgressBar _energyBar =
		null!;


	private Label _outputLabel =
		null!;


	private Label _overclockLabel =
		null!;


	private Label _overclockStatusLabel =
		null!;


	private Button _overclockMinusButton =
		null!;


	private Button _overclockPlusButton =
		null!;


	private readonly Dictionary<QuantumUpgrade, Label>
		_levelLabels =
			[];


	private readonly Dictionary<QuantumUpgrade, Label>
		_effectLabels =
			[];


	private readonly Dictionary<QuantumUpgrade, Button>
		_upgradeButtons =
			[];


	private bool _showQuantum;

	private int _lastRoom =
		-1;


	public event Action<string>? MessageRequested;

	public event Action? StateChanged;


	public QuantumUiController(
		Game root,
		GameState state,
		QuantumService service)
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
		_roomVBox =
			_root.GetNode<VBoxContainer>(
				"MarginContainer/VBoxContainer/RoomPanel/RoomVBox"
			);


		_machineScroll =
			_root.GetNode<ScrollContainer>(
				"MarginContainer/VBoxContainer/RoomPanel/RoomVBox/ScrollContainer"
			);


		CreateNavigation();

		CreateQuantumView();

		CreateMobileScrolling();

		UpdateAll();
	}


	// ==================================================
	// TAB BAR
	// ==================================================

	private void CreateNavigation()
	{
		_navigationCenter =
			new CenterContainer
			{
				Name =
					"QuantumNavigationCenter",

				CustomMinimumSize =
					new Vector2(
						0,
						70
					),

				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,

				MouseFilter =
					Control.MouseFilterEnum.Pass
			};


		_roomVBox.AddChild(
			_navigationCenter
		);


		_roomVBox.MoveChild(
			_navigationCenter,
			0
		);


		PanelContainer panel =
			new()
			{
				CustomMinimumSize =
					new Vector2(
						ContentWidth,
						56
					),

				MouseFilter =
					Control.MouseFilterEnum.Pass
			};


		panel.AddThemeStyleboxOverride(
			"panel",
			CreateTabBarBackgroundStyle()
		);


		_navigationCenter.AddChild(
			panel
		);


		MarginContainer margin =
			CreateCardMargin(
				5,
				5
			);


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
			6
		);


		margin.AddChild(
			row
		);


		_machinesButton =
			CreateNavigationButton(
				"MACHINES"
			);


		_quantumButton =
			CreateNavigationButton(
				"QUANTUM CORE"
			);


		row.AddChild(
			_machinesButton
		);


		row.AddChild(
			_quantumButton
		);


		_machinesButton.Pressed +=
			() =>
			{
				if (NavigationActionBlocked())
					return;


				_showQuantum =
					false;


				ShowCorrectView();

				UpdateTabStyles();

				PlayHaptic();
			};


		_quantumButton.Pressed +=
			() =>
			{
				if (NavigationActionBlocked())
					return;


				_showQuantum =
					true;


				ShowCorrectView();

				UpdateTabStyles();


				_mobileScroll?.ScrollToTop();

				PlayHaptic();
			};


		UpdateTabStyles();
	}


	private static Button CreateNavigationButton(
		string text)
	{
		Button button =
			new()
			{
				Text =
					text,

				CustomMinimumSize =
					new Vector2(
						0,
						46
					),

				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,

				FocusMode =
					Control.FocusModeEnum.None,

				ClipText =
					true
			};


		button.AddThemeFontSizeOverride(
			"font_size",
			14
		);


		return button;
	}


	private void UpdateTabStyles()
	{
		ApplyTabStyle(
			_machinesButton,
			!_showQuantum
		);


		ApplyTabStyle(
			_quantumButton,
			_showQuantum
		);
	}


	private static void ApplyTabStyle(
		Button button,
		bool active)
	{
		Color accent =
			new(
				0.72f,
				0.38f,
				1.0f,
				1.0f
			);


		Color background =
			active
				? new Color(
					0.30f,
					0.10f,
					0.52f,
					0.98f
				)
				: new Color(
					0.045f,
					0.025f,
					0.075f,
					0.90f
				);


		Color border =
			active
				? accent
				: new Color(
					0.27f,
					0.16f,
					0.40f,
					0.78f
				);


		StyleBoxFlat normal =
			CreateButtonStyle(
				background,
				border
			);


		button.AddThemeStyleboxOverride(
			"normal",
			normal
		);


		button.AddThemeStyleboxOverride(
			"pressed",
			normal
		);


		button.AddThemeStyleboxOverride(
			"hover",
			CreateButtonStyle(
				active
					? new Color(
						0.38f,
						0.14f,
						0.64f,
						1.0f
					)
					: new Color(
						0.10f,
						0.055f,
						0.16f,
						0.96f
					),
				active
					? accent
					: new Color(
						0.48f,
						0.28f,
						0.68f,
						0.90f
					)
			)
		);


		button.AddThemeColorOverride(
			"font_color",
			active
				? Colors.White
				: new Color(
					0.76f,
					0.68f,
					0.86f,
					1.0f
				)
		);


		button.AddThemeColorOverride(
			"font_hover_color",
			Colors.White
		);
	}


	// ==================================================
	// PAGE
	// ==================================================

	private void CreateQuantumView()
	{
		_quantumScroll =
			new ScrollContainer
			{
				Name =
					"QuantumScroll",

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


		_roomVBox.AddChild(
			_quantumScroll
		);


		CenterContainer center =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};


		_quantumScroll.AddChild(
			center
		);


		MarginContainer outer =
			new()
			{
				CustomMinimumSize =
					new Vector2(
						ContentWidth,
						0
					)
			};


		outer.AddThemeConstantOverride(
			"margin_top",
			12
		);


		outer.AddThemeConstantOverride(
			"margin_bottom",
			80
		);


		center.AddChild(
			outer
		);


		VBoxContainer content =
			new()
			{
				CustomMinimumSize =
					new Vector2(
						ContentWidth,
						0
					),

				SizeFlagsHorizontal =
					Control.SizeFlags.ShrinkCenter
			};


		content.AddThemeConstantOverride(
			"separation",
			14
		);


		outer.AddChild(
			content
		);


		CreateSummaryCard(
			content
		);


		CreateOverclockCard(
			content
		);


		foreach (
			QuantumUpgrade upgrade
			in UpgradeOrder
		)
		{
			CreateUpgradeCard(
				content,
				upgrade
			);
		}
	}


	private void CreateSummaryCard(
		VBoxContainer parent)
	{
		PanelContainer panel =
			new()
			{
				CustomMinimumSize =
					new Vector2(
						ContentWidth,
						250
					)
			};


		panel.AddThemeStyleboxOverride(
			"panel",
			CreateSummaryStyle()
		);


		parent.AddChild(
			panel
		);


		MarginContainer margin =
			CreateCardMargin(
				22,
				16
			);


		panel.AddChild(
			margin
		);


		VBoxContainer box =
			new();


		box.AddThemeConstantOverride(
			"separation",
			10
		);


		margin.AddChild(
			box
		);


		Label title =
			CreateLabel(
				25,
				"QUANTUM CORE"
			);


		box.AddChild(
			title
		);


		Label subtitle =
			CreateLabel(
				13,
				"Push the Quantum Lab harder for more output, but keep Stability and Quantum Energy under control."
			);


		subtitle.CustomMinimumSize =
			new Vector2(
				0,
				40
			);


		subtitle.Modulate =
			new Color(
				0.82f,
				0.72f,
				0.94f,
				1.0f
			);


		box.AddChild(
			subtitle
		);


		HBoxContainer stabilityRow =
			CreateSummaryRow(
				StabilityIcon
			);


		box.AddChild(
			stabilityRow
		);


		_stabilityLabel =
			CreateSummaryValueLabel();


		stabilityRow.AddChild(
			_stabilityLabel
		);


		_stabilityBar =
			CreateProgressBar(
				new Color(
					0.72f,
					0.38f,
					1.0f,
					1.0f
				)
			);


		box.AddChild(
			_stabilityBar
		);


		HBoxContainer energyRow =
			CreateSummaryRow(
				EnergyIcon
			);


		box.AddChild(
			energyRow
		);


		_energyLabel =
			CreateSummaryValueLabel();


		energyRow.AddChild(
			_energyLabel
		);


		_energyBar =
			CreateProgressBar(
				new Color(
					0.46f,
					0.74f,
					1.0f,
					1.0f
				)
			);


		box.AddChild(
			_energyBar
		);


		_outputLabel =
			CreateLabel(
				14,
				""
			);


		_outputLabel.CustomMinimumSize =
			new Vector2(
				0,
				28
			);


		box.AddChild(
			_outputLabel
		);
	}


	private void CreateOverclockCard(
		VBoxContainer parent)
	{
		PanelContainer panel =
			CreateFixedCard(
				215,
				new Color(
					0.92f,
					0.44f,
					1.0f,
					1.0f
				)
			);


		parent.AddChild(
			panel
		);


		MarginContainer margin =
			CreateCardMargin(
				18,
				16
			);


		panel.AddChild(
			margin
		);


		HBoxContainer row =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,

				SizeFlagsVertical =
					Control.SizeFlags.ExpandFill
			};


		row.AddThemeConstantOverride(
			"separation",
			18
		);


		margin.AddChild(
			row
		);


		row.AddChild(
			CreateIcon(
				OverclockIcon,
				108
			)
		);


		VBoxContainer info =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};


		info.AddThemeConstantOverride(
			"separation",
			8
		);


		row.AddChild(
			info
		);


		Label title =
			CreateLabel(
				20,
				"OVERCLOCK"
			);


		title.HorizontalAlignment =
			HorizontalAlignment.Left;


		info.AddChild(
			title
		);


		_overclockStatusLabel =
			CreateInfoLabel();


		info.AddChild(
			_overclockStatusLabel
		);


		HBoxContainer controls =
			new()
			{
				CustomMinimumSize =
					new Vector2(
						0,
						58
					),

				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};


		controls.AddThemeConstantOverride(
			"separation",
			8
		);


		info.AddChild(
			controls
		);


		_overclockMinusButton =
			CreateOverclockButton(
				"−"
			);


		_overclockPlusButton =
			CreateOverclockButton(
				"+"
			);


		_overclockLabel =
			CreateLabel(
				22,
				"x1.00"
			);


		_overclockLabel.CustomMinimumSize =
			new Vector2(
				130,
				52
			);


		_overclockLabel.SizeFlagsHorizontal =
			Control.SizeFlags.ExpandFill;


		controls.AddChild(
			_overclockMinusButton
		);


		controls.AddChild(
			_overclockLabel
		);


		controls.AddChild(
			_overclockPlusButton
		);


		_overclockMinusButton.Pressed +=
			() =>
			{
				if (NavigationActionBlocked())
					return;


				ChangeOverclock(
					-1
				);
			};


		_overclockPlusButton.Pressed +=
			() =>
			{
				if (NavigationActionBlocked())
					return;


				ChangeOverclock(
					1
				);
			};
	}


	private void CreateUpgradeCard(
		VBoxContainer parent,
		QuantumUpgrade upgrade)
	{
		Color accent =
			GetUpgradeColor(
				upgrade
			);


		PanelContainer panel =
			CreateFixedCard(
				UpgradeCardHeight,
				accent
			);


		parent.AddChild(
			panel
		);


		MarginContainer margin =
			CreateCardMargin(
				18,
				16
			);


		panel.AddChild(
			margin
		);


		HBoxContainer row =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,

				SizeFlagsVertical =
					Control.SizeFlags.ExpandFill
			};


		row.AddThemeConstantOverride(
			"separation",
			18
		);


		margin.AddChild(
			row
		);


		CenterContainer iconCenter =
			new()
			{
				CustomMinimumSize =
					new Vector2(
						122,
						0
					)
			};


		row.AddChild(
			iconCenter
		);


		iconCenter.AddChild(
			CreateIcon(
				GetUpgradeIcon(
					upgrade
				),
				108
			)
		);


		VBoxContainer info =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,

				SizeFlagsVertical =
					Control.SizeFlags.ExpandFill
			};


		info.AddThemeConstantOverride(
			"separation",
			7
		);


		row.AddChild(
			info
		);


		Label title =
			CreateLabel(
				20,
				QuantumService
					.GetUpgradeName(
						upgrade
					)
			);


		title.HorizontalAlignment =
			HorizontalAlignment.Left;


		title.AutowrapMode =
			TextServer.AutowrapMode.Off;


		info.AddChild(
			title
		);


		_levelLabels[
			upgrade
		] =
			CreateInfoLabel();


		info.AddChild(
			_levelLabels[
				upgrade
			]
		);


		_effectLabels[
			upgrade
		] =
			CreateInfoLabel();


		_effectLabels[
			upgrade
		].Modulate =
			new Color(
				0.82f,
				0.75f,
				0.94f,
				1.0f
			);


		info.AddChild(
			_effectLabels[
				upgrade
			]
		);


		Control spacer =
			new()
			{
				SizeFlagsVertical =
					Control.SizeFlags.ExpandFill
			};


		info.AddChild(
			spacer
		);


		HBoxContainer upgradeRow =
			new()
			{
				CustomMinimumSize =
					new Vector2(
						0,
						48
					),

				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};


		upgradeRow.AddThemeConstantOverride(
			"separation",
			8
		);


		info.AddChild(
			upgradeRow
		);


		upgradeRow.AddChild(
			CreateIcon(
				UpgradeIcon,
				32
			)
		);


		Button button =
			new()
			{
				CustomMinimumSize =
					new Vector2(
						0,
						46
					),

				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,

				FocusMode =
					Control.FocusModeEnum.None,

				ClipText =
					true
			};


		button.AddThemeFontSizeOverride(
			"font_size",
			14
		);


		ApplyUpgradeButtonStyle(
			button,
			accent
		);


		button.Pressed +=
			() =>
			{
				if (NavigationActionBlocked())
					return;


				Upgrade(
					upgrade
				);
			};


		_upgradeButtons[
			upgrade
		] =
			button;


		upgradeRow.AddChild(
			button
		);
	}


	// ==================================================
	// ACTIONS
	// ==================================================

	private void ChangeOverclock(
		int direction)
	{
		_service.ChangeOverclock(
			direction
		);


		PlayHaptic();

		Refresh();

		StateChanged?.Invoke();
	}


	private void Upgrade(
		QuantumUpgrade upgrade)
	{
		QuantumUpgradeResult result =
			_service.Upgrade(
				upgrade
			);


		MessageRequested?.Invoke(
			result.Message
		);


		Refresh();


		if (!result.Changed)
			return;


		PlayHaptic();

		StateChanged?.Invoke();
	}


	// ==================================================
	// SCROLL
	// ==================================================

	private void CreateMobileScrolling()
	{
		_mobileScroll =
			new MobileScrollController
			{
				Name =
					"QuantumMobileScroll"
			};


		_root.AddChild(
			_mobileScroll
		);


		_mobileScroll.Setup(
			_quantumScroll
		);
	}


	private bool NavigationActionBlocked()
	{
		return _mobileScroll != null
			&& _mobileScroll.ShouldSuppressTap;
	}


	// ==================================================
	// UPDATE
	// ==================================================

	public void UpdateAll()
	{
		int currentRoom =
			_state.CurrentRoomIndex;


		if (
			currentRoom
			!= _lastRoom
		)
		{
			_lastRoom =
				currentRoom;


			if (
				currentRoom
				!= GameConfig.QuantumRoomIndex
			)
			{
				_showQuantum =
					false;
			}
		}


		ShowCorrectView();

		UpdateTabStyles();

		Refresh();
	}


	public void UpdateRuntime()
	{
		if (
			_state.CurrentRoomIndex
			!= GameConfig.QuantumRoomIndex
			|| !_showQuantum
		)
		{
			return;
		}


		Refresh();
	}


	private void ShowCorrectView()
	{
		bool quantumRoom =
			_state.CurrentRoomIndex
			== GameConfig.QuantumRoomIndex;


		_navigationCenter.Visible =
			quantumRoom;


		if (!quantumRoom)
		{
			_quantumScroll.Hide();

			return;
		}


		_machineScroll.Visible =
			!_showQuantum;


		_quantumScroll.Visible =
			_showQuantum;
	}


	private void Refresh()
	{
		if (
			_state.RoomStates.Count
			<= GameConfig.QuantumRoomIndex
		)
		{
			return;
		}


		QuantumData quantum =
			_service.GetQuantum();


		double energyCapacity =
			_service.GetEnergyCapacity();


		double stabilityPercent =
			Math.Clamp(
				quantum.Stability,
				0.0,
				GameConfig.QuantumMaximumStability
			);


		double energyPercent =
			energyCapacity > 0.0
				? Math.Clamp(
					quantum.Energy
						/ energyCapacity
						* 100.0,
					0.0,
					100.0
				)
				: 0.0;


		_stabilityLabel.Text =
			"STABILITY   "
			+ stabilityPercent.ToString(
				"0.0"
			)
			+ "%";


		_stabilityBar.Value =
			stabilityPercent;


		_energyLabel.Text =
			"QUANTUM ENERGY   "
			+ NumberFormatter.Format(
				quantum.Energy
			)
			+ " / "
			+ NumberFormatter.Format(
				energyCapacity
			);


		_energyBar.Value =
			energyPercent;


		double selectedOverclock =
			_service.GetSelectedOverclockMultiplier();


		double productionMultiplier =
			_service.GetEffectiveProductionMultiplier();


		_overclockLabel.Text =
			"x"
			+ selectedOverclock.ToString(
				"0.00"
			);


		_overclockMinusButton.Disabled =
			quantum.OverclockIndex <= 0;


		_overclockPlusButton.Disabled =
			quantum.OverclockIndex >= 4;


		if (quantum.RecoveryMode)
		{
			_overclockStatusLabel.Text =
				"SYSTEM RECOVERY • Overclock temporarily suspended";
		}
		else
		{
			double drain =
				GameConfig
					.GetQuantumEnergyDrainPerSecond(
						quantum.OverclockIndex
					);


			double stabilityDrain =
				GameConfig
					.GetQuantumStabilityDrainPerSecond(
						quantum.OverclockIndex
					);


			_overclockStatusLabel.Text =
				"Energy "
				+ drain.ToString(
					"0.0"
				)
				+ "/s"
				+ "  •  Stability "
				+ stabilityDrain.ToString(
					"0.00"
				)
				+ "%/s";
		}


		_outputLabel.Text =
			"EFFECTIVE QUANTUM OUTPUT   x"
			+ productionMultiplier.ToString(
				"0.00"
			);


		foreach (
			QuantumUpgrade upgrade
			in UpgradeOrder
		)
		{
			int level =
				_service.GetLevel(
					upgrade
				);


			_levelLabels[
				upgrade
			].Text =
				"LEVEL  "
				+ level
				+ " / "
				+ GameConfig.QuantumUpgradeMaxLevel;


			switch (upgrade)
			{
				case QuantumUpgrade.Stabilizer:
					_effectLabels[
						upgrade
					].Text =
						"Stability recovery  +"
						+ _service
							.GetStabilityRecoveryPerSecond()
							.ToString(
								"0.00"
							)
						+ "% /s";
					break;


				case QuantumUpgrade.EnergyCore:
					_effectLabels[
						upgrade
					].Text =
						"Capacity  "
						+ NumberFormatter.Format(
							energyCapacity
						)
						+ "  •  Regen  +"
						+ _service
							.GetEnergyRegenPerSecond()
							.ToString(
								"0.0"
							)
						+ "/s";
					break;


				case QuantumUpgrade.Amplifier:
					double amplifierBonus =
						(
							1.0
							+ (
								level - 1
							)
							* GameConfig
								.QuantumAmplifierBonusPerLevel
						)
						* 100.0;


					_effectLabels[
						upgrade
					].Text =
						"Overclock bonus efficiency  "
						+ amplifierBonus.ToString(
							"0"
						)
						+ "%";
					break;
			}


			Button button =
				_upgradeButtons[
					upgrade
				];


			if (
				level
				>= GameConfig.QuantumUpgradeMaxLevel
			)
			{
				button.Text =
					"MAX LEVEL";


				button.Disabled =
					true;


				continue;
			}


			double cost =
				_service.GetUpgradeCost(
					upgrade
				);


			button.Text =
				"UPGRADE  •  "
				+ NumberFormatter.Format(
					cost
				);


			button.Disabled =
				_state.Tokens
				< cost;
		}
	}


	// ==================================================
	// HELPERS
	// ==================================================

	private static HBoxContainer CreateSummaryRow(
		Texture2D icon)
	{
		HBoxContainer row =
			new()
			{
				CustomMinimumSize =
					new Vector2(
						0,
						38
					),

				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};


		row.AddThemeConstantOverride(
			"separation",
			8
		);


		row.AddChild(
			CreateIcon(
				icon,
				34
			)
		);


		return row;
	}


	private static Label CreateSummaryValueLabel()
	{
		Label label =
			CreateLabel(
				15,
				""
			);


		label.HorizontalAlignment =
			HorizontalAlignment.Left;


		label.SizeFlagsHorizontal =
			Control.SizeFlags.ExpandFill;


		return label;
	}


	private static PanelContainer CreateFixedCard(
		float height,
		Color accent)
	{
		PanelContainer panel =
			new()
			{
				CustomMinimumSize =
					new Vector2(
						ContentWidth,
						height
					),

				MouseFilter =
					Control.MouseFilterEnum.Pass
			};


		panel.AddThemeStyleboxOverride(
			"panel",
			CreatePanelStyle(
				accent
			)
		);


		return panel;
	}


	private static Button CreateOverclockButton(
		string text)
	{
		Button button =
			new()
			{
				Text =
					text,

				CustomMinimumSize =
					new Vector2(
						58,
						52
					),

				FocusMode =
					Control.FocusModeEnum.None
			};


		button.AddThemeFontSizeOverride(
			"font_size",
			24
		);


		Color accent =
			new(
				0.82f,
				0.42f,
				1.0f,
				1.0f
			);


		button.AddThemeStyleboxOverride(
			"normal",
			CreateButtonStyle(
				new Color(
					0.24f,
					0.08f,
					0.38f,
					0.98f
				),
				accent
			)
		);


		return button;
	}


	private static Label CreateInfoLabel()
	{
		Label label =
			CreateLabel(
				13,
				""
			);


		label.CustomMinimumSize =
			new Vector2(
				0,
				24
			);


		label.HorizontalAlignment =
			HorizontalAlignment.Left;


		label.AutowrapMode =
			TextServer.AutowrapMode.Off;


		return label;
	}


	private static ProgressBar CreateProgressBar(
		Color accent)
	{
		ProgressBar bar =
			new()
			{
				CustomMinimumSize =
					new Vector2(
						0,
						16
					),

				MinValue =
					0.0,

				MaxValue =
					100.0,

				Value =
					0.0,

				ShowPercentage =
					false
			};


		bar.AddThemeStyleboxOverride(
			"background",
			new StyleBoxFlat
			{
				BgColor =
					new Color(
						0.018f,
						0.012f,
						0.035f,
						0.96f
					),

				CornerRadiusTopLeft =
					8,

				CornerRadiusTopRight =
					8,

				CornerRadiusBottomLeft =
					8,

				CornerRadiusBottomRight =
					8
			}
		);


		bar.AddThemeStyleboxOverride(
			"fill",
			new StyleBoxFlat
			{
				BgColor =
					accent,

				CornerRadiusTopLeft =
					8,

				CornerRadiusTopRight =
					8,

				CornerRadiusBottomLeft =
					8,

				CornerRadiusBottomRight =
					8
			}
		);


		return bar;
	}


	private static Texture2D GetUpgradeIcon(
		QuantumUpgrade upgrade)
	{
		return upgrade switch
		{
			QuantumUpgrade.Stabilizer =>
				StabilizerIcon,

			QuantumUpgrade.EnergyCore =>
				EnergyCoreIcon,

			QuantumUpgrade.Amplifier =>
				AmplifierIcon,

			_ =>
				StabilizerIcon
		};
	}


	private static Color GetUpgradeColor(
		QuantumUpgrade upgrade)
	{
		return upgrade switch
		{
			QuantumUpgrade.Stabilizer =>
				new Color(
					0.66f,
					0.34f,
					1.0f,
					1.0f
				),

			QuantumUpgrade.EnergyCore =>
				new Color(
					0.42f,
					0.56f,
					1.0f,
					1.0f
				),

			QuantumUpgrade.Amplifier =>
				new Color(
					0.92f,
					0.38f,
					0.92f,
					1.0f
				),

			_ =>
				Colors.White
		};
	}


	private static TextureRect CreateIcon(
		Texture2D texture,
		float size)
	{
		return new TextureRect
		{
			Texture =
				texture,

			CustomMinimumSize =
				new Vector2(
					size,
					size
				),

			ExpandMode =
				TextureRect.ExpandModeEnum.IgnoreSize,

			StretchMode =
				TextureRect.StretchModeEnum.KeepAspectCentered,

			MouseFilter =
				Control.MouseFilterEnum.Ignore
		};
	}


	private static Label CreateLabel(
		int size,
		string text)
	{
		Label label =
			new()
			{
				Text =
					text,

				AutowrapMode =
					TextServer.AutowrapMode.WordSmart,

				VerticalAlignment =
					VerticalAlignment.Center,

				HorizontalAlignment =
					HorizontalAlignment.Center,

				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};


		label.AddThemeFontSizeOverride(
			"font_size",
			size
		);


		return label;
	}


	private static MarginContainer CreateCardMargin(
		int horizontal,
		int vertical)
	{
		MarginContainer margin =
			new();


		margin.AddThemeConstantOverride(
			"margin_left",
			horizontal
		);


		margin.AddThemeConstantOverride(
			"margin_right",
			horizontal
		);


		margin.AddThemeConstantOverride(
			"margin_top",
			vertical
		);


		margin.AddThemeConstantOverride(
			"margin_bottom",
			vertical
		);


		return margin;
	}


	private static StyleBoxFlat CreateTabBarBackgroundStyle()
	{
		return new StyleBoxFlat
		{
			BgColor =
				new Color(
					0.025f,
					0.012f,
					0.045f,
					0.97f
				),

			BorderColor =
				new Color(
					0.55f,
					0.28f,
					0.82f,
					0.72f
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
				19,

			ShadowColor =
				new Color(
					0,
					0,
					0,
					0.30f
				),

			ShadowSize =
				8
		};
	}


	private static StyleBoxFlat CreateSummaryStyle()
	{
		return new StyleBoxFlat
		{
			BgColor =
				new Color(
					0.075f,
					0.025f,
					0.12f,
					0.97f
				),

			BorderColor =
				new Color(
					0.72f,
					0.38f,
					1.0f,
					0.82f
				),

			BorderWidthLeft =
				2,

			BorderWidthTop =
				2,

			BorderWidthRight =
				2,

			BorderWidthBottom =
				2,

			CornerRadiusTopLeft =
				20,

			CornerRadiusTopRight =
				20,

			CornerRadiusBottomLeft =
				20,

			CornerRadiusBottomRight =
				20,

			ShadowColor =
				new Color(
					0.55f,
					0.16f,
					1.0f,
					0.18f
				),

			ShadowSize =
				12
		};
	}


	private static StyleBoxFlat CreatePanelStyle(
		Color accent)
	{
		return new StyleBoxFlat
		{
			BgColor =
				new Color(
					0.045f,
					0.020f,
					0.070f,
					0.98f
				),

			BorderColor =
				new Color(
					accent.R,
					accent.G,
					accent.B,
					0.74f
				),

			BorderWidthLeft =
				2,

			BorderWidthTop =
				2,

			BorderWidthRight =
				2,

			BorderWidthBottom =
				2,

			CornerRadiusTopLeft =
				18,

			CornerRadiusTopRight =
				18,

			CornerRadiusBottomLeft =
				18,

			CornerRadiusBottomRight =
				18,

			ShadowColor =
				new Color(
					0,
					0,
					0,
					0.34f
				),

			ShadowSize =
				9
		};
	}


	private static StyleBoxFlat CreateButtonStyle(
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


	private static void ApplyUpgradeButtonStyle(
		Button button,
		Color accent)
	{
		button.AddThemeColorOverride(
			"font_color",
			Colors.White
		);


		button.AddThemeColorOverride(
			"font_disabled_color",
			new Color(
				0.44f,
				0.42f,
				0.50f,
				1.0f
			)
		);


		button.AddThemeStyleboxOverride(
			"normal",
			CreateButtonStyle(
				new Color(
					accent.R * 0.32f,
					accent.G * 0.32f,
					accent.B * 0.32f,
					0.98f
				),
				new Color(
					accent.R,
					accent.G,
					accent.B,
					0.86f
				)
			)
		);


		button.AddThemeStyleboxOverride(
			"hover",
			CreateButtonStyle(
				new Color(
					accent.R * 0.46f,
					accent.G * 0.46f,
					accent.B * 0.46f,
					1.0f
				),
				accent
			)
		);


		button.AddThemeStyleboxOverride(
			"pressed",
			CreateButtonStyle(
				new Color(
					accent.R * 0.24f,
					accent.G * 0.24f,
					accent.B * 0.24f,
					1.0f
				),
				accent
			)
		);


		button.AddThemeStyleboxOverride(
			"disabled",
			CreateButtonStyle(
				new Color(
					0.045f,
					0.035f,
					0.060f,
					0.92f
				),
				new Color(
					0.20f,
					0.16f,
					0.25f,
					0.70f
				)
			)
		);
	}


	private static void PlayHaptic()
	{
		Input.VibrateHandheld(
			14,
			0.12f
		);
	}
}
