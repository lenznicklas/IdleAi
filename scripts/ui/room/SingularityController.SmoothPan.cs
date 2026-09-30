using Godot;
using System;

namespace IdleAi;


/*
 * Adds a heavier, smoother free-pan feel to the existing Singularity 2D map
 * without replacing the large SingularityController.cs.
 *
 * Features:
 * - Reduced direct drag distance ("heavier" map)
 * - Smoothed release velocity
 * - Inertia with stronger friction
 * - Rubber-band overscroll at all map edges
 * - Spring/bounce back into the valid map bounds
 *
 * The existing node/sector/map logic remains untouched.
 */
public sealed partial class SingularityController
{
	private const float SmoothPanDragFactor =
		0.72f;

	private const float SmoothPanVelocityBlend =
		0.30f;

	private const float SmoothPanReleaseVelocityFactor =
		0.58f;

	private const float SmoothPanInertiaFriction =
		3.35f;

	private const float SmoothPanSpringStrength =
		42.0f;

	private const float SmoothPanSpringDamping =
		8.4f;

	private const float SmoothPanRubberBandCoefficient =
		0.48f;

	private const float SmoothPanMaximumOverscroll =
		126.0f;

	private const float SmoothPanStopSpeed =
		7.0f;

	private const float SmoothPanSnapDistance =
		0.75f;

	/*
	 * If an existing Sector is already fairly close to the center after a
	 * swipe, gently pull it into the exact center instead of leaving it a few
	 * pixels off. Fast flings still travel freely; snapping starts only after
	 * the map has slowed down enough.
	 */
	private const float SmoothPanSectorSnapRadius =
		185.0f;

	private const float SmoothPanSectorSnapMaxSpeed =
		900.0f;

	private const float SmoothPanSectorSnapStrength =
		30.0f;

	private const float SmoothPanSectorSnapDamping =
		7.6f;


	private SmoothPanPhysicsRunner?
		_smoothPanRunner;

	private Vector2 _smoothPanVelocity;

	private bool _smoothPanDragging;

	private bool _smoothPanWasMoving;

	private ulong _smoothPanLastDragUsec;


	public void EnableSmoothPanPhysics()
	{
		if (
			_smoothPanRunner != null
			|| _panInput == null
			|| _mapViewport == null
			|| _mapWorld == null
		)
		{
			return;
		}

		/*
		 * Reuse the existing input tracker because it already handles:
		 * - touch index tracking
		 * - mouse dragging
		 * - click suppression after a drag
		 *
		 * We only replace its movement callbacks and disable its own inertia.
		 * The new runner below owns inertia and bounce physics.
		 */
		_panInput.Configure(
			_mapViewport,
			() =>
				Visible
				&& (
					_detailOverlay == null
					|| !_detailOverlay.Visible
				),
			OnSmoothMapPanDelta,
			OnSmoothMapPanEnded
		);

		_panInput.SetProcess(
			false
		);

		_smoothPanRunner =
			new SmoothPanPhysicsRunner
			{
				Name =
					"SingularitySmoothPanPhysics"
			};

		_smoothPanRunner.ProcessRequested +=
			ProcessSmoothPanPhysics;

		_smoothPanRunner.InputRequested +=
			OnSmoothPanInput;

		_root.AddChild(
			_smoothPanRunner
		);

		/*
		 * Keep Node buttons visually consistent while a finger touches/drags
		 * across them. Without explicit hover/pressed styles Godot falls back
		 * to the default grey rectangular Button theme on touch-down.
		 */
		EnablePolishedNodeTouchStyles();
	}


	private void OnSmoothPanInput(
		InputEvent @event)
	{
		if (
			_mapViewport == null
			|| !Visible
		)
		{
			return;
		}

		bool pressed =
			@event
				is InputEventScreenTouch touch
			&& touch.Pressed;

		Vector2 position =
			pressed
				&& @event is InputEventScreenTouch screenTouch
					? screenTouch.Position
					: Vector2.Zero;

		if (
			@event
				is InputEventMouseButton mouse
			&& mouse.ButtonIndex
				== MouseButton.Left
			&& mouse.Pressed
		)
		{
			pressed =
				true;

			position =
				mouse.Position;
		}

		if (
			!pressed
			|| !_mapViewport.GetGlobalRect()
				.HasPoint(
					position
				)
		)
		{
			return;
		}

		/*
		 * Touching the map immediately "grabs" it and stops a previous fling.
		 * This makes the map feel physical instead of slippery.
		 */
		_smoothPanVelocity =
			Vector2.Zero;

		_smoothPanDragging =
			false;

		_smoothPanWasMoving =
			false;

		_smoothPanLastDragUsec =
			0;
	}


	private void OnSmoothMapPanDelta(
		Vector2 rawDelta)
	{
		if (
			_mapWorld == null
			|| !GodotObject.IsInstanceValid(
				_mapWorld
			)
			|| _mapViewport == null
			|| !GodotObject.IsInstanceValid(
				_mapViewport
			)
		)
		{
			return;
		}

		ulong now =
			Time.GetTicksUsec();

		float deltaSeconds =
			_smoothPanLastDragUsec > 0
				? Math.Clamp(
					(float)(
						now
						- _smoothPanLastDragUsec
					)
					/ 1_000_000.0f,
					1.0f / 240.0f,
					0.050f
				)
				: 1.0f / 60.0f;

		_smoothPanLastDragUsec =
			now;

		if (!_smoothPanDragging)
		{
			_smoothPanDragging =
				true;

			_smoothPanVelocity =
				Vector2.Zero;
		}

		Vector2 weightedDelta =
			rawDelta
			* SmoothPanDragFactor;

		Vector2 before =
			_mapWorld.Position;

		Vector2 after =
			ApplyRubberBandDrag(
				before,
				weightedDelta
			);

		_mapWorld.Position =
			after;

		Vector2 appliedDelta =
			after - before;

		Vector2 measuredVelocity =
			appliedDelta
			/ MathF.Max(
				deltaSeconds,
				1.0f / 240.0f
			);

		_smoothPanVelocity =
			_smoothPanVelocity.Lerp(
				measuredVelocity,
				SmoothPanVelocityBlend
			);

		_smoothPanWasMoving =
			true;

		/*
		 * Update Current Sector while the finger is still moving. The header and
		 * CURRENT badge therefore change as soon as another Sector becomes the
		 * closest one to the viewport center, not only after inertia stops.
		 */
		UpdateFocusedSectorDuringMotion();

		RefreshMapWorld();
	}


	private void OnSmoothMapPanEnded()
	{
		if (!_smoothPanDragging)
		{
			return;
		}

		_smoothPanDragging =
			false;

		_smoothPanLastDragUsec =
			0;

		/*
		 * A lower release multiplier makes the map feel heavier while still
		 * preserving a short, smooth coast after a fast swipe.
		 */
		_smoothPanVelocity *=
			SmoothPanReleaseVelocityFactor;

		_smoothPanWasMoving =
			true;

		/* Current Sector is already correct before the coast/bounce finishes. */
		UpdateFocusedSectorDuringMotion();
		RefreshMapWorld();
	}


	private void ProcessSmoothPanPhysics(
		double delta)
	{
		if (
			!Visible
			|| _mapWorld == null
			|| _mapViewport == null
			|| !GodotObject.IsInstanceValid(
				_mapWorld
			)
			|| !GodotObject.IsInstanceValid(
				_mapViewport
			)
		)
		{
			_smoothPanVelocity =
				Vector2.Zero;

			_smoothPanDragging =
				false;

			_smoothPanWasMoving =
				false;

			return;
		}

		if (_smoothPanDragging)
			return;

		float dt =
			Math.Clamp(
				(float)delta,
				0.0f,
				0.040f
			);

		if (dt <= 0.0f)
			return;

		/*
		 * Keep the selected/current Sector live while inertia is still running.
		 * This is intentionally done before and after this frame's movement.
		 */
		UpdateFocusedSectorDuringMotion();

		Vector2 position =
			_mapWorld.Position;

		Vector2 boundsTarget =
			GetClampedMapPosition(
				position
			);

		Vector2 boundsDisplacement =
			boundsTarget - position;

		bool outsideBounds =
			boundsDisplacement.LengthSquared()
				> 0.01f;

		bool sectorSnapActive =
			false;

		Vector2 sectorSnapTarget =
			position;

		if (!outsideBounds)
		{
			sectorSnapActive =
				TryGetSectorSnapTarget(
					position,
					out sectorSnapTarget
				);
		}

		if (outsideBounds)
		{
			/*
			 * Edge bounce keeps priority over Sector snapping. First return from
			 * overscroll; then, if a Sector is near the center, it can snap.
			 */
			_smoothPanVelocity +=
				boundsDisplacement
					* SmoothPanSpringStrength
					* dt;

			_smoothPanVelocity *=
				MathF.Exp(
					-SmoothPanSpringDamping
					* dt
				);
		}
		else if (sectorSnapActive)
		{
			Vector2 snapDisplacement =
				sectorSnapTarget - position;

			_smoothPanVelocity +=
				snapDisplacement
					* SmoothPanSectorSnapStrength
					* dt;

			_smoothPanVelocity *=
				MathF.Exp(
					-SmoothPanSectorSnapDamping
					* dt
				);
		}
		else
		{
			_smoothPanVelocity *=
				MathF.Exp(
					-SmoothPanInertiaFriction
					* dt
				);
		}

		if (
			_smoothPanVelocity.LengthSquared()
				> 0.001f
		)
		{
			position +=
				_smoothPanVelocity
					* dt;

			position =
				LimitMaximumOverscroll(
					position
				);

			_mapWorld.Position =
				position;

			_smoothPanWasMoving =
				true;

			UpdateFocusedSectorDuringMotion();
			RefreshMapWorld();
		}

		Vector2 finalTarget =
			sectorSnapActive
				? sectorSnapTarget
				: GetClampedMapPosition(
					_mapWorld.Position
				);

		float distanceToTarget =
			_mapWorld.Position.DistanceTo(
				finalTarget
			);

		if (
			_smoothPanVelocity.Length()
				<= SmoothPanStopSpeed
			&& distanceToTarget
				<= SmoothPanSnapDistance
		)
		{
			_mapWorld.Position =
				finalTarget;

			_smoothPanVelocity =
				Vector2.Zero;

			if (_smoothPanWasMoving)
			{
				_smoothPanWasMoving =
					false;

				UpdateFocusedSectorDuringMotion();
				RefreshRuntime();
				RefreshMapWorld();
			}
		}
	}


	private void UpdateFocusedSectorDuringMotion()
	{
		if (
			_service.SectorCount <= 0
			|| _mapViewport == null
			|| _mapWorld == null
			|| _mapViewport.Size.X <= 1.0f
			|| _mapViewport.Size.Y <= 1.0f
		)
		{
			return;
		}

		int previousSector =
			_service.CurrentSectorIndex;

		UpdateFocusedSectorFromCamera();

		if (
			previousSector
			== _service.CurrentSectorIndex
		)
		{
			return;
		}

		/* Header Sector number updates immediately when focus changes. */
		RefreshRuntime();
	}


	private bool TryGetSectorSnapTarget(
		Vector2 currentPosition,
		out Vector2 targetPosition)
	{
		targetPosition =
			currentPosition;

		if (
			_service.SectorCount <= 0
			|| _mapViewport.Size.X <= 1.0f
			|| _mapViewport.Size.Y <= 1.0f
			|| _smoothPanVelocity.Length()
				> SmoothPanSectorSnapMaxSpeed
		)
		{
			return false;
		}

		int sectorIndex =
			Math.Clamp(
				_service.CurrentSectorIndex,
				0,
				_service.SectorCount - 1
			);

		Vector2 sectorCenter =
			GetSectorWorldTopLeft(
				sectorIndex
			)
			+ new Vector2(
				SectorMapSize / 2.0f,
				SectorMapSize / 2.0f
			);

		Vector2 exactCenterPosition =
			_mapViewport.Size / 2.0f
			- sectorCenter;

		/* Respect map edges while still centering whenever bounds allow it. */
		targetPosition =
			GetClampedMapPosition(
				exactCenterPosition
			);

		return currentPosition.DistanceTo(
			targetPosition
		)
		<= SmoothPanSectorSnapRadius;
	}


	private Vector2 ApplyRubberBandDrag(
		Vector2 current,
		Vector2 delta)
	{
		GetMapPositionLimits(
			out float minX,
			out float maxX,
			out float minY,
			out float maxY
		);

		return new Vector2(
			ApplyRubberBandAxis(
				current.X,
				delta.X,
				minX,
				maxX,
				_mapViewport.Size.X
			),
			ApplyRubberBandAxis(
				current.Y,
				delta.Y,
				minY,
				maxY,
				_mapViewport.Size.Y
			)
		);
	}


	private static float ApplyRubberBandAxis(
		float current,
		float delta,
		float minimum,
		float maximum,
		float viewportLength)
	{
		float dimension =
			MathF.Max(
				240.0f,
				viewportLength
			);

		/*
		 * Already beyond the edge:
		 * - pulling back toward the content follows the finger normally
		 * - pushing farther outward gets progressively heavier
		 */
		if (current < minimum)
		{
			if (delta >= 0.0f)
			{
				return MathF.Min(
					minimum,
					current + delta
				);
			}

			float overshoot =
				minimum - current;

			float resistance =
				1.0f
				/ (
					1.0f
					+ overshoot
						/ dimension
						* 7.0f
				);

			return MathF.Max(
				minimum
					- SmoothPanMaximumOverscroll,
				current
					+ delta
						* 0.34f
						* resistance
			);
		}

		if (current > maximum)
		{
			if (delta <= 0.0f)
			{
				return MathF.Max(
					maximum,
					current + delta
				);
			}

			float overshoot =
				current - maximum;

			float resistance =
				1.0f
				/ (
					1.0f
					+ overshoot
						/ dimension
						* 7.0f
				);

			return MathF.Min(
				maximum
					+ SmoothPanMaximumOverscroll,
				current
					+ delta
						* 0.34f
						* resistance
			);
		}

		float proposed =
			current + delta;

		if (proposed < minimum)
		{
			float overshoot =
				minimum - proposed;

			return minimum
				- RubberBandDistance(
					overshoot,
					dimension
				);
		}

		if (proposed > maximum)
		{
			float overshoot =
				proposed - maximum;

			return maximum
				+ RubberBandDistance(
					overshoot,
					dimension
				);
		}

		return proposed;
	}


	private static float RubberBandDistance(
		float distance,
		float dimension)
	{
		/*
		 * Similar to the resistance curve used by touch UIs:
		 * resistance increases progressively the farther the user drags past
		 * the edge instead of hitting a hard wall.
		 */
		float compressed =
			(
				1.0f
				- 1.0f
					/ (
						distance
							* SmoothPanRubberBandCoefficient
							/ dimension
						+ 1.0f
					)
			)
			* dimension;

		return MathF.Min(
			compressed,
			SmoothPanMaximumOverscroll
		);
	}


	private Vector2 LimitMaximumOverscroll(
		Vector2 position)
	{
		GetMapPositionLimits(
			out float minX,
			out float maxX,
			out float minY,
			out float maxY
		);

		position.X =
			Math.Clamp(
				position.X,
				minX
					- SmoothPanMaximumOverscroll,
				maxX
					+ SmoothPanMaximumOverscroll
			);

		position.Y =
			Math.Clamp(
				position.Y,
				minY
					- SmoothPanMaximumOverscroll,
				maxY
					+ SmoothPanMaximumOverscroll
			);

		return position;
	}


	private Vector2 GetClampedMapPosition(
		Vector2 position)
	{
		GetMapPositionLimits(
			out float minX,
			out float maxX,
			out float minY,
			out float maxY
		);

		return new Vector2(
			Math.Clamp(
				position.X,
				minX,
				maxX
			),
			Math.Clamp(
				position.Y,
				minY,
				maxY
			)
		);
	}


	private void GetMapPositionLimits(
		out float minX,
		out float maxX,
		out float minY,
		out float maxY)
	{
		Rect2 bounds =
			GetMapContentBounds();

		Vector2 viewportSize =
			_mapViewport.Size;

		minX =
			viewportSize.X
			- bounds.End.X;

		maxX =
			-bounds.Position.X;

		minY =
			viewportSize.Y
			- bounds.End.Y;

		maxY =
			-bounds.Position.Y;

		/*
		 * If the complete content is smaller than the viewport on one axis,
		 * there is only one valid resting position: centered.
		 */
		if (minX > maxX)
		{
			float centerX =
				viewportSize.X / 2.0f
				- (
					bounds.Position.X
					+ bounds.Size.X / 2.0f
				);

			minX =
				centerX;

			maxX =
				centerX;
		}

		if (minY > maxY)
		{
			float centerY =
				viewportSize.Y / 2.0f
				- (
					bounds.Position.Y
					+ bounds.Size.Y / 2.0f
				);

			minY =
				centerY;

			maxY =
				centerY;
		}
	}


	private sealed partial class SmoothPanPhysicsRunner
		: Node
	{
		public event Action<double>? ProcessRequested;

		public event Action<InputEvent>? InputRequested;


		public override void _Process(
			double delta)
		{
			ProcessRequested?.Invoke(
				delta
			);
		}


		public override void _Input(
			InputEvent @event)
		{
			InputRequested?.Invoke(
				@event
			);
		}
	}
}
