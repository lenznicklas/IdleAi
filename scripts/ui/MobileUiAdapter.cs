using Godot;
using System;

namespace IdleAi;

public partial class MobileUiAdapter : Node
{
	// ==================================================
	// CONSTANTS
	// ==================================================

	private const float BottomBarBaseHeight =
		88.0f;


	private const int MainSidePadding =
		16;


	private const int MainTopPadding =
		10;


	private const int BottomSidePadding =
		16;


	private const int BottomInnerPadding =
		6;


	private const int OverlayPadding =
		22;


	private const int ScrollDeadzone =
		12;


	// ==================================================
	// ROOT
	// ==================================================

	private Game _root =
		null!;


	// ==================================================
	// SAFE AREA
	// ==================================================

	private readonly record struct SafeInsets(
		float Left,
		float Top,
		float Right,
		float Bottom
	);


	// ==================================================
	// SETUP
	// ==================================================

	public void Setup(
		Game root)
	{
		_root =
			root;
	}


	public override void _Ready()
	{
		ApplyEverything();


		_root.GetViewport().SizeChanged +=
			OnViewportSizeChanged;


		RefreshNextFrame();
	}


	public override void _ExitTree()
	{
		if (
			_root != null
			&& IsInstanceValid(
				_root
			)
		)
		{
			_root.GetViewport().SizeChanged -=
				OnViewportSizeChanged;
		}
	}


	// ==================================================
	// NEXT FRAME
	// ==================================================

	private async void RefreshNextFrame()
	{
		await ToSignal(
			GetTree(),
			SceneTree.SignalName.ProcessFrame
		);


		if (
			_root == null
			|| !IsInstanceValid(
				_root
			)
		)
		{
			return;
		}


		ApplyEverything();
	}


	// ==================================================
	// RESIZE
	// ==================================================

	private void OnViewportSizeChanged()
	{
		ApplyEverything();
	}


	// ==================================================
	// APPLY
	// ==================================================

	private void ApplyEverything()
	{
		if (_root == null)
			return;


		SafeInsets safe =
			GetSafeInsets();


		ApplyMainGameSafeArea(
			safe
		);


		ApplyBottomBarSafeArea(
			safe
		);


		ApplyStatsSafeArea(
			safe
		);


		ApplyPrestigeSafeArea(
			safe
		);


		ApplyMapSafeArea(
			safe
		);


		ApplyShopSafeArea(
			safe
		);


		ApplyLabSafeArea(
			safe
		);


		ConfigureAllScrollContainers(
			_root
		);
	}


	// ==================================================
	// MAIN GAME
	// ==================================================

	private void ApplyMainGameSafeArea(
		SafeInsets safe)
	{
		MarginContainer? margin =
			_root.GetNodeOrNull<MarginContainer>(
				"MarginContainer"
			);


		if (margin == null)
			return;


		margin.AddThemeConstantOverride(
			"margin_left",
			MainSidePadding
			+ RoundUp(
				safe.Left
			)
		);


		margin.AddThemeConstantOverride(
			"margin_top",
			MainTopPadding
			+ RoundUp(
				safe.Top
			)
		);


		margin.AddThemeConstantOverride(
			"margin_right",
			MainSidePadding
			+ RoundUp(
				safe.Right
			)
		);


		/*
		 * Bottom is handled by OffsetBottom because
		 * the BottomBar itself gets taller on devices
		 * with a navigation/gesture safe area.
		 */

		margin.AddThemeConstantOverride(
			"margin_bottom",
			0
		);


		margin.OffsetBottom =
			-(
				BottomBarBaseHeight
				+ safe.Bottom
			);


		VBoxContainer? layout =
			_root.GetNodeOrNull<VBoxContainer>(
				"MarginContainer/VBoxContainer"
			);


		if (layout != null)
		{
			/*
			 * No unwanted strip between TopBar
			 * and the room ScrollContainer.
			 */
			layout.AddThemeConstantOverride(
				"separation",
				0
			);
		}
	}


	// ==================================================
	// BOTTOM BAR
	// ==================================================

	private void ApplyBottomBarSafeArea(
		SafeInsets safe)
	{
		Control? bottomBar =
			_root.GetNodeOrNull<Control>(
				"BottomBar"
			);


		if (bottomBar == null)
			return;


		/*
		 * The background extends all the way to the
		 * physical bottom of the screen.
		 *
		 * The actual buttons are pushed above the
		 * unsafe gesture/navigation area.
		 */

		bottomBar.OffsetTop =
			-(
				BottomBarBaseHeight
				+ safe.Bottom
			);


		bottomBar.OffsetBottom =
			0;


		MarginContainer? margin =
			_root.GetNodeOrNull<MarginContainer>(
				"BottomBar/Margin"
			);


		if (margin == null)
			return;


		margin.AddThemeConstantOverride(
			"margin_left",
			BottomSidePadding
			+ RoundUp(
				safe.Left
			)
		);


		margin.AddThemeConstantOverride(
			"margin_right",
			BottomSidePadding
			+ RoundUp(
				safe.Right
			)
		);


		margin.AddThemeConstantOverride(
			"margin_top",
			BottomInnerPadding
		);


		margin.AddThemeConstantOverride(
			"margin_bottom",
			BottomInnerPadding
			+ RoundUp(
				safe.Bottom
			)
		);
	}


	// ==================================================
	// STATS
	// ==================================================

	private void ApplyStatsSafeArea(
		SafeInsets safe)
	{
		PanelContainer? panel =
			_root.GetNodeOrNull<PanelContainer>(
				"StatsOverlay/StatsPanel"
			);


		if (panel == null)
			return;


		/*
		 * The old scene used a fixed 600x1000 centered
		 * panel. That can extend into a camera cutout,
		 * rounded corner or system bar.
		 *
		 * The Stats panel now fills the safe area with
		 * a small additional padding.
		 */

		panel.AnchorLeft =
			0.0f;

		panel.AnchorTop =
			0.0f;

		panel.AnchorRight =
			1.0f;

		panel.AnchorBottom =
			1.0f;


		panel.OffsetLeft =
			safe.Left
			+ OverlayPadding;


		panel.OffsetTop =
			safe.Top
			+ OverlayPadding;


		panel.OffsetRight =
			-(
				safe.Right
				+ OverlayPadding
			);


		panel.OffsetBottom =
			-(
				safe.Bottom
				+ OverlayPadding
			);


		ScrollContainer? scroll =
			_root.GetNodeOrNull<ScrollContainer>(
				"StatsOverlay/StatsPanel/Margin/Scroll"
			);


		if (scroll != null)
		{
			ConfigureScrollContainer(
				scroll
			);
		}
	}


	// ==================================================
	// PRESTIGE
	// ==================================================

	private void ApplyPrestigeSafeArea(
		SafeInsets safe)
	{
		PanelContainer? panel =
			_root.GetNodeOrNull<PanelContainer>(
				"PrestigeConfirmOverlay/Panel"
			);


		if (panel == null)
			return;


		/*
		 * Slightly larger side padding than Stats,
		 * while still guaranteeing that the panel
		 * stays inside the safe display area.
		 */

		float sidePadding =
			36.0f;


		float verticalPadding =
			MathF.Max(
				60.0f,
				safe.Top
			);


		panel.AnchorLeft =
			0.0f;

		panel.AnchorTop =
			0.0f;

		panel.AnchorRight =
			1.0f;

		panel.AnchorBottom =
			1.0f;


		panel.OffsetLeft =
			safe.Left
			+ sidePadding;


		panel.OffsetTop =
			safe.Top
			+ verticalPadding;


		panel.OffsetRight =
			-(
				safe.Right
				+ sidePadding
			);


		panel.OffsetBottom =
			-(
				safe.Bottom
				+ verticalPadding
			);
	}


	// ==================================================
	// MAP
	// ==================================================

	private void ApplyMapSafeArea(
		SafeInsets safe)
	{
		Control? mapPage =
			_root.GetNodeOrNull<Control>(
				"MapPage"
			);


		if (mapPage != null)
		{
			mapPage.OffsetBottom =
				-(
					BottomBarBaseHeight
						+ safe.Bottom
				);
		}


		MarginContainer? margin =
			_root.GetNodeOrNull<MarginContainer>(
				"MapPage/Margin"
			);


		if (margin == null)
			return;


		margin.AddThemeConstantOverride(
			"margin_left",
			24
			+ RoundUp(
				safe.Left
			)
		);


		margin.AddThemeConstantOverride(
			"margin_top",
			24
			+ RoundUp(
				safe.Top
			)
		);


		margin.AddThemeConstantOverride(
			"margin_right",
			24
			+ RoundUp(
				safe.Right
			)
		);


		margin.AddThemeConstantOverride(
			"margin_bottom",
			16
		);
	}


	// ==================================================
	// SHOP
	// ==================================================

	private void ApplyShopSafeArea(
		SafeInsets safe)
	{
		Control? shopPage =
			_root.GetNodeOrNull<Control>(
				"ShopPage"
			);


		if (shopPage != null)
		{
			shopPage.OffsetBottom =
				-(
					BottomBarBaseHeight
						+ safe.Bottom
				);
		}


		Control? center =
			_root.GetNodeOrNull<Control>(
				"ShopPage/Center"
			);


		if (center == null)
			return;


		center.OffsetLeft =
			safe.Left;

		center.OffsetTop =
			safe.Top;

		center.OffsetRight =
			-safe.Right;

		center.OffsetBottom =
			-safe.Bottom;
	}


	// ==================================================
	// LAB
	// ==================================================

	private void ApplyLabSafeArea(
		SafeInsets safe)
	{
		Control? labPage =
			_root.GetNodeOrNull<Control>(
				"LabPage"
			);


		if (labPage == null)
			return;


		labPage.OffsetBottom =
			-(
				BottomBarBaseHeight
					+ safe.Bottom
			);


		/*
		 * LabController builds this MarginContainer
		 * dynamically, so it does not have a fixed
		 * NodePath. Find the direct MarginContainer
		 * child instead.
		 */

		MarginContainer? labMargin =
			null;


		foreach (
			Node child
			in labPage.GetChildren()
		)
		{
			if (
				child is MarginContainer found
			)
			{
				labMargin =
					found;

				break;
			}
		}


		if (labMargin == null)
			return;


		labMargin.AddThemeConstantOverride(
			"margin_left",
			18
			+ RoundUp(
				safe.Left
			)
		);


		labMargin.AddThemeConstantOverride(
			"margin_top",
			RoundUp(
				safe.Top
			)
		);


		labMargin.AddThemeConstantOverride(
			"margin_right",
			18
			+ RoundUp(
				safe.Right
			)
		);


		labMargin.AddThemeConstantOverride(
			"margin_bottom",
			0
		);
	}


	// ==================================================
	// SCROLL CONTAINERS
	// ==================================================

	private void ConfigureAllScrollContainers(
		Node node)
	{
		if (
			node is ScrollContainer scroll
		)
		{
			ConfigureScrollContainer(
				scroll
			);
		}


		foreach (
			Node child
			in node.GetChildren()
		)
		{
			ConfigureAllScrollContainers(
				child
			);
		}
	}


	private void ConfigureScrollContainer(
		ScrollContainer scroll)
	{
		/*
		 * Horizontal scrolling is not used anywhere
		 * in the mobile UI.
		 */

		scroll.HorizontalScrollMode =
			ScrollContainer.ScrollMode.Disabled;


		/*
		 * Scrolling remains enabled.
		 *
		 * Only the scrollbar itself is hidden.
		 */

		scroll.VerticalScrollMode =
			ScrollContainer.ScrollMode.ShowNever;


		scroll.ScrollDeadzone =
			ScrollDeadzone;


		scroll.FollowFocus =
			false;


		scroll.ClipContents =
			true;


		/*
		 * Buttons and nested Controls otherwise tend
		 * to consume the touch event on Android before
		 * the ScrollContainer sees enough movement.
		 *
		 * PASS keeps buttons clickable while also
		 * allowing the drag to reach ScrollContainer.
		 */

		foreach (
			Node child
			in scroll.GetChildren()
		)
		{
			EnableScrollEventPropagation(
				child
			);
		}
	}


	private void EnableScrollEventPropagation(
		Node node)
	{
		/*
		 * Do not overwrite another nested
		 * ScrollContainer's own input handling.
		 */

		if (
			node is ScrollContainer nestedScroll
		)
		{
			ConfigureScrollContainer(
				nestedScroll
			);

			return;
		}


		if (
			node is Control control
		)
		{
			if (
				control.MouseFilter
				!= Control.MouseFilterEnum.Ignore
			)
			{
				control.MouseFilter =
					Control.MouseFilterEnum.Pass;
			}
		}


		foreach (
			Node child
			in node.GetChildren()
		)
		{
			EnableScrollEventPropagation(
				child
			);
		}
	}


	// ==================================================
	// SAFE AREA CALCULATION
	// ==================================================

	private SafeInsets GetSafeInsets()
	{
		string os =
			OS.GetName();


		/*
		 * Avoid applying the physical-screen safe
		 * rectangle on desktop systems.
		 */

		if (
			os != "Android"
			&& os != "iOS"
		)
		{
			return new SafeInsets(
				0,
				0,
				0,
				0
			);
		}


		Rect2I physicalSafe =
			DisplayServer.GetDisplaySafeArea();


		if (
			physicalSafe.Size.X <= 0
			|| physicalSafe.Size.Y <= 0
		)
		{
			return new SafeInsets(
				0,
				0,
				0,
				0
			);
		}


		Viewport viewport =
			_root.GetViewport();


		Rect2 viewportRect =
			viewport.GetVisibleRect();


		/*
		 * DisplayServer returns physical screen
		 * coordinates.
		 *
		 * Your game uses canvas_items + expand, so
		 * simply using physical pixel values as UI
		 * margins would be wrong.
		 *
		 * Convert them through the final viewport
		 * transform first.
		 */

		Transform2D inverseTransform =
			viewport
				.GetFinalTransform()
				.AffineInverse();


		Vector2 physicalStart =
			new(
				physicalSafe.Position.X,
				physicalSafe.Position.Y
			);


		Vector2 physicalEnd =
			new(
				physicalSafe.Position.X
				+ physicalSafe.Size.X,

				physicalSafe.Position.Y
				+ physicalSafe.Size.Y
			);


		Vector2 safeStart =
			inverseTransform
			* physicalStart;


		Vector2 safeEnd =
			inverseTransform
			* physicalEnd;


		Vector2 viewportStart =
			viewportRect.Position;


		Vector2 viewportEnd =
			viewportRect.Position
			+ viewportRect.Size;


		float left =
			MathF.Max(
				0.0f,
				safeStart.X
				- viewportStart.X
			);


		float top =
			MathF.Max(
				0.0f,
				safeStart.Y
				- viewportStart.Y
			);


		float right =
			MathF.Max(
				0.0f,
				viewportEnd.X
				- safeEnd.X
			);


		float bottom =
			MathF.Max(
				0.0f,
				viewportEnd.Y
				- safeEnd.Y
			);


		/*
		 * Protect against a broken platform value
		 * causing the entire UI to disappear.
		 */

		left =
			MathF.Min(
				left,
				viewportRect.Size.X
				* 0.25f
			);


		right =
			MathF.Min(
				right,
				viewportRect.Size.X
				* 0.25f
			);


		top =
			MathF.Min(
				top,
				viewportRect.Size.Y
				* 0.25f
			);


		bottom =
			MathF.Min(
				bottom,
				viewportRect.Size.Y
				* 0.25f
			);


		return new SafeInsets(
			left,
			top,
			right,
			bottom
		);
	}


	// ==================================================
	// UTIL
	// ==================================================

	private static int RoundUp(
		float value)
	{
		return (int)MathF.Ceiling(
			value
		);
	}
}
