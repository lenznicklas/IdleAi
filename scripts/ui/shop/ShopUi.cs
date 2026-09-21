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


	public static readonly Color Accent =
		new(
			0.25f,
			0.80f,
			1.0f,
			1.0f
		);


	public static readonly Color Panel =
		new(
			0.025f,
			0.045f,
			0.085f,
			0.94f
		);


	public static StyleBoxFlat CreatePanelStyle()
	{
		return new StyleBoxFlat
		{
			BgColor =
				Panel,

			BorderColor =
				new Color(
					Accent.R,
					Accent.G,
					Accent.B,
					0.58f
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
				16,

			CornerRadiusTopRight =
				16,

			CornerRadiusBottomLeft =
				16,

			CornerRadiusBottomRight =
				16
		};
	}


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
					TextServer.AutowrapMode.WordSmart
			};


		label.AddThemeFontSizeOverride(
			"font_size",
			fontSize
		);


		return label;
	}
}
