using Godot;
using System;

namespace IdleAi;

/*
 * Adds The Singularity to the existing ROOM NETWORK without requiring a second
 * hand-maintained copy of MapController.cs.
 *
 * The old "COMING LATER" card is hidden and replaced by the Singularity room
 * node. The connection is drawn with DrawLine, matching the existing map route
 * style; no extra line PNG is required.
 */
public sealed partial class SingularityMapExtension
{
	/*
	 * Singularity intentionally gets a slightly wider card than the regular
	 * rooms. Its long name and inner dark information panel need more breathing
	 * room; at 270 px the dark panel sat almost on top of the rounded outer
	 * border on narrow/mobile rendering.
	 */
	private const float NodeWidth =
		300.0f;

	private const float NodeHeight =
		178.0f;

	private const float InfoPanelWidth =
		160.0f;

	private const float BadgeWidth =
		244.0f;

	private static readonly Vector2 SingularityPosition =
		new(
			48.0f,
			26.0f
		);

	private static readonly Vector2 QuantumCenter =
		new(
			330.0f + 135.0f,
			220.0f + 89.0f
		);

	private static readonly Color Accent =
		new(
			0.92f,
			0.66f,
			0.20f,
			1.0f
		);

	private static readonly Texture2D UnlockedIcon =
		GD.Load<Texture2D>(
			"res://assets/map/room_singularity.png"
		);

	private static readonly Texture2D LockedIcon =
		GD.Load<Texture2D>(
			"res://assets/map/locked_singularity.png"
		);

	private readonly Game _root;
	private readonly GameState _state;
	private readonly SingularityService _service;
	private readonly SingularityController _controller;

	private Control _mapPage =
		null!;

	private Control _canvas =
		null!;

	private Button _roomButton =
		null!;

	private TextureRect _icon =
		null!;

	private Label _stateLabel =
		null!;

	private Label _badgeLabel =
		null!;

	private SingularityMapLine _line =
		null!;

	public event Action<string>? MessageRequested;
	public event Action? GameStateChanged;

	public SingularityMapExtension(
		Game root,
		GameState state,
		SingularityService service,
		SingularityController controller)
	{
		_root = root;
		_state = state;
		_service = service;
		_controller = controller;
	}

	public void Initialize()
	{
		_mapPage =
			_root.GetNode<Control>(
				"MapPage"
			);

		ScrollContainer? scroll =
			FindDescendant<ScrollContainer>(
				_mapPage
			);

		if (scroll == null)
		{
			GD.PushWarning(
				"Singularity map: Map ScrollContainer not found."
			);
			return;
		}

		CenterContainer? center =
			FindDescendant<CenterContainer>(
				scroll
			);

		if (center == null)
		{
			GD.PushWarning(
				"Singularity map: map center not found."
			);
			return;
		}

		foreach (
			Node child
				in center.GetChildren()
		)
		{
			if (
				child is Control control
				&& control.CustomMinimumSize.X >= 600.0f
			)
			{
				_canvas = control;
				break;
			}
		}

		if (_canvas == null)
		{
			GD.PushWarning(
				"Singularity map: canvas not found."
			);
			return;
		}

		HideFutureCard();
		CreateConnectionLine();
		CreateRoomNode();

		_mapPage.VisibilityChanged +=
			OnMapVisibilityChanged;

		_service.Changed +=
			Refresh;

		Refresh();
	}

	private void HideFutureCard()
	{
		foreach (
			Node child
				in _canvas.GetChildren()
		)
		{
			if (
				child is PanelContainer panel
				&& panel.Position.Y < 120.0f
				&& panel.Size.X <= 230.0f
			)
			{
				panel.Hide();
			}
		}
	}

	private void CreateConnectionLine()
	{
		_line =
			new SingularityMapLine
			{
				Name =
					"SingularityMapConnection",

				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};

		_line.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);

		_canvas.AddChild(
			_line
		);

		/*
		 * Route layer is normally the first canvas child. Put our line directly
		 * above it, still behind all room buttons.
		 */
		_canvas.MoveChild(
			_line,
			Math.Min(
				1,
				_canvas.GetChildCount() - 1
			)
		);

		_line.Configure(
			QuantumCenter,
			SingularityPosition
				+ new Vector2(
					NodeWidth / 2.0f,
					NodeHeight / 2.0f
				),
			_service.Unlocked
		);
	}

	private void CreateRoomNode()
	{
		_roomButton =
			new Button
			{
				Name =
					"SingularityRoomNode",

				Position =
					SingularityPosition,

				Size =
					new Vector2(
						NodeWidth,
						NodeHeight
					),

				CustomMinimumSize =
					new Vector2(
						NodeWidth,
						NodeHeight
					),

				FocusMode =
					Control.FocusModeEnum.None,

				ClipContents =
					true
			};

		_roomButton.Pressed +=
			OnPressed;

		_canvas.AddChild(
			_roomButton
		);

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

				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};

		_roomButton.AddChild(
			iconHolder
		);

		_icon =
			new TextureRect
			{
				ExpandMode =
					TextureRect.ExpandModeEnum.IgnoreSize,

				StretchMode =
					TextureRect.StretchModeEnum.KeepAspectCentered,

				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};

		_icon.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);

		iconHolder.AddChild(
			_icon
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
						InfoPanelWidth,
						116
					),

				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};

		info.AddThemeStyleboxOverride(
			"panel",
			new StyleBoxFlat
			{
				BgColor =
					new Color(
						0.015f,
						0.025f,
						0.045f,
						0.78f
					),

				CornerRadiusTopLeft = 14,
				CornerRadiusTopRight = 14,
				CornerRadiusBottomLeft = 14,
				CornerRadiusBottomRight = 14
			}
		);

		_roomButton.AddChild(
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

		VBoxContainer box =
			new();

		box.AddThemeConstantOverride(
			"separation",
			3
		);

		infoMargin.AddChild(
			box
		);

		Label index =
			CreateLabel(
				10,
				"ROOM 5"
			);

		index.HorizontalAlignment =
			HorizontalAlignment.Left;

		index.Modulate =
			Accent.Lightened(
				0.20f
			);

		box.AddChild(
			index
		);

		Label name =
			CreateLabel(
				16,
				"THE\nSINGULARITY"
			);

		name.HorizontalAlignment =
			HorizontalAlignment.Left;

		box.AddChild(
			name
		);

		_stateLabel =
			CreateLabel(
				10,
				""
			);

		_stateLabel.HorizontalAlignment =
			HorizontalAlignment.Left;

		box.AddChild(
			_stateLabel
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
						BadgeWidth,
						30
					),

				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};

		_roomButton.AddChild(
			badge
		);

		_badgeLabel =
			CreateLabel(
				11,
				""
			);

		badge.AddChild(
			_badgeLabel
		);

		RefreshBadgeStyle(
			badge
		);
	}

	private void OnPressed()
	{
		Input.VibrateHandheld(
			14,
			0.14f
		);

		if (!_service.Unlocked)
		{
			SingularityActionResult result =
				_service.Unlock(
					_state
				);

			MessageRequested?.Invoke(
				result.Message
			);

			if (!result.Changed)
			{
				Refresh();
				return;
			}

			GameStateChanged?.Invoke();
			Refresh();
		}

		_mapPage.Hide();
		_controller.Open();
	}

	private void OnMapVisibilityChanged()
	{
		if (!_mapPage.Visible)
			return;

		_controller.Hide();

		Callable
			.From(
				Refresh
			)
			.CallDeferred();
	}

	public void Refresh()
	{
		if (
			_roomButton == null
			|| !GodotObject.IsInstanceValid(
				_roomButton
			)
		)
		{
			return;
		}

		bool unlocked =
			_service.Unlocked;

		_icon.Texture =
			unlocked
				? UnlockedIcon
				: LockedIcon;

		_stateLabel.Text =
			unlocked
				? "TAP TO ENTER"
				: "LOCKED";

		_stateLabel.Modulate =
			unlocked
				? Colors.White
				: new Color(
					0.64f,
					0.67f,
					0.72f,
					1.0f
				);

		_badgeLabel.Text =
			unlocked
				? "ENTER"
				: "UNLOCK • "
					+ NumberFormatter.Format(
						SingularityService
							.TestUnlockTokenCost
					);

		ApplyNodeStyle(
			unlocked
		);

		if (
			_roomButton.GetChild(
				2
			)
				is PanelContainer badge
		)
		{
			RefreshBadgeStyle(
				badge
			);
		}

		_line?.Configure(
			QuantumCenter,
			SingularityPosition
				+ new Vector2(
					NodeWidth / 2.0f,
					NodeHeight / 2.0f
				),
			unlocked
		);

		UpdateProgressLabel();
	}

	private void UpdateProgressLabel()
	{
		foreach (
			Label label
				in FindDescendants<Label>(
					_mapPage
				)
		)
		{
			if (
				!label.Text.Contains(
					"ROOMS DISCOVERED",
					StringComparison.Ordinal
				)
			)
			{
				continue;
			}

			int normalUnlocked = 0;

			foreach (
				RoomState room
					in _state.RoomStates
			)
			{
				if (room.Unlocked)
					normalUnlocked++;
			}

			int totalUnlocked =
				normalUnlocked
				+ (
					_service.Unlocked
						? 1
						: 0
				);

			label.Text =
				totalUnlocked
				+ " / 5 ROOMS DISCOVERED";

			break;
		}
	}

	private void ApplyNodeStyle(
		bool unlocked)
	{
		Color background =
			unlocked
				? new Color(
					Accent.R * 0.22f,
					Accent.G * 0.22f,
					Accent.B * 0.22f,
					0.98f
				)
				: new Color(
					0.035f,
					0.045f,
					0.060f,
					0.96f
				);

		Color border =
			unlocked
				? Accent
				: new Color(
					0.24f,
					0.28f,
					0.34f,
					0.72f
				);

		_roomButton.AddThemeStyleboxOverride(
			"normal",
			CreateButtonStyle(
				background,
				border,
				2
			)
		);

		_roomButton.AddThemeStyleboxOverride(
			"hover",
			CreateButtonStyle(
				background.Lightened(
					0.08f
				),
				border,
				2
			)
		);

		_roomButton.AddThemeStyleboxOverride(
			"pressed",
			CreateButtonStyle(
				background.Darkened(
					0.08f
				),
				border,
				2
			)
		);

		_roomButton.Modulate =
			unlocked
				? Colors.White
				: new Color(
					0.82f,
					0.84f,
					0.88f,
					1.0f
				);
	}

	private void RefreshBadgeStyle(
		PanelContainer badge)
	{
		bool unlocked =
			_service.Unlocked;

		badge.AddThemeStyleboxOverride(
			"panel",
			new StyleBoxFlat
			{
				BgColor =
					unlocked
						? new Color(
							Accent.R * 0.30f,
							Accent.G * 0.30f,
							Accent.B * 0.30f,
							0.98f
						)
						: new Color(
							0.075f,
							0.085f,
							0.105f,
							0.98f
						),

				BorderColor =
					unlocked
						? Accent
						: new Color(
							0.28f,
							0.32f,
							0.38f,
							0.90f
						),

				BorderWidthLeft = 2,
				BorderWidthTop = 2,
				BorderWidthRight = 2,
				BorderWidthBottom = 2,

				CornerRadiusTopLeft = 15,
				CornerRadiusTopRight = 15,
				CornerRadiusBottomLeft = 15,
				CornerRadiusBottomRight = 15
			}
		);
	}

	private static StyleBoxFlat CreateButtonStyle(
		Color background,
		Color border,
		int borderWidth)
	{
		return new StyleBoxFlat
		{
			BgColor = background,
			BorderColor = border,

			BorderWidthLeft = borderWidth,
			BorderWidthTop = borderWidth,
			BorderWidthRight = borderWidth,
			BorderWidthBottom = borderWidth,

			CornerRadiusTopLeft = 24,
			CornerRadiusTopRight = 24,
			CornerRadiusBottomLeft = 24,
			CornerRadiusBottomRight = 24,

			ShadowColor =
				new Color(
					border.R,
					border.G,
					border.B,
					0.20f
				),

			ShadowSize = 8
		};
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

	private static T? FindDescendant<T>(
		Node root)
		where T : Node
	{
		foreach (
			Node child
				in root.GetChildren()
		)
		{
			if (child is T typed)
				return typed;

			T? nested =
				FindDescendant<T>(
					child
				);

			if (nested != null)
				return nested;
		}

		return null;
	}

	private static System.Collections.Generic.List<T>
		FindDescendants<T>(
			Node root)
		where T : Node
	{
		System.Collections.Generic.List<T> result =
			[];

		foreach (
			Node child
				in root.GetChildren()
		)
		{
			if (child is T typed)
			{
				result.Add(
					typed
				);
			}

			result.AddRange(
				FindDescendants<T>(
					child
				)
			);
		}

		return result;
	}

	private sealed partial class SingularityMapLine
		: Control
	{
		private Vector2 _from;
		private Vector2 _to;
		private bool _active;

		public void Configure(
			Vector2 from,
			Vector2 to,
			bool active)
		{
			_from = from;
			_to = to;
			_active = active;
			QueueRedraw();
		}

		public override void _Draw()
		{
			Color color =
				_active
					? Accent
					: new Color(
						0.24f,
						0.28f,
						0.34f,
						0.82f
					);

			DrawLine(
				_from,
				_to,
				new Color(
					color.R,
					color.G,
					color.B,
					0.18f
				),
				_active
					? 16.0f
					: 9.0f,
				true
			);

			DrawLine(
				_from,
				_to,
				color,
				_active
					? 6.0f
					: 3.5f,
				true
			);
		}
	}
}
