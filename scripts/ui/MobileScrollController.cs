using Godot;
using System;

namespace IdleAi;

public partial class MobileScrollController : Node
{
	private const float DragThreshold =
		8.0f;


	/*
	 * Maximum visual rubber-band distance.
	 *
	 * This is not a hard stop.
	 * The tanh curve approaches this value smoothly.
	 */
	private const double RubberBandVisualLimit =
		360.0;


	private const double RubberBandPullStrength =
		0.82;


	/*
	 * Internal safety limit only.
	 * This limit is never reached visually.
	 */
	private const double RawPullSafetyLimit =
		5000.0;


	private const double VelocitySmoothing =
		0.35;


	private const double Friction =
		7.0;


	private const double SpringSpeed =
		8.0;


	private const double MinimumVelocity =
		8.0;


	private const ulong TapBlockAfterDragMs =
		180;


	private ScrollContainer _scroll =
		null!;


	private Control _content =
		null!;


	private bool _allowTopOverscroll =
		true;


	private bool _allowBottomOverscroll =
		true;


	private int _touchIndex =
		-1;


	private bool _tracking;

	private bool _dragging;


	private Vector2 _startPosition;

	private Vector2 _lastPosition;


	private ulong _lastInputTime;

	private ulong _suppressTapUntil;


	private double _velocity;


	/*
	 * Raw pull distance outside the normal
	 * ScrollContainer range.
	 */
	private double _overscrollPull;


	/*
	 * Actual visible rubber-band offset.
	 */
	private double _overscroll;


	private double _appliedOverscroll;


	// ==================================================
	// PUBLIC STATE
	// ==================================================

	public bool ShouldSuppressTap =>
		_dragging
		|| Time.GetTicksMsec()
			< _suppressTapUntil;


	// ==================================================
	// SETUP
	// ==================================================

	public void Setup(
		ScrollContainer scroll,
		bool allowTopOverscroll = true,
		bool allowBottomOverscroll = true)
	{
		_scroll =
			scroll;


		_allowTopOverscroll =
			allowTopOverscroll;


		_allowBottomOverscroll =
			allowBottomOverscroll;


		/*
		 * Disable Godot's own touch scrolling.
		 *
		 * All touch scrolling is handled by this
		 * controller.
		 */
		_scroll.ScrollDeadzone =
			100_000;


		_scroll.HorizontalScrollMode =
			ScrollContainer.ScrollMode.Disabled;


		_scroll.VerticalScrollMode =
			ScrollContainer.ScrollMode.ShowNever;


		_scroll.ClipContents =
			true;


		if (
			_scroll.GetChildCount()
			<= 0
		)
		{
			GD.PushError(
				"MobileScrollController: ScrollContainer has no content child."
			);


			return;
		}


		_content =
			_scroll.GetChild<Control>(
				0
			);


		SetProcess(
			true
		);


		SetProcessInput(
			true
		);
	}


	// ==================================================
	// INPUT
	// ==================================================

	public override void _Input(
		InputEvent @event)
	{
		if (
			_scroll == null
			|| !GodotObject.IsInstanceValid(
				_scroll
			)
			|| !_scroll.IsVisibleInTree()
		)
		{
			return;
		}


		if (
			@event
			is InputEventScreenTouch touch
		)
		{
			HandleTouch(
				touch
			);


			return;
		}


		if (
			@event
			is InputEventScreenDrag drag
		)
		{
			HandleDrag(
				drag
			);
		}
	}


	private void HandleTouch(
		InputEventScreenTouch touch)
	{
		// ==================================================
		// TOUCH START
		// ==================================================

		if (touch.Pressed)
		{
			if (
				!_scroll
					.GetGlobalRect()
					.HasPoint(
						touch.Position
					)
			)
			{
				return;
			}


			_touchIndex =
				touch.Index;


			_tracking =
				true;


			_dragging =
				false;


			_startPosition =
				touch.Position;


			_lastPosition =
				touch.Position;


			_lastInputTime =
				Time.GetTicksMsec();


			/*
			 * Stop old inertia immediately when
			 * the user puts a finger on the list.
			 */
			_velocity =
				0.0;


			return;
		}


		// ==================================================
		// TOUCH END
		// ==================================================

		if (
			!_tracking
			|| touch.Index
			!= _touchIndex
		)
		{
			return;
		}


		if (_dragging)
		{
			_suppressTapUntil =
				Time.GetTicksMsec()
				+ TapBlockAfterDragMs;


			GetViewport()
				.SetInputAsHandled();
		}


		_tracking =
			false;


		_dragging =
			false;


		_touchIndex =
			-1;
	}


	private void HandleDrag(
		InputEventScreenDrag drag)
	{
		if (
			!_tracking
			|| drag.Index
			!= _touchIndex
		)
		{
			return;
		}


		Vector2 totalMovement =
			drag.Position
			- _startPosition;


		// ==================================================
		// TAP -> DRAG
		// ==================================================

		if (!_dragging)
		{
			if (
				Math.Abs(
					totalMovement.Y
				)
				< DragThreshold
			)
			{
				_lastPosition =
					drag.Position;


				return;
			}


			_dragging =
				true;


			_suppressTapUntil =
				ulong.MaxValue;
		}


		float fingerDelta =
			drag.Position.Y
			- _lastPosition.Y;


		double scrollDelta =
			-fingerDelta;


		ulong now =
			Time.GetTicksMsec();


		double deltaTime =
			Math.Max(
				0.001,
				(
					now
					- _lastInputTime
				)
				/ 1000.0
			);


		double instantaneousVelocity =
			scrollDelta
			/ deltaTime;


		_velocity =
			_velocity
			* (
				1.0
				- VelocitySmoothing
			)
			+ instantaneousVelocity
			* VelocitySmoothing;


		ApplyScrollDelta(
			scrollDelta
		);


		_lastPosition =
			drag.Position;


		_lastInputTime =
			now;


		GetViewport()
			.SetInputAsHandled();
	}


	// ==================================================
	// PROCESS
	// ==================================================

	public override void _Process(
		double delta)
	{
		if (
			_scroll == null
			|| !GodotObject.IsInstanceValid(
				_scroll
			)
			|| _content == null
			|| !GodotObject.IsInstanceValid(
				_content
			)
		)
		{
			return;
		}


		if (!_scroll.IsVisibleInTree())
		{
			ResetMotion();


			return;
		}


		/*
		 * Remove the visual transform from the
		 * previous frame before calculating the new one.
		 */
		RemoveAppliedOverscroll();


		if (!_dragging)
		{
			UpdateInertia(
				delta
			);


			UpdateSpring(
				delta
			);
		}


		UpdateRubberBandVisual();

		ApplyVisualOffset();
	}


	// ==================================================
	// INERTIA
	// ==================================================

	private void UpdateInertia(
		double delta)
	{
		if (
			Math.Abs(
				_velocity
			)
			< MinimumVelocity
		)
		{
			_velocity =
				0.0;


			return;
		}


		double movement =
			_velocity
			* delta;


		ApplyScrollDelta(
			movement
		);


		_velocity *=
			Math.Exp(
				-Friction
				* delta
			);


		/*
		 * Increase friction while the content is
		 * inside the rubber-band area.
		 */
		if (
			Math.Abs(
				_overscrollPull
			)
			> 0.01
		)
		{
			double stretch =
				Math.Abs(
					_overscroll
				)
				/ RubberBandVisualLimit;


			double edgeFriction =
				2.0
				+ stretch
				* 12.0;


			_velocity *=
				Math.Exp(
					-edgeFriction
					* delta
				);
		}


		if (
			Math.Abs(
				_velocity
			)
			< MinimumVelocity
		)
		{
			_velocity =
				0.0;
		}
	}


	// ==================================================
	// SPRING
	// ==================================================

	private void UpdateSpring(
		double delta)
	{
		if (
			Math.Abs(
				_overscrollPull
			)
			< 0.05
		)
		{
			_overscrollPull =
				0.0;


			_overscroll =
				0.0;


			return;
		}


		double factor =
			Math.Exp(
				-SpringSpeed
				* delta
			);


		_overscrollPull *=
			factor;


		if (
			Math.Abs(
				_overscrollPull
			)
			< 0.05
		)
		{
			_overscrollPull =
				0.0;


			_overscroll =
				0.0;
		}
	}


	// ==================================================
	// SCROLL
	// ==================================================

	private void ApplyScrollDelta(
		double delta)
	{
		double current =
			_scroll.ScrollVertical;


		double max =
			GetMaximumScroll();


		// ==================================================
		// RETURN FROM TOP OVERSCROLL
		// ==================================================

		if (
			_overscrollPull > 0.0
			&& delta > 0.0
		)
		{
			double pullReduction =
				delta
				/ RubberBandPullStrength;


			if (
				pullReduction
				>= _overscrollPull
			)
			{
				double remainingPull =
					pullReduction
					- _overscrollPull;


				_overscrollPull =
					0.0;


				delta =
					remainingPull
					* RubberBandPullStrength;
			}
			else
			{
				_overscrollPull -=
					pullReduction;


				UpdateRubberBandVisual();


				return;
			}
		}


		// ==================================================
		// RETURN FROM BOTTOM OVERSCROLL
		// ==================================================

		if (
			_overscrollPull < 0.0
			&& delta < 0.0
		)
		{
			double pullReduction =
				-delta
				/ RubberBandPullStrength;


			double currentPull =
				-_overscrollPull;


			if (
				pullReduction
				>= currentPull
			)
			{
				double remainingPull =
					pullReduction
					- currentPull;


				_overscrollPull =
					0.0;


				delta =
					-remainingPull
					* RubberBandPullStrength;
			}
			else
			{
				_overscrollPull +=
					pullReduction;


				UpdateRubberBandVisual();


				return;
			}
		}


		current =
			_scroll.ScrollVertical;


		double requested =
			current
			+ delta;


		// ==================================================
		// TOP
		// ==================================================

		if (requested < 0.0)
		{
			_scroll.ScrollVertical =
				0;


			/*
			 * Some pages, such as the Lab, have
			 * a fixed header directly above their
			 * ScrollContainer.
			 *
			 * Moving the content down there would
			 * expose an empty strip between header
			 * and content.
			 */
			if (!_allowTopOverscroll)
			{
				_overscrollPull =
					0.0;


				_overscroll =
					0.0;


				/*
				 * Stop outward inertia immediately,
				 * otherwise it keeps trying to move
				 * past the top edge.
				 */
				if (_velocity < 0.0)
				{
					_velocity =
						0.0;
				}


				return;
			}


			double excess =
				-requested;


			_overscrollPull +=
				excess
				* RubberBandPullStrength;


			_overscrollPull =
				Math.Clamp(
					_overscrollPull,
					-RawPullSafetyLimit,
					RawPullSafetyLimit
				);


			UpdateRubberBandVisual();


			return;
		}


		// ==================================================
		// BOTTOM
		// ==================================================

		if (requested > max)
		{
			_scroll.ScrollVertical =
				Mathf.RoundToInt(
					(float)max
				);


			if (!_allowBottomOverscroll)
			{
				_overscrollPull =
					0.0;


				_overscroll =
					0.0;


				if (_velocity > 0.0)
				{
					_velocity =
						0.0;
				}


				return;
			}


			double excess =
				requested
				- max;


			_overscrollPull -=
				excess
				* RubberBandPullStrength;


			_overscrollPull =
				Math.Clamp(
					_overscrollPull,
					-RawPullSafetyLimit,
					RawPullSafetyLimit
				);


			UpdateRubberBandVisual();


			return;
		}


		// ==================================================
		// NORMAL
		// ==================================================

		_scroll.ScrollVertical =
			Mathf.RoundToInt(
				(float)requested
			);


		if (
			Math.Abs(
				_overscrollPull
			)
			< 0.05
		)
		{
			_overscrollPull =
				0.0;


			_overscroll =
				0.0;
		}
	}


	private double GetMaximumScroll()
	{
		VScrollBar bar =
			_scroll.GetVScrollBar();


		return Math.Max(
			0.0,
			bar.MaxValue
			- bar.Page
		);
	}


	// ==================================================
	// RUBBER BAND
	// ==================================================

	private void UpdateRubberBandVisual()
	{
		if (
			Math.Abs(
				_overscrollPull
			)
			< 0.001
		)
		{
			_overscroll =
				0.0;


			return;
		}


		double normalized =
			_overscrollPull
			/ RubberBandVisualLimit;


		_overscroll =
			RubberBandVisualLimit
			* Math.Tanh(
				normalized
			);
	}


	// ==================================================
	// VISUAL OFFSET
	// ==================================================

	private void RemoveAppliedOverscroll()
	{
		if (
			Math.Abs(
				_appliedOverscroll
			)
			< 0.001
		)
		{
			_appliedOverscroll =
				0.0;


			return;
		}


		_content.Position -=
			new Vector2(
				0,
				(float)_appliedOverscroll
			);


		_appliedOverscroll =
			0.0;
	}


	private void ApplyVisualOffset()
	{
		if (
			_content == null
			|| !GodotObject.IsInstanceValid(
				_content
			)
		)
		{
			return;
		}


		if (
			Math.Abs(
				_overscroll
			)
			< 0.001
		)
		{
			_appliedOverscroll =
				0.0;


			return;
		}


		_content.Position +=
			new Vector2(
				0,
				(float)_overscroll
			);


		_appliedOverscroll =
			_overscroll;
	}


	// ==================================================
	// PUBLIC
	// ==================================================

	public void ScrollToTop()
	{
		RemoveAppliedOverscroll();


		_tracking =
			false;


		_dragging =
			false;


		_touchIndex =
			-1;


		_velocity =
			0.0;


		_overscrollPull =
			0.0;


		_overscroll =
			0.0;


		_appliedOverscroll =
			0.0;


		_suppressTapUntil =
			0;


		_scroll.ScrollVertical =
			0;
	}


	public void ResetMotion()
	{
		bool wasDragging =
			_dragging;


		if (
			_content != null
			&& GodotObject.IsInstanceValid(
				_content
			)
		)
		{
			RemoveAppliedOverscroll();
		}


		_tracking =
			false;


		_dragging =
			false;


		_touchIndex =
			-1;


		_velocity =
			0.0;


		_overscrollPull =
			0.0;


		_overscroll =
			0.0;


		_appliedOverscroll =
			0.0;


		if (wasDragging)
		{
			_suppressTapUntil =
				Time.GetTicksMsec()
				+ TapBlockAfterDragMs;
		}
	}
}
