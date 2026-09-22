using Godot;
using System;
using System.Collections.Generic;

namespace IdleAi;

public sealed partial class MapController
{
	private const float CanvasWidth =
		650.0f;


	private const float CanvasHeight =
		1180.0f;


	private const float NodeWidth =
		270.0f;


	private const float NodeHeight =
		178.0f;


	private const int NavigationHapticDurationMs =
		14;


	private const float NavigationHapticStrength =
		0.14f;


	private static readonly Texture2D GarageIcon =
		GD.Load<Texture2D>(
			"res://assets/map/room_garage.png"
		);


	private static readonly Texture2D ServerRoomIcon =
		GD.Load<Texture2D>(
			"res://assets/map/room_server_room.png"
		);


	private static readonly Texture2D ServerRoomLockedIcon =
		GD.Load<Texture2D>(
			"res://assets/map/locked_server_room.png"
		);


	private static readonly Texture2D DataCenterIcon =
		GD.Load<Texture2D>(
			"res://assets/map/room_data_center.png"
		);


	private static readonly Texture2D DataCenterLockedIcon =
		GD.Load<Texture2D>(
			"res://assets/map/locked_data_center.png"
		);


	private static readonly Texture2D QuantumLabIcon =
		GD.Load<Texture2D>(
			"res://assets/map/room_quantum_lab.png"
		);


	private static readonly Texture2D QuantumLabLockedIcon =
		GD.Load<Texture2D>(
			"res://assets/map/locked_quantum_lab.png"
		);


	private static readonly Texture2D CurrentRoomIcon =
		GD.Load<Texture2D>(
			"res://assets/map/room_current.png"
		);


	private static readonly Texture2D ComingSoonIcon =
		GD.Load<Texture2D>(
			"res://assets/map/room_coming_soon.png"
		);



	private readonly Game _root;

	private readonly GameState _state;

	private readonly ProgressionService _progression;


	private Control _page =
		null!;


	private PanelContainer _header =
		null!;


	private ScrollContainer _scroll =
		null!;


	private Control _canvas =
		null!;


	private MapRouteLayer _routeLayer =
		null!;


	private Label _progressLabel =
		null!;


	private readonly List<RoomNodeView>
		_roomNodes =
			[];


	public event Action<int>? RoomSelectedRequested;


	public bool Visible =>
		_page.Visible;


	public MapController(
		Game root,
		GameState state,
		ProgressionService progression)
	{
		_root =
			root;


		_state =
			state;


		_progression =
			progression;
	}


	// ==================================================
	// INITIALIZE
	// ==================================================

	public void Initialize()
	{
		_page =
			_root.GetNode<Control>(
				"MapPage"
			);


		ClearOldMapUi();

		CreateModernMapUi();


		_page.Resized +=
			ApplySafeArea;


		ApplySafeArea();

		Hide();
	}


	private void ClearOldMapUi()
	{
		foreach (
			Node child
			in _page.GetChildren()
		)
		{
			child.QueueFree();
		}
	}


	// ==================================================
	// MAIN UI
	// ==================================================

	private void CreateModernMapUi()
	{
		CreateBackground();

		CreateHeader();

		CreateScrollArea();
	}


	private void CreateBackground()
	{
		ColorRect background =
			new()
			{
				Color =
					new Color(
						0.008f,
						0.018f,
						0.035f,
						0.99f
					),

				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};


		background.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);


		_page.AddChild(
			background
		);


		MapGridBackground grid =
			new()
			{
				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};


		grid.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);


		_page.AddChild(
			grid
		);
	}


	private void CreateHeader()
	{
		_header =
			new PanelContainer
			{
				CustomMinimumSize =
					new Vector2(
						0,
						112
					)
			};


		_header.AnchorRight =
			1.0f;


		_header.OffsetLeft =
			22.0f;


		_header.OffsetTop =
			16.0f;


		_header.OffsetRight =
			-22.0f;


		_header.AddThemeStyleboxOverride(
			"panel",
			CreateHeaderStyle()
		);


		_page.AddChild(
			_header
		);


		MarginContainer margin =
			new();


		margin.AddThemeConstantOverride(
			"margin_left",
			20
		);


		margin.AddThemeConstantOverride(
			"margin_top",
			12
		);


		margin.AddThemeConstantOverride(
			"margin_right",
			20
		);


		margin.AddThemeConstantOverride(
			"margin_bottom",
			12
		);


		_header.AddChild(
			margin
		);


		VBoxContainer box =
			new();


		box.AddThemeConstantOverride(
			"separation",
			4
		);


		margin.AddChild(
			box
		);


		Label title =
			CreateLabel(
				27,
				"ROOM NETWORK"
			);


		title.HorizontalAlignment =
			HorizontalAlignment.Left;


		box.AddChild(
			title
		);


		HBoxContainer bottom =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};


		box.AddChild(
			bottom
		);


		Label subtitle =
			CreateLabel(
				12,
				"EXPLORE • BUILD • UNLOCK • AUTOMATE"
			);


		subtitle.Modulate =
			new Color(
				0.56f,
				0.74f,
				0.90f,
				1.0f
			);


		subtitle.HorizontalAlignment =
			HorizontalAlignment.Left;


		subtitle.SizeFlagsHorizontal =
			Control.SizeFlags.ExpandFill;


		bottom.AddChild(
			subtitle
		);


		_progressLabel =
			CreateLabel(
				12,
				""
			);


		_progressLabel.HorizontalAlignment =
			HorizontalAlignment.Right;


		bottom.AddChild(
			_progressLabel
		);
	}


	private void CreateScrollArea()
	{
		_scroll =
			new ScrollContainer
			{
				HorizontalScrollMode =
					ScrollContainer.ScrollMode.Disabled,

				VerticalScrollMode =
					ScrollContainer.ScrollMode.ShowNever,

				ClipContents =
					true,

				MouseFilter =
					Control.MouseFilterEnum.Pass
			};


		_scroll.AnchorRight =
			1.0f;


		_scroll.AnchorBottom =
			1.0f;


		_scroll.OffsetTop =
			138.0f;


		_scroll.OffsetBottom =
			-8.0f;


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


		_canvas =
			new Control
			{
				CustomMinimumSize =
					new Vector2(
						CanvasWidth,
						CanvasHeight
					),

				MouseFilter =
					Control.MouseFilterEnum.Pass
			};


		center.AddChild(
			_canvas
		);


		_routeLayer =
			new MapRouteLayer
			{
				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};


		_routeLayer.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);


		_canvas.AddChild(
			_routeLayer
		);


		CreateFutureNode();

		CreateRoomNodes();


		_routeLayer.MoveToFront();


		foreach (
			RoomNodeView roomNode
			in _roomNodes
		)
		{
			roomNode.Root.MoveToFront();
		}
	}


	// ==================================================
	// ROOM NODES
	// ==================================================

	private void CreateRoomNodes()
	{
		for (
			int roomIndex = 0;
			roomIndex < _state.Rooms.Count;
			roomIndex++
		)
		{
			Vector2 position =
				GetRoomPosition(
					roomIndex
				);


			RoomNodeView node =
				CreateRoomNode(
					roomIndex,
					position
				);


			_roomNodes.Add(
				node
			);


			_canvas.AddChild(
				node.Root
			);
		}
	}


	private RoomNodeView CreateRoomNode(
		int roomIndex,
		Vector2 position)
	{
		RoomData room =
			_state.Rooms[
				roomIndex
			];


		Color accent =
			RoomThemePalette.GetAccentColor(
				roomIndex
			);


		Button root =
			new()
			{
				Position =
					position,

				CustomMinimumSize =
					new Vector2(
						NodeWidth,
						NodeHeight
					),

				Size =
					new Vector2(
						NodeWidth,
						NodeHeight
					),

				FocusMode =
					Control.FocusModeEnum.None,

				ClipContents =
					true
			};


		ApplyNodeStyle(
			root,
			accent,
			false,
			false
		);


		int target =
			roomIndex;


		root.Pressed +=
			() =>
			{
				PlayNavigationHaptic();


				RoomSelectedRequested?.Invoke(
					target
				);
			};


		Control iconHolder =
			new()
			{
				Position =
					new Vector2(
						12,
						27
					),

				Size =
					new Vector2(
						96,
						96
					),

				ClipContents =
					true,

				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};


		root.AddChild(
			iconHolder
		);


		TextureRect icon =
			new()
			{
				Texture =
					GetUnlockedRoomIcon(
						roomIndex
					),

				ExpandMode =
					TextureRect.ExpandModeEnum.IgnoreSize,

				StretchMode =
					TextureRect.StretchModeEnum.KeepAspectCentered,

				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};


		icon.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);


		iconHolder.AddChild(
			icon
		);


		PanelContainer info =
			new()
			{
				Position =
					new Vector2(
						116,
						18
					),

				Size =
					new Vector2(
						142,
						116
					),

				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};


		info.AddThemeStyleboxOverride(
			"panel",
			CreateInfoPanelStyle()
		);


		root.AddChild(
			info
		);


		MarginContainer infoMargin =
			new();


		infoMargin.AddThemeConstantOverride(
			"margin_left",
			10
		);


		infoMargin.AddThemeConstantOverride(
			"margin_top",
			9
		);


		infoMargin.AddThemeConstantOverride(
			"margin_right",
			10
		);


		infoMargin.AddThemeConstantOverride(
			"margin_bottom",
			9
		);


		info.AddChild(
			infoMargin
		);


		VBoxContainer infoBox =
			new();


		infoBox.AddThemeConstantOverride(
			"separation",
			3
		);


		infoMargin.AddChild(
			infoBox
		);


		Label indexLabel =
			CreateLabel(
				10,
				"ROOM "
				+ (
					roomIndex + 1
				)
			);


		indexLabel.HorizontalAlignment =
			HorizontalAlignment.Left;


		indexLabel.Modulate =
			accent.Lightened(
				0.28f
			);


		infoBox.AddChild(
			indexLabel
		);


		Label nameLabel =
			CreateLabel(
				16,
				room.Name.ToUpperInvariant()
			);


		nameLabel.HorizontalAlignment =
			HorizontalAlignment.Left;


		nameLabel.AutowrapMode =
			TextServer.AutowrapMode.WordSmart;


		nameLabel.CustomMinimumSize =
			new Vector2(
				0,
				45
			);


		infoBox.AddChild(
			nameLabel
		);


		Label stateLabel =
			CreateLabel(
				10,
				""
			);


		stateLabel.HorizontalAlignment =
			HorizontalAlignment.Left;


		stateLabel.AutowrapMode =
			TextServer.AutowrapMode.WordSmart;


		infoBox.AddChild(
			stateLabel
		);


		PanelContainer badge =
			new()
			{
				Position =
					new Vector2(
						28,
						139
					),

				Size =
					new Vector2(
						214,
						30
					),

				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};


		badge.AddThemeStyleboxOverride(
			"panel",
			CreateBadgeStyle(
				accent
			)
		);


		root.AddChild(
			badge
		);


		Label badgeLabel =
			CreateLabel(
				11,
				""
			);


		badge.AddChild(
			badgeLabel
		);


		TextureRect currentMarker =
			new()
			{
				Texture =
					CurrentRoomIcon,

				Position =
					new Vector2(
						8,
						7
					),

				Size =
					new Vector2(
						38,
						38
					),

				ExpandMode =
					TextureRect.ExpandModeEnum.IgnoreSize,

				StretchMode =
					TextureRect.StretchModeEnum.KeepAspectCentered,

				MouseFilter =
					Control.MouseFilterEnum.Ignore,

				Visible =
					false
			};


		root.AddChild(
			currentMarker
		);


		return new RoomNodeView(
			root,
			icon,
			stateLabel,
			badge,
			badgeLabel,
			currentMarker
		);
	}


	private void CreateFutureNode()
	{
		PanelContainer node =
			new()
			{
				Position =
					new Vector2(
						93,
						26
					),

				Size =
					new Vector2(
						205,
						106
					),

				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};


		node.AddThemeStyleboxOverride(
			"panel",
			CreateFutureNodeStyle()
		);


		_canvas.AddChild(
			node
		);


		HBoxContainer row =
			new()
			{
				Alignment =
					BoxContainer.AlignmentMode.Center
			};


		row.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);


		row.AddThemeConstantOverride(
			"separation",
			8
		);


		node.AddChild(
			row
		);


		TextureRect image =
			new()
			{
				Texture =
					ComingSoonIcon,

				CustomMinimumSize =
					new Vector2(
						62,
						62
					),

				ExpandMode =
					TextureRect.ExpandModeEnum.IgnoreSize,

				StretchMode =
					TextureRect.StretchModeEnum.KeepAspectCentered,

				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};


		row.AddChild(
			image
		);


		Label label =
			CreateLabel(
				11,
				"NEXT FRONTIER\nCOMING LATER"
			);


		label.HorizontalAlignment =
			HorizontalAlignment.Left;


		label.Modulate =
			new Color(
				0.62f,
				0.70f,
				0.80f,
				1.0f
			);


		row.AddChild(
			label
		);
	}


	private static Vector2 GetRoomPosition(
		int roomIndex)
	{
		return roomIndex switch
		{
			0 =>
				new Vector2(
					48,
					940
				),

			1 =>
				new Vector2(
					330,
					700
				),

			2 =>
				new Vector2(
					48,
					460
				),

			3 =>
				new Vector2(
					330,
					220
				),

			_ =>
				new Vector2(
					190,
					940
					- roomIndex
					* 220
				)
		};
	}


	// ==================================================
	// ICONS
	// ==================================================

	private static Texture2D GetUnlockedRoomIcon(
		int roomIndex)
	{
		return roomIndex switch
		{
			0 =>
				GarageIcon,

			1 =>
				ServerRoomIcon,

			2 =>
				DataCenterIcon,

			3 =>
				QuantumLabIcon,

			_ =>
				ComingSoonIcon
		};
	}


	private static Texture2D GetLockedRoomIcon(
		int roomIndex)
	{
		return roomIndex switch
		{
			1 =>
				ServerRoomLockedIcon,

			2 =>
				DataCenterLockedIcon,

			3 =>
				QuantumLabLockedIcon,

			_ =>
				GetUnlockedRoomIcon(
					roomIndex
				)
		};
	}


	// ==================================================
	// REFRESH
	// ==================================================

	public void Refresh()
	{
		int unlockedCount =
			0;


		int count =
			Math.Min(
				_roomNodes.Count,
				_state.RoomStates.Count
			);


		for (
			int i = 0;
			i < count;
			i++
		)
		{
			bool unlocked =
				_state.RoomStates[
					i
				].Unlocked;


			bool current =
				i
				== _state.CurrentRoomIndex;


			if (unlocked)
			{
				unlockedCount++;
			}


			Color accent =
				RoomThemePalette.GetAccentColor(
					i
				);


			RoomNodeView node =
				_roomNodes[
					i
				];


			node.Icon.Texture =
				unlocked
					? GetUnlockedRoomIcon(
						i
					)
					: GetLockedRoomIcon(
						i
					);


			node.CurrentMarker.Visible =
				current;


			ApplyNodeStyle(
				node.Root,
				accent,
				current,
				!unlocked
			);


			node.Badge.AddThemeStyleboxOverride(
				"panel",
				unlocked
					? CreateBadgeStyle(
						accent
					)
					: CreateLockedBadgeStyle()
			);


			if (current)
			{
				node.StateLabel.Text =
					"CURRENT LOCATION";


				node.StateLabel.Modulate =
					Colors.White;


				node.BadgeLabel.Text =
					"● CURRENT";
			}
			else if (unlocked)
			{
				node.StateLabel.Text =
					"TAP TO ENTER";


				node.StateLabel.Modulate =
					new Color(
						0.72f,
						0.84f,
						0.94f,
						1.0f
					);


				node.BadgeLabel.Text =
					"ENTER";
			}
			else
			{
				double cost =
					_progression
						.GetRoomUnlockCost(
							i
						);


				node.StateLabel.Text =
					"LOCKED";


				node.StateLabel.Modulate =
					new Color(
						0.64f,
						0.67f,
						0.72f,
						1.0f
					);


				node.BadgeLabel.Text =
					"UNLOCK • "
					+ NumberFormatter.Format(
						cost
					);
			}
		}


		_progressLabel.Text =
			unlockedCount
			+ " / "
			+ _state.Rooms.Count
			+ " ROOMS DISCOVERED";


		UpdateRouteData();
	}


	private void UpdateRouteData()
	{
		List<Vector2> centers =
			[];


		List<Color> colors =
			[];


		List<bool> unlocked =
			[];


		for (
			int i = 0;
			i < _state.Rooms.Count;
			i++
		)
		{
			Vector2 pos =
				GetRoomPosition(
					i
				);


			centers.Add(
				pos
				+ new Vector2(
					NodeWidth / 2.0f,
					NodeHeight / 2.0f
				)
			);


			colors.Add(
				RoomThemePalette
					.GetAccentColor(
						i
					)
			);


			unlocked.Add(
				_state.RoomStates[
					i
				].Unlocked
			);
		}


		_routeLayer.SetData(
			centers,
			colors,
			unlocked
		);
	}


	// ==================================================
	// OPEN / CLOSE
	// ==================================================

	public void Open()
	{
		ApplySafeArea();

		Refresh();


		_page.Show();

		_page.MoveToFront();


		Callable
			.From(
				ScrollToCurrentRoom
			)
			.CallDeferred();
	}


	public void Hide()
	{
		_page.Hide();
	}


	private void ScrollToCurrentRoom()
	{
		if (
			_scroll == null
			|| !GodotObject.IsInstanceValid(
				_scroll
			)
		)
		{
			return;
		}


		Vector2 roomPosition =
			GetRoomPosition(
				_state.CurrentRoomIndex
			);


		double wanted =
			roomPosition.Y
			- _scroll.Size.Y
			* 0.35;


		_scroll.ScrollVertical =
			(int)Math.Max(
				0.0,
				wanted
			);
	}


	// ==================================================
	// SAFE AREA / NOTCH
	// ==================================================

	private void ApplySafeArea()
	{
		if (
			_header == null
			|| _scroll == null
		)
		{
			return;
		}


		float safeTop =
			GetSafeTopInset();


		/*
		 * Background/grid may extend behind the notch.
		 * Interactive/text content starts below it.
		 */
		_header.OffsetTop =
			safeTop
			+ 16.0f;


		_scroll.OffsetTop =
			safeTop
			+ 138.0f;
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


		Rect2 viewportRect =
			_root.GetViewport()
				.GetVisibleRect();


		if (
			windowSize.X <= 0
			|| windowSize.Y <= 0
			|| safeArea.Size.X <= 0
			|| safeArea.Size.Y <= 0
		)
		{
			return 34.0f;
		}


		float scaleY =
			viewportRect.Size.Y
			/ windowSize.Y;


		float top =
			safeArea.Position.Y
			* scaleY;


		/*
		 * Some Android devices report a safe inset that
		 * is slightly too small, so keep the same
		 * minimum fallback used by MobileUiAdapter.
		 */
		return MathF.Max(
			top,
			26.0f
		);
	}


	// ==================================================
	// STYLES
	// ==================================================

	private static void ApplyNodeStyle(
		Button button,
		Color accent,
		bool current,
		bool locked)
	{
		Color normalBackground =
			locked
				? new Color(
					0.035f,
					0.045f,
					0.060f,
					0.96f
				)
				: new Color(
					accent.R * 0.24f,
					accent.G * 0.24f,
					accent.B * 0.24f,
					0.97f
				);


		Color border =
			locked
				? new Color(
					0.24f,
					0.28f,
					0.34f,
					0.72f
				)
				: accent;


		int borderWidth =
			current
				? 4
				: 2;


		button.AddThemeStyleboxOverride(
			"normal",
			CreateNodeStyle(
				normalBackground,
				border,
				borderWidth,
				current
					? 16
					: 8
			)
		);


		button.AddThemeStyleboxOverride(
			"hover",
			CreateNodeStyle(
				locked
					? new Color(
						0.05f,
						0.06f,
						0.08f,
						0.98f
					)
					: new Color(
						accent.R * 0.38f,
						accent.G * 0.38f,
						accent.B * 0.38f,
						0.98f
					),
				border,
				borderWidth,
				14
			)
		);


		button.AddThemeStyleboxOverride(
			"pressed",
			CreateNodeStyle(
				locked
					? normalBackground
					: new Color(
						accent.R * 0.18f,
						accent.G * 0.18f,
						accent.B * 0.18f,
						1.0f
					),
				border,
				borderWidth,
				6
			)
		);


		button.Modulate =
			locked
				? new Color(
					0.82f,
					0.84f,
					0.88f,
					1.0f
				)
				: Colors.White;
	}


	private static StyleBoxFlat CreateNodeStyle(
		Color background,
		Color border,
		int borderWidth,
		int shadowSize)
	{
		return new StyleBoxFlat
		{
			BgColor =
				background,

			BorderColor =
				border,

			BorderWidthLeft =
				borderWidth,

			BorderWidthTop =
				borderWidth,

			BorderWidthRight =
				borderWidth,

			BorderWidthBottom =
				borderWidth,

			CornerRadiusTopLeft =
				24,

			CornerRadiusTopRight =
				24,

			CornerRadiusBottomLeft =
				24,

			CornerRadiusBottomRight =
				24,

			ShadowColor =
				new Color(
					border.R,
					border.G,
					border.B,
					0.22f
				),

			ShadowSize =
				shadowSize
		};
	}


	private static StyleBoxFlat CreateBadgeStyle(
		Color accent)
	{
		return new StyleBoxFlat
		{
			BgColor =
				new Color(
					accent.R * 0.34f,
					accent.G * 0.34f,
					accent.B * 0.34f,
					0.98f
				),

			BorderColor =
				accent,

			BorderWidthLeft =
				2,

			BorderWidthTop =
				2,

			BorderWidthRight =
				2,

			BorderWidthBottom =
				2,

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


	private static StyleBoxFlat CreateLockedBadgeStyle()
	{
		return new StyleBoxFlat
		{
			BgColor =
				new Color(
					0.075f,
					0.085f,
					0.105f,
					0.98f
				),

			BorderColor =
				new Color(
					0.28f,
					0.32f,
					0.38f,
					0.90f
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
				15,

			CornerRadiusTopRight =
				15,

			CornerRadiusBottomLeft =
				15,

			CornerRadiusBottomRight =
				15
		};
	}


	private static StyleBoxFlat CreateInfoPanelStyle()
	{
		return new StyleBoxFlat
		{
			BgColor =
				new Color(
					0.015f,
					0.025f,
					0.045f,
					0.78f
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


	private static StyleBoxFlat CreateFutureNodeStyle()
	{
		return new StyleBoxFlat
		{
			BgColor =
				new Color(
					0.035f,
					0.050f,
					0.070f,
					0.92f
				),

			BorderColor =
				new Color(
					0.25f,
					0.32f,
					0.40f,
					0.60f
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
				20
		};
	}


	private static StyleBoxFlat CreateHeaderStyle()
	{
		return new StyleBoxFlat
		{
			BgColor =
				new Color(
					0.018f,
					0.040f,
					0.075f,
					0.97f
				),

			BorderColor =
				new Color(
					0.08f,
					0.55f,
					0.92f,
					0.62f
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
				24,

			CornerRadiusTopRight =
				24,

			CornerRadiusBottomLeft =
				24,

			CornerRadiusBottomRight =
				24,

			ShadowColor =
				new Color(
					0.0f,
					0.45f,
					0.90f,
					0.18f
				),

			ShadowSize =
				10
		};
	}


	private static Label CreateLabel(
		int fontSize,
		string text)
	{
		Label label =
			new()
			{
				Text =
					text,

				HorizontalAlignment =
					HorizontalAlignment.Center,

				VerticalAlignment =
					VerticalAlignment.Center,

				MouseFilter =
					Control.MouseFilterEnum.Ignore,

				AutowrapMode =
					TextServer.AutowrapMode.Off
			};


		label.AddThemeFontSizeOverride(
			"font_size",
			fontSize
		);


		return label;
	}


	private static void PlayNavigationHaptic()
	{
		Input.VibrateHandheld(
			NavigationHapticDurationMs,
			NavigationHapticStrength
		);
	}


	// ==================================================
	// VIEW DATA
	// ==================================================

	private sealed record RoomNodeView(
		Button Root,
		TextureRect Icon,
		Label StateLabel,
		PanelContainer Badge,
		Label BadgeLabel,
		TextureRect CurrentMarker
	);


	// ==================================================
	// ROUTES
	// ==================================================

	private sealed partial class MapRouteLayer
		: Control
	{
		private readonly List<Vector2>
			_centers =
				[];


		private readonly List<Color>
			_colors =
				[];


		private readonly List<bool>
			_unlocked =
				[];


		public void SetData(
			IReadOnlyList<Vector2> centers,
			IReadOnlyList<Color> colors,
			IReadOnlyList<bool> unlocked)
		{
			_centers.Clear();

			_colors.Clear();

			_unlocked.Clear();


			for (
				int i = 0;
				i < centers.Count;
				i++
			)
			{
				_centers.Add(
					centers[
						i
					]
				);


				_colors.Add(
					colors[
						i
					]
				);


				_unlocked.Add(
					unlocked[
						i
					]
				);
			}


			QueueRedraw();
		}


		public override void _Draw()
		{
			if (_centers.Count < 2)
				return;


			for (
				int i = 0;
				i < _centers.Count - 1;
				i++
			)
			{
				Vector2 from =
					_centers[
						i
					];


				Vector2 to =
					_centers[
						i + 1
					];


				bool active =
					_unlocked[
						i
					]
					&& _unlocked[
						i + 1
					];


				Color routeColor =
					active
						? _colors[
							i + 1
						]
						: new Color(
							0.24f,
							0.28f,
							0.34f,
							0.82f
						);


				DrawLine(
					from,
					to,
					new Color(
						routeColor.R,
						routeColor.G,
						routeColor.B,
						0.18f
					),
					active
						? 16.0f
						: 9.0f,
					true
				);


				DrawLine(
					from,
					to,
					routeColor,
					active
						? 6.0f
						: 3.5f,
					true
				);

			}
		}
	}


	// ==================================================
	// GRID BACKGROUND
	// ==================================================

	private sealed partial class MapGridBackground
		: Control
	{
		public override void _Draw()
		{
			Color lineColor =
				new(
					0.06f,
					0.20f,
					0.34f,
					0.20f
				);


			const float spacing =
				44.0f;


			for (
				float x = -200.0f;
				x < Size.X + 200.0f;
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
					lineColor,
					1.0f
				);
			}


			for (
				float x = 0.0f;
				x < Size.X + 400.0f;
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
					lineColor,
					1.0f
				);
			}
		}
	}
}
