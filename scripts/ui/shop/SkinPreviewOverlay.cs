using Godot;
using System;

namespace IdleAi;

public sealed class SkinPreviewOverlay
{
	private readonly Game _root;

	private readonly string _overlayName;

	private Control _overlay =
		null!;

	private PanelContainer _panel =
		null!;

	private Label _title =
		null!;

	private TextureRect _image =
		null!;

	private Label _hint =
		null!;

	private Tween? _openTween;

	private ulong _blockCloseUntil;


	public bool Visible =>
		_overlay != null
		&& _overlay.Visible;


	public SkinPreviewOverlay(
		Game root,
		string overlayName = "SkinPreviewOverlay")
	{
		_root =
			root;

		_overlayName =
			overlayName;
	}


	public void Initialize()
	{
		CreateUi();

		Close();
	}


	public void Open(
		Texture2D? texture,
		string title)
	{
		if (texture == null)
			return;

		_title.Text =
			title;

		_image.Texture =
			texture;

		_blockCloseUntil =
			Time.GetTicksMsec()
				+ 120;

		_overlay.Show();

		/*
		 * The BottomBar is intentionally moved to front by the
		 * normal Shop navigation code. Moving this overlay to
		 * front here guarantees the large preview is above the
		 * collection header AND above the BottomBar.
		 */
		_overlay.MoveToFront();

		PlayOpenAnimation();
	}


	public void Close()
	{
		_openTween?.Kill();

		if (_panel != null)
		{
			_panel.Scale =
				Vector2.One;

			_panel.Modulate =
				Colors.White;
		}

		if (_overlay != null)
		{
			_overlay.Hide();
		}
	}


	private void CreateUi()
	{
		_overlay =
			new Control
			{
				Name =
					_overlayName,

				MouseFilter =
					Control.MouseFilterEnum.Stop,

				ZIndex =
					1000
			};

		_overlay.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);

		/*
		 * Requirement: tapping ANYWHERE closes the preview.
		 *
		 * All visual children below use MouseFilter.Ignore, so
		 * the full-screen overlay itself receives taps even when
		 * the player taps directly on the large image/panel.
		 */
		_overlay.GuiInput +=
			OnOverlayInput;

		_root.AddChild(
			_overlay
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
					Control.MouseFilterEnum.Ignore
			};

		dim.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);

		_overlay.AddChild(
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
			28;

		center.OffsetTop =
			28;

		center.OffsetRight =
			-28;

		center.OffsetBottom =
			-28;

		_overlay.AddChild(
			center
		);

		_panel =
			new PanelContainer
			{
				CustomMinimumSize =
					new Vector2(
						620,
						760
					),

				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};

		_panel.AddThemeStyleboxOverride(
			"panel",
			CreatePanelStyle()
		);

		center.AddChild(
			_panel
		);

		MarginContainer margin =
			new()
			{
				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};

		margin.AddThemeConstantOverride(
			"margin_left",
			28
		);

		margin.AddThemeConstantOverride(
			"margin_top",
			24
		);

		margin.AddThemeConstantOverride(
			"margin_right",
			28
		);

		margin.AddThemeConstantOverride(
			"margin_bottom",
			22
		);

		_panel.AddChild(
			margin
		);

		VBoxContainer box =
			new()
			{
				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,

				SizeFlagsVertical =
					Control.SizeFlags.ExpandFill,

				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};

		box.AddThemeConstantOverride(
			"separation",
			14
		);

		margin.AddChild(
			box
		);

		_title =
			new Label
			{
				HorizontalAlignment =
					HorizontalAlignment.Center,

				AutowrapMode =
					TextServer.AutowrapMode.WordSmart,

				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};

		_title.AddThemeFontSizeOverride(
			"font_size",
			24
		);

		_title.AddThemeColorOverride(
			"font_color",
			Colors.White
		);

		box.AddChild(
			_title
		);

		PanelContainer imageFrame =
			new()
			{
				CustomMinimumSize =
					new Vector2(
						0,
						610
					),

				SizeFlagsHorizontal =
					Control.SizeFlags.ExpandFill,

				SizeFlagsVertical =
					Control.SizeFlags.ExpandFill,

				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};

		imageFrame.AddThemeStyleboxOverride(
			"panel",
			CreateImageFrameStyle()
		);

		box.AddChild(
			imageFrame
		);

		_image =
			new TextureRect
			{
				ExpandMode =
					TextureRect.ExpandModeEnum.IgnoreSize,

				StretchMode =
					TextureRect.StretchModeEnum.KeepAspectCentered,

				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};

		_image.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);

		_image.OffsetLeft =
			14;

		_image.OffsetTop =
			14;

		_image.OffsetRight =
			-14;

		_image.OffsetBottom =
			-14;

		imageFrame.AddChild(
			_image
		);

		_hint =
			new Label
			{
				Text =
					"TAP ANYWHERE TO CLOSE",

				HorizontalAlignment =
					HorizontalAlignment.Center,

				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};

		_hint.AddThemeFontSizeOverride(
			"font_size",
			12
		);

		_hint.AddThemeColorOverride(
			"font_color",
			new Color(
				0.68f,
				0.72f,
				0.80f,
				1.0f
			)
		);

		box.AddChild(
			_hint
		);
	}


	private void OnOverlayInput(
		InputEvent @event)
	{
		if (
			Time.GetTicksMsec()
				< _blockCloseUntil
		)
		{
			return;
		}

		bool released =
			false;

		if (
			@event
				is InputEventScreenTouch touch
			&& !touch.Pressed
		)
		{
			released =
				true;
		}

		if (
			@event
				is InputEventMouseButton mouse
			&& !mouse.Pressed
			&& mouse.ButtonIndex
				== MouseButton.Left
		)
		{
			released =
				true;
		}

		if (!released)
			return;

		_overlay
			.GetViewport()
			.SetInputAsHandled();

		Callable
			.From(
				Close
			)
			.CallDeferred();
	}


	private void PlayOpenAnimation()
	{
		_openTween?.Kill();

		_panel.PivotOffset =
			_panel.Size
			/ 2.0f;

		_panel.Scale =
			new Vector2(
				0.92f,
				0.92f
			);

		_panel.Modulate =
			new Color(
				1.0f,
				1.0f,
				1.0f,
				0.70f
			);

		_openTween =
			_root.CreateTween();

		_openTween.SetParallel(
			true
		);

		_openTween.TweenProperty(
			_panel,
			"scale",
			new Vector2(
				1.015f,
				1.015f
			),
			0.18
		)
		.SetTrans(
			Tween.TransitionType.Cubic
		)
		.SetEase(
			Tween.EaseType.Out
		);

		_openTween.TweenProperty(
			_panel,
			"modulate:a",
			1.0f,
			0.18
		);

		_openTween
			.Chain()
			.TweenProperty(
				_panel,
				"scale",
				Vector2.One,
				0.11
			)
			.SetTrans(
				Tween.TransitionType.Sine
			)
			.SetEase(
				Tween.EaseType.Out
			);
	}


	private static StyleBoxFlat CreatePanelStyle()
	{
		return new StyleBoxFlat
		{
			BgColor =
				new Color(
					0.02f,
					0.035f,
					0.06f,
					0.985f
				),

			BorderWidthLeft =
				2,

			BorderWidthTop =
				2,

			BorderWidthRight =
				2,

			BorderWidthBottom =
				2,

			BorderColor =
				new Color(
					0.0f,
					0.65f,
					1.0f,
					0.82f
				),

			CornerRadiusTopLeft =
				18,

			CornerRadiusTopRight =
				18,

			CornerRadiusBottomLeft =
				18,

			CornerRadiusBottomRight =
				18,

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


	private static StyleBoxFlat CreateImageFrameStyle()
	{
		return new StyleBoxFlat
		{
			BgColor =
				new Color(
					0.015f,
					0.025f,
					0.045f,
					0.96f
				),

			BorderWidthLeft =
				1,

			BorderWidthTop =
				1,

			BorderWidthRight =
				1,

			BorderWidthBottom =
				1,

			BorderColor =
				new Color(
					0.2f,
					0.45f,
					0.75f,
					0.55f
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
}
