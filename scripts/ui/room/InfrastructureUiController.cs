using Godot;
using System;
using System.Collections.Generic;

namespace IdleAi;

public sealed class InfrastructureUiController
{
	private const float ContentWidth =
		560.0f;


	private const float CardHeight =
		245.0f;


	private static readonly InfrastructureSystem[] SystemOrder =
	[
		InfrastructureSystem.Power,
		InfrastructureSystem.Cooling,
		InfrastructureSystem.Storage
	];


	private static readonly Texture2D PowerIcon =
		GD.Load<Texture2D>(
			"res://assets/infrastructure/power.png"
		);


	private static readonly Texture2D CoolingIcon =
		GD.Load<Texture2D>(
			"res://assets/infrastructure/cooling.png"
		);


	private static readonly Texture2D StorageIcon =
		GD.Load<Texture2D>(
			"res://assets/infrastructure/storage.png"
		);


	private static readonly Texture2D TemperatureIcon =
		GD.Load<Texture2D>(
			"res://assets/infrastructure/temperature.png"
		);


	private static readonly Texture2D UpgradeIcon =
		GD.Load<Texture2D>(
			"res://assets/pipeline/pipeline_upgrade.png"
		);


	private readonly Game _root;

	private readonly GameState _state;

	private readonly InfrastructureService _service;


	private VBoxContainer _roomVBox =
		null!;


	private ScrollContainer _machineScroll =
		null!;


	private CenterContainer _navigationCenter =
		null!;


	private Button _machinesButton =
		null!;


	private Button _infrastructureButton =
		null!;


	private ScrollContainer _infrastructureScroll =
		null!;


	private MobileScrollController _mobileScroll =
		null!;


	private Label _temperatureLabel =
		null!;


	private Label _efficiencyLabel =
		null!;


	private readonly Dictionary<InfrastructureSystem, Label>
		_levelLabels =
			[];


	private readonly Dictionary<InfrastructureSystem, Label>
		_statusLabels =
			[];


	private readonly Dictionary<InfrastructureSystem, ProgressBar>
		_usageBars =
			[];


	private readonly Dictionary<InfrastructureSystem, Button>
		_upgradeButtons =
			[];


	private bool _showInfrastructure;

	private int _lastRoom =
		-1;


	private bool _displayInitialized;


	private ulong _lastSmoothTicks;


	private double _displayTemperature;


	private double _displayOutputPercent;


	private readonly Dictionary<InfrastructureSystem, double>
		_displayUsagePercent =
			[];


	public event Action<string>? MessageRequested;

	public event Action? StateChanged;


	public InfrastructureUiController(
		Game root,
		GameState state,
		InfrastructureService service)
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
		_roomVBox =
			_root.GetNode<VBoxContainer>(
				"MarginContainer/VBoxContainer/RoomPanel/RoomVBox"
			);


		_machineScroll =
			_root.GetNode<ScrollContainer>(
				"MarginContainer/VBoxContainer/RoomPanel/RoomVBox/ScrollContainer"
			);


		CreateNavigation();

		CreateInfrastructureView();

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
					"InfrastructureNavigationCenter",

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
			new();


		margin.AddThemeConstantOverride(
			"margin_left",
			5
		);


		margin.AddThemeConstantOverride(
			"margin_top",
			5
		);


		margin.AddThemeConstantOverride(
			"margin_right",
			5
		);


		margin.AddThemeConstantOverride(
			"margin_bottom",
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


		_infrastructureButton =
			CreateNavigationButton(
				"INFRASTRUCTURE"
			);


		row.AddChild(
			_machinesButton
		);


		row.AddChild(
			_infrastructureButton
		);


		_machinesButton.Pressed +=
			() =>
			{
				if (NavigationActionBlocked())
					return;


				_showInfrastructure =
					false;


				ShowCorrectView();

				UpdateTabStyles();

				PlayHaptic();
			};


		_infrastructureButton.Pressed +=
			() =>
			{
				if (NavigationActionBlocked())
					return;


				_showInfrastructure =
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
			!_showInfrastructure
		);


		ApplyTabStyle(
			_infrastructureButton,
			_showInfrastructure
		);
	}


	private static void ApplyTabStyle(
		Button button,
		bool active)
	{
		Color accent =
			new(
				0.18f,
				0.92f,
				0.55f,
				1.0f
			);


		Color background =
			active
				? new Color(
					0.05f,
					0.36f,
					0.23f,
					0.98f
				)
				: new Color(
					0.025f,
					0.055f,
					0.070f,
					0.88f
				);


		Color border =
			active
				? accent
				: new Color(
					0.12f,
					0.28f,
					0.24f,
					0.76f
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
			"hover",
			CreateButtonStyle(
				active
					? new Color(
						0.06f,
						0.44f,
						0.28f,
						1.0f
					)
					: new Color(
						0.04f,
						0.12f,
						0.10f,
						0.96f
					),
				active
					? accent
					: new Color(
						0.22f,
						0.60f,
						0.45f,
						0.90f
					)
			)
		);


		button.AddThemeStyleboxOverride(
			"pressed",
			normal
		);


		button.AddThemeColorOverride(
			"font_color",
			active
				? Colors.White
				: new Color(
					0.66f,
					0.78f,
					0.73f,
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

	private void CreateInfrastructureView()
	{
		_infrastructureScroll =
			new ScrollContainer
			{
				Name =
					"InfrastructureScroll",

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
			_infrastructureScroll
		);


		CenterContainer center =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};


		_infrastructureScroll.AddChild(
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


		foreach (
			InfrastructureSystem system
			in SystemOrder
		)
		{
			CreateSystemCard(
				content,
				system
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
						165
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
			8
		);


		margin.AddChild(
			box
		);


		Label title =
			CreateLabel(
				25,
				"DATA CENTER INFRASTRUCTURE"
			);


		box.AddChild(
			title
		);


		Label subtitle =
			CreateLabel(
				13,
				"More machines create more load. Upgrade the infrastructure to keep the Data Center efficient."
			);


		subtitle.CustomMinimumSize =
			new Vector2(
				0,
				36
			);


		subtitle.Modulate =
			new Color(
				0.72f,
				0.84f,
				0.78f,
				1.0f
			);


		box.AddChild(
			subtitle
		);


		HBoxContainer stats =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};


		stats.AddThemeConstantOverride(
			"separation",
			10
		);


		box.AddChild(
			stats
		);


		HBoxContainer temperatureStat =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};


		temperatureStat.AddThemeConstantOverride(
			"separation",
			6
		);


		temperatureStat.AddChild(
			CreateIcon(
				TemperatureIcon,
				36
			)
		);


		_temperatureLabel =
			CreateLabel(
				15,
				""
			);


		_temperatureLabel.CustomMinimumSize =
			new Vector2(
				0,
				58
			);


		_temperatureLabel.SizeFlagsHorizontal =
			Control.SizeFlags.ExpandFill;


		temperatureStat.AddChild(
			_temperatureLabel
		);


		_efficiencyLabel =
			CreateLabel(
				15,
				""
			);


		_efficiencyLabel.CustomMinimumSize =
			new Vector2(
				0,
				58
			);


		_efficiencyLabel.SizeFlagsHorizontal =
			Control.SizeFlags.ExpandFill;


		stats.AddChild(
			temperatureStat
		);


		stats.AddChild(
			_efficiencyLabel
		);
	}


	private void CreateSystemCard(
		VBoxContainer parent,
		InfrastructureSystem system)
	{
		PanelContainer panel =
			new()
			{
				CustomMinimumSize =
					new Vector2(
						ContentWidth,
						CardHeight
					),

				MouseFilter =
					Control.MouseFilterEnum.Pass
			};


		Color accent =
			GetSystemColor(
				system
			);


		panel.AddThemeStyleboxOverride(
			"panel",
			CreatePanelStyle(
				accent
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
				GetSystemIcon(
					system
				),
				108
			)
		);


		VBoxContainer information =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,

				SizeFlagsVertical =
					Control.SizeFlags.ExpandFill
			};


		information.AddThemeConstantOverride(
			"separation",
			7
		);


		row.AddChild(
			information
		);


		Label title =
			CreateLabel(
				20,
				InfrastructureService
					.GetSystemName(
						system
					)
			);


		title.HorizontalAlignment =
			HorizontalAlignment.Left;


		title.AutowrapMode =
			TextServer.AutowrapMode.Off;


		information.AddChild(
			title
		);


		_levelLabels[
			system
		] =
			CreateInfoLabel();


		information.AddChild(
			_levelLabels[
				system
			]
		);


		_statusLabels[
			system
		] =
			CreateInfoLabel();


		_statusLabels[
			system
		].Modulate =
			new Color(
				0.72f,
				0.84f,
				0.78f,
				1.0f
			);


		information.AddChild(
			_statusLabels[
				system
			]
		);


		_usageBars[
			system
		] =
			CreateUsageBar(
				accent
			);


		information.AddChild(
			_usageBars[
				system
			]
		);


		Control spacer =
			new()
			{
				SizeFlagsVertical =
					Control.SizeFlags.ExpandFill
			};


		information.AddChild(
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


		information.AddChild(
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


				UpgradeSystem(
					system
				);
			};


		_upgradeButtons[
			system
		] =
			button;


		upgradeRow.AddChild(
			button
		);
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
					"InfrastructureMobileScroll"
			};


		_root.AddChild(
			_mobileScroll
		);


		_mobileScroll.Setup(
			_infrastructureScroll
		);
	}


	private bool NavigationActionBlocked()
	{
		return _mobileScroll != null
			&& _mobileScroll.ShouldSuppressTap;
	}


	// ==================================================
	// ACTIONS
	// ==================================================

	private void UpgradeSystem(
		InfrastructureSystem system)
	{
		InfrastructureUpgradeResult result =
			_service.Upgrade(
				system
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
				== GameConfig.InfrastructureRoomIndex
			)
			{
				_displayInitialized =
					false;


				_lastSmoothTicks =
					Time.GetTicksMsec();
			}
			else
			{
				_showInfrastructure =
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
			!= GameConfig.InfrastructureRoomIndex
			|| !_showInfrastructure
		)
		{
			return;
		}


		Refresh(
			animate:
				true
		);
	}


	private void ShowCorrectView()
	{
		bool dataCenter =
			_state.CurrentRoomIndex
			== GameConfig.InfrastructureRoomIndex;


		_navigationCenter.Visible =
			dataCenter;


		if (!dataCenter)
		{
			_infrastructureScroll.Hide();

			return;
		}


		_machineScroll.Visible =
			!_showInfrastructure;


		_infrastructureScroll.Visible =
			_showInfrastructure;
	}


	private void Refresh(
		bool animate = false)
	{
		if (
			_state.RoomStates.Count
			<= GameConfig.InfrastructureRoomIndex
		)
		{
			return;
		}


		double load =
			_service.GetCurrentLoad();


		double targetTemperature =
			_service.GetTemperatureCelsius();


		double targetOutputPercent =
			_service.GetProductionMultiplier()
			* 100.0;


		Dictionary<InfrastructureSystem, double>
			targetUsage =
				[];


		foreach (
			InfrastructureSystem system
			in SystemOrder
		)
		{
			double capacity =
				_service.GetCapacity(
					system
				);


			targetUsage[
				system
			] =
				GetUsagePercent(
					load,
					capacity
				);
		}


		if (!_displayInitialized)
		{
			_displayTemperature =
				targetTemperature;


			_displayOutputPercent =
				targetOutputPercent;


			foreach (
				InfrastructureSystem system
				in SystemOrder
			)
			{
				_displayUsagePercent[
					system
				] =
					targetUsage[
						system
					];
			}


			_displayInitialized =
				true;


			_lastSmoothTicks =
				Time.GetTicksMsec();
		}
		else if (animate)
		{
			ulong now =
				Time.GetTicksMsec();


			double delta =
				_lastSmoothTicks == 0
					? 0.0
					: (
						now
						- _lastSmoothTicks
					)
					/ 1000.0;


			_lastSmoothTicks =
				now;


			/*
			 * Exponential smoothing feels natural on a
			 * mobile UI and is independent of frame rate.
			 *
			 * Larger value = faster visual reaction.
			 */
			double smoothing =
				1.0
				- Math.Exp(
					-2.2
					* Math.Clamp(
						delta,
						0.0,
						0.10
					)
				);


			_displayTemperature =
				Lerp(
					_displayTemperature,
					targetTemperature,
					smoothing
				);


			_displayOutputPercent =
				Lerp(
					_displayOutputPercent,
					targetOutputPercent,
					smoothing
				);


			foreach (
				InfrastructureSystem system
				in SystemOrder
			)
			{
				double current =
					_displayUsagePercent
						.GetValueOrDefault(
							system,
							targetUsage[
								system
							]
						);


				_displayUsagePercent[
					system
				] =
					Lerp(
						current,
						targetUsage[
							system
						],
						smoothing
					);
			}
		}


		_temperatureLabel.Text =
			"TEMPERATURE\n"
			+ _displayTemperature.ToString(
				"0"
			)
			+ "°C";


		_temperatureLabel.Modulate =
			GetTemperatureColor(
				_displayTemperature
			);


		_efficiencyLabel.Text =
			"ROOM OUTPUT\n"
			+ _displayOutputPercent.ToString(
				"0"
			)
			+ "%";


		foreach (
			InfrastructureSystem system
			in SystemOrder
		)
		{
			int level =
				_service.GetLevel(
					system
				);


			double capacity =
				_service.GetCapacity(
					system
				);


			_levelLabels[
				system
			].Text =
				"LEVEL  "
				+ level
				+ " / "
				+ GameConfig.InfrastructureMaxLevel;


			switch (system)
			{
				case InfrastructureSystem.Power:
					_statusLabels[
						system
					].Text =
						"LOAD  "
						+ NumberFormatter.Format(
							load
						)
						+ "  /  "
						+ NumberFormatter.Format(
							capacity
						);
					break;


				case InfrastructureSystem.Cooling:
					_statusLabels[
						system
					].Text =
						"COOLING LOAD  "
						+ NumberFormatter.Format(
							load
						)
						+ "  /  "
						+ NumberFormatter.Format(
							capacity
						)
						+ "  •  "
						+ _displayTemperature.ToString(
							"0"
						)
						+ "°C";
					break;


				case InfrastructureSystem.Storage:
					_statusLabels[
						system
					].Text =
						"DATA LOAD  "
						+ NumberFormatter.Format(
							load
						)
						+ "  /  "
						+ NumberFormatter.Format(
							capacity
						);
					break;
			}


			_usageBars[
				system
			].Value =
				_displayUsagePercent
					.GetValueOrDefault(
						system,
						0.0
					);


			Button button =
				_upgradeButtons[
					system
				];


			if (
				level
				>= GameConfig.InfrastructureMaxLevel
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
					system
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

	private static double Lerp(
		double from,
		double to,
		double weight)
	{
		return from
			+ (
				to - from
			)
			* Math.Clamp(
				weight,
				0.0,
				1.0
			);
	}


	private static double GetUsagePercent(
		double load,
		double capacity)
	{
		if (capacity <= 0.0)
			return 100.0;


		return Math.Clamp(
			load / capacity * 100.0,
			0.0,
			100.0
		);
	}


	// ==================================================
	// UI HELPERS
	// ==================================================


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


	private static ProgressBar CreateUsageBar(
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
						0.01f,
						0.025f,
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


	private static Texture2D GetSystemIcon(
		InfrastructureSystem system)
	{
		return system switch
		{
			InfrastructureSystem.Power =>
				PowerIcon,

			InfrastructureSystem.Cooling =>
				CoolingIcon,

			InfrastructureSystem.Storage =>
				StorageIcon,

			_ =>
				PowerIcon
		};
	}


	private static Color GetSystemColor(
		InfrastructureSystem system)
	{
		return system switch
		{
			InfrastructureSystem.Power =>
				new Color(
					1.0f,
					0.78f,
					0.22f,
					1.0f
				),

			InfrastructureSystem.Cooling =>
				new Color(
					0.18f,
					0.72f,
					1.0f,
					1.0f
				),

			InfrastructureSystem.Storage =>
				new Color(
					0.24f,
					0.92f,
					0.58f,
					1.0f
				),

			_ =>
				Colors.White
		};
	}


	private static Color GetTemperatureColor(
		double temperature)
	{
		if (temperature < 60.0)
		{
			return new Color(
				0.55f,
				1.0f,
				0.72f,
				1.0f
			);
		}


		if (temperature < 78.0)
		{
			return new Color(
				1.0f,
				0.82f,
				0.30f,
				1.0f
			);
		}


		return new Color(
			1.0f,
			0.42f,
			0.32f,
			1.0f
		);
	}


	private static StyleBoxFlat CreateTabBarBackgroundStyle()
	{
		return new StyleBoxFlat
		{
			BgColor =
				new Color(
					0.012f,
					0.035f,
					0.040f,
					0.96f
				),

			BorderColor =
				new Color(
					0.16f,
					0.62f,
					0.43f,
					0.66f
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
					0.28f
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
					0.025f,
					0.085f,
					0.065f,
					0.97f
				),

			BorderColor =
				new Color(
					0.20f,
					0.90f,
					0.56f,
					0.80f
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
					0.05f,
					0.90f,
					0.45f,
					0.14f
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
					0.018f,
					0.045f,
					0.045f,
					0.98f
				),

			BorderColor =
				new Color(
					accent.R,
					accent.G,
					accent.B,
					0.72f
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
					0.32f
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
				0.42f,
				0.48f,
				0.55f,
				1.0f
			)
		);


		button.AddThemeStyleboxOverride(
			"normal",
			CreateButtonStyle(
				new Color(
					accent.R * 0.33f,
					accent.G * 0.33f,
					accent.B * 0.33f,
					0.98f
				),
				new Color(
					accent.R,
					accent.G,
					accent.B,
					0.84f
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
					0.035f,
					0.050f,
					0.055f,
					0.92f
				),
				new Color(
					0.15f,
					0.20f,
					0.19f,
					0.70f
				)
			)
		);
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


	private static void PlayHaptic()
	{
		Input.VibrateHandheld(
			14,
			0.12f
		);
	}
}
