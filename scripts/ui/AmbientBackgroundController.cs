using Godot;
using System;

namespace IdleAi;

public sealed class AmbientBackgroundController
{
	private readonly Game _root;


	private Control _layer =
		null!;


	private GpuParticles2D _particles =
		null!;


	private readonly ColorRect[] _lightLines =
		new ColorRect[4];


	private Tween? _lineTween;

	private Tween? _pulseTween;


	private ColorRect _pulseOverlay =
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
		CreateLayer();

		CreateParticles();

		CreateLightLines();

		CreatePulseOverlay();
	}


	// ==================================================
	// MAIN AMBIENT LAYER
	// ==================================================

	private void CreateLayer()
	{
		_layer =
			new Control
			{
				MouseFilter =
					Control.MouseFilterEnum.Ignore
			};


		_layer.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);


		_root.AddChild(
			_layer
		);


		/*
		 * Root layout:
		 *
		 * 0 Background
		 * 1 Ambient
		 * 2+ UI
		 *
		 * Therefore ambient effects are visible
		 * above the room background but below
		 * machines and interface.
		 */

		_root.MoveChild(
			_layer,
			1
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
				Amount =
					26,

				Lifetime =
					7.0,

				Randomness =
					0.45f,

				Explosiveness =
					0.0f,

				Emitting =
					true,

				Position =
					new Vector2(
						360.0f,
						1280.0f
					)
			};


		ParticleProcessMaterial material =
			new()
			{
				EmissionShape =
					ParticleProcessMaterial
						.EmissionShapeEnum
						.Box,

				EmissionBoxExtents =
					new Vector3(
						360.0f,
						30.0f,
						0.0f
					),

				Direction =
					new Vector3(
						0.0f,
						-1.0f,
						0.0f
					),

				Spread =
					20.0f,

				InitialVelocityMin =
					20.0f,

				InitialVelocityMax =
					45.0f,

				Gravity =
					Vector3.Zero,

				ScaleMin =
					0.7f,

				ScaleMax =
					1.6f
			};


		Gradient particleGradient =
			new();


		particleGradient.SetColor(
			0,
			new Color(
				1,
				1,
				1,
				0
			)
		);


		particleGradient.AddPoint(
			0.15f,
			new Color(
				1,
				1,
				1,
				0.55f
			)
		);


		particleGradient.AddPoint(
			0.75f,
			new Color(
				1,
				1,
				1,
				0.30f
			)
		);


		particleGradient.SetColor(
			particleGradient.GetPointCount() - 1,
			new Color(
				1,
				1,
				1,
				0
			)
		);

		GradientTexture1D gradientTexture =
		new()
		{
			Gradient =
				particleGradient
		};

		material.ColorRamp =
			gradientTexture;


		_particles.ProcessMaterial =
			material;


		_layer.AddChild(
			_particles
		);
	}


	// ==================================================
	// LIGHT LINES
	// ==================================================

	private void CreateLightLines()
	{
		for (
			int i = 0;
			i < _lightLines.Length;
			i++
		)
		{
			ColorRect line =
				new()
				{
					Size =
						new Vector2(
							180.0f,
							2.0f
						),

					CustomMinimumSize =
						new Vector2(
							180.0f,
							2.0f
						),

					MouseFilter =
						Control.MouseFilterEnum.Ignore
				};


			line.Position =
				new Vector2(
					-220.0f,
					210.0f
					+ i * 245.0f
				);


			_lightLines[
				i
			] =
				line;


			_layer.AddChild(
				line
			);
		}


		StartLightLineAnimation();
	}


	// ==================================================
	// LIGHT LINE ANIMATION
	// ==================================================

	private void StartLightLineAnimation()
	{
		_lineTween?.Kill();


		_lineTween =
			_root.CreateTween();


		_lineTween.SetLoops();


		for (
			int i = 0;
			i < _lightLines.Length;
			i++
		)
		{
			ColorRect line =
				_lightLines[
					i
				];


			float startY =
				line.Position.Y;


			_lineTween.TweenProperty(
				line,
				"position",
				new Vector2(
					760.0f,
					startY
				),
				3.8
				+ i * 0.45
			)
			.SetTrans(
				Tween.TransitionType.Sine
			)
			.SetEase(
				Tween.EaseType.InOut
			);


			_lineTween.TweenCallback(
				Callable.From(
					() =>
					{
						line.Position =
							new Vector2(
								-220.0f,
								startY
							);
					}
				)
			);


			_lineTween.TweenInterval(
				0.6
				+ i * 0.15
			);
		}
	}


	// ==================================================
	// ROOM GLOW
	// ==================================================

	private void CreatePulseOverlay()
	{
		_pulseOverlay =
			new ColorRect
			{
				MouseFilter =
					Control.MouseFilterEnum.Ignore,

				Color =
					new Color(
						1,
						1,
						1,
						0.05f
					)
			};


		_pulseOverlay.SetAnchorsAndOffsetsPreset(
			Control.LayoutPreset.FullRect
		);


		_layer.AddChild(
			_pulseOverlay
		);


		StartPulseAnimation();
	}


	private void StartPulseAnimation()
	{
		_pulseTween?.Kill();


		_pulseOverlay.Modulate =
			new Color(
				1,
				1,
				1,
				0.03f
			);


		_pulseTween =
			_root.CreateTween();


		_pulseTween.SetLoops();


		_pulseTween.TweenProperty(
			_pulseOverlay,
			"modulate:a",
			0.10f,
			3.5
		)
		.SetTrans(
			Tween.TransitionType.Sine
		)
		.SetEase(
			Tween.EaseType.InOut
		);


		_pulseTween.TweenProperty(
			_pulseOverlay,
			"modulate:a",
			0.025f,
			3.5
		)
		.SetTrans(
			Tween.TransitionType.Sine
		)
		.SetEase(
			Tween.EaseType.InOut
		);
	}


	// ==================================================
	// CHANGE ROOM
	// ==================================================

	public void SetRoom(
		int roomIndex)
	{
		if (
			roomIndex ==
			_currentRoom
		)
		{
			return;
		}


		_currentRoom =
			roomIndex;


		Color roomColor =
			GetRoomColor(
				roomIndex
			);


		ApplyRoomColor(
			roomColor
		);


		ApplyRoomIntensity(
			roomIndex
		);
	}


	// ==================================================
	// ROOM COLORS
	// ==================================================

	private static Color GetRoomColor(
		int roomIndex)
	{
		return roomIndex switch
		{
			// Garage
			0 =>
				new Color(
					0.05f,
					0.65f,
					1.0f,
					1.0f
				),

			// Server Room
			1 =>
				new Color(
					1.0f,
					0.12f,
					0.18f,
					1.0f
				),

			// Data Center
			2 =>
				new Color(
					0.10f,
					1.0f,
					0.42f,
					1.0f
				),

			// Quantum Lab
			3 =>
				new Color(
					0.68f,
					0.22f,
					1.0f,
					1.0f
				),

			_ =>
				Colors.White
		};
	}


	// ==================================================
	// APPLY COLOR
	// ==================================================

	private void ApplyRoomColor(
		Color color)
	{
		_particles.Modulate =
			new Color(
				color.R,
				color.G,
				color.B,
				0.50f
			);


		foreach (
			ColorRect line
			in _lightLines
		)
		{
			line.Color =
				new Color(
					color.R,
					color.G,
					color.B,
					0.20f
				);
		}


		_pulseOverlay.Color =
			new Color(
				color.R,
				color.G,
				color.B,
				1.0f
			);
	}


	// ==================================================
	// ROOM INTENSITY
	// ==================================================

	private void ApplyRoomIntensity(
		int roomIndex)
	{
		switch (roomIndex)
		{
			// Garage:
			// quietest room
			case 0:
				_particles.Amount =
					18;

				_particles.Lifetime =
					8.0;

				break;


			// Server Room
			case 1:
				_particles.Amount =
					24;

				_particles.Lifetime =
					7.0;

				break;


			// Data Center
			case 2:
				_particles.Amount =
					30;

				_particles.Lifetime =
					6.5;

				break;


			// Quantum Lab:
			// most active room
			case 3:
				_particles.Amount =
					38;

				_particles.Lifetime =
					6.0;

				break;


			default:
				_particles.Amount =
					20;

				_particles.Lifetime =
					7.0;

				break;
		}


		_particles.Restart();
	}


	// ==================================================
	// CLEANUP
	// ==================================================

	public void Cleanup()
	{
		_lineTween?.Kill();

		_pulseTween?.Kill();
	}
}
