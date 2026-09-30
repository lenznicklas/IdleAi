using Godot;
using System;
using System.Collections.Generic;

namespace IdleAi;


/*
 * Touch-state polish for the 2D Singularity Node buttons.
 *
 * Godot Buttons have separate theme states (normal / hover / pressed / focus).
 * The map originally supplied only "normal", so Android touch-down could fall
 * back to the project/default Button style: grey rectangle + hard corners.
 *
 * This partial keeps every state inside the visual language of the Node itself.
 */
public sealed partial class SingularityController
{
	private readonly Dictionary<ulong, SingularityNodeType>
		_polishedNodeButtonTypes =
			[];

	private NodeTouchStyleRunner?
		_nodeTouchStyleRunner;


	public void EnablePolishedNodeTouchStyles()
	{
		if (_nodeTouchStyleRunner != null)
			return;

		_nodeTouchStyleRunner =
			new NodeTouchStyleRunner
			{
				Name =
					"SingularityNodeTouchStyleRunner"
			};

		_nodeTouchStyleRunner.ProcessRequested +=
			RefreshPolishedNodeTouchStyles;

		_root.AddChild(
			_nodeTouchStyleRunner
		);

		/*
		 * Node purchases / sales / type changes can reuse the same Button.
		 * Clear the cache so its interaction palette is rebuilt immediately.
		 */
		_service.Changed +=
			() =>
			{
				_polishedNodeButtonTypes.Clear();
				RefreshPolishedNodeTouchStyles();
			};

		RefreshPolishedNodeTouchStyles();
	}


	private void RefreshPolishedNodeTouchStyles()
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

		foreach (
			SectorMapView sectorView
				in _sectorMapViews.Values
		)
		{
			SingularitySectorData sector =
				_service.GetSector(
					sectorView.SectorIndex
				);

			int count =
				Math.Min(
					sectorView.Nodes.Count,
					sector.Nodes.Count
				);

			for (
				int i = 0;
				i < count;
				i++
			)
			{
				Button button =
					sectorView.Nodes[
						i
					].Root;

				if (!GodotObject.IsInstanceValid(
					button
				))
				{
					continue;
				}

				SingularityNodeType type =
					sector.Nodes[
						i
					].Type;

				ulong id =
					button.GetInstanceId();

				if (
					_polishedNodeButtonTypes.TryGetValue(
						id,
						out SingularityNodeType cachedType
					)
					&& cachedType == type
				)
				{
					continue;
				}

				ApplyPolishedNodeTouchStyle(
					button,
					type
				);

				_polishedNodeButtonTypes[
					id
				] =
					type;
			}
		}
	}


	private static void ApplyPolishedNodeTouchStyle(
		Button button,
		SingularityNodeType type)
	{
		Color border =
			type == SingularityNodeType.Empty
				? new Color(
					0.30f,
					0.33f,
					0.39f,
					0.90f
				)
				: GetNodeColor(
					type
				);

		Color normalBackground =
			type == SingularityNodeType.Empty
				? new Color(
					0.025f,
					0.030f,
					0.040f,
					0.96f
				)
				: new Color(
					border.R * 0.20f,
					border.G * 0.20f,
					border.B * 0.20f,
					0.98f
				);

		/*
		 * Hover is intentionally almost identical to normal. On a touchscreen,
		 * moving a finger across a Button can briefly count as hover, so a large
		 * visual change here would flicker while the player pans the map.
		 */
		Color hoverBackground =
			normalBackground.Lightened(
				0.035f
			);

		/*
		 * Pressed keeps exactly the same rounded geometry. It gets only a subtle
		 * darker fill and a slightly brighter border instead of becoming grey.
		 */
		Color pressedBackground =
			normalBackground.Darkened(
				0.08f
			);

		Color pressedBorder =
			border.Lightened(
				0.08f
			);

		button.AddThemeStyleboxOverride(
			"hover",
			CreatePolishedNodeStateStyle(
				hoverBackground,
				border,
				0.22f
			)
		);

		button.AddThemeStyleboxOverride(
			"pressed",
			CreatePolishedNodeStateStyle(
				pressedBackground,
				pressedBorder,
				0.28f
			)
		);

		/*
		 * Some Godot themes request hover_pressed while a pointer/finger remains
		 * over an actively pressed Button. Explicitly define it as well so there
		 * is never a fallback to the default rectangular style.
		 */
		button.AddThemeStyleboxOverride(
			"hover_pressed",
			CreatePolishedNodeStateStyle(
				pressedBackground,
				pressedBorder,
				0.30f
			)
		);

		/* FocusMode is already None, but an empty focus StyleBox guarantees that
		 * keyboard/controller focus can never add a square outline later. */
		button.AddThemeStyleboxOverride(
			"focus",
			new StyleBoxEmpty()
		);
	}


	private static StyleBoxFlat CreatePolishedNodeStateStyle(
		Color background,
		Color border,
		float glowAlpha)
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

			CornerRadiusTopLeft = 18,
			CornerRadiusTopRight = 18,
			CornerRadiusBottomLeft = 18,
			CornerRadiusBottomRight = 18,

			ShadowColor =
				new Color(
					border.R,
					border.G,
					border.B,
					glowAlpha
				),

			ShadowSize = 6
		};
	}


	private sealed partial class NodeTouchStyleRunner
		: Node
	{
		public event Action? ProcessRequested;


		public override void _Process(
			double delta)
		{
			ProcessRequested?.Invoke();
		}
	}
}
