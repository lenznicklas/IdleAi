using Godot;
using System;
using System.Collections.Generic;

namespace IdleAi;


/*
 * First playable Singularity draft.
 *
 * - Permanent unlock at 50aa Tokens for testing.
 * - Infinite sectors (8 nodes each).
 * - Compute / Amplifier / Cooling / Quantum nodes.
 * - Adjacency bonuses.
 * - Core upgrades.
 * - Separate Singularity Matter economy.
 * - Lines are rendered directly with DrawLine; no line PNG is used.
 */
public sealed partial class SingularityController
{
	private const float BottomReservedSpace =
		132.0f;

	private static readonly Color Accent =
		new(
			0.92f,
			0.66f,
			0.20f,
			1.0f
		);

	private static readonly Color ComputeColor =
		new(
			0.32f,
			0.72f,
			0.96f,
			1.0f
		);

	private static readonly Color AmplifierColor =
		new(
			0.98f,
			0.60f,
			0.20f,
			1.0f
		);

	private static readonly Color CoolingColor =
		new(
			0.30f,
			0.92f,
			0.88f,
			1.0f
		);

	private static readonly Color QuantumColor =
		new(
			0.78f,
			0.36f,
			0.96f,
			1.0f
		);

	private static readonly Texture2D CoreTexture =
		GD.Load<Texture2D>(
			"res://assets/singularity/core.png"
		);

	private static readonly Texture2D EmptyNodeTexture =
		GD.Load<Texture2D>(
			"res://assets/singularity/node_empty.png"
		);

	private static readonly Texture2D ComputeNodeTexture =
		GD.Load<Texture2D>(
			"res://assets/singularity/node_compute.png"
		);

	private static readonly Texture2D AmplifierNodeTexture =
		GD.Load<Texture2D>(
			"res://assets/singularity/node_amplifier.png"
		);

	private static readonly Texture2D CoolingNodeTexture =
		GD.Load<Texture2D>(
			"res://assets/singularity/node_cooling.png"
		);

	private static readonly Texture2D QuantumNodeTexture =
		GD.Load<Texture2D>(
			"res://assets/singularity/node_quantum.png"
		);

	private static readonly Texture2D MatterIcon =
		GD.Load<Texture2D>(
			"res://assets/ui/singularity_matter.png"
		);

	private static readonly Texture2D CoreLevelIcon =
		GD.Load<Texture2D>(
			"res://assets/ui/core_level.png"
		);

	private static readonly Texture2D SectorIcon =
		GD.Load<Texture2D>(
			"res://assets/ui/sector.png"
		);

	private readonly Game _root;

	private readonly SingularityService _service;

	private Control _page =
		null!;

	private PanelContainer _header =
		null!;

	private Label _matterValue =
		null!;

	private Label _coreValue =
		null!;

	private Label _sectorValue =
		null!;

	private Label _outputValue =
		null!;

	private ScrollContainer _scroll =
		null!;

	private VBoxContainer _content =
		null!;

	private Control _network =
		null!;

	private SingularityLinkLayer _linkLayer =
		null!;

	private Button _coreButton =
		null!;

	private readonly List<NodeButtonView>
		_nodeViews =
			[];

	private Label _sectorTitle =
		null!;

	private Label _sectorOutput =
		null!;

	private Button _previousSectorButton =
		null!;

	private Button _nextSectorButton =
		null!;

	private Label _selectionTitle =
		null!;

	private Label _selectionDetails =
		null!;

	private HBoxContainer _buildButtons =
		null!;

	private Button _upgradeNodeButton =
		null!;

	private Button _upgradeCoreButton =
		null!;

	private Label _message =
		null!;

	private Timer _runtimeTimer =
		null!;

	private Timer _saveTimer =
		null!;

	private int _selectedNode =
		-1;


	public event Action<string>? MessageRequested;


	public bool Visible =>
		_page != null
		&& _page.Visible;


	public SingularityController(
		Game root,
		SingularityService service)
	{
		_root =
			root;

		_service =
			service;
	}


	public void Initialize()
	{
		CreatePage();
		CreateTimers();

		_service.Changed +=
			RefreshAll;

		Hide();
	}


	public void Open()
	{
		if (!_service.Unlocked)
			return;

		_selectedNode =
			-1;

		RefreshAll();

		_page.Show();
		_page.MoveToFront();

		ApplySafeArea();
	}


	public void Hide()
	{
		_page?.Hide();
	}


	private void CreateTimers()
	{
		_runtimeTimer =
			new Timer
			{
				Name =
					"SingularityRuntimeTimer",

				WaitTime =
					0.20,

				OneShot =
					false,

				Autostart =
					true
			};

		_runtimeTimer.Timeout +=
			() =>
			{
				_service.Update(
					_runtimeTimer.WaitTime
				);

				if (Visible)
				{
					RefreshRuntime();
				}
			};

		_root.AddChild(
			_runtimeTimer
		);

		_saveTimer =
			new Timer
			{
				Name =
					"SingularityAutosaveTimer",

				WaitTime =
					10.0,

				OneShot =
					false,

				Autostart =
					true
			};

		_saveTimer.Timeout +=
			_service.Save;

		_root.AddChild(
			_saveTimer
		);
	}


	private void CreatePage()
	{
		_page =
			new Control
			{
				Name =
					"SingularityPage",

				MouseFilter =
					Control.MouseFilterEnum.Ignore,

				ZIndex =
					600
			};

		_page.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);

		_root.AddChild(
			_page
		);

		ColorRect background =
			new()
			{
				Color =
					new Color(
						0.008f,
						0.010f,
						0.018f,
						1.0f
					),

				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};

		background.AnchorRight =
			1.0f;

		background.AnchorBottom =
			1.0f;

		background.OffsetBottom =
			-BottomReservedSpace;

		_page.AddChild(
			background
		);

		SingularityGridBackground grid =
			new()
			{
				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};

		grid.AnchorRight =
			1.0f;

		grid.AnchorBottom =
			1.0f;

		grid.OffsetBottom =
			-BottomReservedSpace;

		_page.AddChild(
			grid
		);

		CreateHeader();
		CreateScrollContent();

		_page.Resized +=
			ApplySafeArea;
	}


	private void CreateHeader()
	{
		_header =
			new PanelContainer
			{
				Name =
					"SingularityHeader",

				CustomMinimumSize =
					new Vector2(
						0,
						142
					),

				MouseFilter =
					Control.MouseFilterEnum.Stop
			};

		_header.AnchorRight =
			1.0f;

		_header.OffsetLeft =
			18.0f;

		_header.OffsetTop =
			14.0f;

		_header.OffsetRight =
			-18.0f;

		_header.AddThemeStyleboxOverride(
			"panel",
			CreatePanelStyle(
				new Color(
					0.040f,
					0.028f,
					0.012f,
					0.98f
				),
				Accent,
				20
			)
		);

		_page.AddChild(
			_header
		);

		MarginContainer margin =
			new();

		margin.AddThemeConstantOverride(
			"margin_left",
			18
		);

		margin.AddThemeConstantOverride(
			"margin_top",
			10
		);

		margin.AddThemeConstantOverride(
			"margin_right",
			18
		);

		margin.AddThemeConstantOverride(
			"margin_bottom",
			10
		);

		_header.AddChild(
			margin
		);

		VBoxContainer box =
			new();

		box.AddThemeConstantOverride(
			"separation",
			7
		);

		margin.AddChild(
			box
		);

		HBoxContainer top =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};

		top.AddThemeConstantOverride(
			"separation",
			8
		);

		box.AddChild(
			top
		);

		Button mapButton =
			new()
			{
				Text =
					"‹ MAP",

				CustomMinimumSize =
					new Vector2(
						90,
						42
					),

				FocusMode =
					Control.FocusModeEnum.None
			};

		mapButton.Pressed +=
			() =>
			{
				Hide();

				Control map =
					_root.GetNode<Control>(
						"MapPage"
					);

				map.Show();
				map.MoveToFront();
			};

		top.AddChild(
			mapButton
		);

		Label title =
			CreateLabel(
				26,
				"THE SINGULARITY"
			);

		title.HorizontalAlignment =
			HorizontalAlignment.Left;

		title.SizeFlagsHorizontal =
			Control.SizeFlags.ExpandFill;

		top.AddChild(
			title
		);

		Label output =
			CreateLabel(
				13,
				"OUTPUT"
			);

		output.Modulate =
			Accent.Lightened(
				0.16f
			);

		top.AddChild(
			output
		);

		_outputValue =
			CreateLabel(
				16,
				"0/s"
			);

		_outputValue.CustomMinimumSize =
			new Vector2(
				130,
				0
			);

		_outputValue.HorizontalAlignment =
			HorizontalAlignment.Right;

		top.AddChild(
			_outputValue
		);

		HBoxContainer stats =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};

		stats.AddThemeConstantOverride(
			"separation",
			8
		);

		box.AddChild(
			stats
		);

		stats.AddChild(
			CreateStatCard(
				MatterIcon,
				"MATTER",
				out _matterValue
			)
		);

		stats.AddChild(
			CreateStatCard(
				CoreLevelIcon,
				"CORE",
				out _coreValue
			)
		);

		stats.AddChild(
			CreateStatCard(
				SectorIcon,
				"SECTOR",
				out _sectorValue
			)
		);
	}


	private Control CreateStatCard(
		Texture2D texture,
		string label,
		out Label value)
	{
		PanelContainer panel =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,

				CustomMinimumSize =
					new Vector2(
						0,
						64
					)
			};

		panel.AddThemeStyleboxOverride(
			"panel",
			CreatePanelStyle(
				new Color(
					0.025f,
					0.026f,
					0.034f,
					0.98f
				),
				new Color(
					0.28f,
					0.25f,
					0.18f,
					0.92f
				),
				12
			)
		);

		HBoxContainer row =
			new()
			{
				Alignment =
					BoxContainer.AlignmentMode.Center
			};

		row.AddThemeConstantOverride(
			"separation",
			6
		);

		panel.AddChild(
			row
		);

		TextureRect icon =
			new()
			{
				Texture = texture,

				CustomMinimumSize =
					new Vector2(
						36,
						36
					),

				ExpandMode =
					TextureRect.ExpandModeEnum.IgnoreSize,

				StretchMode =
					TextureRect.StretchModeEnum.KeepAspectCentered,

				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};

		row.AddChild(
			icon
		);

		VBoxContainer text =
			new();

		row.AddChild(
			text
		);

		Label name =
			CreateLabel(
				9,
				label
			);

		name.Modulate =
			new Color(
				0.72f,
				0.72f,
				0.74f,
				1.0f
			);

		text.AddChild(
			name
		);

		value =
			CreateLabel(
				15,
				"0"
			);

		text.AddChild(
			value
		);

		return panel;
	}


	private void CreateScrollContent()
	{
		_scroll =
			new ScrollContainer
			{
				Name =
					"SingularityScroll",

				HorizontalScrollMode =
					ScrollContainer.ScrollMode.Disabled,

				VerticalScrollMode =
					ScrollContainer.ScrollMode.ShowNever,

				MouseFilter =
					Control.MouseFilterEnum.Pass
			};

		_scroll.AnchorRight =
			1.0f;

		_scroll.AnchorBottom =
			1.0f;

		_scroll.OffsetTop =
			166.0f;

		_scroll.OffsetBottom =
			-BottomReservedSpace;

		_page.AddChild(
			_scroll
		);

		CenterContainer center =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,

				SizeFlagsVertical =
					Control.SizeFlags.ShrinkBegin
			};

		_scroll.AddChild(
			center
		);

		_content =
			new VBoxContainer
			{
				CustomMinimumSize =
					new Vector2(
						650,
						0
					),

				SizeFlagsHorizontal =
					Control.SizeFlags.ShrinkCenter
			};

		_content.AddThemeConstantOverride(
			"separation",
			12
		);

		center.AddChild(
			_content
		);

		CreateSectorNavigation();
		CreateNetwork();
		CreateCoreUpgradePanel();
		CreateSelectionPanel();
		CreateMessagePanel();
	}


	private void CreateSectorNavigation()
	{
		PanelContainer panel =
			new();

		panel.AddThemeStyleboxOverride(
			"panel",
			CreatePanelStyle(
				new Color(
					0.018f,
					0.020f,
					0.028f,
					0.96f
				),
				new Color(
					0.24f,
					0.22f,
					0.18f,
					0.76f
				),
				14
			)
		);

		_content.AddChild(
			panel
		);

		HBoxContainer row =
			new()
			{
				CustomMinimumSize =
					new Vector2(
						0,
						72
					)
			};

		panel.AddChild(
			row
		);

		_previousSectorButton =
			new Button
			{
				Text =
					"‹",

				CustomMinimumSize =
					new Vector2(
						72,
						60
					),

				FocusMode =
					Control.FocusModeEnum.None
			};

		_previousSectorButton.AddThemeFontSizeOverride(
			"font_size",
			28
		);

		_previousSectorButton.Pressed +=
			() =>
			{
				if (
					_service.GoToPreviousSector()
				)
				{
					_selectedNode =
						-1;

					RefreshAll();
				}
			};

		row.AddChild(
			_previousSectorButton
		);

		VBoxContainer center =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};

		row.AddChild(
			center
		);

		_sectorTitle =
			CreateLabel(
				20,
				"SECTOR 1"
			);

		center.AddChild(
			_sectorTitle
		);

		_sectorOutput =
			CreateLabel(
				12,
				"0 Matter/s"
			);

		_sectorOutput.Modulate =
			Accent.Lightened(
				0.12f
			);

		center.AddChild(
			_sectorOutput
		);

		_nextSectorButton =
			new Button
			{
				Text =
					"›",

				CustomMinimumSize =
					new Vector2(
						72,
						60
					),

				FocusMode =
					Control.FocusModeEnum.None
			};

		_nextSectorButton.AddThemeFontSizeOverride(
			"font_size",
			28
		);

		_nextSectorButton.Pressed +=
			OnNextSectorPressed;

		row.AddChild(
			_nextSectorButton
		);
	}


	private void CreateNetwork()
	{
		PanelContainer frame =
			new()
			{
				CustomMinimumSize =
					new Vector2(
						650,
						650
					)
			};

		frame.AddThemeStyleboxOverride(
			"panel",
			CreatePanelStyle(
				new Color(
					0.010f,
					0.012f,
					0.020f,
					0.98f
				),
				new Color(
					0.20f,
					0.18f,
					0.14f,
					0.82f
				),
				24
			)
		);

		_content.AddChild(
			frame
		);

		_network =
			new Control
			{
				CustomMinimumSize =
					new Vector2(
						650,
						650
					)
			};

		frame.AddChild(
			_network
		);

		_linkLayer =
			new SingularityLinkLayer
			{
				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};

		_linkLayer.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);

		_network.AddChild(
			_linkLayer
		);

		_coreButton =
			new Button
			{
				Position =
					new Vector2(
						245,
						245
					),

				Size =
					new Vector2(
						160,
						160
					),

				FocusMode =
					Control.FocusModeEnum.None,

				TooltipText =
					"Singularity Core",

				ClipContents =
					true
			};

		_coreButton.AddThemeStyleboxOverride(
			"normal",
			CreateNodeStyle(
				new Color(
					0.06f,
					0.04f,
					0.015f,
					0.92f
				),
				Accent,
				false
			)
		);

		_coreButton.Pressed +=
			() =>
			{
				_selectedNode =
					-1;

				RefreshSelection();
			};

		_network.AddChild(
			_coreButton
		);

		TextureRect coreImage =
			new()
			{
				Texture =
					CoreTexture,

				Position =
					new Vector2(
						8,
						8
					),

				Size =
					new Vector2(
						144,
						144
					),

				ExpandMode =
					TextureRect.ExpandModeEnum.IgnoreSize,

				StretchMode =
					TextureRect.StretchModeEnum.KeepAspectCentered,

				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};

		_coreButton.AddChild(
			coreImage
		);

		for (
			int i = 0;
			i < SingularityService.NodesPerSector;
			i++
		)
		{
			int index =
				i;

			Vector2 center =
				GetNodeCenter(
					i
				);

			Button button =
				new()
				{
					Position =
						center
						- new Vector2(
							58,
							58
						),

					Size =
						new Vector2(
							116,
							116
						),

					CustomMinimumSize =
						new Vector2(
							116,
							116
						),

					FocusMode =
						Control.FocusModeEnum.None,

					ClipContents =
						true
				};

			button.Pressed +=
				() =>
				{
					_selectedNode =
						index;

					RefreshSelection();
				};

			_network.AddChild(
				button
			);

			TextureRect icon =
				new()
				{
					Position =
						new Vector2(
							10,
							8
						),

					Size =
						new Vector2(
							96,
							82
						),

					ExpandMode =
						TextureRect.ExpandModeEnum.IgnoreSize,

					StretchMode =
						TextureRect.StretchModeEnum.KeepAspectCentered,

					MouseFilter =
						Control.MouseFilterEnum.Ignore
				};

			button.AddChild(
				icon
			);

			Label level =
				CreateLabel(
					11,
					""
				);

			level.Position =
				new Vector2(
					6,
					89
				);

			level.Size =
				new Vector2(
					104,
					22
				);

			level.MouseFilter =
				Control.MouseFilterEnum.Ignore;

			button.AddChild(
				level
			);

			_nodeViews.Add(
				new NodeButtonView(
					button,
					icon,
					level
				)
			);
		}

		_linkLayer.SetCenters(
			new Vector2(
				325,
				325
			),
			GetAllNodeCenters()
		);
	}


	private void CreateCoreUpgradePanel()
	{
		PanelContainer panel =
			new();

		panel.AddThemeStyleboxOverride(
			"panel",
			CreatePanelStyle(
				new Color(
					0.040f,
					0.030f,
					0.015f,
					0.98f
				),
				Accent,
				16
			)
		);

		_content.AddChild(
			panel
		);

		MarginContainer margin =
			new();

		margin.AddThemeConstantOverride(
			"margin_left",
			16
		);

		margin.AddThemeConstantOverride(
			"margin_top",
			12
		);

		margin.AddThemeConstantOverride(
			"margin_right",
			16
		);

		margin.AddThemeConstantOverride(
			"margin_bottom",
			12
		);

		panel.AddChild(
			margin
		);

		HBoxContainer row =
			new();

		margin.AddChild(
			row
		);

		VBoxContainer text =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};

		row.AddChild(
			text
		);

		Label title =
			CreateLabel(
				17,
				"SINGULARITY CORE"
			);

		title.HorizontalAlignment =
			HorizontalAlignment.Left;

		text.AddChild(
			title
		);

		Label description =
			CreateLabel(
				11,
				"Core levels boost every Compute Node and generate Matter."
			);

		description.HorizontalAlignment =
			HorizontalAlignment.Left;

		description.AutowrapMode =
			TextServer.AutowrapMode.WordSmart;

		text.AddChild(
			description
		);

		_upgradeCoreButton =
			new Button
			{
				CustomMinimumSize =
					new Vector2(
						190,
						62
					),

				FocusMode =
					Control.FocusModeEnum.None
			};

		_upgradeCoreButton.Pressed +=
			() =>
			{
				HandleResult(
					_service.UpgradeCore()
				);
			};

		row.AddChild(
			_upgradeCoreButton
		);
	}


	private void CreateSelectionPanel()
	{
		PanelContainer panel =
			new();

		panel.AddThemeStyleboxOverride(
			"panel",
			CreatePanelStyle(
				new Color(
					0.018f,
					0.020f,
					0.030f,
					0.98f
				),
				new Color(
					0.24f,
					0.28f,
					0.34f,
					0.86f
				),
				16
			)
		);

		_content.AddChild(
			panel
		);

		MarginContainer margin =
			new();

		margin.AddThemeConstantOverride(
			"margin_left",
			16
		);

		margin.AddThemeConstantOverride(
			"margin_top",
			14
		);

		margin.AddThemeConstantOverride(
			"margin_right",
			16
		);

		margin.AddThemeConstantOverride(
			"margin_bottom",
			14
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

		_selectionTitle =
			CreateLabel(
				18,
				"SELECT A NODE"
			);

		box.AddChild(
			_selectionTitle
		);

		_selectionDetails =
			CreateLabel(
				12,
				"Tap a node to build or upgrade it."
			);

		_selectionDetails.AutowrapMode =
			TextServer.AutowrapMode.WordSmart;

		box.AddChild(
			_selectionDetails
		);

		_buildButtons =
			new HBoxContainer
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};

		_buildButtons.AddThemeConstantOverride(
			"separation",
			6
		);

		box.AddChild(
			_buildButtons
		);

		AddBuildButton(
			SingularityNodeType.Compute,
			"CMP"
		);

		AddBuildButton(
			SingularityNodeType.Amplifier,
			"AMP"
		);

		AddBuildButton(
			SingularityNodeType.Cooling,
			"CLG"
		);

		AddBuildButton(
			SingularityNodeType.Quantum,
			"QNT"
		);

		_upgradeNodeButton =
			new Button
			{
				CustomMinimumSize =
					new Vector2(
						0,
						58
					),

				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,

				FocusMode =
					Control.FocusModeEnum.None
			};

		_upgradeNodeButton.Pressed +=
			OnUpgradeSelectedNode;

		box.AddChild(
			_upgradeNodeButton
		);
	}


	private void AddBuildButton(
		SingularityNodeType type,
		string shortName)
	{
		Button button =
			new()
			{
				Name =
					type.ToString(),

				Text =
					shortName,

				CustomMinimumSize =
					new Vector2(
						0,
						54
					),

				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,

				FocusMode =
					Control.FocusModeEnum.None
			};

		button.Pressed +=
			() =>
			{
				if (_selectedNode < 0)
					return;

				HandleResult(
					_service.BuildNode(
						_service.CurrentSectorIndex,
						_selectedNode,
						type
					)
				);
			};

		_buildButtons.AddChild(
			button
		);
	}


	private void CreateMessagePanel()
	{
		_message =
			CreateLabel(
				12,
				"Compute Nodes create Matter. Amplifier/Cooling buff neighboring nodes. Quantum buffs the whole sector."
			);

		_message.AutowrapMode =
			TextServer.AutowrapMode.WordSmart;

		_message.CustomMinimumSize =
			new Vector2(
				0,
				70
			);

		_message.Modulate =
			new Color(
				0.74f,
				0.78f,
				0.84f,
				1.0f
			);

		_content.AddChild(
			_message
		);
	}


	private void OnNextSectorPressed()
	{
		if (
			_service.GoToNextExistingSector()
		)
		{
			_selectedNode =
				-1;

			RefreshAll();

			return;
		}

		HandleResult(
			_service.UnlockNextSector()
		);
	}


	private void OnUpgradeSelectedNode()
	{
		if (_selectedNode < 0)
			return;

		HandleResult(
			_service.UpgradeNode(
				_service.CurrentSectorIndex,
				_selectedNode
			)
		);
	}


	private void HandleResult(
		SingularityActionResult result)
	{
		_message.Text =
			result.Message;

		MessageRequested?.Invoke(
			result.Message
		);

		if (result.Changed)
		{
			RefreshAll();
		}
	}


	private void RefreshAll()
	{
		if (
			_page == null
			|| !GodotObject.IsInstanceValid(
				_page
			)
		)
		{
			return;
		}

		RefreshRuntime();
		RefreshSector();
		RefreshNodes();
		RefreshSelection();
	}


	private void RefreshRuntime()
	{
		_matterValue.Text =
			NumberFormatter.Format(
				_service.Matter
			);

		_coreValue.Text =
			"L"
			+ _service.CoreLevel;

		_sectorValue.Text =
			_service.SectorNumber.ToString();

		_outputValue.Text =
			NumberFormatter.Format(
				_service.GetTotalOutputPerSecond()
			)
			+ "/s";

		_sectorOutput.Text =
			NumberFormatter.Format(
				_service.GetSectorOutputPerSecond(
					_service.CurrentSectorIndex
				)
			)
			+ " Matter/s";
	}


	private void RefreshSector()
	{
		_sectorTitle.Text =
			"SECTOR "
			+ _service.SectorNumber;

		_previousSectorButton.Disabled =
			_service.CurrentSectorIndex
			<= 0;

		if (
			_service.CurrentSectorIndex
			< _service.SectorCount - 1
		)
		{
			_nextSectorButton.Text =
				"›";

			_nextSectorButton.Disabled =
				false;
		}
		else
		{
			double cost =
				_service.GetNextSectorUnlockCost();

			_nextSectorButton.Text =
				_service.CanUnlockNextSector()
					? "+"
					: "›";

			_nextSectorButton.TooltipText =
				"Next sector: "
				+ NumberFormatter.Format(
					cost
				)
				+ " Matter";

			_nextSectorButton.Disabled =
				false;
		}

		_upgradeCoreButton.Text =
			"UPGRADE CORE\n"
			+ NumberFormatter.Format(
				_service.GetCoreUpgradeCost()
			);
	}


	private void RefreshNodes()
	{
		SingularitySectorData sector =
			_service.GetCurrentSector();

		bool[] active =
			new bool[
				SingularityService.NodesPerSector
			];

		for (
			int i = 0;
			i < _nodeViews.Count;
			i++
		)
		{
			SingularityNodeData node =
				sector.Nodes[
					i
				];

			NodeButtonView view =
				_nodeViews[
					i
				];

			view.Icon.Texture =
				GetNodeTexture(
					node.Type
				);

			active[
				i
			] =
				node.Type
				!= SingularityNodeType.Empty;

			Color accent =
				GetNodeColor(
					node.Type
				);

			view.Root.AddThemeStyleboxOverride(
				"normal",
				CreateNodeStyle(
					node.Type
						== SingularityNodeType.Empty
							? new Color(
								0.025f,
								0.030f,
								0.040f,
								0.96f
							)
							: new Color(
								accent.R * 0.20f,
								accent.G * 0.20f,
								accent.B * 0.20f,
								0.98f
							),
					node.Type
						== SingularityNodeType.Empty
							? new Color(
								0.24f,
								0.27f,
								0.32f,
								0.82f
							)
							: accent,
					i == _selectedNode
				)
			);

			view.Level.Text =
				node.Type
					== SingularityNodeType.Empty
						? "EMPTY"
						: "LV "
							+ node.Level;
		}

		_linkLayer.SetActiveNodes(
			active
		);
	}


	private void RefreshSelection()
	{
		if (_selectedNode < 0)
		{
			_selectionTitle.Text =
				"SINGULARITY CORE";

			_selectionDetails.Text =
				"Core Level "
				+ _service.CoreLevel
				+ " • "
				+ NumberFormatter.Format(
					_service.GetCoreOutputPerSecond()
				)
				+ " base Matter/s\n"
				+ "Every Core Level also boosts Compute Nodes by +10%.";

			_buildButtons.Hide();
			_upgradeNodeButton.Hide();

			return;
		}

		SingularityNodeData node =
			_service.GetNode(
				_service.CurrentSectorIndex,
				_selectedNode
			);

		if (
			node.Type
			== SingularityNodeType.Empty
		)
		{
			_selectionTitle.Text =
				"EMPTY NODE "
				+ (
					_selectedNode + 1
				);

			_selectionDetails.Text =
				"Choose what to construct. Adjacent slots are the two neighboring nodes around the ring.";

			_buildButtons.Show();
			_upgradeNodeButton.Hide();

			foreach (
				Node child
					in _buildButtons.GetChildren()
			)
			{
				if (
					child is not Button button
					|| !Enum.TryParse(
						button.Name.ToString(),
						out SingularityNodeType type
					)
				)
				{
					continue;
				}

				bool unlocked =
					_service.IsNodeTypeUnlocked(
						type
					);

				double cost =
					_service.GetBuildCost(
						_service.CurrentSectorIndex,
						type
					);

				button.Disabled =
					!unlocked;

				button.Text =
					GetNodeShortName(
						type
					)
					+ "\n"
					+ (
						unlocked
							? NumberFormatter.Format(
								cost
							)
							: _service.GetNodeUnlockText(
								type
							)
					);
			}

			return;
		}

		_buildButtons.Hide();
		_upgradeNodeButton.Show();

		double upgradeCost =
			_service.GetNodeUpgradeCost(
				_service.CurrentSectorIndex,
				_selectedNode
			);

		_selectionTitle.Text =
			node.Type.ToString().ToUpperInvariant()
			+ " NODE • LV "
			+ node.Level;

		_selectionDetails.Text =
			GetNodeDescription(
				node.Type
			);

		if (
			node.Type
			== SingularityNodeType.Compute
		)
		{
			_selectionDetails.Text +=
				"\nCurrent output: "
				+ NumberFormatter.Format(
					_service.GetNodeDisplayedOutput(
						_service.CurrentSectorIndex,
						_selectedNode
					)
				)
				+ " Matter/s";
		}

		_upgradeNodeButton.Text =
			"UPGRADE NODE\n"
			+ NumberFormatter.Format(
				upgradeCost
			);
	}


	private static string GetNodeDescription(
		SingularityNodeType type)
	{
		return type switch
		{
			SingularityNodeType.Compute =>
				"Produces Singularity Matter continuously.",

			SingularityNodeType.Amplifier =>
				"+15% output per level to each adjacent Compute Node.",

			SingularityNodeType.Cooling =>
				"+10% speed/output per level to each adjacent Compute Node.",

			SingularityNodeType.Quantum =>
				"+12% output per level to every Compute Node in this sector.",

			_ =>
				""
		};
	}


	private static string GetNodeShortName(
		SingularityNodeType type)
	{
		return type switch
		{
			SingularityNodeType.Compute => "CMP",
			SingularityNodeType.Amplifier => "AMP",
			SingularityNodeType.Cooling => "CLG",
			SingularityNodeType.Quantum => "QNT",
			_ => "---"
		};
	}


	private static Texture2D GetNodeTexture(
		SingularityNodeType type)
	{
		return type switch
		{
			SingularityNodeType.Compute =>
				ComputeNodeTexture,

			SingularityNodeType.Amplifier =>
				AmplifierNodeTexture,

			SingularityNodeType.Cooling =>
				CoolingNodeTexture,

			SingularityNodeType.Quantum =>
				QuantumNodeTexture,

			_ =>
				EmptyNodeTexture
		};
	}


	private static Color GetNodeColor(
		SingularityNodeType type)
	{
		return type switch
		{
			SingularityNodeType.Compute =>
				ComputeColor,

			SingularityNodeType.Amplifier =>
				AmplifierColor,

			SingularityNodeType.Cooling =>
				CoolingColor,

			SingularityNodeType.Quantum =>
				QuantumColor,

			_ =>
				new Color(
					0.32f,
					0.34f,
					0.38f,
					1.0f
				)
		};
	}


	private static Vector2 GetNodeCenter(
		int index)
	{
		double angle =
			-Math.PI / 2.0
			+ index
			* (
				Math.PI * 2.0
				/ SingularityService.NodesPerSector
			);

		const double radius =
			235.0;

		return new Vector2(
			325.0f
				+ (float)(
					Math.Cos(
						angle
					)
					* radius
				),
			325.0f
				+ (float)(
					Math.Sin(
						angle
					)
					* radius
				)
		);
	}


	private static List<Vector2> GetAllNodeCenters()
	{
		List<Vector2> centers =
			[];

		for (
			int i = 0;
			i < SingularityService.NodesPerSector;
			i++
		)
		{
			centers.Add(
				GetNodeCenter(
					i
				)
			);
		}

		return centers;
	}


	private void ApplySafeArea()
	{
		float safeTop =
			GetSafeTopInset();

		_header.OffsetTop =
			safeTop
			+ 14.0f;

		_scroll.OffsetTop =
			safeTop
			+ 166.0f;
	}


	private float GetSafeTopInset()
	{
		string os =
			OS.GetName();

		if (
			os != "Android"
			&& os != "iOS"
		)
		{
			return 0.0f;
		}

		Rect2I safeArea =
			DisplayServer.GetDisplaySafeArea();

		Vector2I windowSize =
			DisplayServer.WindowGetSize();

		Rect2 viewport =
			_root.GetViewport()
				.GetVisibleRect();

		if (
			windowSize.Y <= 0
			|| safeArea.Size.Y <= 0
		)
		{
			return 30.0f;
		}

		float scaleY =
			viewport.Size.Y
			/ windowSize.Y;

		return MathF.Max(
			safeArea.Position.Y
				* scaleY,
			26.0f
		);
	}


	private static Label CreateLabel(
		int size,
		string text)
	{
		Label label =
			new()
			{
				Text = text,

				HorizontalAlignment =
					HorizontalAlignment.Center,

				VerticalAlignment =
					VerticalAlignment.Center,

				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};

		label.AddThemeFontSizeOverride(
			"font_size",
			size
		);

		return label;
	}


	private static StyleBoxFlat CreatePanelStyle(
		Color background,
		Color border,
		int radius)
	{
		return new StyleBoxFlat
		{
			BgColor = background,
			BorderColor = border,

			BorderWidthLeft = 1,
			BorderWidthTop = 1,
			BorderWidthRight = 1,
			BorderWidthBottom = 1,

			CornerRadiusTopLeft = radius,
			CornerRadiusTopRight = radius,
			CornerRadiusBottomLeft = radius,
			CornerRadiusBottomRight = radius
		};
	}


	private static StyleBoxFlat CreateNodeStyle(
		Color background,
		Color border,
		bool selected)
	{
		int width =
			selected
				? 4
				: 2;

		return new StyleBoxFlat
		{
			BgColor = background,
			BorderColor = border,

			BorderWidthLeft = width,
			BorderWidthTop = width,
			BorderWidthRight = width,
			BorderWidthBottom = width,

			CornerRadiusTopLeft = 18,
			CornerRadiusTopRight = 18,
			CornerRadiusBottomLeft = 18,
			CornerRadiusBottomRight = 18,

			ShadowColor =
				new Color(
					border.R,
					border.G,
					border.B,
					selected
						? 0.42f
						: 0.18f
				),

			ShadowSize =
				selected
					? 12
					: 5
		};
	}


	private sealed record NodeButtonView(
		Button Root,
		TextureRect Icon,
		Label Level
	);


	private sealed partial class SingularityLinkLayer
		: Control
	{
		private Vector2 _core;

		private readonly List<Vector2>
			_nodes =
				[];

		private bool[] _active =
			Array.Empty<bool>();


		public void SetCenters(
			Vector2 core,
			IReadOnlyList<Vector2> nodes)
		{
			_core =
				core;

			_nodes.Clear();

			foreach (
				Vector2 node
					in nodes
			)
			{
				_nodes.Add(
					node
				);
			}

			_active =
				new bool[
					_nodes.Count
				];

			QueueRedraw();
		}


		public void SetActiveNodes(
			bool[] active)
		{
			_active =
				active;

			QueueRedraw();
		}


		public override void _Draw()
		{
			for (
				int i = 0;
				i < _nodes.Count;
				i++
			)
			{
				bool active =
					i < _active.Length
					&& _active[
						i
					];

				Color color =
					active
						? Accent
						: new Color(
							0.22f,
							0.24f,
							0.30f,
							0.72f
						);

				DrawLine(
					_core,
					_nodes[
						i
					],
					new Color(
						color.R,
						color.G,
						color.B,
						active
							? 0.18f
							: 0.10f
					),
					active
						? 13.0f
						: 7.0f,
					true
				);

				DrawLine(
					_core,
					_nodes[
						i
					],
					color,
					active
						? 4.0f
						: 2.5f,
					true
				);
			}
		}
	}


	private sealed partial class SingularityGridBackground
		: Control
	{
		public override void _Draw()
		{
			Color line =
				new(
					0.34f,
					0.24f,
					0.08f,
					0.10f
				);

			const float spacing =
				46.0f;

			for (
				float x = -240.0f;
				x < Size.X + 240.0f;
				x += spacing
			)
			{
				DrawLine(
					new Vector2(
						x,
						0
					),
					new Vector2(
						x + 420.0f,
						Size.Y
					),
					line,
					1.0f
				);
			}

			for (
				float x = 0.0f;
				x < Size.X + 480.0f;
				x += spacing
			)
			{
				DrawLine(
					new Vector2(
						x,
						0
					),
					new Vector2(
						x - 420.0f,
						Size.Y
					),
					line,
					1.0f
				);
			}
		}
	}
}
