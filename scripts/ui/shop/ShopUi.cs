using Godot;

namespace IdleAi;

internal static class ShopUi
{
	public static readonly Texture2D Background =
		GD.Load<Texture2D>(
			"res://assets/background/lab_background.png"
		);


	public static readonly Texture2D DataShard =
		GD.Load<Texture2D>(
			"res://assets/shop/data_shard.png"
		);


	public static readonly Texture2D Boost =
		GD.Load<Texture2D>(
			"res://assets/shop/shop_boost.png"
		);


	public static readonly Texture2D Luck =
		GD.Load<Texture2D>(
			"res://assets/shop/shop_luck.png"
		);


	public static readonly Texture2D Instant =
		GD.Load<Texture2D>(
			"res://assets/shop/shop_instant.png"
		);


	public static readonly Texture2D Production =
		GD.Load<Texture2D>(
			"res://assets/shop/shop_production.png"
		);


	public static readonly Texture2D Offline =
		GD.Load<Texture2D>(
			"res://assets/shop/shop_offline.png"
		);


	public static readonly Texture2D Cosmetics =
		GD.Load<Texture2D>(
			"res://assets/shop/shop_cosmetics.png"
		);


	public static readonly Texture2D Locked =
		GD.Load<Texture2D>(
			"res://assets/shop/shop_locked.png"
		);


	public static readonly Texture2D Owned =
		GD.Load<Texture2D>(
			"res://assets/shop/shop_owned.png"
		);


	// ==================================================
	// COLORS
	// ==================================================

	public static readonly Color Accent =
		new(
			0.22f,
			0.78f,
			1.0f,
			1.0f
		);


	public static readonly Color AccentSoft =
		new(
			0.18f,
			0.56f,
			0.95f,
			1.0f
		);


	public static readonly Color Gold =
		new(
			1.0f,
			0.78f,
			0.24f,
			1.0f
		);


	public static readonly Color Green =
		new(
			0.24f,
			0.92f,
			0.58f,
			1.0f
		);


	public static readonly Color Purple =
		new(
			0.68f,
			0.42f,
			1.0f,
			1.0f
		);


	public static readonly Color Panel =
		new(
			0.022f,
			0.040f,
			0.078f,
			0.96f
		);


	public static readonly Color PanelBright =
		new(
			0.035f,
			0.065f,
			0.12f,
			0.98f
		);


	public static readonly Color TextPrimary =
		new(
			0.90f,
			0.96f,
			1.0f,
			1.0f
		);


	public static readonly Color TextSecondary =
		new(
			0.58f,
			0.72f,
			0.84f,
			1.0f
		);


	public static readonly Color DisabledText =
		new(
			0.40f,
			0.48f,
			0.58f,
			1.0f
		);


	// ==================================================
	// PANELS
	// ==================================================

	public static StyleBoxFlat CreatePanelStyle()
	{
		StyleBoxFlat style =
			new()
			{
				BgColor =
					Panel,

				BorderColor =
					new Color(
						Accent.R,
						Accent.G,
						Accent.B,
						0.45f
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
						0.38f
					),

				ShadowSize =
					10
			};


		style.ContentMarginLeft =
			0;

		style.ContentMarginRight =
			0;

		style.ContentMarginTop =
			0;

		style.ContentMarginBottom =
			0;


		return style;
	}


	public static StyleBoxFlat CreateShardPanelStyle()
	{
		return new StyleBoxFlat
		{
			BgColor =
				new Color(
					0.035f,
					0.085f,
					0.15f,
					0.98f
				),

			BorderColor =
				new Color(
					Accent.R,
					Accent.G,
					Accent.B,
					0.82f
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
					0.05f,
					0.48f,
					1.0f,
					0.22f
				),

			ShadowSize =
				14
		};
	}


	public static StyleBoxFlat CreateSectionStyle()
	{
		return new StyleBoxFlat
		{
			BgColor =
				new Color(
					0.02f,
					0.032f,
					0.062f,
					0.72f
				),

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


	// ==================================================
	// BUTTONS
	// ==================================================

	public static void ApplyPrimaryButtonStyle(
		Button button)
	{
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


		button.AddThemeColorOverride(
			"font_disabled_color",
			DisabledText
		);


		button.AddThemeFontSizeOverride(
			"font_size",
			16
		);


		button.AddThemeStyleboxOverride(
			"normal",
			CreateButtonStyle(
				new Color(
					0.08f,
					0.42f,
					0.72f,
					1.0f
				),

				new Color(
					0.18f,
					0.72f,
					1.0f,
					1.0f
				)
			)
		);


		button.AddThemeStyleboxOverride(
			"hover",
			CreateButtonStyle(
				new Color(
					0.10f,
					0.52f,
					0.88f,
					1.0f
				),

				new Color(
					0.32f,
					0.85f,
					1.0f,
					1.0f
				)
			)
		);


		button.AddThemeStyleboxOverride(
			"pressed",
			CreateButtonStyle(
				new Color(
					0.055f,
					0.30f,
					0.55f,
					1.0f
				),

				new Color(
					0.15f,
					0.62f,
					0.92f,
					1.0f
				)
			)
		);


		button.AddThemeStyleboxOverride(
			"disabled",
			CreateButtonStyle(
				new Color(
					0.045f,
					0.06f,
					0.085f,
					0.92f
				),

				new Color(
					0.16f,
					0.22f,
					0.30f,
					0.72f
				)
			)
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
				13,

			CornerRadiusTopRight =
				13,

			CornerRadiusBottomLeft =
				13,

			CornerRadiusBottomRight =
				13,

			ShadowColor =
				new Color(
					0,
					0,
					0,
					0.25f
				),

			ShadowSize =
				6
		};
	}


	// ==================================================
	// LABEL
	// ==================================================

	public static Label CreateLabel(
		int fontSize)
	{
		Label label =
			new()
			{
				HorizontalAlignment =
					HorizontalAlignment.Center,

				VerticalAlignment =
					VerticalAlignment.Center,

				AutowrapMode =
					TextServer.AutowrapMode.WordSmart,

				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};


		label.AddThemeFontSizeOverride(
			"font_size",
			fontSize
		);


		label.AddThemeColorOverride(
			"font_color",
			TextPrimary
		);


		return label;
	}


	public static Label CreateMutedLabel(
		int fontSize)
	{
		Label label =
			CreateLabel(
				fontSize
			);


		label.AddThemeColorOverride(
			"font_color",
			TextSecondary
		);


		return label;
	}
}
