# AI Archive regression checks

From the repository root:

```sh
dotnet build IdleAi.csproj
dotnet build tests/AiArchive/AiArchive.csproj
ARCHIVE_REPO_ROOT="$PWD" XDG_DATA_HOME=/tmp/idle-archive-ui-check-data godot-mono --headless --path tests/AiArchive
```

The runner uses real catalogs/progress services with in-memory art and the game's
Orbitron font. It bypasses Game startup and initializes no save or external services.
Checks cover distinct Bot Skins/Machine Skins categories and permanent discovery
across real bot purchase/sale, tier upgrades, Prestige, inventory removal, JSON
save/load and legacy migration. Badge names/artwork and ended-event discoveries
remain catalog driven. UI checks open every category, navigate back,
repeat openings, switch pages within one frame, and close/reopen at portrait sizes
720×1600 (the 1080×2400 aspect ratio) and 360×800.

Every displayed Control's rectangle, scale, and font size is checked after native
deferred layout and across eight subsequent frames/readability scans. This catches
delayed text-metric changes and opening layout/scale movement. Hover/pressed styles,
scrolling, card borders, and unrelated overlays retain their existing implementation.
Headless checks do not replace an Android rendering check.

Permanent collection IDs live in the main save's additive `Archive.Discovered` set.
Load seeds known entries from current slots, room reward history, lifetime machine
spending, cosmetic sidecars and Event entitlements. Acquisition paths also record
IDs before an item can be sold or reset. Removing inventory never removes IDs.
Old saves cannot identify previously sold bot rarities without surviving evidence;
migration preserves provable history and does not invent unknown discoveries.

The normal Badges category contains one evolving Level Badge after total level 150.
`Archive.HighestLevelBadgeTier` increases automatically and never decreases; legacy
claimed-level evidence seeds it on load without awarding Data Shards. The old claim
service/overlay and shard indicator are removed. NumberedBadgeView draws fitted,
centered text over the shared image; UI tests cover single through eight-digit tiers.

Level Badge celebrations establish a silent baseline after save hydration. Tests
cover one presentation at the first threshold, coalescing rapid/multiple thresholds,
reusing a visible popup, fade/scale completion and removal, input passthrough,
load/reset suppression, no shard grants, and cleanup during an active animation.
Sparks use ten short-lived border glints and no idle processing or particle textures.

Real gameplay integration (crosses 149 → 150 through the normal slot action,
checks the saved Archive tier, fade cleanup, unchanged shards and silent restart):

```sh
XDG_DATA_HOME=/tmp/idle-badge-popup-scene-test godot-mono --headless --path . --script tests/AiArchive/badge_popup_scene_smoke.gd
```
