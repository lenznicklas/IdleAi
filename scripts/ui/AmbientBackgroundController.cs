using Godot;
using System;

namespace IdleAi;

public sealed class AmbientBackgroundController
{
	private const int ParticleCount =
		26;


	private const int LightLineCount =
		4;


	private readonly Game _root;


	private Control _layer =
		null!;


	private ColorRect _pulseOverlay =
		null!;


	private readonly ColorRect[] _particles =
		new ColorRect[ParticleCount];


	private readonly Tween?[] _particleTweens =
		new Tween?[ParticleCount];


	private readonly ColorRect[] _lightLines =
		new ColorRect[LightLineCount];


	private readonly Tween?[] _lineTweens =
		new Tween?[LightLineCount];


	private Tween? _pulseTween;


	private int _currentRoom =
		-1;


	private Color _currentColor =
		Colors.White;


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

		CreatePulseOverlay();

		CreateParticles();

		CreateLightLines();
	}


	// ==================================================
	// MAIN LAYER
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
		 * Background = index 0
		 * Ambient    = index 1
		 * UI         = above ambient
		 */

		_root.MoveChild(
			_layer,
			1
		);
	}


	// ==================================================
	// PULSE / AMBIENT GLOW
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
						0.025f
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
				0.35f
			);


		_pulseTween =
			_root.CreateTween();


		_pulseTween.SetLoops();


		_pulseTween.TweenProperty(
			_pulseOverlay,
			"modulate:a",
			0.75f,
			4.0
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
			0.25f,
			4.0
		)
		.SetTrans(
			Tween.TransitionType.Sine
		)
		.SetEase(
			Tween.EaseType.InOut
		);
	}


	// ==================================================
	// PARTICLES
	// ==================================================

	private void CreateParticles()
	{
		Vector2 viewportSize =
			GetViewportSize();


		for (
			int i = 0;
			i < ParticleCount;
			i++
		)
		{
			float size =
				(float)GD.RandRange(
					3.0,
					8.0
				);


			ColorRect particle =
				new()
				{
					Size =
						new Vector2(
							size,
							size
						),

					CustomMinimumSize =
						new Vector2(
							size,
							size
						),

					Color =
						new Color(
							1,
							1,
							1,
							0.65f
						),

					MouseFilter =
						Control.MouseFilterEnum.Ignore
				};


			/*
			 * Spread the initial particles over the
			 * full height so we don't have to wait
			 * several seconds before seeing them.
			 */

			particle.Position =
				new Vector2(
					(float)GD.RandRange(
						10.0,
						Math.Max(
							20.0,
							viewportSize.X - 10.0
						)
					),

					(float)GD.RandRange(
						0.0,
						Math.Max(
							100.0,
							viewportSize.Y
						)
					)
				);


			particle.Modulate =
				new Color(
					1,
					1,
					1,
					(float)GD.RandRange(
						0.30,
						0.80
					)
				);


			_particles[i] =
				particle;


			_layer.AddChild(
				particle
			);


			StartParticleAnimation(
				i
			);
		}
	}


	// ==================================================
	// PARTICLE ANIMATION
	// ==================================================

	private void StartParticleAnimation(
		int index)
	{
		ColorRect particle =
			_particles[
				index
			];


		_particleTweens[
			index
		]?.Kill();


		Vector2 viewportSize =
			GetViewportSize();


		float startX =
			particle.Position.X;


		float startY =
			particle.Position.Y;


		/*
		 * Slight sideways drift.
		 */

		float horizontalDrift =
			(float)GD.RandRange(
				-45.0,
				45.0
			);


		/*
		 * Different speeds keep the movement
		 * from looking synchronized.
		 */

		double duration =
			GD.RandRange(
				7.0,
				13.0
			);


		/*
		 * If a particle starts halfway up the
		 * screen, shorten its first trip.
		 */

		double distanceFactor =
			Math.Clamp(
				(
					startY + 30.0
				)
				/ (
					viewportSize.Y + 30.0
				),
				0.15,
				1.0
			);


		double firstDuration =
			duration
			* distanceFactor;


		Tween tween =
			_root.CreateTween();


		_particleTweens[
			index
		] =
			tween;


		/*
		 * First movement from the random initial
		 * position to the top.
		 */

		tween.TweenProperty(
			particle,
			"position",
			new Vector2(
				startX
				+ horizontalDrift,

				-30.0f
			),
			firstDuration
		)
		.SetTrans(
			Tween.TransitionType.Linear
		);


		tween.TweenCallback(
			Callable.From(
				() =>
					RestartParticle(
						index
					)
			)
		);
	}


	private void RestartParticle(
		int index)
	{
		if (
			index < 0
			|| index >= _particles.Length
		)
		{
			return;
		}


		ColorRect particle =
			_particles[
				index
			];


		if (
			particle == null
			|| !GodotObject.IsInstanceValid(
				particle
			)
		)
		{
			return;
		}


		Vector2 viewportSize =
			GetViewportSize();


		float size =
			(float)GD.RandRange(
				3.0,
				8.0
			);


		particle.Size =
			new Vector2(
				size,
				size
			);


		particle.CustomMinimumSize =
			new Vector2(
				size,
				size
			);


		/*
		 * THIS is the important part:
		 *
		 * Reappear slightly below the screen,
		 * then move upward.
		 */

		float startX =
			(float)GD.RandRange(
				10.0,
				Math.Max(
					20.0,
					viewportSize.X - 10.0
				)
			);


		particle.Position =
			new Vector2(
				startX,
				viewportSize.Y
				+ (float)GD.RandRange(
					5.0,
					40.0
				)
			);


		particle.Modulate =
			new Color(
				1,
				1,
				1,
				(float)GD.RandRange(
					0.30,
					0.80
				)
			);


		float horizontalDrift =
			(float)GD.RandRange(
				-55.0,
				55.0
			);


		double duration =
			GD.RandRange(
				8.0,
				14.0
			);


		Tween tween =
			_root.CreateTween();


		_particleTweens[
			index
		] =
			tween;


		tween.TweenProperty(
			particle,
			"position",
			new Vector2(
				startX
				+ horizontalDrift,

				-40.0f
			),
			duration
		)
		.SetTrans(
			Tween.TransitionType.Linear
		);


		tween.TweenCallback(
			Callable.From(
				() =>
					RestartParticle(
						index
					)
			)
		);
	}


	// ==================================================
	// LIGHT LINES
	// ==================================================

	private void CreateLightLines()
	{
		Vector2 viewportSize =
			GetViewportSize();


		for (
			int i = 0;
			i < LightLineCount;
			i++
		)
		{
			ColorRect line =
				new()
				{
					Size =
						new Vector2(
							140.0f,
							2.0f
						),

					CustomMinimumSize =
						new Vector2(
							140.0f,
							2.0f
						),

					Color =
						new Color(
							1,
							1,
							1,
							0.15f
						),

					MouseFilter =
						Control.MouseFilterEnum.Ignore
				};


			line.Position =
				new Vector2(
					(float)GD.RandRange(
						-300.0,
						0.0
					),

					viewportSize.Y
					* (
						0.20f
						+ i * 0.20f
					)
				);


			_lightLines[
				i
			] =
				line;


			_layer.AddChild(
				line
			);


			StartLightLineAnimation(
				i
			);
		}
	}


	// ==================================================
	// INDIVIDUAL LIGHT LINE
	// ==================================================

	private void StartLightLineAnimation(
		int index)
	{
		ColorRect line =
			_lightLines[
				index
			];


		_lineTweens[
			index
		]?.Kill();


		Vector2 viewportSize =
			GetViewportSize();


		float y =
			line.Position.Y;


		line.Position =
			new Vector2(
				(float)GD.RandRange(
					-350.0,
					-150.0
				),

				y
			);


		double duration =
			GD.RandRange(
				5.5,
				9.0
			);


		Tween tween =
			_root.CreateTween();


		_lineTweens[
			index
		] =
			tween;


		tween.TweenInterval(
			GD.RandRange(
				0.3,
				2.5
			)
		);


		tween.TweenProperty(
			line,
			"position:x",
			viewportSize.X
			+ 250.0f,
			duration
		)
		.SetTrans(
			Tween.TransitionType.Linear
		);


		tween.TweenCallback(
			Callable.From(
				() =>
					StartLightLineAnimation(
						index
					)
			)
		);
	}


	// ==================================================
	// CHANGE ROOM
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


		_currentColor =
			GetRoomColor(
				roomIndex
			);


		ApplyRoomColor();

		ApplyRoomIntensity(
			roomIndex
		);
	}


	// ==================================================
	// ROOM COLOR
	// ==================================================

	private static Color GetRoomColor(
		int roomIndex)
	{
		return roomIndex switch
		{
			0 =>
				new Color(
					0.10f,
					0.70f,
					1.00f,
					1.0f
				),

			1 =>
				new Color(
					1.00f,
					0.18f,
					0.20f,
					1.0f
				),

			2 =>
				new Color(
					0.10f,
					1.00f,
					0.40f,
					1.0f
				),

			3 =>
				new Color(
					0.70f,
					0.25f,
					1.00f,
					1.0f
				),

			_ =>
				Colors.White
		};
	}


	// ==================================================
	// APPLY ROOM COLORS
	// ==================================================

	private void ApplyRoomColor()
	{
		foreach (
			ColorRect particle
			in _particles
		)
		{
			if (particle == null)
				continue;


			particle.Color =
				new Color(
					_currentColor.R,
					_currentColor.G,
					_currentColor.B,
					0.80f
				);
		}


		foreach (
			ColorRect line
			in _lightLines
		)
		{
			if (line == null)
				continue;


			line.Color =
				new Color(
					_currentColor.R,
					_currentColor.G,
					_currentColor.B,
					0.18f
				);
		}


		_pulseOverlay.Color =
			new Color(
				_currentColor.R,
				_currentColor.G,
				_currentColor.B,
				0.035f
			);
	}


	// ==================================================
	// ROOM INTENSITY
	// ==================================================

	private void ApplyRoomIntensity(
		int roomIndex)
	{
		/*
		 * Instead of changing the number of
		 * particles at runtime, vary visibility.
		 */

		int visibleParticles =
			roomIndex switch
			{
				0 => 18,
				1 => 21,
				2 => 24,
				3 => 26,
				_ => 18
			};


		for (
			int i = 0;
			i < _particles.Length;
			i++
		)
		{
			_particles[
				i
			].Visible =
				i < visibleParticles;
		}
	}


	// ==================================================
	// VIEWPORT SIZE
	// ==================================================

	private Vector2 GetViewportSize()
	{
		Vector2 size =
			_root.GetViewportRect()
				.Size;


		/*
		 * Safety fallback for initialization before
		 * the viewport has received its final size.
		 */

		if (size.X <= 0.0f)
		{
			size.X =
				720.0f;
		}


		if (size.Y <= 0.0f)
		{
			size.Y =
				1280.0f;
		}


		return size;
	}


	// ==================================================
	// CLEANUP
	// ==================================================

	public void Cleanup()
	{
		_pulseTween?.Kill();


		foreach (
			Tween? tween
			in _particleTweens
		)
		{
			tween?.Kill();
		}


		foreach (
			Tween? tween
			in _lineTweens
		)
		{
			tween?.Kill();
		}
	}
}
