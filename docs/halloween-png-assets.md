# Halloween PNG integration

Inspected all 17 PNGs in `assets/halloween_event/`, including a visual contact sheet, native dimensions, alpha bounds and Godot import remaps. All sprites/icons are 1254×1254 RGBA; the portrait background is 941×1672 RGB. Source PNG bytes, filenames, dimensions and transparent padding were not edited. No new artwork or placeholders were created.

All filenames/paths below are case-sensitive. `HalloweenEventAssets` is the central path catalog; HalloweenEventDefinition maps those paths to existing content. No Halloween path appears in generic UI code.

| Source file | Classification | Used for |
| --- | --- | --- |
| event_icon.png | Event identity | Map Event card; subtle final-results and Archive identity |
| background.png | Background | Full Event screen, aspect-cover with existing dark tint/opaque panels |
| souls.png | Currency | Souls header, generator costs, Prestige progress, currency rewards, Shop prices/resource rewards, end/Archive icons |
| cursed_core.png | Currency | Cursed Cores header, Prestige panel, core rewards, core-denominated Shop prices, end/Archive icons |
| abandoned_terminal.png | Generator | Abandoned Terminal |
| haunted_workstation.png | Generator | Haunted Workstation |
| possessed_server.png | Generator | Possessed Server |
| cursed_datacenter.png | Generator | Cursed Datacenter |
| soul_processor.png | Generator | Soul Processor |
| nightmare_ai_core.png | Generator | Nightmare AI Core; existing saved generator ID remains nightmare_core |
| ghost_bot.png | Bot | Ghost Bot card/detail and its track reward |
| pumpkin_bot.png | Bot | Pumpkin Bot card/detail and its track reward |
| skeleton_bot.png | Bot | Skeleton Bot card/detail and its track reward |
| reaper_bot.png | Bot | Reaper Bot card/detail and its track reward |
| reward_chest.png | Resource cache / Shop | Recovered Soul Cache Shop offer |
| machine_skin_pack.png | Cosmetic pack | Final four-room Halloween Machine Skin Pack reward preview |
| bot_skin_pack.png | Cosmetic pack | Halloween Bot Skin Pack Reward Track and Shop previews |

## Missing, fallback and unused artwork

- Successfully used: all 17 supplied PNGs.
- Relevant but unused: none.
- Unclear-purpose assets: none.
- Dedicated challenge-type, lock, claimed/check and archive/end-screen PNGs: absent. Existing text/status styling remains; end/Archive reuse the event identity and currency icons. Halloween currently has no configured Challenges; no tasks/gameplay were added just to display artwork. Generic challenge rendering remains unchanged.
- Dedicated Data Shard PNG in this folder: absent; retain the existing shared `assets/shop/data_shard.png`.
- Individual Haunted Garage skin PNG in this folder: absent; retain its actual existing `assets/machines/room_1/skin_halloween/machine_4.png` for the single-room offer/reward. The four-room pack preview is not substituted for an individual skin.
- No Halloween Event Bot offers are currently configured in its Shop. Inventory/details/track use the dedicated PNGs; no offers/rewards were added or changed to expose more images.
- Manual cropping/cleanup required: none for the supplied assets. Transparent padding and glow edges are preserved; the full source rectangles fit inside bounded slots.

## Generic presentation changes

Added `EventPrestigeConfiguration.IconPath` with the existing generic core image as the default, keeping other Events compatible. Header/Prestige widgets use this configuration, and Shop core-price fallback now uses it too. Generator cost icons use the containing Event currency image. End/Archive rows reuse configured identity/currency icons and show existing Prestige earnings statistics. Missing retired archive definitions do not reserve an empty icon row.

All sprites use existing bounded EventImageView slots or existing aspect-fit Map/header controls. The background ignores native minimum size and uses aspect cover; existing alpha tint (.45) and nearly opaque card panels keep text readable. Source texture dimensions never become card minimum sizes. No opening scale, bounce or border animation was added.

Textures flow through the existing shared EventTextureCache. Resource assignment occurs when the Event/content is built or its identity changes, not every production tick. Repeated uses of the same PNG share the same underlying Texture2D resource.

## Gameplay/save preservation

Schedules, Event IDs, generator IDs, unlocks, production, upgrade costs/growth, bot multipliers, Prestige formula, milestones/reward amounts/IDs, Shop prices/limits and skin ownership IDs are unchanged. No save schema changes. Dedicated tests verify the existing economy/content counts and stable nightmare_core ID alongside the new visual mappings.

## Import and export compatibility

Godot generated 17 `.png.import` sidecars and corresponding imported textures. Each source path and imported destination was checked on Linux with exact casing, and all 17 textures load through ResourceLoader. Lossless 2D imports preserve alpha and are platform-neutral. Original files remain untouched.

The existing Android preset uses `all_resources`; its only resource exclusion is `promo/*`, so the Halloween PNG imports are included even though paths are defined in C#. No export preset or Android configuration changes were needed. Debug and ExportRelease builds validate the managed code. Resource paths/scaling are tested on Linux; no physical Android device/APK runtime test was performed.

The editor import completed successfully. It emits an unrelated existing Android-plugin shutdown message about EditorSettings (`export/android/shutdown_adb_on_exit`); there were no missing PNG/imported-texture errors.

## Files changed

Added:

- `scripts/game/events/HalloweenEventAssets.cs` (and Godot `.uid` metadata)
- `tests/Events/HalloweenAssets.cs.txt`
- `tests/Events/halloween_assets_scene_smoke.gd`
- `docs/halloween-png-assets.md`
- 17 Godot-generated `assets/halloween_event/*.png.import` sidecars. The 17 supplied PNGs themselves are unchanged.

Modified:

- `scripts/game/events/HalloweenEventDefinition.cs`
- `scripts/data/EventGameplayDefinition.cs` (generic Prestige icon configuration only)
- `scripts/ui/events/EventView.cs`
- `scripts/ui/events/EventPrestigePanel.cs`
- `scripts/ui/events/EventGameplayCards.cs`
- `scripts/ui/events/EventShopCard.cs`
- `tests/Events/Events.csproj`
- `tests/Events/Program.cs.txt`
- `scripts/game/events/README.md`

Other existing workspace edits were preserved. Godot's editor may also generate `.uid` metadata for pre-existing untracked scripts; their source code was not changed by this integration.

## Validation results

- Debug / ExportRelease: zero compilation errors; existing repository warnings remain.
- Event suite: 403 assertions passed, including save/progression/reward/shop/bot regressions and visual-only balance contracts.
- New Halloween PNG scene fixture: 1,823 checks, zero failures.
- Existing Halloween gameplay scene: zero failures.
- Existing Event tab scene: 2,203 checks, zero failures.
- Existing normal/Event/Singularity navigation scene: 122 checks, zero failures.
- Existing Event Shop scene: 910 checks, zero failures.

## Active-definition binding fix

The Event page previously pinned the ID selected by `OpenCurrent()`. A later manager selection (including a debug schedule override) updated the active Event/Map but left the open page rendering cached Test Protocol cards. `EventView` now observes changes to `DisplayEvent` and follows the selected definition. Its existing rebuild path then updates the background, currencies, theme, generator/bot/reward/challenge/shop cards and dismisses obsolete overlays. Opening Event always resets to the manager's selection. Only explicit Archive viewing stays pinned intentionally.

The initial binding fix did not address the live catalog: Test Protocol was enabled from January 2026 through January 2030, while Halloween started October 20. On October 14 the manager correctly selected Test Protocol. The catalog now disables Test Protocol for normal play (explicit Debug force still enables it for that session), and Halloween's fixed UTC start is October 14, 2026, with November 3 end unchanged. Disabled last-event IDs cannot become display fallbacks. No save migration, reset or asset changes were needed.

The regression fixture now opens Test Protocol before forcing Halloween, switches both ways repeatedly with every tab already built, checks actual generator/bot names, rejects stale visible text/images, checks the Map after each switch and verifies that EventPage is reused. Debug and ExportRelease builds pass with zero errors. Headless scene checks cover all seven tabs and three portrait sizes; this does not replace a physical Android visual review.

## Live-selection follow-up

The real local save was inspected read-only: `LastEventId` was `event_test_001` and it contained no Halloween progress. The original selection was caused by the enabled sandbox's January 2026–January 2030 window and Halloween's future October 20 start, rather than catalog order or an asset-load failure. The previous binding fix alone therefore could not make Halloween active on October 14.

Normal selection now considers enabled, in-window, non-ended definitions, ordered by type, latest start and stable ID. Halloween is enabled October 14–November 3 UTC. Test Protocol is disabled in the catalog in both build configurations. A saved ID is historical state, not an override, and a disabled saved definition cannot be the fallback display. Without an active Event, enabled last-event results may still display. Debug force explicitly enables its selected definition, holds that sandbox session even after expiration, and does not persist schedule overrides; restarting restores the live catalog. Only explicit Archive browsing pins historical UI content. Opening Event logs active and rendered IDs in DEBUG.

Files changed for this follow-up:

- Runtime/configuration: `scripts/game/events/EventCatalog.cs`, `scripts/game/events/HalloweenEventDefinition.cs`, `scripts/game/events/EventManager.cs`, `scripts/ui/events/EventView.cs`.
- Domain tests: `tests/Events/Gameplay.cs.txt`, `tests/Events/HalloweenAssets.cs.txt`.
- Scene regressions: `tests/Events/halloween_assets_scene_smoke.gd`, `tests/Events/navigation_scene_smoke.gd`, `tests/Events/scene_smoke.gd`, `tests/Events/shop_scene_smoke.gd`, `tests/Events/bots_scene_smoke.gd`, `tests/Events/challenges_scene_smoke.gd`, `tests/Events/rewards_scene_smoke.gd`. Sandbox-specific fixtures now explicitly force Test Protocol instead of relying on it being the live Event.
- Test helper: `tests/Events/image_layout_checks.gd` now targets a generator image for synthetic 240×160 source-size checks, instead of accidentally selecting the newer, smaller header currency slot. The older combined scene fixture also now checks the existing real Shop instead of its removed placeholder.
- Documentation: `scripts/game/events/README.md`, `docs/halloween-png-assets.md`.

Validation: Debug and ExportRelease build with zero errors (existing warnings remain); Event domain suite passes 410 assertions. Halloween asset scene passes 6,858 checks, including unforced live startup plus repeated forces both ways. All 17 exact-case PNG paths load; generator and bot names/images, Souls/Cursed Core images, background, rewards, Shop and portrait bounds are checked. Halloween has no configured Challenges or dedicated challenge PNGs; the generic empty state is used, with no Test Protocol task substitution. No asset files or save data were modified.

Other final scene regressions pass: normal navigation 155 checks, tabs 2,203, Rewards 840, Challenges 998, Bots 67 and Shop 910. Combined sandbox and Halloween gameplay scenes also finish with zero failures. These run headless on Linux using isolated save profiles; no physical Android visual test was performed. Existing Godot scene UID fallback and isolated domain fixture asset warnings remain unrelated to selection; the exact-case Halloween PNG scene has no missing Halloween resource errors.
- All 17 `.png.import` source/destination paths and existing imported texture files verified with exact casing.
- `git diff --check`: passed.

The new scene fixture opens Halloween via the existing development-only schedule override in an isolated /tmp save profile. It checks the real Map icon, background fitting/tint, both currencies, six generators and their cost icons, all four Bot details, track/shop previews, final/Archive decorations, texture instance reuse, and portrait bounds at 1080×2400, 720×1600 and 540×960 across every Event tab. It does not change production event dates or the player's normal saves.
