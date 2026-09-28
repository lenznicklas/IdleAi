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
	/*
	 * Fallback only. At runtime Singularity reads the actual BottomBar height
	 * so it always matches the normal rooms exactly.
	 */
	private const float DefaultBottomBarHeight =
		88.0f;

	private const int SingularityPageZIndex =
		600;

	private const int SingularityBottomBarZIndex =
		700;

	private const float SectorMapSize =
		516.0f;

	private const float SectorMapSpacing =
		656.0f;

	private const float SectorMapNodeRadius =
		182.0f;

	private const float SectorMapNodeButtonSize =
		91.0f;

	private const float SectorVirtualizationMargin =
		760.0f;

	private const float MapBoundsMargin =
		320.0f;

	private const float WorldCanvasSize =
		200_000.0f;

	private static readonly Vector2 WorldOrigin =
		new(
			WorldCanvasSize / 2.0f,
			WorldCanvasSize / 2.0f
		);

	private static readonly List<Vector2I> SectorGridCache =
		[
			Vector2I.Zero
		];

	private static Vector2I _spiralCursor =
		Vector2I.Zero;

	private static Vector2I _spiralDirection =
		new(
			1,
			0
		);

	private static int _spiralStepLength =
		1;

	private static int _spiralStepProgress;

	private static int _spiralLegsAtCurrentLength;

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

	private ColorRect _pageBackground =
		null!;

	private SingularityGridBackground _gridBackground =
		null!;

	private Control _bottomBar =
		null!;

	private int _normalBottomBarZIndex;

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



	// 2D map / camera -----------------------------------------------------
	private Control _mapViewport =
		null!;

	private Control _mapWorld =
		null!;

	private InterSectorLinkLayer _interSectorLinks =
		null!;

	private SingularityPanInput _panInput =
		null!;

	private Button _centerMapButton =
		null!;

	private readonly Dictionary<int, SectorMapView>
		_sectorMapViews =
			[];

	private readonly Dictionary<Vector2I, int>
		_sectorIndexByGrid =
			[];

	private int _cachedSectorCoordinateCount;

	private int _cachedBoundsSectorCount =
		-1;

	private Rect2 _cachedMapBounds;

	private Control? _expansionSectorView;

	private bool _cameraInitialized;


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
		_bottomBar =
			_root.GetNode<Control>(
				"BottomBar"
			);

		_normalBottomBarZIndex =
			_bottomBar.ZIndex;

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
		ApplySingularityBottomBarLayer();

		RefreshAll();

		_page.Show();
		_page.MoveToFront();

		/*
		 * MoveToFront() changes sibling order, while the explicit ZIndex keeps
		 * the complete normal BottomBar above the Singularity page.
		 */
		_bottomBar.MoveToFront();

		ApplySafeArea();

		Callable
			.From(
				() =>
				{
					CenterOnSector(
						_service.CurrentSectorIndex,
						false
					);

					RefreshMapWorld();
				}
			)
			.CallDeferred();
	}


	public void Hide()
	{
		bool wasVisible =
			_page != null
			&& _page.Visible;

		CloseDetailOverlay();

		_panInput?.StopMotion();

		_page?.Hide();

		if (wasVisible)
		{
			RestoreNormalBottomBarTheme();
			RestoreNormalBottomBarLayer();
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
					SingularityPageZIndex
			};

		_page.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);

		_root.AddChild(
			_page
		);


		_pageBackground =
			new ColorRect
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

		_pageBackground.AnchorRight =
			1.0f;

		_pageBackground.AnchorBottom =
			1.0f;

		_page.AddChild(
			_pageBackground
		);


		_gridBackground =
			new SingularityGridBackground
			{
				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};

		_gridBackground.AnchorRight =
			1.0f;

		_gridBackground.AnchorBottom =
			1.0f;

		_page.AddChild(
			_gridBackground
		);


		CreateHeader();
		CreateMapViewport();

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



	// ==================================================
	// FREE-PANNING 2D SECTOR MAP
	// ==================================================

	private void CreateMapViewport()
	{
		_mapViewport =
			new Control
			{
				Name =
					"SingularityMapViewport",

				ClipContents =
					true,

				MouseFilter =
					Control.MouseFilterEnum.Stop
			};

		_mapViewport.AnchorRight =
			1.0f;

		_mapViewport.AnchorBottom =
			1.0f;

		_mapViewport.OffsetTop =
			166.0f;

		_page.AddChild(
			_mapViewport
		);


		_mapWorld =
			new Control
			{
				Name =
					"SingularityMapWorld",

				Position =
					Vector2.Zero,

				Size =
					new Vector2(
						WorldCanvasSize,
						WorldCanvasSize
					),

				CustomMinimumSize =
					new Vector2(
						WorldCanvasSize,
						WorldCanvasSize
					),

				MouseFilter =
					Control.MouseFilterEnum.Pass
			};

		_mapViewport.AddChild(
			_mapWorld
		);


		_interSectorLinks =
			new InterSectorLinkLayer
			{
				Name =
					"InterSectorLinks",

				Position =
					Vector2.Zero,

				Size =
					new Vector2(
						WorldCanvasSize,
						WorldCanvasSize
					),

				MouseFilter =
					Control.MouseFilterEnum.Ignore,

				ZIndex =
					0
			};

		_mapWorld.AddChild(
			_interSectorLinks
		);


		_panInput =
			new SingularityPanInput
			{
				Name =
					"SingularityPanInput"
			};

		_page.AddChild(
			_panInput
		);

		_panInput.Configure(
			_mapViewport,
			() =>
				Visible
				&& (
					_detailOverlay == null
					|| !_detailOverlay.Visible
				),
			OnMapPanDelta,
			OnMapPanEnded
		);


		_centerMapButton =
			new Button
			{
				Name =
					"CenterMapButton",

				Text =
					"◎ CENTER",

				AnchorLeft =
					1.0f,

				AnchorTop =
					1.0f,

				AnchorRight =
					1.0f,

				AnchorBottom =
					1.0f,

				OffsetLeft =
					-145.0f,

				OffsetTop =
					-62.0f,

				OffsetRight =
					-14.0f,

				OffsetBottom =
					-14.0f,

				FocusMode =
					Control.FocusModeEnum.None,

				ZIndex =
					20
			};

		ApplyGoldButtonStyle(
			_centerMapButton
		);

		_centerMapButton.Pressed +=
			() =>
			{
				if (_panInput.ShouldSuppressClick)
					return;

				PlayHaptic();

				CenterOnSector(
					_service.CurrentSectorIndex,
					true
				);
			};

		_mapViewport.AddChild(
			_centerMapButton
		);


		_mapViewport.Resized +=
			() =>
			{
				ClampMapPosition();
				RefreshMapWorld();
			};
	}


	private void OnMapPanDelta(
		Vector2 delta)
	{
		if (
			_mapWorld == null
			|| !GodotObject.IsInstanceValid(
				_mapWorld
			)
		)
		{
			return;
		}

		_mapWorld.Position +=
			delta;

		ClampMapPosition();
		RefreshMapWorld();
	}


	private void OnMapPanEnded()
	{
		UpdateFocusedSectorFromCamera();
		RefreshRuntime();
		RefreshMapWorld();
	}


	private void CenterOnSector(
		int sectorIndex,
		bool playHaptic)
	{
		if (
			_mapViewport == null
			|| _mapWorld == null
			|| _mapViewport.Size.X <= 1.0f
			|| _mapViewport.Size.Y <= 1.0f
		)
		{
			return;
		}

		sectorIndex =
			Math.Clamp(
				sectorIndex,
				0,
				Math.Max(
					0,
					_service.SectorCount - 1
				)
			);

		if (playHaptic)
		{
			PlayHaptic();
		}

		_panInput?.StopMotion();

		Vector2 sectorCenter =
			GetSectorWorldTopLeft(
				sectorIndex
			)
			+ new Vector2(
				SectorMapSize / 2.0f,
				SectorMapSize / 2.0f
			);

		_mapWorld.Position =
			_mapViewport.Size / 2.0f
			- sectorCenter;

		_cameraInitialized =
			true;

		ClampMapPosition();
		RefreshMapWorld();
	}


	private void UpdateFocusedSectorFromCamera()
	{
		if (
			_service.SectorCount <= 0
			|| _mapViewport.Size.X <= 1.0f
			|| _mapViewport.Size.Y <= 1.0f
		)
		{
			return;
		}

		Vector2 cameraCenterInWorld =
			-_mapWorld.Position
			+ _mapViewport.Size / 2.0f;

		int nearest =
			0;

		float nearestDistanceSquared =
			float.MaxValue;

		for (
			int i = 0;
			i < _service.SectorCount;
			i++
		)
		{
			Vector2 center =
				GetSectorWorldTopLeft(
					i
				)
				+ new Vector2(
					SectorMapSize / 2.0f,
					SectorMapSize / 2.0f
				);

			float distanceSquared =
				cameraCenterInWorld.DistanceSquaredTo(
					center
				);

			if (distanceSquared >= nearestDistanceSquared)
				continue;

			nearestDistanceSquared =
				distanceSquared;

			nearest =
				i;
		}

		_service.SelectSector(
			nearest
		);
	}


	private void ClampMapPosition()
	{
		if (
			_mapViewport == null
			|| _mapWorld == null
			|| _mapViewport.Size.X <= 1.0f
			|| _mapViewport.Size.Y <= 1.0f
		)
		{
			return;
		}

		Rect2 bounds =
			GetMapContentBounds();

		Vector2 viewportSize =
			_mapViewport.Size;

		Vector2 position =
			_mapWorld.Position;

		float minX =
			viewportSize.X
			- bounds.End.X;

		float maxX =
			-bounds.Position.X;

		float minY =
			viewportSize.Y
			- bounds.End.Y;

		float maxY =
			-bounds.Position.Y;

		if (minX > maxX)
		{
			position.X =
				viewportSize.X / 2.0f
				- (
					bounds.Position.X
					+ bounds.Size.X / 2.0f
				);
		}
		else
		{
			position.X =
				Math.Clamp(
					position.X,
					minX,
					maxX
				);
		}

		if (minY > maxY)
		{
			position.Y =
				viewportSize.Y / 2.0f
				- (
					bounds.Position.Y
					+ bounds.Size.Y / 2.0f
				);
		}
		else
		{
			position.Y =
				Math.Clamp(
					position.Y,
					minY,
					maxY
				);
		}

		_mapWorld.Position =
			position;
	}


	private Rect2 GetMapContentBounds()
	{
		if (
			_cachedBoundsSectorCount
			== _service.SectorCount
		)
		{
			return _cachedMapBounds;
		}

		int countIncludingExpansion =
			Math.Max(
				1,
				_service.SectorCount + 1
			);

		Vector2 first =
			GetSectorWorldTopLeft(
				0
			);

		float minX =
			first.X;

		float minY =
			first.Y;

		float maxX =
			first.X + SectorMapSize;

		float maxY =
			first.Y + SectorMapSize;

		for (
			int i = 1;
			i < countIncludingExpansion;
			i++
		)
		{
			Vector2 topLeft =
				GetSectorWorldTopLeft(
					i
				);

			minX =
				MathF.Min(
					minX,
					topLeft.X
				);

			minY =
				MathF.Min(
					minY,
					topLeft.Y
				);

			maxX =
				MathF.Max(
					maxX,
					topLeft.X + SectorMapSize
				);

			maxY =
				MathF.Max(
					maxY,
					topLeft.Y + SectorMapSize
				);
		}

		_cachedMapBounds =
			new Rect2(
				new Vector2(
					minX - MapBoundsMargin,
					minY - MapBoundsMargin
				),
				new Vector2(
					maxX - minX
						+ MapBoundsMargin * 2.0f,
					maxY - minY
						+ MapBoundsMargin * 2.0f
				)
			);

		_cachedBoundsSectorCount =
			_service.SectorCount;

		return _cachedMapBounds;
	}


	private void EnsureSectorCoordinateCache()
	{
		if (
			_cachedSectorCoordinateCount
			> _service.SectorCount
		)
		{
			_sectorIndexByGrid.Clear();
			_cachedSectorCoordinateCount =
				0;
		}

		while (
			_cachedSectorCoordinateCount
			< _service.SectorCount
		)
		{
			int index =
				_cachedSectorCoordinateCount;

			_sectorIndexByGrid[
				GetSectorGridPosition(
					index
				)
			] =
				index;

			_cachedSectorCoordinateCount++;
		}
	}


	private void RefreshMapWorld()
	{
		if (
			_mapViewport == null
			|| _mapWorld == null
			|| _mapViewport.Size.X <= 1.0f
			|| _mapViewport.Size.Y <= 1.0f
		)
		{
			return;
		}

		if (!_cameraInitialized)
		{
			CenterOnSector(
				_service.CurrentSectorIndex,
				false
			);

			return;
		}

		Rect2 visibleWorld =
			new Rect2(
				-_mapWorld.Position,
				_mapViewport.Size
			)
			.Grow(
				SectorVirtualizationMargin
			);

		EnsureSectorCoordinateCache();

		HashSet<int> wanted =
			[];

		/*
		 * Virtualization is coordinate-based: only grid cells intersecting the
		 * camera + margin are considered. Runtime therefore stays proportional to
		 * what is visible instead of to the lifetime Sector count.
		 */
		Rect2 centerSearch =
			visibleWorld.Grow(
				SectorMapSize / 2.0f
			);

		int minGridX =
			(int)MathF.Floor(
				(
					centerSearch.Position.X
					- WorldOrigin.X
				)
				/ SectorMapSpacing
			);

		int maxGridX =
			(int)MathF.Ceiling(
				(
					centerSearch.End.X
					- WorldOrigin.X
				)
				/ SectorMapSpacing
			);

		int minGridY =
			(int)MathF.Floor(
				(
					centerSearch.Position.Y
					- WorldOrigin.Y
				)
				/ SectorMapSpacing
			);

		int maxGridY =
			(int)MathF.Ceiling(
				(
					centerSearch.End.Y
					- WorldOrigin.Y
				)
				/ SectorMapSpacing
			);

		for (
			int gridY = minGridY;
			gridY <= maxGridY;
			gridY++
		)
		{
			for (
				int gridX = minGridX;
				gridX <= maxGridX;
				gridX++
			)
			{
				if (!_sectorIndexByGrid.TryGetValue(
					new Vector2I(
						gridX,
						gridY
					),
					out int sectorIndex
				))
				{
					continue;
				}

				wanted.Add(
					sectorIndex
				);

				EnsureSectorMapView(
					sectorIndex
				);
			}
		}

		List<int> toRemove =
			[];

		foreach (
			KeyValuePair<int, SectorMapView> pair
				in _sectorMapViews
		)
		{
			if (!wanted.Contains(
				pair.Key
			))
			{
				toRemove.Add(
					pair.Key
				);
			}
		}

		foreach (
			int index
				in toRemove
		)
		{
			SectorMapView view =
				_sectorMapViews[
					index
				];

			_sectorMapViews.Remove(
				index
			);

			view.Root.QueueFree();
		}

		foreach (
			SectorMapView view
				in _sectorMapViews.Values
		)
		{
			RefreshSectorMapView(
				view
			);
		}

		RefreshExpansionSector(
			visibleWorld
		);

		RefreshInterSectorConnections(
			visibleWorld
		);
	}


	private void EnsureSectorMapView(
		int sectorIndex)
	{
		if (_sectorMapViews.ContainsKey(
			sectorIndex
		))
		{
			return;
		}

		Control root =
			new()
			{
				Name =
					"Sector_"
					+ (sectorIndex + 1),

				Position =
					GetSectorWorldTopLeft(
						sectorIndex
					),

				Size =
					new Vector2(
						SectorMapSize,
						SectorMapSize
					),

				CustomMinimumSize =
					new Vector2(
						SectorMapSize,
						SectorMapSize
					),

				MouseFilter =
					Control.MouseFilterEnum.Pass,

				ZIndex =
					2
			};

		_mapWorld.AddChild(
			root
		);


		PanelContainer frame =
			new()
			{
				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};

		frame.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);

		root.AddChild(
			frame
		);


		SectorLocalLinkLayer links =
			new()
			{
				MouseFilter =
					Control.MouseFilterEnum.Ignore,

				ZIndex =
					1
			};

		links.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);

		root.AddChild(
			links
		);


		Label title =
			CreateLabel(
				17,
				"SECTOR "
					+ (sectorIndex + 1)
			);

		title.Position =
			new Vector2(
				18,
				12
			);

		title.Size =
			new Vector2(
				SectorMapSize - 36.0f,
				28.0f
			);

		title.AddThemeColorOverride(
			"font_color",
			Accent.Lightened(
				0.10f
			)
		);

		root.AddChild(
			title
		);


		Label output =
			CreateLabel(
				11,
				"0 Matter/s"
			);

		output.Position =
			new Vector2(
				18,
				39
			);

		output.Size =
			new Vector2(
				SectorMapSize - 36.0f,
				24.0f
			);

		output.Modulate =
			new Color(
				0.76f,
				0.72f,
				0.62f,
				1.0f
			);

		root.AddChild(
			output
		);


		Label currentBadge =
			CreateLabel(
				10,
				"CURRENT"
			);

		currentBadge.Position =
			new Vector2(
				SectorMapSize - 98.0f,
				14.0f
			);

		currentBadge.Size =
			new Vector2(
				82.0f,
				24.0f
			);

		currentBadge.AddThemeColorOverride(
			"font_color",
			Accent
		);

		root.AddChild(
			currentBadge
		);


		if (sectorIndex == 0)
		{
			const float coreSize =
				108.0f;

			Vector2 corePosition =
				new Vector2(
					SectorMapSize / 2.0f
						- coreSize / 2.0f,
					SectorMapSize / 2.0f
						- coreSize / 2.0f
				);

			Control holder =
				new()
				{
					Position =
						corePosition,

					Size =
						new Vector2(
							coreSize,
							coreSize
						),

					ClipContents =
						true,

					MouseFilter =
						Control.MouseFilterEnum.Ignore,

					ZIndex =
						3
				};

			root.AddChild(
				holder
			);

			TextureRect image =
				new()
				{
					Texture =
						CoreTexture,

					ExpandMode =
						TextureRect.ExpandModeEnum.IgnoreSize,

					StretchMode =
						TextureRect.StretchModeEnum.KeepAspectCentered,

					MouseFilter =
						Control.MouseFilterEnum.Ignore
				};

			image.SetAnchorsAndOffsetsPreset(
				Control.LayoutPreset.FullRect
			);

			holder.AddChild(
				image
			);

			Button coreButton =
				new()
				{
					Position =
						corePosition,

					Size =
						new Vector2(
							coreSize,
							coreSize
						),

					FocusMode =
						Control.FocusModeEnum.None,

					TooltipText =
						"Singularity Core",

					ZIndex =
						4
				};

			ApplyTransparentButtonStyle(
				coreButton
			);

			coreButton.Pressed +=
				() =>
				{
					if (_panInput.ShouldSuppressClick)
						return;

					_service.SelectSector(
						0
					);

					RefreshRuntime();
					RefreshMapWorld();
					PlayHaptic();
					OpenCoreOverlay();
				};

			root.AddChild(
				coreButton
			);
		}
		else
		{
			Button hub =
				new()
				{
					Text =
						"S"
						+ (sectorIndex + 1),

					Position =
						new Vector2(
							SectorMapSize / 2.0f - 46.0f,
							SectorMapSize / 2.0f - 46.0f
						),

					Size =
						new Vector2(
							92.0f,
							92.0f
						),

					FocusMode =
						Control.FocusModeEnum.None,

					ZIndex =
						3
				};

			ApplyGoldButtonStyle(
				hub
			);

			hub.Pressed +=
				() =>
				{
					if (_panInput.ShouldSuppressClick)
						return;

					_service.SelectSector(
						sectorIndex
					);

					RefreshRuntime();
					RefreshMapWorld();
				};

			root.AddChild(
				hub
			);
		}


		List<NodeButtonView> nodeViews =
			[];

		for (
			int nodeIndex = 0;
			nodeIndex < SingularityService.NodesPerSector;
			nodeIndex++
		)
		{
			int capturedNode =
				nodeIndex;

			Vector2 center =
				GetMapNodeCenter(
					nodeIndex
				);

			Button button =
				new()
				{
					Position =
						center
						- new Vector2(
							SectorMapNodeButtonSize / 2.0f,
							SectorMapNodeButtonSize / 2.0f
						),

					Size =
						new Vector2(
							SectorMapNodeButtonSize,
							SectorMapNodeButtonSize
						),

					FocusMode =
						Control.FocusModeEnum.None,

					ClipContents =
						true,

					ZIndex =
						4
				};

			button.Pressed +=
				() =>
				{
					if (_panInput.ShouldSuppressClick)
						return;

					_service.SelectSector(
						sectorIndex
					);

					RefreshRuntime();
					RefreshMapWorld();
					PlayHaptic();
					OpenNodeOverlay(
						capturedNode
					);
				};

			root.AddChild(
				button
			);


			TextureRect icon =
				new()
				{
					Position =
						new Vector2(
							8,
							6
						),

					Size =
						new Vector2(
							SectorMapNodeButtonSize - 16.0f,
							SectorMapNodeButtonSize - 28.0f
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
					9,
					""
				);

			level.Position =
				new Vector2(
					4,
					SectorMapNodeButtonSize - 23.0f
				);

			level.Size =
				new Vector2(
					SectorMapNodeButtonSize - 8.0f,
					18.0f
				);

			button.AddChild(
				level
			);

			nodeViews.Add(
				new NodeButtonView(
					button,
					icon,
					level
				)
			);
		}


		SectorMapView view =
			new(
				sectorIndex,
				root,
				frame,
				links,
				title,
				output,
				currentBadge,
				nodeViews
			);

		_sectorMapViews[
			sectorIndex
		] =
			view;

		RefreshSectorMapView(
			view
		);
	}


	private void RefreshSectorMapView(
		SectorMapView view)
	{
		SingularitySectorData sector =
			_service.GetSector(
				view.SectorIndex
			);

		bool isCurrent =
			view.SectorIndex
			== _service.CurrentSectorIndex;

		view.Frame.AddThemeStyleboxOverride(
			"panel",
			CreatePanelStyle(
				isCurrent
					? new Color(
						0.050f,
						0.035f,
						0.012f,
						0.97f
					)
					: new Color(
						0.010f,
						0.013f,
						0.020f,
						0.95f
					),
				isCurrent
					? Accent
					: new Color(
						0.24f,
						0.20f,
						0.13f,
						0.88f
					),
				24
			)
		);

		view.CurrentBadge.Visible =
			isCurrent;

		view.Title.Text =
			"SECTOR "
			+ (view.SectorIndex + 1);

		view.Output.Text =
			NumberFormatter.Format(
				_service.GetSectorOutputPerSecond(
					view.SectorIndex
				)
			)
			+ " Matter/s";

		bool[] active =
			new bool[
				SingularityService.NodesPerSector
			];

		for (
			int i = 0;
			i < view.Nodes.Count;
			i++
		)
		{
			SingularityNodeData node =
				sector.Nodes[
					i
				];

			NodeButtonView nodeView =
				view.Nodes[
					i
				];

			active[
				i
			] =
				node.Type
				!= SingularityNodeType.Empty;

			nodeView.Icon.Texture =
				GetNodeTexture(
					node.Type
				);

			Color accent =
				GetNodeColor(
					node.Type
				);

			nodeView.Root.AddThemeStyleboxOverride(
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

			nodeView.Level.Text =
				node.Type
					== SingularityNodeType.Empty
						? "EMPTY"
						: "LV "
							+ node.Level;
		}

		view.Links.SetActiveNodes(
			active
		);
	}


	private void RefreshExpansionSector(
		Rect2 visibleWorld)
	{
		int nextSectorIndex =
			_service.SectorCount;

		Rect2 rect =
			new Rect2(
				GetSectorWorldTopLeft(
					nextSectorIndex
				),
				new Vector2(
					SectorMapSize,
					SectorMapSize
				)
			);

		if (!visibleWorld.Intersects(
			rect
		))
		{
			if (_expansionSectorView != null)
			{
				_expansionSectorView.QueueFree();
				_expansionSectorView =
					null;
			}

			return;
		}

		if (
			_expansionSectorView != null
			&& GodotObject.IsInstanceValid(
				_expansionSectorView
			)
		)
		{
			_expansionSectorView.Position =
				rect.Position;

			UpdateExpansionSectorText();
			return;
		}

		PanelContainer panel =
			new()
			{
				Name =
					"NextSectorExpansion",

				Position =
					rect.Position,

				Size =
					rect.Size,

				CustomMinimumSize =
					rect.Size,

				ZIndex =
					2
			};

		panel.AddThemeStyleboxOverride(
			"panel",
			CreatePanelStyle(
				new Color(
					0.040f,
					0.028f,
					0.010f,
					0.72f
				),
				new Color(
					Accent.R,
					Accent.G,
					Accent.B,
					0.58f
				),
				24
			)
		);

		_mapWorld.AddChild(
			panel
		);

		Button button =
			new()
			{
				Name =
					"UnlockNextSectorButton",

				FocusMode =
					Control.FocusModeEnum.None
			};

		button.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);

		button.AddThemeFontSizeOverride(
			"font_size",
			18
		);

		ApplyGoldButtonStyle(
			button
		);

		button.Pressed +=
			() =>
			{
				if (_panInput.ShouldSuppressClick)
					return;

				PlayHaptic();

				int unlockedIndex =
					_service.SectorCount;

				SingularityActionResult result =
					_service.UnlockNextSector();

				MessageRequested?.Invoke(
					result.Message
				);

				if (!result.Changed)
				{
					UpdateExpansionSectorText();
					return;
				}

				RefreshAll();

				Callable
					.From(
						() =>
						{
							CenterOnSector(
								unlockedIndex,
								false
							);

							RefreshMapWorld();
						}
					)
					.CallDeferred();
			};

		panel.AddChild(
			button
		);

		_expansionSectorView =
			panel;

		UpdateExpansionSectorText();
	}


	private void UpdateExpansionSectorText()
	{
		if (
			_expansionSectorView == null
			|| !GodotObject.IsInstanceValid(
				_expansionSectorView
			)
			|| _expansionSectorView.GetChildCount() == 0
			|| _expansionSectorView.GetChild(0)
				is not Button button
		)
		{
			return;
		}

		int nextNumber =
			_service.SectorCount + 1;

		bool ready =
			_service.CanUnlockNextSector();

		button.Text =
			ready
				? "+  EXPAND NETWORK\nSECTOR "
					+ nextNumber
					+ "\n"
					+ NumberFormatter.Format(
						_service.GetNextSectorUnlockCost()
					)
					+ " MATTER"
				: "SECTOR "
					+ nextNumber
					+ " LOCKED\nFILL ALL 8 NODES IN SECTOR "
					+ (
						_service.GetFrontierSectorIndex()
						+ 1
					);
	}


	private void RefreshInterSectorConnections(
		Rect2 visibleWorld)
	{
		EnsureSectorCoordinateCache();

		List<MapConnectionSegment> segments =
			[];

		foreach (
			int sectorIndex
				in _sectorMapViews.Keys
		)
		{
			Vector2I position =
				GetSectorGridPosition(
					sectorIndex
				);

			TryAddInterSectorConnection(
				sectorIndex,
				position + new Vector2I(
					1,
					0
				),
				2,
				6,
				_sectorIndexByGrid,
				visibleWorld,
				segments
			);

			TryAddInterSectorConnection(
				sectorIndex,
				position + new Vector2I(
					0,
					1
				),
				4,
				0,
				_sectorIndexByGrid,
				visibleWorld,
				segments
			);
		}

		_interSectorLinks.SetSegments(
			segments
		);
	}


	private void TryAddInterSectorConnection(
		int fromSector,
		Vector2I neighborPosition,
		int fromNode,
		int toNode,
		IReadOnlyDictionary<Vector2I, int> byPosition,
		Rect2 visibleWorld,
		List<MapConnectionSegment> segments)
	{
		if (!byPosition.TryGetValue(
			neighborPosition,
			out int toSector
		))
		{
			return;
		}

		Vector2 from =
			GetSectorWorldTopLeft(
				fromSector
			)
			+ GetMapNodeCenter(
				fromNode
			);

		Vector2 to =
			GetSectorWorldTopLeft(
				toSector
			)
			+ GetMapNodeCenter(
				toNode
			);

		Rect2 segmentBounds =
			new Rect2(
				new Vector2(
					MathF.Min(
						from.X,
						to.X
					),
					MathF.Min(
						from.Y,
						to.Y
					)
				),
				new Vector2(
					MathF.Abs(
						to.X - from.X
					)
						+ 2.0f,
					MathF.Abs(
						to.Y - from.Y
					)
						+ 2.0f
				)
			);

		if (!visibleWorld.Grow(
			180.0f
		).Intersects(
			segmentBounds
		))
		{
			return;
		}

		bool active =
			_service.GetNode(
				fromSector,
				fromNode
			).Type
				!= SingularityNodeType.Empty
			&& _service.GetNode(
					toSector,
					toNode
				).Type
					!= SingularityNodeType.Empty;

		segments.Add(
			new MapConnectionSegment(
				from,
				to,
				active
			)
		);
	}


	private static Vector2 GetMapNodeCenter(
		int index)
	{
		double angle =
			-Math.PI / 2.0
			+ index
			* (
				Math.PI * 2.0
				/ SingularityService.NodesPerSector
			);

		return new Vector2(
			SectorMapSize / 2.0f
				+ (float)(
					Math.Cos(
						angle
					)
					* SectorMapNodeRadius
				),
			SectorMapSize / 2.0f
				+ (float)(
					Math.Sin(
						angle
					)
					* SectorMapNodeRadius
				)
		);
	}


	private static Vector2 GetSectorWorldTopLeft(
		int sectorIndex)
	{
		Vector2I grid =
			GetSectorGridPosition(
				sectorIndex
			);

		Vector2 center =
			WorldOrigin
			+ new Vector2(
				grid.X * SectorMapSpacing,
				grid.Y * SectorMapSpacing
			);

		return center
			- new Vector2(
				SectorMapSize / 2.0f,
				SectorMapSize / 2.0f
			);
	}


	/*
	 * Deterministic square spiral:
	 *
	 *          6 -- 7 -- 8
	 *          |         |
	 *          5    0 -- 1
	 *          |         |
	 *          4 -- 3 -- 2
	 *
	 * Every next Sector is cardinally adjacent to the previous one. Because the
	 * position is derived only from the Sector index, old saves need no X/Y
	 * migration and an arbitrary number of Sectors remains possible.
	 */
	private static Vector2I GetSectorGridPosition(
		int sectorIndex)
	{
		sectorIndex =
			Math.Max(
				0,
				sectorIndex
			);

		/*
		 * Cache the deterministic spiral incrementally. This keeps map refreshes
		 * O(number of sectors) instead of repeatedly recalculating every path from
		 * Sector 1, which matters once an endgame save contains hundreds or
		 * thousands of Sectors.
		 */
		while (
			SectorGridCache.Count
			<= sectorIndex
		)
		{
			_spiralCursor +=
				_spiralDirection;

			SectorGridCache.Add(
				_spiralCursor
			);

			_spiralStepProgress++;

			if (
				_spiralStepProgress
				< _spiralStepLength
			)
			{
				continue;
			}

			_spiralStepProgress =
				0;

			_spiralDirection =
				new Vector2I(
					-_spiralDirection.Y,
					_spiralDirection.X
				);

			_spiralLegsAtCurrentLength++;

			if (
				_spiralLegsAtCurrentLength
				< 2
			)
			{
				continue;
			}

			_spiralLegsAtCurrentLength =
				0;

			_spiralStepLength++;
		}

		return SectorGridCache[
			sectorIndex
		];
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


		double sellRefund =
			_service.GetNodeSellRefund(
				_service.CurrentSectorIndex,
				_selectedNode
			);


		Button sell =
			new()
			{
				Text =
					"SELL NODE\nREFUND "
					+ NumberFormatter.Format(
						sellRefund
					)
					+ " MATTER",

				CustomMinimumSize =
					new Vector2(
						0,
						62
					),

				FocusMode =
					Control.FocusModeEnum.None
			};

		ApplySellButtonStyle(
			sell
		);

		sell.Pressed +=
			() =>
			{
				PlayHaptic();
				OpenSellNodeConfirmation();
			};

		_detailContent.AddChild(
			sell
		);
	}


	private void OpenSellNodeConfirmation()
	{
		if (_selectedNode < 0)
			return;

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
			BuildNodeOverlay();
			return;
		}

		SingularityNodeType type =
			node.Type;

		double refund =
			_service.GetNodeSellRefund(
				_service.CurrentSectorIndex,
				_selectedNode
			);

		ClearDetailContent();


		Label title =
			CreateLabel(
				25,
				"SELL "
					+ type.ToString().ToUpperInvariant()
					+ " NODE?"
			);

		title.AddThemeColorOverride(
			"font_color",
			new Color(
				1.0f,
				0.42f,
				0.30f,
				1.0f
			)
		);

		_detailContent.AddChild(
			title
		);


		_detailContent.AddChild(
			CreateOverlayImage(
				GetNodeTexture(
					type
				),
				160
			)
		);


		Label info =
			CreateLabel(
				14,
				"The slot becomes empty again.\nYou receive one third of the Matter invested in this Node.\n\nRefund: "
					+ NumberFormatter.Format(
						refund
					)
					+ " Matter"
			);

		info.AutowrapMode =
			TextServer.AutowrapMode.WordSmart;

		_detailContent.AddChild(
			info
		);


		Button confirm =
			new()
			{
				Text =
					"CONFIRM SELL\n+"
					+ NumberFormatter.Format(
						refund
					)
					+ " MATTER",

				CustomMinimumSize =
					new Vector2(
						0,
						68
					),

				FocusMode =
					Control.FocusModeEnum.None
			};

		ApplySellButtonStyle(
			confirm
		);

		confirm.Pressed +=
			SellSelectedNode;

		_detailContent.AddChild(
			confirm
		);


		Button cancel =
			new()
			{
				Text =
					"CANCEL",

				CustomMinimumSize =
					new Vector2(
						0,
						54
					),

				FocusMode =
					Control.FocusModeEnum.None
			};

		ApplyNodeTypeButtonStyle(
			cancel,
			new Color(
				0.44f,
				0.48f,
				0.56f,
				1.0f
			)
		);

		cancel.Pressed +=
			() =>
			{
				PlayHaptic();
				BuildNodeOverlay();
			};

		_detailContent.AddChild(
			cancel
		);
	}


	private void SellSelectedNode()
	{
		if (_selectedNode < 0)
			return;

		PlayHaptic();

		SingularityActionResult result =
			_service.SellNode(
				_service.CurrentSectorIndex,
				_selectedNode
			);

		MessageRequested?.Invoke(
			result.Message
		);

		if (!result.Changed)
		{
			BuildNodeOverlay();
			return;
		}

		CloseDetailOverlay();
		RefreshAll();
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
		RefreshMapWorld();
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
			(_service.CurrentSectorIndex + 1)
				.ToString();

		_outputValue.Text =
			NumberFormatter.Format(
				_service.GetTotalOutputPerSecond()
			)
			+ "/s";
	}




	// ==================================================
	// BOTTOM BAR THEME
	// ==================================================

	private void ApplySingularityBottomBarLayer()
	{
		if (
			_bottomBar == null
			|| !GodotObject.IsInstanceValid(
				_bottomBar
			)
		)
		{
			return;
		}

		/*
		 * SingularityPage has its own high ZIndex. Without lifting BottomBar as
		 * well, only the portion not covered by the page remains visible.
		 */
		_bottomBar.ZIndex =
			SingularityBottomBarZIndex;

		_bottomBar.MoveToFront();
	}


	private void RestoreNormalBottomBarLayer()
	{
		if (
			_bottomBar == null
			|| !GodotObject.IsInstanceValid(
				_bottomBar
			)
		)
		{
			return;
		}

		_bottomBar.ZIndex =
			_normalBottomBarZIndex;
	}


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




	private void ApplySafeArea()
	{
		float safeTop =
			GetSafeTopInset();

		float bottomBarHeight =
			GetActualBottomBarHeight();

		_header.OffsetTop =
			safeTop
			+ 14.0f;

		if (
			_mapViewport != null
			&& GodotObject.IsInstanceValid(
				_mapViewport
			)
		)
		{
			_mapViewport.OffsetTop =
				safeTop
				+ 166.0f;

			_mapViewport.OffsetBottom =
				-bottomBarHeight;
		}

		_pageBackground.OffsetBottom =
			-bottomBarHeight;

		_gridBackground.OffsetBottom =
			-bottomBarHeight;

		ClampMapPosition();
		RefreshMapWorld();
	}


	private float GetActualBottomBarHeight()
	{
		if (
			_bottomBar != null
			&& GodotObject.IsInstanceValid(
				_bottomBar
			)
			&& _bottomBar.Size.Y > 1.0f
		)
		{
			return _bottomBar.Size.Y;
		}

		return DefaultBottomBarHeight;
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


	private static void ApplySellButtonStyle(
		Button button)
	{
		Color accent =
			new Color(
				0.92f,
				0.24f,
				0.16f,
				1.0f
			);

		ApplyNodeTypeButtonStyle(
			button,
			accent
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



	private sealed record SectorMapView(
		int SectorIndex,
		Control Root,
		PanelContainer Frame,
		SectorLocalLinkLayer Links,
		Label Title,
		Label Output,
		Label CurrentBadge,
		List<NodeButtonView> Nodes
	);


	private readonly record struct MapConnectionSegment(
		Vector2 From,
		Vector2 To,
		bool Active
	);


	private sealed partial class SectorLocalLinkLayer
		: Control
	{
		private bool[] _active =
			new bool[
				SingularityService.NodesPerSector
			];


		public void SetActiveNodes(
			bool[] active)
		{
			_active =
				active;

			QueueRedraw();
		}


		public override void _Draw()
		{
			Vector2 center =
				new Vector2(
					SectorMapSize / 2.0f,
					SectorMapSize / 2.0f
				);

			for (
				int i = 0;
				i < SingularityService.NodesPerSector;
				i++
			)
			{
				Vector2 node =
					GetMapNodeCenter(
						i
					);

				bool active =
					i < _active.Length
					&& _active[
						i
					];

				DrawConnection(
					center,
					node,
					active
						? Accent
						: new Color(
							0.22f,
							0.24f,
							0.30f,
							0.35f
						),
					active
					);
			}

			for (
				int i = 0;
				i < SingularityService.NodesPerSector;
				i++
			)
			{
				int next =
					(
						i + 1
					)
					% SingularityService.NodesPerSector;

				bool active =
					i < _active.Length
					&& next < _active.Length
					&& _active[
						i
					]
					&& _active[
						next
					];

				if (!active)
					continue;

				DrawConnection(
					GetMapNodeCenter(
						i
					),
					GetMapNodeCenter(
						next
					),
					Accent,
					true
				);
			}
		}


		private void DrawConnection(
			Vector2 from,
			Vector2 to,
			Color color,
			bool active)
		{
			DrawLine(
				from,
				to,
				new Color(
					color.R,
					color.G,
					color.B,
					active
						? 0.18f
						: 0.08f
				),
				active
					? 10.0f
					: 4.0f,
				true
			);

			DrawLine(
				from,
				to,
				color,
				active
					? 3.2f
					: 1.2f,
				true
			);
		}
	}


	private sealed partial class InterSectorLinkLayer
		: Control
	{
		private List<MapConnectionSegment> _segments =
			[];


		public void SetSegments(
			IReadOnlyList<MapConnectionSegment> segments)
		{
			_segments =
				new List<MapConnectionSegment>(
					segments
				);

			QueueRedraw();
		}


		public override void _Draw()
		{
			foreach (
				MapConnectionSegment segment
					in _segments
			)
			{
				Color color =
					segment.Active
						? new Color(
							1.0f,
							0.72f,
							0.20f,
							0.98f
						)
						: new Color(
							0.48f,
							0.34f,
							0.12f,
							0.54f
						);

				DrawLine(
					segment.From,
					segment.To,
					new Color(
						color.R,
						color.G,
						color.B,
						segment.Active
							? 0.22f
							: 0.10f
					),
					segment.Active
						? 18.0f
						: 9.0f,
					true
				);

				DrawLine(
					segment.From,
					segment.To,
					color,
					segment.Active
						? 5.5f
						: 2.4f,
					true
				);
			}
		}
	}


	private sealed partial class SingularityPanInput
		: Control
	{
		private const float DragThreshold =
			10.0f;

		private Control? _viewport;

		private Func<bool>? _canPan;

		private Action<Vector2>? _panDelta;

		private Action? _panEnded;

		private bool _trackingTouch;

		private int _touchIndex =
			-1;

		private bool _trackingMouse;

		private bool _dragging;

		private Vector2 _totalDrag;

		private Vector2 _velocity;

		private ulong _suppressClickUntil;


		public bool ShouldSuppressClick =>
			Time.GetTicksMsec()
			< _suppressClickUntil;


		public void Configure(
			Control viewport,
			Func<bool> canPan,
			Action<Vector2> panDelta,
			Action panEnded)
		{
			_viewport =
				viewport;

			_canPan =
				canPan;

			_panDelta =
				panDelta;

			_panEnded =
				panEnded;

			MouseFilter =
				Control.MouseFilterEnum.Ignore;

			SetProcess(
				true
			);
		}


		public void StopMotion()
		{
			_velocity =
				Vector2.Zero;

			_trackingTouch =
				false;

			_trackingMouse =
				false;

			_dragging =
				false;
		}


		public override void _Input(
			InputEvent @event)
		{
			if (
				_viewport == null
				|| _canPan == null
				|| !_canPan()
			)
			{
				return;
			}

			if (
				@event is InputEventScreenTouch touch
			)
			{
				HandleTouch(
					touch
				);

				return;
			}

			if (
				@event is InputEventScreenDrag drag
			)
			{
				HandleTouchDrag(
					drag
				);

				return;
			}

			if (
				@event is InputEventMouseButton mouseButton
				&& mouseButton.ButtonIndex
					== MouseButton.Left
			)
			{
				HandleMouseButton(
					mouseButton
				);

				return;
			}

			if (
				@event is InputEventMouseMotion mouseMotion
				&& _trackingMouse
			)
			{
				HandleMouseMotion(
					mouseMotion
				);
			}
		}


		public override void _Process(
			double delta)
		{
			if (
				_trackingTouch
				|| _trackingMouse
				|| _velocity.LengthSquared()
					< 64.0f
				|| _canPan == null
				|| !_canPan()
			)
			{
				return;
			}

			_panDelta?.Invoke(
				_velocity
				* (float)delta
			);

			float damping =
				MathF.Pow(
					0.035f,
					(float)delta
				);

			_velocity *=
				damping;

			if (
				_velocity.LengthSquared()
				< 64.0f
			)
			{
				_velocity =
					Vector2.Zero;

				_panEnded?.Invoke();
			}
		}


		private bool IsInsideViewport(
			Vector2 position)
		{
			return _viewport != null
				&& _viewport.GetGlobalRect()
					.HasPoint(
						position
					);
		}


		private void HandleTouch(
			InputEventScreenTouch touch)
		{
			if (touch.Pressed)
			{
				if (!IsInsideViewport(
					touch.Position
				))
				{
					return;
				}

				_trackingTouch =
					true;

				_touchIndex =
					touch.Index;

				_dragging =
					false;

				_totalDrag =
					Vector2.Zero;

				_velocity =
					Vector2.Zero;

				return;
			}

			if (
				!_trackingTouch
				|| touch.Index != _touchIndex
			)
			{
				return;
			}

			if (_dragging)
			{
				_suppressClickUntil =
					Time.GetTicksMsec()
					+ 180;

				GetViewport()
					.SetInputAsHandled();
			}

			_trackingTouch =
				false;

			_touchIndex =
				-1;

			_dragging =
				false;

			_panEnded?.Invoke();
		}


		private void HandleTouchDrag(
			InputEventScreenDrag drag)
		{
			if (
				!_trackingTouch
				|| drag.Index != _touchIndex
			)
			{
				return;
			}

			_totalDrag +=
				drag.Relative;

			if (
				!_dragging
				&& _totalDrag.Length()
					>= DragThreshold
			)
			{
				_dragging =
					true;
			}

			if (!_dragging)
				return;

			_velocity =
				drag.Velocity;

			_panDelta?.Invoke(
				drag.Relative
			);

			GetViewport()
				.SetInputAsHandled();
		}


		private void HandleMouseButton(
			InputEventMouseButton mouseButton)
		{
			if (mouseButton.Pressed)
			{
				if (!IsInsideViewport(
					mouseButton.Position
				))
				{
					return;
				}

				_trackingMouse =
					true;

				_dragging =
					false;

				_totalDrag =
					Vector2.Zero;

				_velocity =
					Vector2.Zero;

				return;
			}

			if (!_trackingMouse)
				return;

			if (_dragging)
			{
				_suppressClickUntil =
					Time.GetTicksMsec()
					+ 180;

				GetViewport()
					.SetInputAsHandled();
			}

			_trackingMouse =
				false;

			_dragging =
				false;

			_panEnded?.Invoke();
		}


		private void HandleMouseMotion(
			InputEventMouseMotion motion)
		{
			_totalDrag +=
				motion.Relative;

			if (
				!_dragging
				&& _totalDrag.Length()
					>= DragThreshold
			)
			{
				_dragging =
					true;
			}

			if (!_dragging)
				return;

			/* Mouse velocity is not directly supplied here; estimate it. */
			_velocity =
				motion.Relative
				* 60.0f;

			_panDelta?.Invoke(
				motion.Relative
			);

			GetViewport()
				.SetInputAsHandled();
		}
	}

	// ==================================================
	// LINES
	// ==================================================



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
