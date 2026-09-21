using Godot;

namespace IdleAi;

public readonly record struct RoomThemeTextures(
	Texture2D Bar,
	Texture2D CardNormal,
	Texture2D CardHover,
	Texture2D CardPressed
);


public static class RoomThemePalette
{
	public static RoomThemeTextures Create(
		int room)
	{
		(
			Color main,
			Color secondary
		) =
			GetColors(
				room
			);


		Texture2D bar =
			CreateGradientTexture(
				main,
				secondary
			);


		Texture2D normal =
			CreateGradientTexture(
				main.Darkened(
					0.18f
				),

				secondary.Darkened(
					0.18f
				)
			);


		Texture2D hover =
			CreateGradientTexture(
				main.Darkened(
					0.10f
				),

				secondary.Darkened(
					0.10f
				)
			);


		Texture2D pressed =
			CreateGradientTexture(
				main.Darkened(
					0.28f
				),

				secondary.Darkened(
					0.28f
				)
			);


		return new RoomThemeTextures(
			bar,
			normal,
			hover,
			pressed
		);
	}


	private static (
		Color Main,
		Color Secondary
	) GetColors(
		int room)
	{
		return room switch
		{
			0 =>
				(
					new Color(
						0.015f,
						0.27f,
						0.52f,
						1
					),

					new Color(
						0.01f,
						0.22f,
						0.44f,
						1
					)
				),

			1 =>
				(
					new Color(
						0.50f,
						0.07f,
						0.10f,
						1
					),

					new Color(
						0.41f,
						0.05f,
						0.08f,
						1
					)
				),

			2 =>
				(
					new Color(
						0.04f,
						0.39f,
						0.20f,
						1
					),

					new Color(
						0.03f,
						0.32f,
						0.16f,
						1
					)
				),

			3 =>
				(
					new Color(
						0.33f,
						0.10f,
						0.52f,
						1
					),

					new Color(
						0.27f,
						0.08f,
						0.44f,
						1
					)
				),

			_ =>
				(
					new Color(
						0.10f,
						0.10f,
						0.10f,
						1
					),

					new Color(
						0.08f,
						0.08f,
						0.08f,
						1
					)
				)
		};
	}


	private static GradientTexture2D CreateGradientTexture(
		Color first,
		Color second)
	{
		Gradient gradient =
			new();


		gradient.SetColor(
			0,
			first
		);


		gradient.SetColor(
			1,
			second
		);


		return new GradientTexture2D
		{
			Gradient =
				gradient,

			Width =
				512,

			Height =
				96,

			Fill =
				GradientTexture2D.FillEnum.Linear,

			FillFrom =
				new Vector2(
					0,
					0.5f
				),

			FillTo =
				new Vector2(
					1,
					0.5f
				)
		};
	}
}
