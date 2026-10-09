using Godot;
using System;

namespace IdleAi;


/*
 * Cold-start information / news overlay.
 *
 * It is shown once per running app process. Android/iOS suspend/resume keeps
 * the same process and therefore does NOT show it again. After the app process
 * has been fully closed/killed, static state is reset and the next launch
 * shows the overlay again.
 *
 * Content is intentionally kept in StartupInfoContent.cs so future update
 * cards/skins/news can be changed without editing this UI class.
 */
public sealed partial class StartupInfoOverlayController
	: Node
{
	private const float PanelMaxWidth =
		650.0f;

	private const float CloseSize =
		58.0f;

	private static readonly Texture2D CloseTexture =
		GD.Load<Texture2D>(
			"res://assets/ui/x.png"
		);

	private static bool _shownThisProcess;


	private readonly Game _root;


	private Control _overlay =
		null!;

	private PanelContainer _panel =
		null!;

	private ScrollContainer _scroll =
		null!;

	private MobileScrollController _mobileScroll =
		null!;


	public StartupInfoOverlayController(
		Game root)
	{
		_root =
			root;
	}


	public async void InitializeAndShow()
	{
		if (_shownThisProcess)
			return;

		_shownThisProcess =
			true;

		CreateUi();

		/*
		 * Wait until Game._Ready(), save loading, the normal HUD and deferred
		 * feature bootstraps have had a chance to settle. This prevents the
		 * overlay from briefly appearing behind another startup-built page.
		 */
		await _root.ToSignal(
			_root.GetTree(),
			SceneTree.SignalName.ProcessFrame
		);

		await _root.ToSignal(
			_root.GetTree(),
			SceneTree.SignalName.ProcessFrame
		);

		if (
			!GodotObject.IsInstanceValid(
				_overlay
			)
		)
		{
			return;
		}

		_overlay.Show();
		_overlay.MoveToFront();

		_mobileScroll?.ScrollToTop();
	}


	public void Hide()
	{
		_mobileScroll?.ResetMotion();

		_overlay?.Hide();
	}


	private void CreateUi()
	{
		_overlay =
			new Control
			{
				Name =
					"ColdStartInfoOverlay",

				MouseFilter =
					Control.MouseFilterEnum.Stop,

				ZIndex =
					4000,

				Visible =
					false
			};

		_overlay.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);

		_root.AddChild(
			_overlay
		);


		ColorRect dim =
			new()
			{
				Color =
					new Color(
						0.0f,
						0.0f,
						0.0f,
						0.88f
					),

				MouseFilter =
					Control.MouseFilterEnum.Stop
			};

		dim.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);

		_overlay.AddChild(
			dim
		);


		MarginContainer outerMargin =
			new()
			{
				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};

		outerMargin.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);

		outerMargin.AddThemeConstantOverride(
			"margin_left",
			18
		);

		outerMargin.AddThemeConstantOverride(
			"margin_top",
			(int)MathF.Ceiling(
				GetSafeTopInset()
				+ 18.0f
			)
		);

		outerMargin.AddThemeConstantOverride(
			"margin_right",
			18
		);

		outerMargin.AddThemeConstantOverride(
			"margin_bottom",
			34
		);

		_overlay.AddChild(
			outerMargin
		);


		CenterContainer center =
			new()
			{
				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};

		outerMargin.AddChild(
			center
		);


		_panel =
			new PanelContainer
			{
				CustomMinimumSize =
					new Vector2(
						MathF.Min(
							PanelMaxWidth,
							_root.GetViewport()
								.GetVisibleRect()
								.Size.X
								- 36.0f
						),
						0
					),

				SizeFlagsVertical =
					Control.SizeFlags.ExpandFill,

				MouseFilter =
					Control.MouseFilterEnum.Stop
			};

		_panel.AddThemeStyleboxOverride(
			"panel",
			CreatePanelStyle()
		);

		center.AddChild(
			_panel
		);


		MarginContainer panelMargin =
			new();

		panelMargin.AddThemeConstantOverride(
			"margin_left",
			22
		);

		panelMargin.AddThemeConstantOverride(
			"margin_top",
			24
		);

		panelMargin.AddThemeConstantOverride(
			"margin_right",
			22
		);

		panelMargin.AddThemeConstantOverride(
			"margin_bottom",
			22
		);

		_panel.AddChild(
			panelMargin
		);


		VBoxContainer main =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,

				SizeFlagsVertical =
					Control.SizeFlags.ExpandFill
			};

		main.AddThemeConstantOverride(
			"separation",
			10
		);

		panelMargin.AddChild(
			main
		);


		CreateHeader(
			main
		);

		main.AddChild(
			new HSeparator()
		);


		_scroll =
			new ScrollContainer
			{
				HorizontalScrollMode =
					ScrollContainer.ScrollMode.Disabled,

				VerticalScrollMode =
					ScrollContainer.ScrollMode.ShowNever,

				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,

				SizeFlagsVertical =
					Control.SizeFlags.ExpandFill,

				MouseFilter =
					Control.MouseFilterEnum.Pass
			};

		main.AddChild(
			_scroll
		);


		MarginContainer scrollMargin =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};

		scrollMargin.AddThemeConstantOverride(
			"margin_top",
			6
		);

		scrollMargin.AddThemeConstantOverride(
			"margin_bottom",
			12
		);

		_scroll.AddChild(
			scrollMargin
		);


		VBoxContainer content =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill
			};

		content.AddThemeConstantOverride(
			"separation",
			14
		);

		scrollMargin.AddChild(
			content
		);


		foreach (
			StartupInfoItem item
				in StartupInfoContent.Build()
		)
		{
			content.AddChild(
				CreateContentCard(
					item
				)
			);
		}


		Button continueButton =
			new()
			{
				Text =
					"CONTINUE",

				CustomMinimumSize =
					new Vector2(
						0,
						60
					),

				FocusMode =
					Control.FocusModeEnum.None
			};

		ApplyPrimaryButtonStyle(
			continueButton
		);

		continueButton.Pressed +=
			Hide;

		main.AddChild(
			continueButton
		);


		AddCloseButton();


		_mobileScroll =
			new MobileScrollController
			{
				Name =
					"StartupInfoMobileScroll"
			};

		_overlay.AddChild(
			_mobileScroll
		);

		_mobileScroll.Setup(
			_scroll,
			allowTopOverscroll: true,
			allowBottomOverscroll: true
		);
	}


	private void CreateHeader(
		VBoxContainer parent)
	{
		Label eyebrow =
			CreateLabel(
				12,
				StartupInfoContent.Eyebrow
			);

		eyebrow.HorizontalAlignment =
			HorizontalAlignment.Left;

		eyebrow.AddThemeColorOverride(
			"font_color",
			new Color(
				0.48f,
				0.82f,
				1.0f,
				1.0f
			)
		);

		parent.AddChild(
			eyebrow
		);


		Label headline =
			CreateLabel(
				31,
				StartupInfoContent.Headline
			);

		headline.HorizontalAlignment =
			HorizontalAlignment.Left;

		parent.AddChild(
			headline
		);


		Label subheadline =
			CreateLabel(
				14,
				StartupInfoContent.Subheadline
			);

		subheadline.HorizontalAlignment =
			HorizontalAlignment.Left;

		subheadline.AutowrapMode =
			TextServer.AutowrapMode.WordSmart;

		subheadline.AddThemeColorOverride(
			"font_color",
			new Color(
				0.68f,
				0.74f,
				0.82f,
				1.0f
			)
		);

		parent.AddChild(
			subheadline
		);
	}


	private Control CreateContentCard(
		StartupInfoItem item)
	{
		PanelContainer panel =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,

				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};

		panel.AddThemeStyleboxOverride(
			"panel",
			CreateCardStyle()
		);


		MarginContainer margin =
			new();

		margin.AddThemeConstantOverride(
			"margin_left",
			16
		);

		margin.AddThemeConstantOverride(
			"margin_top",
			16
		);

		margin.AddThemeConstantOverride(
			"margin_right",
			16
		);

		margin.AddThemeConstantOverride(
			"margin_bottom",
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


		if (
			!string.IsNullOrWhiteSpace(
				item.ImagePath
			)
			&& ResourceLoader.Exists(
				item.ImagePath
			)
		)
		{
			Texture2D? texture =
				GD.Load<Texture2D>(
					item.ImagePath
				);

			if (texture != null)
			{
				TextureRect image =
					new()
					{
						Texture =
							texture,

						CustomMinimumSize =
							new Vector2(
								0,
								150
							),

						ExpandMode =
							TextureRect.ExpandModeEnum.IgnoreSize,

						StretchMode =
							TextureRect.StretchModeEnum.KeepAspectCentered,

						MouseFilter =
							Control.MouseFilterEnum.Ignore
					};

				box.AddChild(
					image
				);
			}
		}


		Label kicker =
			CreateLabel(
				11,
				item.Kicker
			);

		kicker.HorizontalAlignment =
			HorizontalAlignment.Left;

		kicker.AddThemeColorOverride(
			"font_color",
			new Color(
				0.95f,
				0.69f,
				0.24f,
				1.0f
			)
		);

		box.AddChild(
			kicker
		);


		Label title =
			CreateLabel(
				20,
				item.Title
			);

		title.HorizontalAlignment =
			HorizontalAlignment.Left;

		title.AutowrapMode =
			TextServer.AutowrapMode.WordSmart;

		box.AddChild(
			title
		);


		Label body =
			CreateLabel(
				13,
				item.Body
			);

		body.HorizontalAlignment =
			HorizontalAlignment.Left;

		body.AutowrapMode =
			TextServer.AutowrapMode.WordSmart;

		body.AddThemeColorOverride(
			"font_color",
			new Color(
				0.72f,
				0.77f,
				0.84f,
				1.0f
			)
		);

		box.AddChild(
			body
		);


		return panel;
	}


	private void AddCloseButton()
	{
		TextureButton close =
			new()
			{
				Name =
					"StartupInfoClose",

				TextureNormal =
					CloseTexture,

				IgnoreTextureSize =
					true,

				StretchMode =
					TextureButton.StretchModeEnum.KeepAspectCentered,

				FocusMode =
					Control.FocusModeEnum.None,

				MouseFilter =
					Control.MouseFilterEnum.Stop,

				ZIndex =
					50
			};

		float safeTop =
			GetSafeTopInset();

		close.AnchorLeft =
			1.0f;

		close.AnchorTop =
			0.0f;

		close.AnchorRight =
			1.0f;

		close.AnchorBottom =
			0.0f;

		close.OffsetLeft =
			-CloseSize
			- 16.0f;

		close.OffsetTop =
			safeTop
			+ 12.0f;

		close.OffsetRight =
			-16.0f;

		close.OffsetBottom =
			safeTop
			+ 12.0f
			+ CloseSize;

		close.Pressed +=
			Hide;

		/*
		 * Keep the X outside PanelContainer. Container parents automatically
		 * control child rectangles; placing the close button directly on the
		 * overlay guarantees its top-right offsets stay intact.
		 */
		_overlay.AddChild(
			close
		);

		close.MoveToFront();
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
			return 28.0f;
		}

		float scaleY =
			viewportRect.Size.Y
			/ windowSize.Y;

		return MathF.Max(
			safeArea.Position.Y
				* scaleY,
			22.0f
		);
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


	private static StyleBoxFlat CreatePanelStyle()
	{
		return new StyleBoxFlat
		{
			BgColor =
				new Color(
					0.012f,
					0.020f,
					0.038f,
					0.995f
				),

			BorderColor =
				new Color(
					0.22f,
					0.66f,
					0.94f,
					0.88f
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
				22,

			CornerRadiusTopRight =
				22,

			CornerRadiusBottomLeft =
				22,

			CornerRadiusBottomRight =
				22,

			ShadowColor =
				new Color(
					0,
					0,
					0,
					0.55f
				),

			ShadowSize =
				18
		};
	}


	private static StyleBoxFlat CreateCardStyle()
	{
		return new StyleBoxFlat
		{
			BgColor =
				new Color(
					0.028f,
					0.043f,
					0.072f,
					0.98f
				),

			BorderColor =
				new Color(
					0.20f,
					0.30f,
					0.43f,
					0.74f
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
				16,

			CornerRadiusTopRight =
				16,

			CornerRadiusBottomLeft =
				16,

			CornerRadiusBottomRight =
				16
		};
	}


	private static void ApplyPrimaryButtonStyle(
		Button button)
	{
		Color accent =
			new(
				0.20f,
				0.70f,
				1.0f,
				1.0f
			);

		button.AddThemeStyleboxOverride(
			"normal",
			CreateButtonStyle(
				accent.Darkened(
					0.52f
				),
				accent
			)
		);

		button.AddThemeStyleboxOverride(
			"hover",
			CreateButtonStyle(
				accent.Darkened(
					0.34f
				),
				accent.Lightened(
					0.10f
				)
			)
		);

		button.AddThemeStyleboxOverride(
			"pressed",
			CreateButtonStyle(
				accent.Darkened(
					0.18f
				),
				accent
			)
		);

		button.AddThemeColorOverride(
			"font_color",
			Colors.White
		);

		button.AddThemeColorOverride(
			"font_hover_color",
			Colors.White
		);

		button.AddThemeColorOverride(
			"font_pressed_color",
			Colors.White
		);
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
}
