using Godot;
using System;

namespace IdleAi;

public partial class MobileUiAdapter : Node
{
	private const float BottomBarHeight =
		174.0f;


	private const int MainHorizontalPadding =
		16;


	private const int MainExtraTopPadding =
		10;


	private const int BottomHorizontalPadding =
		26;


	private const int BottomTopPadding =
		4;


	private const int BottomExtraBottomPadding =
		18;


	private const float NavigationButtonWidth =
		92.0f;


	private const float NavigationButtonHeight =
		78.0f;


	private const int OverlayPadding =
		22;


	private const int ShopHorizontalPadding =
		18;


	private const int ShopExtraTopPadding =
		6;


	private Game _root =
		null!;


	// ==================================================
	// SAFE INSETS
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
		ApplyLayout();


		_root.GetViewport().SizeChanged +=
			OnViewportSizeChanged;


		ApplyAgainNextFrame();
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
	// DELAYED REFRESH
	// ==================================================

	private async void ApplyAgainNextFrame()
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


		ApplyLayout();
	}


	private void OnViewportSizeChanged()
	{
		ApplyLayout();
	}


	// ==================================================
	// MAIN
	// ==================================================

	private void ApplyLayout()
	{
		SafeInsets safe =
			GetSafeInsets();


		ApplyMainLayout(
			safe
		);


		ApplyBottomBar(
			safe
		);


		ApplyNavigationButtons();


		ApplyMapPage(
			safe
		);


		ApplyShopPage(
			safe
		);


		ApplyLabPage(
			safe
		);


		ApplyStatsOverlay(
			safe
		);


		ApplyPrestigeOverlay(
			safe
		);
	}


	// ==================================================
	// MAIN GAME
	// ==================================================

	private void ApplyMainLayout(
		SafeInsets safe)
	{
		MarginContainer? main =
			_root.GetNodeOrNull<MarginContainer>(
				"MarginContainer"
			);


		if (main == null)
			return;


		main.AddThemeConstantOverride(
			"margin_left",
			MainHorizontalPadding
			+ Ceil(
				safe.Left
			)
		);


		main.AddThemeConstantOverride(
			"margin_top",
			MainExtraTopPadding
			+ Ceil(
				safe.Top
			)
		);


		main.AddThemeConstantOverride(
			"margin_right",
			MainHorizontalPadding
			+ Ceil(
				safe.Right
			)
		);


		main.AddThemeConstantOverride(
			"margin_bottom",
			0
		);


		main.OffsetBottom =
			-(
				BottomBarHeight
				+ safe.Bottom
			);


		VBoxContainer? layout =
			_root.GetNodeOrNull<VBoxContainer>(
				"MarginContainer/VBoxContainer"
			);


		if (layout != null)
		{
			layout.AddThemeConstantOverride(
				"separation",
				0
			);
		}
	}


	// ==================================================
	// BOTTOM BAR
	// ==================================================

	private void ApplyBottomBar(
		SafeInsets safe)
	{
		Control? bar =
			_root.GetNodeOrNull<Control>(
				"BottomBar"
			);


		if (bar == null)
			return;


		bar.OffsetTop =
			-(
				BottomBarHeight
				+ safe.Bottom
			);


		bar.OffsetBottom =
			0;


		MarginContainer? margin =
			_root.GetNodeOrNull<MarginContainer>(
				"BottomBar/Margin"
			);


		if (margin == null)
			return;


		margin.AddThemeConstantOverride(
			"margin_left",
			BottomHorizontalPadding
			+ Ceil(
				safe.Left
			)
		);


		margin.AddThemeConstantOverride(
			"margin_right",
			BottomHorizontalPadding
			+ Ceil(
				safe.Right
			)
		);


		margin.AddThemeConstantOverride(
			"margin_top",
			BottomTopPadding
		);


		margin.AddThemeConstantOverride(
			"margin_bottom",
			BottomExtraBottomPadding
			+ Ceil(
				safe.Bottom
			)
		);


		HBoxContainer? hbox =
			_root.GetNodeOrNull<HBoxContainer>(
				"BottomBar/Margin/HBox"
			);


		if (hbox != null)
		{
			hbox.AddThemeConstantOverride(
				"separation",
				10
			);
		}
	}


	// ==================================================
	// NAVIGATION BUTTONS
	// ==================================================

	private void ApplyNavigationButtons()
	{
		ConfigureNavigationButton(
			_root.GetNodeOrNull<TextureButton>(
				"BottomBar/Margin/HBox/MapButton"
			)
		);


		ConfigureNavigationButton(
			_root.GetNodeOrNull<TextureButton>(
				"BottomBar/Margin/HBox/LabButton"
			)
		);


		ConfigureNavigationButton(
			_root.GetNodeOrNull<TextureButton>(
				"BottomBar/Margin/HBox/ShopButton"
			)
		);
	}


	private static void ConfigureNavigationButton(
		TextureButton? button)
	{
		if (button == null)
			return;


		button.CustomMinimumSize =
			new Vector2(
				NavigationButtonWidth,
				NavigationButtonHeight
			);


		button.IgnoreTextureSize =
			true;


		button.StretchMode =
			TextureButton.StretchModeEnum
				.KeepAspectCentered;


		button.SizeFlagsVertical =
			Control.SizeFlags.ShrinkCenter;
	}


	// ==================================================
	// MAP
	// ==================================================

	private void ApplyMapPage(
		SafeInsets safe)
	{
		Control? page =
			_root.GetNodeOrNull<Control>(
				"MapPage"
			);


		if (page == null)
			return;


		page.OffsetBottom =
			-(
				BottomBarHeight
				+ safe.Bottom
			);


		MarginContainer? margin =
			_root.GetNodeOrNull<MarginContainer>(
				"MapPage/Margin"
			);


		if (margin == null)
			return;


		margin.AddThemeConstantOverride(
			"margin_left",
			28
			+ Ceil(
				safe.Left
			)
		);


		margin.AddThemeConstantOverride(
			"margin_top",
			20
			+ Ceil(
				safe.Top
			)
		);


		margin.AddThemeConstantOverride(
			"margin_right",
			28
			+ Ceil(
				safe.Right
			)
		);


		margin.AddThemeConstantOverride(
			"margin_bottom",
			18
		);
	}


	// ==================================================
	// SHOP
	// ==================================================

	private void ApplyShopPage(
		SafeInsets safe)
	{
		Control? page =
			_root.GetNodeOrNull<Control>(
				"ShopPage"
			);


		if (page == null)
			return;


		/*
		 * Background remains FULLSCREEN.
		 *
		 * This is important:
		 * the background may continue behind the notch,
		 * but the actual shop content must not.
		 */
		page.AnchorLeft =
			0.0f;


		page.AnchorTop =
			0.0f;


		page.AnchorRight =
			1.0f;


		page.AnchorBottom =
			1.0f;


		page.OffsetLeft =
			0.0f;


		page.OffsetTop =
			0.0f;


		page.OffsetRight =
			0.0f;


		page.OffsetBottom =
			0.0f;


		MarginContainer? margin =
			_root.GetNodeOrNull<MarginContainer>(
				"ShopPage/ShopMargin"
			);


		if (margin == null)
			return;


		margin.AnchorLeft =
			0.0f;


		margin.AnchorTop =
			0.0f;


		margin.AnchorRight =
			1.0f;


		margin.AnchorBottom =
			1.0f;


		margin.OffsetLeft =
			0.0f;


		margin.OffsetTop =
			0.0f;


		margin.OffsetRight =
			0.0f;


		/*
		 * Content ends exactly above the BottomBar.
		 */
		margin.OffsetBottom =
			-(
				BottomBarHeight
				+ safe.Bottom
			);


		margin.AddThemeConstantOverride(
			"margin_left",
			ShopHorizontalPadding
			+ Ceil(
				safe.Left
			)
		);


		/*
		 * SHOP and the Data-Shard bar are moved below
		 * the phone's notch / status area.
		 */
		margin.AddThemeConstantOverride(
			"margin_top",
			Ceil(
				safe.Top
			)
			+ ShopExtraTopPadding
		);


		margin.AddThemeConstantOverride(
			"margin_right",
			ShopHorizontalPadding
			+ Ceil(
				safe.Right
			)
		);


		margin.AddThemeConstantOverride(
			"margin_bottom",
			0
		);
	}


	// ==================================================
	// LAB
	// ==================================================

	private void ApplyLabPage(
		SafeInsets safe)
	{
		Control? lab =
			_root.GetNodeOrNull<Control>(
				"LabPage"
			);


		if (lab == null)
			return;


		lab.OffsetBottom =
			-(
				BottomBarHeight
				+ safe.Bottom
			);


		MarginContainer? margin =
			null;


		foreach (
			Node child
			in lab.GetChildren()
		)
		{
			if (
				child
				is MarginContainer found
			)
			{
				margin =
					found;


				break;
			}
		}


		if (margin == null)
			return;


		margin.AddThemeConstantOverride(
			"margin_left",
			18
			+ Ceil(
				safe.Left
			)
		);


		margin.AddThemeConstantOverride(
			"margin_top",
			Ceil(
				safe.Top
			)
		);


		margin.AddThemeConstantOverride(
			"margin_right",
			18
			+ Ceil(
				safe.Right
			)
		);


		margin.AddThemeConstantOverride(
			"margin_bottom",
			0
		);
	}


	// ==================================================
	// STATS
	// ==================================================

	private void ApplyStatsOverlay(
		SafeInsets safe)
	{
		PanelContainer? panel =
			_root.GetNodeOrNull<PanelContainer>(
				"StatsOverlay/StatsPanel"
			);


		if (panel == null)
			return;


		panel.AnchorLeft =
			0;


		panel.AnchorTop =
			0;


		panel.AnchorRight =
			1;


		panel.AnchorBottom =
			1;


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
	}


	// ==================================================
	// PRESTIGE
	// ==================================================

	private void ApplyPrestigeOverlay(
		SafeInsets safe)
	{
		PanelContainer? panel =
			_root.GetNodeOrNull<PanelContainer>(
				"PrestigeConfirmOverlay/Panel"
			);


		if (panel == null)
			return;


		panel.AnchorLeft =
			0;


		panel.AnchorTop =
			0;


		panel.AnchorRight =
			1;


		panel.AnchorBottom =
			1;


		panel.OffsetLeft =
			safe.Left
			+ 42;


		panel.OffsetRight =
			-(
				safe.Right
				+ 42
			);


		panel.OffsetTop =
			safe.Top
			+ 95;


		panel.OffsetBottom =
			-(
				safe.Bottom
				+ 175
			);
	}


	// ==================================================
	// SAFE AREA
	// ==================================================

	private SafeInsets GetSafeInsets()
	{
		string os =
			OS.GetName();


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
			return new SafeInsets(
				0,
				34,
				0,
				24
			);
		}


		float scaleX =
			viewportRect.Size.X
			/ windowSize.X;


		float scaleY =
			viewportRect.Size.Y
			/ windowSize.Y;


		float left =
			safeArea.Position.X
			* scaleX;


		float top =
			safeArea.Position.Y
			* scaleY;


		float rightPhysical =
			windowSize.X
			- (
				safeArea.Position.X
				+ safeArea.Size.X
			);


		float bottomPhysical =
			windowSize.Y
			- (
				safeArea.Position.Y
				+ safeArea.Size.Y
			);


		float right =
			rightPhysical
			* scaleX;


		float bottom =
			bottomPhysical
			* scaleY;


		top =
			MathF.Max(
				top,
				26.0f
			);


		bottom =
			MathF.Max(
				bottom,
				12.0f
			);


		return new SafeInsets(
			MathF.Max(
				0,
				left
			),

			MathF.Max(
				0,
				top
			),

			MathF.Max(
				0,
				right
			),

			MathF.Max(
				0,
				bottom
			)
		);
	}


	// ==================================================
	// UTIL
	// ==================================================

	private static int Ceil(
		float value)
	{
		return (int)
			MathF.Ceiling(
				value
			);
	}
}
