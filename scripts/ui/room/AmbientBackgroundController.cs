using Godot;

namespace IdleAi;

public sealed class AmbientBackgroundController
{
	private const int ParticleAmount =
		80;


	private const double ParticleLifetime =
		20;


	private const float ParticleOpacity =
		0.65f;


	private const float MinimumEmissionWidth =
		200.0f;


	private readonly Game _root;


	private GpuParticles2D _particles =
		null!;


	private TextureRect _background =
		null!;


	private int _currentRoom =
		-1;


	// ==================================================
	// CONSTRUCTOR
	// ==================================================

	public AmbientBackgroundController(
		Game root)
	{
		_root =
			root;
	}


	// ==================================================
	// INITIALIZE
	// ==================================================

	public void Initialize()
	{
		CacheBackground();

		ConfigureLayering();

		CreateParticles();


		_root.Resized +=
			UpdateParticlePosition;


		UpdateParticlePosition();


		SetRoom(
			0
		);
	}


	// ==================================================
	// BACKGROUND
	// ==================================================

	private void CacheBackground()
	{
		_background =
			_root.GetNode<TextureRect>(
				"Background"
			);
	}


	private void ConfigureLayering()
	{
		/*
		 * Background:
		 * Z = -10
		 *
		 * Particles:
		 * Z = -5
		 *
		 * Normal UI:
		 * Z = 0
		 */

		_background.ZIndex =
			-10;
	}


	// ==================================================
	// PARTICLES
	// ==================================================

	private void CreateParticles()
	{
		_particles =
			new GpuParticles2D
			{
				Name =
					"AmbientParticles",

				Amount =
					ParticleAmount,

				Lifetime =
					ParticleLifetime,

				Preprocess =
					ParticleLifetime,

				Randomness =
					0.45f,

				VisibilityRect =
					new Rect2(
						-1000,
						-2200,
						2000,
						2600
					),

				ZIndex =
					-5,

				Emitting =
					true
			};


		_root.AddChild(
			_particles
		);


		int backgroundIndex =
			_background.GetIndex();


		_root.MoveChild(
			_particles,
			backgroundIndex + 1
		);


		CreateParticleMaterial();

		CreateParticleTexture();
	}


	// ==================================================
	// PROCESS MATERIAL
	// ==================================================

	private void CreateParticleMaterial()
	{
		ParticleProcessMaterial material =
			new()
			{
				EmissionShape =
					ParticleProcessMaterial
						.EmissionShapeEnum
						.Box,

				EmissionBoxExtents =
					new Vector3(
						350.0f,
						14.0f,
						1.0f
					),

				Direction =
					new Vector3(
						0.0f,
						-1.0f,
						0.0f
					),

				Spread =
					28.0f,

				InitialVelocityMin =
					28.0f,

				InitialVelocityMax =
					60.0f,

				Gravity =
					new Vector3(
						0.0f,
						-3.0f,
						0.0f
					),

				ScaleMin =
					0.15f,

				ScaleMax =
					0.25f
			};


		_particles.ProcessMaterial =
			material;
	}


	// ==================================================
	// PARTICLE TEXTURE
	// ==================================================

	private void CreateParticleTexture()
	{
		Gradient gradient =
			new();


		gradient.SetColor(
			0,
			new Color(
				1.0f,
				1.0f,
				1.0f,
				0.0f
			)
		);


		gradient.AddPoint(
			0.18f,
			new Color(
				1.0f,
				1.0f,
				1.0f,
				1.0f
			)
		);


		gradient.AddPoint(
			0.58f,
			new Color(
				1.0f,
				1.0f,
				1.0f,
				0.75f
			)
		);


		gradient.AddPoint(
			0.82f,
			new Color(
				1.0f,
				1.0f,
				1.0f,
				0.25f
			)
		);


		gradient.SetColor(
			1,
			new Color(
				1.0f,
				1.0f,
				1.0f,
				0.0f
			)
		);


		GradientTexture2D texture =
			new()
			{
				Gradient =
					gradient,

				Width =
					40,

				Height =
					40,

				Fill =
					GradientTexture2D
						.FillEnum
						.Radial,

				FillFrom =
					new Vector2(
						0.5f,
						0.5f
					),

				FillTo =
					new Vector2(
						1.0f,
						0.5f
					)
			};


		_particles.Texture =
			texture;
	}


	// ==================================================
	// POSITION
	// ==================================================

	private void UpdateParticlePosition()
	{
		if (
			_particles == null
			|| !GodotObject.IsInstanceValid(
				_particles
			)
		)
		{
			return;
		}


		/*
		 * Emit slightly below the bottom of the screen.
		 */
		_particles.Position =
			new Vector2(
				_root.Size.X
					/ 2.0f,

				_root.Size.Y
					+ 30.0f
			);


		if (
			_particles.ProcessMaterial
			is ParticleProcessMaterial material
		)
		{
			float width =
				Mathf.Max(
					MinimumEmissionWidth,
					_root.Size.X
						* 0.58f
				);


			material.EmissionBoxExtents =
				new Vector3(
					width,
					14.0f,
					1.0f
				);
		}
	}


	// ==================================================
	// ROOM
	// ==================================================

	public void SetRoom(
		int roomIndex)
	{
		if (
			roomIndex
			== _currentRoom
		)
		{
			return;
		}


		_currentRoom =
			roomIndex;


		if (
			_particles.ProcessMaterial
			is not ParticleProcessMaterial material
		)
		{
			return;
		}


		Color roomColor =
			GetRoomColor(
				roomIndex
			);


		roomColor.A *=
			ParticleOpacity;


		material.Color =
			roomColor;


		_particles.Emitting =
			true;


		_particles.Restart();
	}


	// ==================================================
	// ROOM COLORS
	// ==================================================

	private static Color GetRoomColor(
		int roomIndex)
	{
		return roomIndex switch
		{
			// Garage - Blue
			0 =>
				new Color(
					0.12f,
					0.68f,
					1.00f,
					0.90f
				),

			// Server Room - Red
			1 =>
				new Color(
					1.00f,
					0.18f,
					0.20f,
					0.88f
				),

			// Data Center - Green
			2 =>
				new Color(
					0.15f,
					1.00f,
					0.45f,
					0.88f
				),

			// Quantum Lab - Purple
			3 =>
				new Color(
					0.72f,
					0.30f,
					1.00f,
					0.92f
				),

			_ =>
				new Color(
					1.0f,
					1.0f,
					1.0f,
					0.75f
				)
		};
	}
}
