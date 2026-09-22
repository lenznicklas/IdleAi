using Godot;
using System;
using System.Collections.Generic;

namespace IdleAi;

public sealed class PipelineUiController
{
	private const float ContentWidth =
		560.0f;


	private const float StageCardHeight =
		255.0f;


	private static readonly PipelineStage[] StageOrder =
	[
		PipelineStage.Compute,
		PipelineStage.Data,
		PipelineStage.Model,
		PipelineStage.Output
	];


	private static readonly Texture2D ComputeIcon =
		GD.Load<Texture2D>(
			"res://assets/pipeline/pipeline_compute.png"
		);


	private static readonly Texture2D DataIcon =
		GD.Load<Texture2D>(
			"res://assets/pipeline/pipeline_data.png"
		);


	private static readonly Texture2D ModelIcon =
		GD.Load<Texture2D>(
			"res://assets/pipeline/pipeline_model.png"
		);


	private static readonly Texture2D OutputIcon =
		GD.Load<Texture2D>(
			"res://assets/pipeline/pipeline_output.png"
		);


	private static readonly Texture2D UpgradeIcon =
		GD.Load<Texture2D>(
			"res://assets/pipeline/pipeline_upgrade.png"
		);


	private readonly Game _root;

	private readonly GameState _state;

	private readonly PipelineService _service;


	private VBoxContainer _roomVBox =
		null!;


	private ScrollContainer _machineScroll =
		null!;


	private CenterContainer _navigationCenter =
		null!;


	private PanelContainer _navigationPanel =
		null!;


	private Button _machinesButton =
		null!;


	private Button _pipelineButton =
		null!;


	private ScrollContainer _pipelineScroll =
		null!;


	private MobileScrollController _mobileScroll =
		null!;


	private Label _machineInputLabel =
		null!;


	private Label _tokenOutputLabel =
		null!;


	private readonly Dictionary<PipelineStage, Label>
		_levelLabels =
			[];


	private readonly Dictionary<PipelineStage, Label>
		_capacityLabels =
			[];


	private readonly Dictionary<PipelineStage, Label>
		_bufferLabels =
			[];


	private readonly Dictionary<PipelineStage, ProgressBar>
		_progressBars =
			[];


	private readonly Dictionary<PipelineStage, Label>
		_cycleLabels =
			[];


	private readonly Dictionary<PipelineStage, Button>
		_upgradeButtons =
			[];


	private bool _showPipeline;

	private int _lastRoom =
		-1;


	public event Action<string>? MessageRequested;

	public event Action? StateChanged;


	public PipelineUiController(
		Game root,
		GameState state,
		PipelineService service)
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

		CreatePipelineView();

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
					"PipelineNavigationCenter",

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


		_navigationPanel =
			new PanelContainer
			{
				Name =
					"PipelineNavigation",

				CustomMinimumSize =
					new Vector2(
						ContentWidth,
						56
					),

				MouseFilter =
					Control.MouseFilterEnum.Pass
			};


		_navigationPanel.AddThemeStyleboxOverride(
			"panel",
			CreateTabBarBackgroundStyle()
		);


		_navigationCenter.AddChild(
			_navigationPanel
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


		_navigationPanel.AddChild(
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


		_pipelineButton =
			CreateNavigationButton(
				"PIPELINE"
			);


		row.AddChild(
			_machinesButton
		);


		row.AddChild(
			_pipelineButton
		);


		_machinesButton.Pressed +=
			() =>
			{
				if (NavigationActionBlocked())
					return;


				_showPipeline =
					false;


				ShowCorrectView();

				UpdateTabStyles();

				PlayHaptic();
			};


		_pipelineButton.Pressed +=
			() =>
			{
				if (NavigationActionBlocked())
					return;


				_showPipeline =
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
					Control.FocusModeEnum.None
			};


		button.AddThemeFontSizeOverride(
			"font_size",
			15
		);


		return button;
	}


	private void UpdateTabStyles()
	{
		ApplyTabStyle(
			_machinesButton,
			active:
				!_showPipeline
		);


		ApplyTabStyle(
			_pipelineButton,
			active:
				_showPipeline
		);
	}


	private static void ApplyTabStyle(
		Button button,
		bool active)
	{
		Color accent =
			new(
				0.14f,
				0.68f,
				1.0f,
				1.0f
			);


		Color background =
			active
				? new Color(
					0.07f,
					0.34f,
					0.58f,
					0.98f
				)
				: new Color(
					0.025f,
					0.055f,
					0.095f,
					0.85f
				);


		Color border =
			active
				? accent
				: new Color(
					0.12f,
					0.25f,
					0.36f,
					0.75f
				);


		StyleBoxFlat normal =
			CreateButtonStyle(
				background,
				border
			);


		StyleBoxFlat hover =
			CreateButtonStyle(
				active
					? new Color(
						0.08f,
						0.40f,
						0.66f,
						1.0f
					)
					: new Color(
						0.04f,
						0.10f,
						0.16f,
						0.95f
					),
				active
					? accent
					: new Color(
						0.20f,
						0.45f,
						0.65f,
						0.90f
					)
			);


		button.AddThemeStyleboxOverride(
			"normal",
			normal
		);


		button.AddThemeStyleboxOverride(
			"hover",
			hover
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
					0.62f,
					0.74f,
					0.84f,
					1.0f
				)
		);


		button.AddThemeColorOverride(
			"font_hover_color",
			Colors.White
		);
	}


	// ==================================================
	// PIPELINE VIEW
	// ==================================================

	private void CreatePipelineView()
	{
		_pipelineScroll =
			new ScrollContainer
			{
				Name =
					"PipelineScroll",

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
			_pipelineScroll
		);


		CenterContainer center =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};


		_pipelineScroll.AddChild(
			center
		);


		MarginContainer outerMargin =
			new()
			{
				CustomMinimumSize =
					new Vector2(
						ContentWidth,
						0
					)
			};


		outerMargin.AddThemeConstantOverride(
			"margin_top",
			12
		);


		outerMargin.AddThemeConstantOverride(
			"margin_bottom",
			80
		);


		center.AddChild(
			outerMargin
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
			12
		);


		outerMargin.AddChild(
			content
		);


		CreateSummaryCard(
			content
		);


		for (
			int i = 0;
			i < StageOrder.Length;
			i++
		)
		{
			CreateStageCard(
				content,
				StageOrder[
					i
				]
			);


			if (
				i
				< StageOrder.Length - 1
			)
			{
				CreateConnector(
					content
				);
			}
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
						150
					),

				SizeFlagsHorizontal =
					Control.SizeFlags.ShrinkCenter
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
				"SERVER PIPELINE"
			);


		title.HorizontalAlignment =
			HorizontalAlignment.Center;


		box.AddChild(
			title
		);


		Label subtitle =
			CreateLabel(
				13,
				"Machines create material. Each stage processes it in cycles until AI Output converts it into Tokens."
			);


		subtitle.CustomMinimumSize =
			new Vector2(
				0,
				38
			);


		subtitle.Modulate =
			new Color(
				0.70f,
				0.82f,
				0.92f,
				1.0f
			);


		subtitle.HorizontalAlignment =
			HorizontalAlignment.Center;


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


		_machineInputLabel =
			CreateStatLabel();


		_tokenOutputLabel =
			CreateStatLabel();


		stats.AddChild(
			_machineInputLabel
		);


		stats.AddChild(
			_tokenOutputLabel
		);
	}


	private void CreateStageCard(
		VBoxContainer parent,
		PipelineStage stage)
	{
		CenterContainer center =
			new()
			{
				CustomMinimumSize =
					new Vector2(
						ContentWidth,
						StageCardHeight
					),

				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};


		parent.AddChild(
			center
		);


		PanelContainer panel =
			new()
			{
				CustomMinimumSize =
					new Vector2(
						ContentWidth,
						StageCardHeight
					),

				SizeFlagsHorizontal =
					Control.SizeFlags.ShrinkCenter,

				MouseFilter =
					Control.MouseFilterEnum.Pass
			};


		panel.AddThemeStyleboxOverride(
			"panel",
			CreatePanelStyle(
				GetStageColor(
					stage
				)
			)
		);


		center.AddChild(
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
				CustomMinimumSize =
					new Vector2(
						0,
						StageCardHeight - 32
					),

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
				GetStageIcon(
					stage
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
			4
		);


		row.AddChild(
			information
		);


		Label title =
			CreateLabel(
				19,
				PipelineService
					.GetStageName(
						stage
					)
			);


		title.CustomMinimumSize =
			new Vector2(
				0,
				30
			);


		title.HorizontalAlignment =
			HorizontalAlignment.Left;


		title.AutowrapMode =
			TextServer.AutowrapMode.Off;


		information.AddChild(
			title
		);


		_levelLabels[
			stage
		] =
			CreateFixedInfoLabel();


		information.AddChild(
			_levelLabels[
				stage
			]
		);


		_capacityLabels[
			stage
		] =
			CreateFixedInfoLabel();


		_capacityLabels[
			stage
		].Modulate =
			new Color(
				0.68f,
				0.80f,
				0.92f,
				1.0f
			);


		information.AddChild(
			_capacityLabels[
				stage
			]
		);


		_bufferLabels[
			stage
		] =
			CreateFixedInfoLabel();


		information.AddChild(
			_bufferLabels[
				stage
			]
		);


		_progressBars[
			stage
		] =
			CreateCycleProgressBar(
				GetStageColor(
					stage
				)
			);


		information.AddChild(
			_progressBars[
				stage
			]
		);


		_cycleLabels[
			stage
		] =
			CreateFixedInfoLabel();


		_cycleLabels[
			stage
		].HorizontalAlignment =
			HorizontalAlignment.Center;


		_cycleLabels[
			stage
		].AddThemeFontSizeOverride(
			"font_size",
			12
		);


		information.AddChild(
			_cycleLabels[
				stage
			]
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
			GetStageColor(
				stage
			)
		);


		button.Pressed +=
			() =>
			{
				if (NavigationActionBlocked())
					return;


				UpgradeStage(
					stage
				);
			};


		_upgradeButtons[
			stage
		] =
			button;


		upgradeRow.AddChild(
			button
		);
	}


	private static Label CreateFixedInfoLabel()
	{
		Label label =
			CreateLabel(
				13,
				""
			);


		label.CustomMinimumSize =
			new Vector2(
				0,
				22
			);


		label.HorizontalAlignment =
			HorizontalAlignment.Left;


		label.AutowrapMode =
			TextServer.AutowrapMode.Off;


		return label;
	}


	private static ProgressBar CreateCycleProgressBar(
		Color accent)
	{
		ProgressBar bar =
			new()
			{
				CustomMinimumSize =
					new Vector2(
						0,
						14
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


		StyleBoxFlat background =
			new()
			{
				BgColor =
					new Color(
						0.01f,
						0.025f,
						0.045f,
						0.95f
					),

				CornerRadiusTopLeft =
					7,

				CornerRadiusTopRight =
					7,

				CornerRadiusBottomLeft =
					7,

				CornerRadiusBottomRight =
					7
			};


		StyleBoxFlat fill =
			new()
			{
				BgColor =
					accent,

				CornerRadiusTopLeft =
					7,

				CornerRadiusTopRight =
					7,

				CornerRadiusBottomLeft =
					7,

				CornerRadiusBottomRight =
					7
			};


		bar.AddThemeStyleboxOverride(
			"background",
			background
		);


		bar.AddThemeStyleboxOverride(
			"fill",
			fill
		);


		return bar;
	}


	private static void CreateConnector(
		VBoxContainer parent)
	{
		CenterContainer center =
			new()
			{
				CustomMinimumSize =
					new Vector2(
						ContentWidth,
						28
					)
			};


		parent.AddChild(
			center
		);


		Label arrow =
			CreateLabel(
				26,
				"↓"
			);


		arrow.Modulate =
			new Color(
				0.30f,
				0.75f,
				1.0f,
				0.82f
			);


		center.AddChild(
			arrow
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
					"PipelineMobileScroll"
			};


		_root.AddChild(
			_mobileScroll
		);


		_mobileScroll.Setup(
			_pipelineScroll
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

	private void UpgradeStage(
		PipelineStage stage)
	{
		PipelineUpgradeResult result =
			_service.Upgrade(
				stage
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
				!= GameConfig.PipelineRoomIndex
			)
			{
				_showPipeline =
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
			!= GameConfig.PipelineRoomIndex
			|| !_showPipeline
		)
		{
			return;
		}


		Refresh();
	}


	private void ShowCorrectView()
	{
		bool serverRoom =
			_state.CurrentRoomIndex
			== GameConfig.PipelineRoomIndex;


		_navigationCenter.Visible =
			serverRoom;


		if (!serverRoom)
		{
			_machineScroll.Show();

			_pipelineScroll.Hide();

			return;
		}


		_machineScroll.Visible =
			!_showPipeline;


		_pipelineScroll.Visible =
			_showPipeline;
	}


	private void Refresh()
	{
		if (
			_state.RoomStates.Count
			<= GameConfig.PipelineRoomIndex
		)
		{
			return;
		}


		PipelineData pipeline =
			_service.GetPipeline();


		_machineInputLabel.Text =
			"MACHINE INPUT\n+"
			+ NumberFormatter.Format(
				_service
					.GetEstimatedMachineInputPerSecond()
			)
			+ " material/s";


		_tokenOutputLabel.Text =
			"TOKEN OUTPUT\n+"
			+ NumberFormatter.Format(
				_service
					.GetSteadyTokenOutputPerSecond(
						includeTemporaryShopBoost:
							true
					)
			)
			+ " /s";


		foreach (
			PipelineStage stage
			in StageOrder
		)
		{
			int level =
				_service.GetLevel(
					stage
				);


			double capacity =
				_service.GetCapacity(
					stage
				);


			double batchCapacity =
				_service.GetBatchCapacity(
					stage
				);


			double waiting =
				_service.GetBufferBeforeStage(
					stage
				);


			double duration =
				_service.GetCycleDuration(
					stage
				);


			double remaining =
				_service.GetCycleRemaining(
					stage
				);


			bool running =
				remaining > 0.0;


			_levelLabels[
				stage
			].Text =
				"LEVEL  "
				+ level;


			_capacityLabels[
				stage
			].Text =
				"BATCH  "
				+ NumberFormatter.Format(
					batchCapacity
				)
				+ "  •  "
				+ duration.ToString(
					"0.0"
				)
				+ "s";


			_bufferLabels[
				stage
			].Text =
				"WAITING  "
				+ NumberFormatter.Format(
					waiting
				)
				+ "  •  "
				+ NumberFormatter.Format(
					capacity
				)
				+ "/s";


			if (running)
			{
				double progress =
					(
						duration
						- remaining
					)
					/ duration
					* 100.0;


				_progressBars[
					stage
				].Value =
					Math.Clamp(
						progress,
						0.0,
						100.0
					);


				_cycleLabels[
					stage
				].Text =
					"PROCESSING  •  "
					+ Math.Max(
						0.0,
						remaining
					)
					.ToString(
						"0.0"
					)
					+ "s";
			}
			else
			{
				_progressBars[
					stage
				].Value =
					0.0;


				_cycleLabels[
					stage
				].Text =
					waiting > 0.0
						? "STARTING..."
						: "WAITING FOR INPUT";
			}


			Button button =
				_upgradeButtons[
					stage
				];



			double cost =
				_service.GetUpgradeCost(
					stage
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
	// STYLE
	// ==================================================

	private static StyleBoxFlat CreateTabBarBackgroundStyle()
	{
		return new StyleBoxFlat
		{
			BgColor =
				new Color(
					0.012f,
					0.030f,
					0.055f,
					0.96f
				),

			BorderColor =
				new Color(
					0.12f,
					0.46f,
					0.70f,
					0.65f
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


	private static StyleBoxFlat CreateSummaryStyle()
	{
		return new StyleBoxFlat
		{
			BgColor =
				new Color(
					0.025f,
					0.065f,
					0.115f,
					0.97f
				),

			BorderColor =
				new Color(
					0.18f,
					0.62f,
					1.0f,
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
					0.45f,
					1.0f,
					0.16f
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
					0.038f,
					0.070f,
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
					accent.R * 0.35f,
					accent.G * 0.35f,
					accent.B * 0.35f,
					0.98f
				),
				new Color(
					accent.R,
					accent.G,
					accent.B,
					0.85f
				)
			)
		);


		button.AddThemeStyleboxOverride(
			"hover",
			CreateButtonStyle(
				new Color(
					accent.R * 0.48f,
					accent.G * 0.48f,
					accent.B * 0.48f,
					1.0f
				),
				accent
			)
		);


		button.AddThemeStyleboxOverride(
			"pressed",
			CreateButtonStyle(
				new Color(
					accent.R * 0.25f,
					accent.G * 0.25f,
					accent.B * 0.25f,
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
					0.070f,
					0.92f
				),
				new Color(
					0.15f,
					0.20f,
					0.26f,
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


	private static Label CreateStatLabel()
	{
		Label label =
			CreateLabel(
				15,
				""
			);


		label.CustomMinimumSize =
			new Vector2(
				0,
				58
			);


		label.SizeFlagsHorizontal =
			Control.SizeFlags.ExpandFill;


		StyleBoxFlat style =
			new()
			{
				BgColor =
					new Color(
						0.01f,
						0.03f,
						0.055f,
						0.82f
					),

				CornerRadiusTopLeft =
					12,

				CornerRadiusTopRight =
					12,

				CornerRadiusBottomLeft =
					12,

				CornerRadiusBottomRight =
					12
			};


		label.AddThemeStyleboxOverride(
			"normal",
			style
		);


		return label;
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


	private static Texture2D GetStageIcon(
		PipelineStage stage)
	{
		return stage switch
		{
			PipelineStage.Compute =>
				ComputeIcon,

			PipelineStage.Data =>
				DataIcon,

			PipelineStage.Model =>
				ModelIcon,

			PipelineStage.Output =>
				OutputIcon,

			_ =>
				ComputeIcon
		};
	}


	private static Color GetStageColor(
		PipelineStage stage)
	{
		return stage switch
		{
			PipelineStage.Compute =>
				new Color(
					0.18f,
					0.62f,
					1.0f,
					1.0f
				),

			PipelineStage.Data =>
				new Color(
					0.12f,
					0.86f,
					0.90f,
					1.0f
				),

			PipelineStage.Model =>
				new Color(
					0.67f,
					0.40f,
					1.0f,
					1.0f
				),

			PipelineStage.Output =>
				new Color(
					0.22f,
					0.92f,
					0.55f,
					1.0f
				),

			_ =>
				Colors.White
		};
	}


	private static void PlayHaptic()
	{
		Input.VibrateHandheld(
			14,
			0.12f
		);
	}
}
