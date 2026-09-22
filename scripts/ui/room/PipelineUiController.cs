using Godot;
using System;
using System.Collections.Generic;

namespace IdleAi;

public sealed class PipelineUiController
{
	private static readonly PipelineStage[] StageOrder =
	[
		PipelineStage.Compute,
		PipelineStage.Data,
		PipelineStage.Model,
		PipelineStage.Output
	];

	private static readonly Texture2D ComputeIcon =
		GD.Load<Texture2D>("res://assets/pipeline/pipeline_compute.png");
	private static readonly Texture2D DataIcon =
		GD.Load<Texture2D>("res://assets/pipeline/pipeline_data.png");
	private static readonly Texture2D ModelIcon =
		GD.Load<Texture2D>("res://assets/pipeline/pipeline_model.png");
	private static readonly Texture2D OutputIcon =
		GD.Load<Texture2D>("res://assets/pipeline/pipeline_output.png");
	private static readonly Texture2D BottleneckIcon =
		GD.Load<Texture2D>("res://assets/pipeline/pipeline_bottleneck.png");
	private static readonly Texture2D UpgradeIcon =
		GD.Load<Texture2D>("res://assets/pipeline/pipeline_upgrade.png");

	private readonly Game _root;
	private readonly GameState _state;
	private readonly PipelineService _service;

	private VBoxContainer _roomVBox = null!;
	private ScrollContainer _machineScroll = null!;
	private HBoxContainer _navigation = null!;
	private Button _machinesButton = null!;
	private Button _pipelineButton = null!;
	private ScrollContainer _pipelineScroll = null!;
	private MobileScrollController _mobileScroll = null!;

	private Label _efficiencyLabel = null!;
	private Label _multiplierLabel = null!;
	private Label _bottleneckLabel = null!;

	private readonly Dictionary<PipelineStage, Label> _levelLabels = [];
	private readonly Dictionary<PipelineStage, Label> _capacityLabels = [];
	private readonly Dictionary<PipelineStage, PanelContainer> _cards = [];
	private readonly Dictionary<PipelineStage, TextureRect> _bottleneckIcons = [];
	private readonly Dictionary<PipelineStage, Button> _upgradeButtons = [];

	private bool _showPipeline;
	private int _lastRoom = -1;

	public event Action<string>? MessageRequested;
	public event Action? StateChanged;

	public PipelineUiController(
		Game root,
		GameState state,
		PipelineService service)
	{
		_root = root;
		_state = state;
		_service = service;
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
		CreatePipelineView();
		CreateMobileScrolling();
		UpdateAll();
	}

	private void CreateNavigation()
	{
		_navigation = new HBoxContainer
		{
			Name = "PipelineNavigation",
			CustomMinimumSize = new Vector2(0, 48),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			MouseFilter = Control.MouseFilterEnum.Pass
		};

		_navigation.AddThemeConstantOverride("separation", 8);

		_roomVBox.AddChild(_navigation);
		_roomVBox.MoveChild(_navigation, 0);

		_machinesButton = CreateNavigationButton("MACHINES");
		_pipelineButton = CreateNavigationButton("PIPELINE");

		_navigation.AddChild(_machinesButton);
		_navigation.AddChild(_pipelineButton);

		_machinesButton.Pressed += () =>
		{
			if (NavigationActionBlocked())
				return;

			_showPipeline = false;
			ShowCorrectView();
			PlayHaptic();
		};

		_pipelineButton.Pressed += () =>
		{
			if (NavigationActionBlocked())
				return;

			_showPipeline = true;
			ShowCorrectView();
			_mobileScroll?.ScrollToTop();
			PlayHaptic();
		};
	}

	private static Button CreateNavigationButton(string text)
	{
		Button button = new()
		{
			Text = text,
			CustomMinimumSize = new Vector2(0, 44),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			FocusMode = Control.FocusModeEnum.None
		};

		button.AddThemeFontSizeOverride("font_size", 15);
		return button;
	}

	private void CreatePipelineView()
	{
		_pipelineScroll = new ScrollContainer
		{
			Name = "PipelineScroll",
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			SizeFlagsVertical = Control.SizeFlags.ExpandFill,
			HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
			VerticalScrollMode = ScrollContainer.ScrollMode.ShowNever,
			ClipContents = true,
			MouseFilter = Control.MouseFilterEnum.Pass
		};

		_roomVBox.AddChild(_pipelineScroll);

		MarginContainer margin = new();
		margin.AddThemeConstantOverride("margin_left", 6);
		margin.AddThemeConstantOverride("margin_top", 14);
		margin.AddThemeConstantOverride("margin_right", 6);
		margin.AddThemeConstantOverride("margin_bottom", 70);
		_pipelineScroll.AddChild(margin);

		VBoxContainer content = new()
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		content.AddThemeConstantOverride("separation", 12);
		margin.AddChild(content);

		CreateSummaryCard(content);

		for (int i = 0; i < StageOrder.Length; i++)
		{
			CreateStageCard(content, StageOrder[i]);

			if (i < StageOrder.Length - 1)
				CreateConnector(content);
		}
	}

	private void CreateSummaryCard(VBoxContainer parent)
	{
		PanelContainer panel = new()
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};

		panel.AddThemeStyleboxOverride(
			"panel",
			CreatePanelStyle(new Color(0.18f, 0.60f, 1.0f, 1.0f))
		);

		parent.AddChild(panel);

		MarginContainer margin = CreateCardMargin(18, 14);
		panel.AddChild(margin);

		VBoxContainer box = new();
		box.AddThemeConstantOverride("separation", 6);
		margin.AddChild(box);

		Label title = CreateLabel(24, "SERVER PIPELINE");
		title.HorizontalAlignment = HorizontalAlignment.Center;
		box.AddChild(title);

		Label subtitle =
			CreateLabel(
				13,
				"Keep all four stages balanced to maximize production."
			);
		subtitle.Modulate =
			new Color(0.70f, 0.82f, 0.92f, 1.0f);
		subtitle.HorizontalAlignment = HorizontalAlignment.Center;
		box.AddChild(subtitle);

		HBoxContainer stats = new()
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		stats.AddThemeConstantOverride("separation", 8);
		box.AddChild(stats);

		_efficiencyLabel = CreateStatLabel();
		_multiplierLabel = CreateStatLabel();

		stats.AddChild(_efficiencyLabel);
		stats.AddChild(_multiplierLabel);

		_bottleneckLabel = CreateLabel(14, "BOTTLENECK");
		_bottleneckLabel.HorizontalAlignment =
			HorizontalAlignment.Center;
		box.AddChild(_bottleneckLabel);
	}

	private void CreateStageCard(
		VBoxContainer parent,
		PipelineStage stage)
	{
		PanelContainer panel = new()
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			MouseFilter = Control.MouseFilterEnum.Pass
		};

		_cards[stage] = panel;
		parent.AddChild(panel);

		MarginContainer margin = CreateCardMargin(14, 14);
		panel.AddChild(margin);

		HBoxContainer row = new()
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			MouseFilter = Control.MouseFilterEnum.Pass
		};
		row.AddThemeConstantOverride("separation", 14);
		margin.AddChild(row);

		row.AddChild(
			CreateIcon(
				GetStageIcon(stage),
				92
			)
		);

		VBoxContainer information = new()
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			MouseFilter = Control.MouseFilterEnum.Pass
		};
		information.AddThemeConstantOverride("separation", 5);
		row.AddChild(information);

		HBoxContainer titleRow = new()
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		information.AddChild(titleRow);

		Label title =
			CreateLabel(
				18,
				PipelineService.GetStageName(stage)
			);
		title.HorizontalAlignment = HorizontalAlignment.Left;
		title.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		titleRow.AddChild(title);

		TextureRect warningIcon =
			CreateIcon(
				BottleneckIcon,
				30
			);
		_bottleneckIcons[stage] = warningIcon;
		titleRow.AddChild(warningIcon);

		_levelLabels[stage] = CreateLabel(14, "LEVEL 1");
		_levelLabels[stage].HorizontalAlignment =
			HorizontalAlignment.Left;
		information.AddChild(_levelLabels[stage]);

		_capacityLabels[stage] =
			CreateLabel(
				13,
				"CAPACITY 100 /s"
			);
		_capacityLabels[stage].HorizontalAlignment =
			HorizontalAlignment.Left;
		_capacityLabels[stage].Modulate =
			new Color(0.68f, 0.80f, 0.92f, 1.0f);
		information.AddChild(_capacityLabels[stage]);

		HBoxContainer upgradeRow = new()
		{
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
		};
		upgradeRow.AddThemeConstantOverride("separation", 8);
		information.AddChild(upgradeRow);

		upgradeRow.AddChild(
			CreateIcon(
				UpgradeIcon,
				34
			)
		);

		Button button = new()
		{
			CustomMinimumSize = new Vector2(0, 50),
			SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
			FocusMode = Control.FocusModeEnum.None
		};
		button.AddThemeFontSizeOverride("font_size", 14);

		button.Pressed += () =>
		{
			if (NavigationActionBlocked())
				return;

			UpgradeStage(stage);
		};

		_upgradeButtons[stage] = button;
		upgradeRow.AddChild(button);
	}

	private static void CreateConnector(VBoxContainer parent)
	{
		Label arrow = CreateLabel(25, "↓");
		arrow.CustomMinimumSize = new Vector2(0, 24);
		arrow.Modulate = new Color(0.30f, 0.75f, 1.0f, 0.85f);
		parent.AddChild(arrow);
	}

	private void CreateMobileScrolling()
	{
		_mobileScroll = new MobileScrollController
		{
			Name = "PipelineMobileScroll"
		};

		_root.AddChild(_mobileScroll);
		_mobileScroll.Setup(_pipelineScroll);
	}

	private bool NavigationActionBlocked()
	{
		return _mobileScroll != null
			&& _mobileScroll.ShouldSuppressTap;
	}

	private void UpgradeStage(PipelineStage stage)
	{
		PipelineUpgradeResult result =
			_service.Upgrade(stage);

		MessageRequested?.Invoke(result.Message);
		Refresh();

		if (!result.Changed)
			return;

		PlayHaptic();
		StateChanged?.Invoke();
	}

	public void UpdateAll()
	{
		int currentRoom = _state.CurrentRoomIndex;

		if (currentRoom != _lastRoom)
		{
			_lastRoom = currentRoom;

			if (currentRoom != GameConfig.PipelineRoomIndex)
				_showPipeline = false;
		}

		ShowCorrectView();
		Refresh();
	}

	public void UpdateRuntime()
	{
		if (
			_state.CurrentRoomIndex != GameConfig.PipelineRoomIndex
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

		_navigation.Visible = serverRoom;

		if (!serverRoom)
		{
			_machineScroll.Show();
			_pipelineScroll.Hide();
			return;
		}

		_machineScroll.Visible = !_showPipeline;
		_pipelineScroll.Visible = _showPipeline;

		_machinesButton.Disabled = !_showPipeline;
		_pipelineButton.Disabled = _showPipeline;
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

		PipelineStage bottleneck =
			_service.GetBottleneck();

		double efficiency =
			_service.GetEfficiency();

		double multiplier =
			_service.GetProductionMultiplier();

		_efficiencyLabel.Text =
			"EFFICIENCY\n"
			+ (efficiency * 100.0).ToString("0")
			+ "%";

		_multiplierLabel.Text =
			"ROOM BOOST\nx"
			+ multiplier.ToString("0.00");

		_bottleneckLabel.Text =
			"BOTTLENECK: "
			+ PipelineService.GetStageShortName(bottleneck);

		foreach (PipelineStage stage in StageOrder)
		{
			int level = _service.GetLevel(stage);
			double capacity = _service.GetCapacity(stage);
			bool isBottleneck = stage == bottleneck;

			_levelLabels[stage].Text =
				"LEVEL "
				+ level
				+ " / "
				+ GameConfig.PipelineMaxLevel;

			_capacityLabels[stage].Text =
				"CAPACITY  "
				+ NumberFormatter.Format(capacity)
				+ " /s";

			_bottleneckIcons[stage].Visible = isBottleneck;

			_cards[stage].AddThemeStyleboxOverride(
				"panel",
				CreatePanelStyle(
					isBottleneck
						? new Color(1.0f, 0.45f, 0.16f, 1.0f)
						: GetStageColor(stage)
				)
			);

			Button button = _upgradeButtons[stage];

			if (level >= GameConfig.PipelineMaxLevel)
			{
				button.Text = "MAX LEVEL";
				button.Disabled = true;
				continue;
			}

			double cost = _service.GetUpgradeCost(stage);

			button.Text =
				"UPGRADE  •  "
				+ NumberFormatter.Format(cost);

			button.Disabled =
				_state.Tokens < cost;
		}
	}

	private static StyleBoxFlat CreatePanelStyle(Color accent)
	{
		return new StyleBoxFlat
		{
			BgColor =
				new Color(0.02f, 0.04f, 0.075f, 0.95f),
			BorderColor =
				new Color(accent.R, accent.G, accent.B, 0.72f),
			BorderWidthLeft = 2,
			BorderWidthTop = 2,
			BorderWidthRight = 2,
			BorderWidthBottom = 2,
			CornerRadiusTopLeft = 16,
			CornerRadiusTopRight = 16,
			CornerRadiusBottomLeft = 16,
			CornerRadiusBottomRight = 16,
			ShadowColor = new Color(0, 0, 0, 0.30f),
			ShadowSize = 8
		};
	}

	private static MarginContainer CreateCardMargin(
		int horizontal,
		int vertical)
	{
		MarginContainer margin = new();
		margin.AddThemeConstantOverride("margin_left", horizontal);
		margin.AddThemeConstantOverride("margin_right", horizontal);
		margin.AddThemeConstantOverride("margin_top", vertical);
		margin.AddThemeConstantOverride("margin_bottom", vertical);
		return margin;
	}

	private static Label CreateLabel(
		int size,
		string text)
	{
		Label label = new()
		{
			Text = text,
			AutowrapMode = TextServer.AutowrapMode.WordSmart,
			VerticalAlignment = VerticalAlignment.Center,
			HorizontalAlignment = HorizontalAlignment.Center,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
		label.AddThemeFontSizeOverride("font_size", size);
		return label;
	}

	private static Label CreateStatLabel()
	{
		Label label = CreateLabel(15, "");
		label.CustomMinimumSize = new Vector2(0, 58);
		label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
		return label;
	}

	private static TextureRect CreateIcon(
		Texture2D texture,
		float size)
	{
		return new TextureRect
		{
			Texture = texture,
			CustomMinimumSize = new Vector2(size, size),
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
			MouseFilter = Control.MouseFilterEnum.Ignore
		};
	}

	private static Texture2D GetStageIcon(PipelineStage stage)
	{
		return stage switch
		{
			PipelineStage.Compute => ComputeIcon,
			PipelineStage.Data => DataIcon,
			PipelineStage.Model => ModelIcon,
			PipelineStage.Output => OutputIcon,
			_ => ComputeIcon
		};
	}

	private static Color GetStageColor(PipelineStage stage)
	{
		return stage switch
		{
			PipelineStage.Compute =>
				new Color(0.18f, 0.62f, 1.0f, 1.0f),
			PipelineStage.Data =>
				new Color(0.12f, 0.86f, 0.90f, 1.0f),
			PipelineStage.Model =>
				new Color(0.67f, 0.40f, 1.0f, 1.0f),
			PipelineStage.Output =>
				new Color(0.22f, 0.92f, 0.55f, 1.0f),
			_ => Colors.White
		};
	}

	private static void PlayHaptic()
	{
		Input.VibrateHandheld(14, 0.12f);
	}
}
