using Godot;

namespace IdleAi;


/*
 * Gives every visible Singularity Sector more breathing room without changing
 * any gameplay/map coordinates.
 *
 * The Node ring, Core, Sector center, snap targets, inter-Sector links and
 * cross-Sector adjacency all keep their existing coordinates. Only the visual
 * Sector frame grows outward, so the outer Nodes no longer sit directly on
 * the border / look clipped.
 *
 * The sector frame itself is now deliberately text-free. The header already
 * shows the current sector, so the per-frame CURRENT / SECTOR N / output text
 * is redundant and only adds visual clutter.
 */
public sealed partial class SingularityController
{
	private const float LargerSectorFramePadding =
		32.0f;

	private Timer?
		_largerSectorFrameTimer;


	public void EnableLargerSectorFrames()
	{
		if (_largerSectorFrameTimer != null)
			return;

		ApplyLargerSectorFrames();

		_largerSectorFrameTimer =
			new Timer
			{
				Name =
					"SingularityLargerSectorFrameTimer",

				WaitTime =
					0.20,

				OneShot =
					false,

				Autostart =
					true
			};

		_largerSectorFrameTimer.Timeout +=
			ApplyLargerSectorFrames;

		_root.AddChild(
			_largerSectorFrameTimer
		);
	}


	private void ApplyLargerSectorFrames()
	{
		if (_sectorMapViews.Count == 0)
			return;

		foreach (
			SectorMapView view
				in _sectorMapViews.Values
		)
		{
			if (
				view.Frame == null
				|| !GodotObject.IsInstanceValid(
					view.Frame
				)
			)
			{
				continue;
			}

			/*
			 * Sector root remains SectorMapSize x SectorMapSize.
			 * PanelContainer is allowed to draw outside that root because the
			 * root itself does not clip its children.
			 *
			 * Effective visible frame:
			 * 516 + 32 + 32 = 580 px with the current map constants.
			 */
			view.Frame.AnchorLeft =
				0.0f;

			view.Frame.AnchorTop =
				0.0f;

			view.Frame.AnchorRight =
				1.0f;

			view.Frame.AnchorBottom =
				1.0f;

			view.Frame.OffsetLeft =
				-LargerSectorFramePadding;

			view.Frame.OffsetTop =
				-LargerSectorFramePadding;

			view.Frame.OffsetRight =
				LargerSectorFramePadding;

			view.Frame.OffsetBottom =
				LargerSectorFramePadding;

			/*
			 * Frame is decorative. It must never intercept Node/Core input even
			 * though its rect now extends beyond the Sector root.
			 */
			view.Frame.MouseFilter =
				Control.MouseFilterEnum.Ignore;

			/*
			 * The main Singularity header already exposes the current Sector.
			 * Hide the three redundant labels inside each individual Sector box:
			 * - CURRENT
			 * - SECTOR N
			 * - the output/status line below SECTOR N
			 *
			 * RefreshSectorMapView() still updates these labels internally, but
			 * this recurring visual pass keeps them hidden even after refreshes or
			 * sector virtualization creates a new frame.
			 */
			if (
				view.CurrentBadge != null
				&& GodotObject.IsInstanceValid(
					view.CurrentBadge
				)
			)
			{
				view.CurrentBadge.Visible =
					false;
			}

			if (
				view.Title != null
				&& GodotObject.IsInstanceValid(
					view.Title
				)
			)
			{
				view.Title.Visible =
					false;
			}

			if (
				view.Output != null
				&& GodotObject.IsInstanceValid(
					view.Output
				)
			)
			{
				view.Output.Visible =
					false;
			}
		}
	}
}
