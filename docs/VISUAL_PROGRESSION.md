# Visual progression implementation and review

## Rendering map

- `MachineSlot` builds a machine `TextureButton` inside `_machineVisual`, itself inside a 165px holder in each grid card. Existing looping Tweens move this holder; bots animate separately. Locked slots display the room's empty texture.
- `MachineData.Texture` is the selected skin texture; `DefaultTexture` remains the original. `MachineSkinService` selects per-room, per-tier textures (including retro packs). The effects never set either texture or change the art's modulate.
- `MachineDetailsOverlay` displays the same texture in a 150px `TextureRect`. It now uses the same effects controller. `MachineDetailsPreviewBinder` still enlarges the original texture through `SkinPreviewOverlay`; its click discovery and interaction remain intact.
- `game.tscn` has a root background `TextureRect`, room panel/scroll/grid, top bar and bottom bar. Most UI and machine controls are generated at runtime. `RoomUiController` selects the existing room map and refreshes the visible slots.
- `AmbientBackgroundController` already has 80 GPU ambient particles, background Z=-10 and particles Z=-5. New room decorations use Z=-7, below all normal UI. Existing particles are unchanged.
- Existing Modulate/Tween use mostly handles button feedback, popups, machine/bot movement and room unlock transitions. No normal machine shader or AnimationPlayer progression path was found; new effects use neither shaders nor AnimationPlayer.

## Derived stages

Machine stage is `clamp(MachineTier, 0, 5)`. Machine level is intentionally ignored. Current normal rooms have tiers 0–3; stages 4–5 accommodate future tiers without new assets.

| Tier/stage | Added presentation |
| --- | --- |
| 0 | Original sprite only; no progression effects or completion flash |
| 1 | One tiny LED with a subtle soft localized glow |
| 2 | Up to three compact status lights with staggered pulses |
| 3 | Three localized light zones, one tiny drifting mote, short completion sparks |
| 4 | Four small light zones and up to three tiny motes |
| 5+ | Five small light zones with staggered shimmer and compact completion sparks |

Running accents are brighter than idle accents. Pulse period is `clamp(cycleSeconds * 0.18, 0.45, 2.4)` seconds; idle pulse takes five seconds. Existing holder motion follows that period with a bounded speed scale. Neither changes production timing. Completion events produce a 0.35-second local Tween flash, coalescing rapid completions. No screen flash, additional audio or vibration.

For an unlocked room with slots, room stage is:

`clamp(floor(2 * expansion + 2 * averageTier / 3), 0, 4)`

`expansion = (unlockedSlots - 1) / (totalSlots - 1)`; a single-slot room uses zero expansion. Average tier uses unlocked machines only and clamps each to the supported machine stage range. Locked/empty rooms produce stage zero.

| Room stage | Decorations |
| --- | --- |
| 0 | Existing map |
| 1 | One pair of work-light strips |
| 2 | Additional strips and waveform monitor |
| 3 | More monitors plus animated cable/data sweep; Quantum energy motes |
| 4 | Four light pairs, three monitors and six ambient energy motes |

Garage retains its established blue industrial palette; Server Room red, Data Center green, Quantum purple. All colors come from `RoomThemePalette`. Normal decorations are hidden while `SingularityPage` is visible and are disabled for room indices 4+. Its gold identity and economy remain untouched.

## Files

New classes:

- `scripts/ui/effects/VisualProgressionRules.cs`: pure stage and visual timing calculations.
- `scripts/ui/effects/MachineVisualEffectsController.cs`: independent sprite-surface lights, soft glows and completion feedback.
- `scripts/ui/effects/RoomVisualEffectsController.cs`: derived ambient decoration and Singularity visibility compatibility.

Integration changes:

- `scripts/ui/MachineSlot.cs`: delegates themes/state/completion effects and adjusts existing animation speed.
- `scripts/ui/MachineDetailsOverlay.cs`: reuses the effects on its preview image.
- `scripts/ui/room/RoomUiController.cs`: refreshes room effects and routes visible-room completion events.
- `scripts/game/ProductionService.cs`: adds a presentation notification after cycle completion; reward/timing logic unchanged.

Tests: `tests/VisualProgression/VisualProgression.csproj` and `Program.cs`.

## Performance and compatibility

- One lightweight draw control per machine plus one room control. No new particle systems, shaders or repeated node spawning; machine glows share one small generated radial texture.
- Maximum three machine motes and six room motes. These are draw primitives rather than allocated particles.
- Machine redraws capped at 30Hz, room redraws at 20Hz. No managed object creation inside either `_Process` method.
- Hidden/offscreen machine effects skip animation/redraw work; clip visibility uses the owning scroll rectangle. Completion flashes are ignored offscreen and coalesced when rapid.
- Room aggregation occurs on the existing full UI refresh path, not each animation frame.
- Completion and reveal Tweens are killed/replaced and cleaned up on exit. Production event subscription is removed with the game root.
- Saves and SaveVersion are unchanged; stage reconstructs from existing tier, slot unlock and room unlock state. No economy, cost, multiplier, research, prestige or room unlock formula changed.
- Skin textures remain unmodified. Light placement samples opaque sprite interiors and follows texture aspect ratio, supporting default, retro and future skins. Enlarged skin previews intentionally remain texture-only.

## Automated validation

- `dotnet build --no-restore`: passed, 0 errors; 254 existing addon warnings.
- VisualProgression: 24 deterministic checks, including tiers 0–5, extreme clamps, room expansion, advanced machines, developed rooms, reset-state inputs and cycle speed limits.
- Existing suites passed: Settings 47; OfflineProgress 663; Prestige 37,568; GameSave 3,416; Singularity 213; Integrations 206; SettingsAudio 10 assertions.
- Godot C# headless fresh-game startup passed without runtime errors.
- Temporary Godot C# engine smoke exercised all eight populated slots, stages 0–5, completion Tween execution, Quantum room effects, Singularity hide/resume and actual `RoomState.ResetForNewRun` reset. Passed.
- Existing persistence failure tests emit intentional warning/error messages while testing corrupt-save handling; assertions passed.

## Manual visual checks still required

Automated headless checks establish runtime validity, not visual quality or device FPS. No screenshots were captured.

Machine checklist:

- [ ] Fresh tier-0 machine; upgrade through tiers 1, 2 and 3.
- [ ] Idle, manual start, cycle completion and stop; confirm no whole-screen flash.
- [ ] Slow and fast machines; confirm motion and pulse remain readable.
- [ ] Attach/change/break a bot; verify visual speed follows actual cycle duration.
- [ ] Equip default, another equipped skin and retro skins in each room.
- [ ] Open machine details; tap the image to confirm original skin enlargement still works.
- [ ] Save/reload a developed room; prestige and verify base visuals return.

Room checklist:

- [ ] Fresh Garage: base map, no progression decorations.
- [ ] Partially unlocked Garage: work lights emerge.
- [ ] Fully developed Garage: multiple lights, monitors and cable activity.
- [ ] Server Room red accents; Data Center green accents.
- [ ] Quantum purple animation and energy motes.
- [ ] Switch rooms and open/close Singularity: no normal room decorations on its page.

Performance checklist:

- [ ] Populate all visible slots and run all machines together.
- [ ] Scroll slots offscreen and verify effects cease activity outside the scroll area.
- [ ] Check an older Android device using the Godot profiler: compare frame time/FPS before and after, at idle and while all machines run.
- [ ] Confirm there is no growing particle/node count or persistent Tween accumulation.

Godot inspection areas: `game.tscn` root `RoomVisualEffects` (behind UI), slot `_machineVisual` children named `MachineVisualEffects`, and the machine image inside the details overlay. Compare tier 0 versus tier 3 and room stage 0 versus stage 4 in portrait layout. Inspect edge lights, monitor placement, skin outlines and completion brightness at actual device size.
