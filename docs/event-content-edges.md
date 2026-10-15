# Event content transitions

The shared scroll viewport clipped opaque card backgrounds directly against the page, while its 12px layout gaps had no transition. This made every tab end at a hard horizontal edge.

`EventContentFrame` now wraps the existing scroller, occupying exactly the same expanding layout region. A faint themed backdrop extends through the gaps above/below the viewport and becomes transparent at both ends. The top shadow appears only while scrolled, softening clipped cards without dimming an unscrolled heading. The bottom gradient darkens toward the clipping edge, then blends back to transparent through the gap before the tab buttons. It never overlaps the navigation buttons. These are cached Godot GradientTexture2D/StyleBoxTexture panels; no shader, new artwork or repeated texture loads are required.

All layers ignore mouse/touch input. Existing viewport clipping, MobileScrollController, per-tab scroll offsets, modal handling and header/navigation positions remain unchanged. The fade height adapts to short viewports (48px maximum inside the bottom edge). A shared 48px trailing clearance allows the final content to scroll fully above the fade. No production/save/navigation mechanics changed.

Files changed for this task:

- Added `scripts/ui/events/EventContentFrame.cs` and Godot `.uid` metadata.
- Modified `scripts/ui/events/EventView.cs` to install/theme the shared frame and add trailing clearance.
- Modified `tests/Events/tabs_scene_smoke.gd` with shared transition geometry, input passthrough, gradient endpoint, top-scroll state and last-content clearance assertions for all seven tabs and four portrait sizes.
- Added this document.

Validation:

- Debug and ExportRelease builds: zero errors; existing warnings remain.
- Event tab scene: 2,595 checks, zero failures.
- Halloween/Test Protocol asset scene: 7,047 checks, zero failures.
- Normal/Event/Singularity navigation scene: 155 checks, zero failures. That fixture reports its existing shutdown resource warning after passing.
- Inspected actual OpenGL portrait captures of all seven Halloween tabs at the top, while scrolling and at the end. Captures used an isolated profile and are stored in `/tmp/idle-ai-event-edges/`.
- No physical Android device run.

The rendered comparison also exposed an existing auto-wrapped-label sizing issue: some description/Prestige/empty-state labels have 1px height. A runtime comparison with the original direct-scroller layout reproduced identical bounds. It is independent of these background transitions and was left outside this styling change.

Other existing workspace edits, including the bot-image fix and its tests/documentation, were preserved.
