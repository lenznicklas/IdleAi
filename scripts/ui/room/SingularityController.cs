using Godot;
using System;
using System.Collections.Generic;

namespace IdleAi;


/*
 * Singularity room UI.
 *
 * This version intentionally uses overlays for every interaction:
 * - Empty node -> node-type purchase overlay.
 * - Filled node -> info + upgrade overlay.
 * - Core -> Core info + "buy next Core" overlay.
 *
 * No upgrade/purchase controls are permanently shown below the network.
 */
public sealed partial class SingularityController
{
	private const float BottomReservedSpace =
		132.0f;

	private const int NavigationHapticDurationMs =
		12;

	private const float NavigationHapticStrength =
		0.12f;

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

	private readonly GameState _state;

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

	private TextureRect _coreImage =
		null!;

	private Button _coreHitbox =
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

	private Label _hint =
		null!;


	private Control _detailOverlay =
		null!;

	private PanelContainer _detailPanel =
		null!;

	private VBoxContainer _detailContent =
		null!;

	private int _selectedNode =
		-1;

	private bool _detailIsCore;


	private Timer _runtimeTimer =
		null!;

	private Timer _saveTimer =
		null!;


	public event Action<string>? MessageRequested;


	public bool Visible =>
		_page != null
		&& _page.Visible;


	public SingularityController(
		Game root,
		GameState state,
		SingularityService service)
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
		CreatePage();
		CreateDetailOverlay();
		CreateTimers();

		_service.Changed +=
			OnServiceChanged;

		if (CoreTexture == null)
		{
			GD.PushWarning(
				"Singularity: core.png could not be loaded."
			);
		}

		Hide();
	}


	public void Open()
	{
		if (!_service.Unlocked)
			return;

		CloseDetailOverlay();

		ApplySingularityBottomBarTheme();

		RefreshAll();

		_page.Show();
		_page.MoveToFront();

		ApplySafeArea();
	}


	public void Hide()
	{
		bool wasVisible =
			_page != null
			&& _page.Visible;

		CloseDetailOverlay();

		_page?.Hide();

		if (wasVisible)
		{
			RestoreNormalBottomBarTheme();
		}
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


	// ==================================================
	// PAGE
	// ==================================================

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
				PlayHaptic();

				Hide();

				Control map =
					_root.GetNode<Control>(
						"MapPage"
					);

				map.Show();
				map.MoveToFront();
			};

		ApplyGoldButtonStyle(
			mapButton
		);

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
				"CORES",
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
		string title,
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
				title
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
		CreateHint();
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
				PlayHaptic();

				if (
					_service.GoToPreviousSector()
				)
				{
					CloseDetailOverlay();
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


		/*
		 * Core rendering fix:
		 *
		 * The image is now a direct TextureRect child of the network, not a
		 * child of a themed Button. A completely transparent Button is placed
		 * above it only as a hitbox. The Button can therefore never paint over
		 * core.png.
		 */
		_coreImage =
			new TextureRect
			{
				Name =
					"SingularityCoreImage",

				Texture =
					CoreTexture,

				Position =
					new Vector2(
						225,
						225
					),

				Size =
					new Vector2(
						200,
						200
					),

				ExpandMode =
					TextureRect.ExpandModeEnum.IgnoreSize,

				StretchMode =
					TextureRect.StretchModeEnum.KeepAspectCentered,

				MouseFilter =
					Control.MouseFilterEnum.Ignore,

				ZIndex =
					2
			};

		_network.AddChild(
			_coreImage
		);


		if (CoreTexture == null)
		{
			Label fallback =
				CreateLabel(
					18,
					"CORE"
				);

			fallback.SetAnchorsAndOffsetsPreset(
				Control.LayoutPreset.FullRect
			);

			_coreImage.AddChild(
				fallback
			);
		}


		_coreHitbox =
			new Button
			{
				Name =
					"SingularityCoreHitbox",

				Position =
					new Vector2(
						225,
						225
					),

				Size =
					new Vector2(
						200,
						200
					),

				FocusMode =
					Control.FocusModeEnum.None,

				TooltipText =
					"Open Singularity Core",

				ZIndex =
					3
			};

		ApplyTransparentButtonStyle(
			_coreHitbox
		);

		_coreHitbox.Pressed +=
			() =>
			{
				PlayHaptic();
				OpenCoreOverlay();
			};

		_network.AddChild(
			_coreHitbox
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
						true,

					ZIndex =
						2
				};

			button.Pressed +=
				() =>
				{
					PlayHaptic();
					OpenNodeOverlay(
						index
					);
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


	private void CreateHint()
	{
		_hint =
			CreateLabel(
				12,
				"Tap the Core or any Node to open its overlay."
			);

		_hint.CustomMinimumSize =
			new Vector2(
				0,
				54
			);

		_hint.Modulate =
			new Color(
				0.72f,
				0.72f,
				0.76f,
				1.0f
			);

		_content.AddChild(
			_hint
		);
	}


	// ==================================================
	// DETAIL OVERLAY
	// ==================================================

	private void CreateDetailOverlay()
	{
		_detailOverlay =
			new Control
			{
				Name =
					"SingularityDetailOverlay",

				MouseFilter =
					Control.MouseFilterEnum.Stop,

				ZIndex =
					1500
			};

		_detailOverlay.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);

		_root.AddChild(
			_detailOverlay
		);


		ColorRect dim =
			new()
			{
				Color =
					new Color(
						0,
						0,
						0,
						0.80f
					),

				MouseFilter =
					Control.MouseFilterEnum.Stop
			};

		dim.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);

		dim.GuiInput +=
			OnOverlayDimInput;

		_detailOverlay.AddChild(
			dim
		);


		CenterContainer center =
			new()
			{
				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};

		center.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);

		center.OffsetLeft =
			18;

		center.OffsetTop =
			34;

		center.OffsetRight =
			-18;

		center.OffsetBottom =
			-34;

		_detailOverlay.AddChild(
			center
		);


		_detailPanel =
			new PanelContainer
			{
				CustomMinimumSize =
					new Vector2(
						590,
						0
					),

				MouseFilter =
					Control.MouseFilterEnum.Stop
			};

		_detailPanel.AddThemeStyleboxOverride(
			"panel",
			CreatePanelStyle(
				new Color(
					0.018f,
					0.020f,
					0.028f,
					0.995f
				),
				Accent,
				22
			)
		);

		center.AddChild(
			_detailPanel
		);


		MarginContainer margin =
			new();

		margin.AddThemeConstantOverride(
			"margin_left",
			24
		);

		margin.AddThemeConstantOverride(
			"margin_top",
			54
		);

		margin.AddThemeConstantOverride(
			"margin_right",
			24
		);

		margin.AddThemeConstantOverride(
			"margin_bottom",
			24
		);

		_detailPanel.AddChild(
			margin
		);


		_detailContent =
			new VBoxContainer
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};

		_detailContent.AddThemeConstantOverride(
			"separation",
			12
		);

		margin.AddChild(
			_detailContent
		);


		OverlayCloseButton.Add(
			_detailPanel,
			CloseDetailOverlay
		);


		_detailOverlay.Hide();
	}


	private void OpenCoreOverlay()
	{
		_selectedNode =
			-1;

		_detailIsCore =
			true;

		BuildCoreOverlay();

		_detailOverlay.Show();
		_detailOverlay.MoveToFront();
	}


	private void OpenNodeOverlay(
		int nodeIndex)
	{
		_selectedNode =
			Math.Clamp(
				nodeIndex,
				0,
				SingularityService.NodesPerSector - 1
			);

		_detailIsCore =
			false;

		BuildNodeOverlay();

		_detailOverlay.Show();
		_detailOverlay.MoveToFront();
	}


	private void CloseDetailOverlay()
	{
		_detailOverlay?.Hide();

		_selectedNode =
			-1;

		_detailIsCore =
			false;
	}


	private void RefreshOpenDetailOverlay()
	{
		if (
			_detailOverlay == null
			|| !_detailOverlay.Visible
		)
		{
			return;
		}

		if (_detailIsCore)
		{
			BuildCoreOverlay();
		}
		else if (_selectedNode >= 0)
		{
			BuildNodeOverlay();
		}
	}


	private void ClearDetailContent()
	{
		foreach (
			Node child
				in _detailContent.GetChildren()
		)
		{
			_detailContent.RemoveChild(
				child
			);

			child.QueueFree();
		}
	}


	private void BuildCoreOverlay()
	{
		ClearDetailContent();


		Label title =
			CreateLabel(
				26,
				"SINGULARITY CORE"
			);

		title.AddThemeColorOverride(
			"font_color",
			Accent.Lightened(
				0.12f
			)
		);

		_detailContent.AddChild(
			title
		);


		TextureRect image =
			CreateOverlayImage(
				CoreTexture,
				220
			);

		_detailContent.AddChild(
			image
		);


		Label core =
			CreateLabel(
				20,
				"CORE "
					+ _service.CoreCount
			);

		_detailContent.AddChild(
			core
		);


		Label included =
			CreateLabel(
				12,
				_service.CoreCount == 1
					? "Core 1 is included automatically when The Singularity is initialized."
					: "Installed Cores: "
						+ _service.CoreCount
			);

		included.AutowrapMode =
			TextServer.AutowrapMode.WordSmart;

		included.Modulate =
			new Color(
				0.72f,
				0.76f,
				0.82f,
				1.0f
			);

		_detailContent.AddChild(
			included
		);


		PanelContainer stats =
			CreateInfoCard();

		_detailContent.AddChild(
			stats
		);


		VBoxContainer statBox =
			new();

		statBox.AddThemeConstantOverride(
			"separation",
			7
		);

		stats.AddChild(
			statBox
		);


		statBox.AddChild(
			CreateLabel(
				14,
				"Core generation: "
					+ NumberFormatter.Format(
						_service.GetCoreOutputPerSecond()
					)
					+ " Matter/s"
			)
		);


		statBox.AddChild(
			CreateLabel(
				14,
				"Compute network multiplier: x"
					+ _service
						.GetComputeCoreMultiplier()
						.ToString(
							"F2"
						)
			)
		);


		int nextCore =
			_service.CoreCount + 1;

		double nextOutput =
			SingularityService
				.GetCoreOutputPerSecond(
					nextCore
				);

		Label next =
			CreateLabel(
				13,
				"Core "
					+ nextCore
					+ " → "
					+ NumberFormatter.Format(
						nextOutput
					)
					+ " Core Matter/s and +10% network output"
			);

		next.AutowrapMode =
			TextServer.AutowrapMode.WordSmart;

		statBox.AddChild(
			next
		);


		Button buy =
			new Button
			{
				Text =
					"BUY CORE "
					+ nextCore
					+ "\n"
					+ NumberFormatter.Format(
						_service.GetNextCoreCost()
					)
					+ " MATTER",

				CustomMinimumSize =
					new Vector2(
						0,
						70
					),

				FocusMode =
					Control.FocusModeEnum.None
			};

		ApplyGoldButtonStyle(
			buy
		);

		buy.Pressed +=
			() =>
			{
				PlayHaptic();

				HandleResult(
					_service.BuyNextCore()
				);
			};

		_detailContent.AddChild(
			buy
		);
	}


	private void BuildNodeOverlay()
	{
		ClearDetailContent();

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
			BuildEmptyNodeOverlay();

			return;
		}

		BuildFilledNodeOverlay(
			node
		);
	}


	private void BuildEmptyNodeOverlay()
	{
		Label title =
			CreateLabel(
				25,
				"EMPTY NODE "
					+ (
						_selectedNode + 1
					)
			);

		title.AddThemeColorOverride(
			"font_color",
			Accent.Lightened(
				0.12f
			)
		);

		_detailContent.AddChild(
			title
		);


		_detailContent.AddChild(
			CreateOverlayImage(
				EmptyNodeTexture,
				170
			)
		);


		Label hint =
			CreateLabel(
				13,
				"Choose which Node to construct here."
			);

		hint.Modulate =
			new Color(
				0.74f,
				0.78f,
				0.84f,
				1.0f
			);

		_detailContent.AddChild(
			hint
		);


		AddNodePurchaseOption(
			SingularityNodeType.Compute,
			ComputeNodeTexture,
			"COMPUTE NODE",
			"Produces Singularity Matter continuously."
		);

		AddNodePurchaseOption(
			SingularityNodeType.Amplifier,
			AmplifierNodeTexture,
			"AMPLIFIER NODE",
			"+15% output per level to each adjacent Compute Node."
		);

		AddNodePurchaseOption(
			SingularityNodeType.Cooling,
			CoolingNodeTexture,
			"COOLING NODE",
			"+10% speed/output per level to each adjacent Compute Node."
		);

		AddNodePurchaseOption(
			SingularityNodeType.Quantum,
			QuantumNodeTexture,
			"QUANTUM NODE",
			"+12% output per level to every Compute Node in this Sector."
		);
	}


	private void AddNodePurchaseOption(
		SingularityNodeType type,
		Texture2D texture,
		string title,
		string description)
	{
		PanelContainer card =
			CreateInfoCard();

		_detailContent.AddChild(
			card
		);


		MarginContainer margin =
			new();

		margin.AddThemeConstantOverride(
			"margin_left",
			10
		);

		margin.AddThemeConstantOverride(
			"margin_top",
			8
		);

		margin.AddThemeConstantOverride(
			"margin_right",
			10
		);

		margin.AddThemeConstantOverride(
			"margin_bottom",
			8
		);

		card.AddChild(
			margin
		);


		HBoxContainer row =
			new();

		row.AddThemeConstantOverride(
			"separation",
			10
		);

		margin.AddChild(
			row
		);


		TextureRect icon =
			new()
			{
				Texture =
					texture,

				CustomMinimumSize =
					new Vector2(
						70,
						70
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
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};

		row.AddChild(
			text
		);


		Label name =
			CreateLabel(
				15,
				title
			);

		name.HorizontalAlignment =
			HorizontalAlignment.Left;

		text.AddChild(
			name
		);


		Label desc =
			CreateLabel(
				11,
				description
			);

		desc.HorizontalAlignment =
			HorizontalAlignment.Left;

		desc.AutowrapMode =
			TextServer.AutowrapMode.WordSmart;

		desc.Modulate =
			new Color(
				0.70f,
				0.74f,
				0.80f,
				1.0f
			);

		text.AddChild(
			desc
		);


		bool unlocked =
			_service.IsNodeTypeUnlocked(
				type
			);

		double cost =
			_service.GetBuildCost(
				_service.CurrentSectorIndex,
				type
			);


		Button buy =
			new()
			{
				Text =
					unlocked
						? "BUY\n"
							+ NumberFormatter.Format(
								cost
							)
						: "LOCKED\n"
							+ _service.GetNodeUnlockText(
								type
							),

				CustomMinimumSize =
					new Vector2(
						120,
						68
					),

				FocusMode =
					Control.FocusModeEnum.None,

				Disabled =
					!unlocked
			};

		ApplyNodeTypeButtonStyle(
			buy,
			GetNodeColor(
				type
			)
		);

		buy.Pressed +=
			() =>
			{
				PlayHaptic();

				HandleResult(
					_service.BuildNode(
						_service.CurrentSectorIndex,
						_selectedNode,
						type
					)
				);
			};

		row.AddChild(
			buy
		);
	}


	private void BuildFilledNodeOverlay(
		SingularityNodeData node)
	{
		Color accent =
			GetNodeColor(
				node.Type
			);


		Label title =
			CreateLabel(
				25,
				node.Type.ToString().ToUpperInvariant()
					+ " NODE"
			);

		title.AddThemeColorOverride(
			"font_color",
			accent.Lightened(
				0.12f
			)
		);

		_detailContent.AddChild(
			title
		);


		_detailContent.AddChild(
			CreateOverlayImage(
				GetNodeTexture(
					node.Type
				),
				190
			)
		);


		Label level =
			CreateLabel(
				19,
				"LEVEL "
					+ node.Level
			);

		_detailContent.AddChild(
			level
		);


		Label description =
			CreateLabel(
				13,
				GetNodeDescription(
					node.Type
				)
			);

		description.AutowrapMode =
			TextServer.AutowrapMode.WordSmart;

		description.Modulate =
			new Color(
				0.72f,
				0.76f,
				0.82f,
				1.0f
			);

		_detailContent.AddChild(
			description
		);


		PanelContainer stats =
			CreateInfoCard();

		_detailContent.AddChild(
			stats
		);


		VBoxContainer statBox =
			new();

		statBox.AddThemeConstantOverride(
			"separation",
			7
		);

		stats.AddChild(
			statBox
		);


		if (
			node.Type
			== SingularityNodeType.Compute
		)
		{
			statBox.AddChild(
				CreateLabel(
					14,
					"Current output: "
						+ NumberFormatter.Format(
							_service.GetNodeDisplayedOutput(
								_service.CurrentSectorIndex,
								_selectedNode
							)
						)
						+ " Matter/s"
				)
			);
		}
		else
		{
			statBox.AddChild(
				CreateLabel(
					14,
					GetNodeLevelEffectText(
						node
					)
				)
			);
		}


		double upgradeCost =
			_service.GetNodeUpgradeCost(
				_service.CurrentSectorIndex,
				_selectedNode
			);


		Button upgrade =
			new()
			{
				Text =
					"UPGRADE TO LEVEL "
					+ (
						node.Level + 1
					)
					+ "\n"
					+ NumberFormatter.Format(
						upgradeCost
					)
					+ " MATTER",

				CustomMinimumSize =
					new Vector2(
						0,
						70
					),

				FocusMode =
					Control.FocusModeEnum.None
			};

		ApplyNodeTypeButtonStyle(
			upgrade,
			accent
		);

		upgrade.Pressed +=
			() =>
			{
				PlayHaptic();

				HandleResult(
					_service.UpgradeNode(
						_service.CurrentSectorIndex,
						_selectedNode
					)
				);
			};

		_detailContent.AddChild(
			upgrade
		);
	}


	private void OnOverlayDimInput(
		InputEvent @event)
	{
		bool released =
			@event
				is InputEventScreenTouch touch
			&& !touch.Pressed;

		released |=
			@event
				is InputEventMouseButton mouse
			&& !mouse.Pressed
			&& mouse.ButtonIndex
				== MouseButton.Left;

		if (!released)
			return;

		_detailOverlay
			.GetViewport()
			.SetInputAsHandled();

		Callable
			.From(
				CloseDetailOverlay
			)
			.CallDeferred();
	}


	// ==================================================
	// ACTIONS / REFRESH
	// ==================================================

	private void OnNextSectorPressed()
	{
		PlayHaptic();

		if (
			_service.GoToNextExistingSector()
		)
		{
			CloseDetailOverlay();
			RefreshAll();

			return;
		}

		HandleResult(
			_service.UnlockNextSector()
		);
	}


	private void HandleResult(
		SingularityActionResult result)
	{
		MessageRequested?.Invoke(
			result.Message
		);

		if (!result.Changed)
			return;

		RefreshAll();
		RefreshOpenDetailOverlay();
	}


	private void OnServiceChanged()
	{
		RefreshAll();
		RefreshOpenDetailOverlay();
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
	}


	private void RefreshRuntime()
	{
		_matterValue.Text =
			NumberFormatter.Format(
				_service.Matter
			);

		_coreValue.Text =
			_service.CoreCount.ToString();

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

			_nextSectorButton.TooltipText =
				"Next Sector";

			return;
		}

		_nextSectorButton.Text =
			_service.CanUnlockNextSector()
				? "+"
				: "›";

		_nextSectorButton.TooltipText =
			"Next Sector • "
			+ NumberFormatter.Format(
				_service.GetNextSectorUnlockCost()
			)
			+ " Matter";
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
							: accent
				)
			);

			view.Root.AddThemeStyleboxOverride(
				"hover",
				CreateNodeStyle(
					new Color(
						accent.R * 0.28f,
						accent.G * 0.28f,
						accent.B * 0.28f,
						0.98f
					),
					accent
				)
			);

			view.Root.AddThemeStyleboxOverride(
				"pressed",
				CreateNodeStyle(
					new Color(
						accent.R * 0.16f,
						accent.G * 0.16f,
						accent.B * 0.16f,
						1.0f
					),
					accent
				)
			);

			view.Level.Text =
				node.Type
					== SingularityNodeType.Empty
						? "EMPTY"
						: "LV "
							+ node.Level;
		}


		/*
		 * The link layer now receives the occupied-state array.
		 * It draws:
		 * 1. Core -> occupied Node links.
		 * 2. Node -> Node links whenever both adjacent ring nodes are occupied.
		 */
		_linkLayer.SetActiveNodes(
			active
		);
	}


	// ==================================================
	// BOTTOM BAR THEME
	// ==================================================

	private void ApplySingularityBottomBarTheme()
	{
		TextureRect? background =
			_root.GetNodeOrNull<TextureRect>(
				"BottomBar/Background"
			);

		if (background == null)
			return;

		background.Texture =
			RoomThemePalette
				.Create(
					4
				)
				.Bar;
	}


	private void RestoreNormalBottomBarTheme()
	{
		TextureRect? background =
			_root.GetNodeOrNull<TextureRect>(
				"BottomBar/Background"
			);

		if (background == null)
			return;

		background.Texture =
			RoomThemePalette
				.Create(
					_state.CurrentRoomIndex
				)
				.Bar;
	}


	// ==================================================
	// TEXT / HELPERS
	// ==================================================

	private static string GetNodeDescription(
		SingularityNodeType type)
	{
		return type switch
		{
			SingularityNodeType.Compute =>
				"Produces Singularity Matter continuously.",

			SingularityNodeType.Amplifier =>
				"Boosts the two neighboring Compute Nodes.",

			SingularityNodeType.Cooling =>
				"Speeds up the two neighboring Compute Nodes.",

			SingularityNodeType.Quantum =>
				"Multiplies every Compute Node in this Sector.",

			_ =>
				""
		};
	}


	private static string GetNodeLevelEffectText(
		SingularityNodeData node)
	{
		return node.Type switch
		{
			SingularityNodeType.Amplifier =>
				"Adjacent Compute boost: +"
				+ (
					15
					* node.Level
				)
				+ "%",

			SingularityNodeType.Cooling =>
				"Adjacent Compute speed/output: +"
				+ (
					10
					* node.Level
				)
				+ "%",

			SingularityNodeType.Quantum =>
				"Sector Compute boost: +"
				+ (
					12
					* node.Level
				)
				+ "%",

			_ =>
				""
		};
	}


	private static TextureRect CreateOverlayImage(
		Texture2D texture,
		float height)
	{
		return new TextureRect
		{
			Texture =
				texture,

			CustomMinimumSize =
				new Vector2(
					0,
					height
				),

			ExpandMode =
				TextureRect.ExpandModeEnum.IgnoreSize,

			StretchMode =
				TextureRect.StretchModeEnum.KeepAspectCentered,

			MouseFilter =
				Control.MouseFilterEnum.Ignore
		};
	}


	private static PanelContainer CreateInfoCard()
	{
		PanelContainer panel =
			new();

		panel.AddThemeStyleboxOverride(
			"panel",
			CreatePanelStyle(
				new Color(
					0.030f,
					0.032f,
					0.044f,
					0.98f
				),
				new Color(
					0.23f,
					0.25f,
					0.30f,
					0.84f
				),
				14
			)
		);

		return panel;
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
					0.34f,
					0.35f,
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
		Color border)
	{
		return new StyleBoxFlat
		{
			BgColor = background,
			BorderColor = border,

			BorderWidthLeft = 2,
			BorderWidthTop = 2,
			BorderWidthRight = 2,
			BorderWidthBottom = 2,

			CornerRadiusTopLeft = 18,
			CornerRadiusTopRight = 18,
			CornerRadiusBottomLeft = 18,
			CornerRadiusBottomRight = 18,

			ShadowColor =
				new Color(
					border.R,
					border.G,
					border.B,
					0.20f
				),

			ShadowSize = 6
		};
	}


	private static void ApplyTransparentButtonStyle(
		Button button)
	{
		StyleBoxEmpty empty =
			new();

		button.AddThemeStyleboxOverride(
			"normal",
			empty
		);

		button.AddThemeStyleboxOverride(
			"hover",
			empty
		);

		button.AddThemeStyleboxOverride(
			"pressed",
			empty
		);

		button.AddThemeStyleboxOverride(
			"focus",
			empty
		);
	}


	private static void ApplyGoldButtonStyle(
		Button button)
	{
		ApplyNodeTypeButtonStyle(
			button,
			Accent
		);
	}


	private static void ApplyNodeTypeButtonStyle(
		Button button,
		Color accent)
	{
		button.AddThemeStyleboxOverride(
			"normal",
			CreateActionButtonStyle(
				accent.Darkened(
					0.48f
				),
				accent
			)
		);

		button.AddThemeStyleboxOverride(
			"hover",
			CreateActionButtonStyle(
				accent.Darkened(
					0.28f
				),
				accent.Lightened(
					0.08f
				)
			)
		);

		button.AddThemeStyleboxOverride(
			"pressed",
			CreateActionButtonStyle(
				accent.Darkened(
					0.58f
				),
				accent
			)
		);

		button.AddThemeStyleboxOverride(
			"disabled",
			CreateActionButtonStyle(
				new Color(
					0.07f,
					0.075f,
					0.085f,
					0.96f
				),
				new Color(
					0.22f,
					0.24f,
					0.28f,
					0.82f
				)
			)
		);
	}


	private static StyleBoxFlat CreateActionButtonStyle(
		Color background,
		Color border)
	{
		return new StyleBoxFlat
		{
			BgColor =
				background,

			BorderColor =
				border,

			BorderWidthLeft = 2,
			BorderWidthTop = 2,
			BorderWidthRight = 2,
			BorderWidthBottom = 2,

			CornerRadiusTopLeft = 14,
			CornerRadiusTopRight = 14,
			CornerRadiusBottomLeft = 14,
			CornerRadiusBottomRight = 14,

			ContentMarginLeft = 10,
			ContentMarginRight = 10,
			ContentMarginTop = 7,
			ContentMarginBottom = 7
		};
	}


	private static void PlayHaptic()
	{
		Input.VibrateHandheld(
			NavigationHapticDurationMs,
			NavigationHapticStrength
		);
	}


	private sealed record NodeButtonView(
		Button Root,
		TextureRect Icon,
		Label Level
	);


	// ==================================================
	// LINES
	// ==================================================

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
			/*
			 * 1) Draw occupied adjacent Node -> Node links.
			 *
			 * The ring wraps around, therefore Node 8 and Node 1 are also
			 * neighbors.
			 */
			for (
				int i = 0;
				i < _nodes.Count;
				i++
			)
			{
				int next =
					(
						i + 1
					)
					% _nodes.Count;

				bool bothOccupied =
					i < _active.Length
					&& next < _active.Length
					&& _active[
						i
					]
					&& _active[
						next
					];

				if (!bothOccupied)
					continue;

				DrawConnection(
					_nodes[
						i
					],
					_nodes[
						next
					],
					new Color(
						0.96f,
						0.70f,
						0.22f,
						0.94f
					),
					10.0f,
					3.4f
				);
			}


			/*
			 * 2) Draw Core -> occupied Node links.
			 * Empty nodes only keep a very subtle guide line.
			 */
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
							0.42f
						);

				DrawConnection(
					_core,
					_nodes[
						i
					],
					color,
					active
						? 13.0f
						: 5.0f,
					active
						? 4.0f
						: 1.5f
				);
			}
		}


		private void DrawConnection(
			Vector2 from,
			Vector2 to,
			Color color,
			float glowWidth,
			float lineWidth)
		{
			DrawLine(
				from,
				to,
				new Color(
					color.R,
					color.G,
					color.B,
					0.18f
				),
				glowWidth,
				true
			);

			DrawLine(
				from,
				to,
				color,
				lineWidth,
				true
			);
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
