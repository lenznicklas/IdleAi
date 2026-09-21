using Godot;

namespace IdleAi;

public sealed class AmbientBackgroundController
{
	private readonly Game _root;


	private GpuParticles2D _particles =
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
		CreateParticles();


		_root.Resized +=
			UpdateParticlePosition;


		UpdateParticlePosition();


		SetRoom(
			0
		);
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
					45,

				Lifetime =
					7.0,

				Preprocess =
					7.0,

				Randomness =
					0.45f,

				VisibilityRect =
					new Rect2(
						-500,
						-1800,
						1000,
						2000
					),

				ZIndex =
					-5,

				Emitting =
					true
			};


		_root.AddChild(
			_particles
		);


		/*
		 * Background is usually the first child.
		 *
		 * Put particles directly above the background
		 * but underneath the actual game UI.
		 */
		if (
			_root.GetChildCount()
			> 1
		)
		{
			_root.MoveChild(
				_particles,
				1
			);
		}


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
							350,
							12,
							1
						),

					Direction =
						new Vector3(
							0,
							-1,
							0
						),

					Spread =
						24.0f,

					InitialVelocityMin =
						22.0f,

					InitialVelocityMax =
						48.0f,

					Gravity =
						new Vector3(
							0,
							-4,
							0
						),

					ScaleMin =
						0.35f,

					ScaleMax =
						1.15f
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
				1,
				1,
				1,
				0.0f
			)
		);


		gradient.AddPoint(
			0.15f,
			new Color(
				1,
				1,
				1,
				0.8f
			)
		);


		gradient.AddPoint(
			0.65f,
			new Color(
				1,
				1,
				1,
				0.45f
			)
		);


		gradient.SetColor(
			1,
			new Color(
				1,
				1,
				1,
				0.0f
			)
		);


		GradientTexture2D texture =
			new()
				{
					Gradient =
						gradient,

					Width =
						32,

					Height =
						32,

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
		 * Emit particles just underneath the visible
		 * screen so they float upwards through the UI.
		 */
		_particles.Position =
			new Vector2(
				_root.Size.X
					/ 2.0f,

				_root.Size.Y
					+ 25.0f
			);


		/*
		 * Adjust the horizontal emission area to the
		 * current phone / viewport width.
		 */
		if (
			_particles.ProcessMaterial
			is ParticleProcessMaterial material
		)
		{
			material.EmissionBoxExtents =
				new Vector3(
					Mathf.Max(
						200.0f,
						_root.Size.X
							* 0.55f
					),

					12,

					1
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


		material.Color =
			GetRoomColor(
				roomIndex
			);


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
			0 =>
				new Color(
					0.10f,
					0.65f,
					1.00f,
					0.52f
				),

			1 =>
				new Color(
					1.00f,
					0.18f,
					0.20f,
					0.50f
				),

			2 =>
				new Color(
					0.15f,
					1.00f,
					0.45f,
					0.50f
				),

			3 =>
				new Color(
					0.72f,
					0.30f,
					1.00f,
					0.55f
				),

			_ =>
				new Color(
					1,
					1,
					1,
					0.40f
				)
		};
	}
}
