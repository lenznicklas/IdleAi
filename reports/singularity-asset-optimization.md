# Singularity asset optimization

## Candidates recorded before resizing

| File | Original | Proposed | Intended display (UI px) |
|---|---|---|---|
| `assets/singularity/core.png` | 1402×1122 | 512×410 | 237×190 |
| `assets/singularity/node_empty.png` | 1402×1122 | 256×205 | 237×190 |
| `assets/singularity/node_compute.png` | 1254×1254 | 256×256 | 190×190 |
| `assets/singularity/node_amplifier.png` | 1254×1254 | 256×256 | 190×190 |
| `assets/singularity/node_cooling.png` | 1254×1254 | 256×256 | 190×190 |
| `assets/singularity/node_quantum.png` | 1254×1254 | 256×256 | 190×190 |
| `assets/ui/singularity_matter.png` | 1254×1254 | 128×128 | 36×36 |
| `assets/ui/sector.png` | 1254×1254 | 128×128 | 36×36 |
| `assets/map/room_singularity.png` | 1448×1086 | 512×384 | 200×150 |
| `assets/map/locked_singularity.png` | 1448×1086 | 256×192 | 96×72 |

Sizes are logical UI pixels; physical pixels vary with viewport scaling. Core and startup artwork retain extra resolution. Rectangles use IgnoreSize and KeepAspectCentered; map sizes are explicit. Aspect ratios are retained within integer pixel rounding.

## Results

PNG source bytes: 21,275,602 → 1,478,553; saved 19,797,049 bytes (18.88 MiB).

Estimated RGBA8 texture storage without mipmaps: 59.99 → 3.06 MiB (94.9% reduction for these ten textures). This is a texture estimate, not measured device GPU usage or frame rate.

All imports unchanged: lossless compression, no mipmaps, alpha-border correction enabled. No code/scenes/layout/hitbox changes.

## Checked and unchanged

- `assets/bots/common.png`: 512×384. Already optimized; retained for shared previews.
- `assets/bots/epic.png`: 512×486. Already optimized; retained for shared previews.
- `assets/bots/rare.png`: 512×384. Already optimized; retained for shared previews.
- `assets/bots/legendary.png`: 512×384. Already optimized; retained for shared previews.
- `assets/bots/korpo_skin/common.png`: 512×384. Already optimized; retained for shared previews.
- `assets/bots/korpo_skin/epic.png`: 512×384. Already optimized; retained for shared previews.
- `assets/bots/korpo_skin/rare.png`: 512×384. Already optimized; retained for shared previews.
- `assets/bots/korpo_skin/legendary.png`: 512×512. Already optimized; retained for shared previews.
- `assets/bots/military_skin/common.png`: 512×468. Already optimized; retained for shared previews.
- `assets/bots/military_skin/epic.png`: 512×486. Already optimized; retained for shared previews.
- `assets/bots/military_skin/rare.png`: 512×385. Already optimized; retained for shared previews.
- `assets/bots/military_skin/legendary.png`: 512×385. Already optimized; retained for shared previews.
- `assets/bots/nature_overgrown_skin/common.png`: 512×385. Already optimized; retained for shared previews.
- `assets/bots/nature_overgrown_skin/epic.png`: 512×486. Already optimized; retained for shared previews.
- `assets/bots/nature_overgrown_skin/rare.png`: 512×463. Already optimized; retained for shared previews.
- `assets/bots/nature_overgrown_skin/legendary.png`: 512×385. Already optimized; retained for shared previews.
- `assets/bots/steampunk_skin/common.png`: 512×384. Already optimized; retained for shared previews.
- `assets/bots/steampunk_skin/epic.png`: 410×512. Already optimized; retained for shared previews.
- `assets/bots/steampunk_skin/rare.png`: 512×384. Already optimized; retained for shared previews.
- `assets/bots/steampunk_skin/legendary.png`: 512×384. Already optimized; retained for shared previews.
- `assets/bots/halloween_skin/common.png`: 512×384. Already optimized; retained for shared previews.
- `assets/bots/halloween_skin/offer.png`: 1672×941. Shop/startup offer, outside Singularity; excluded.
- `assets/bots/halloween_skin/epic.png`: 512×384. Already optimized; retained for shared previews.
- `assets/bots/halloween_skin/rare.png`: 512×384. Already optimized; retained for shared previews.
- `assets/bots/halloween_skin/legendary.png`: 512×384. Already optimized; retained for shared previews.
- `assets/bots/apocalypse_skin/common.png`: 512×512. Already optimized; retained for shared previews.
- `assets/bots/apocalypse_skin/epic.png`: 512×481. Already optimized; retained for shared previews.
- `assets/bots/apocalypse_skin/rare.png`: 512×384. Already optimized; retained for shared previews.
- `assets/bots/apocalypse_skin/legendary.png`: 512×410. Already optimized; retained for shared previews.
- `assets/ui/core_level.png`: 256×256. Already optimized; 36×36 stat icon.

Bot use: SingularityController.Bots.cs uses 45×45 map badges and 150px-high details; BotCatalog and BotSkinCatalog select all 28 base/skin textures. Prior reports/asset-optimization-summary.md records their completed optimization. Sector frames/connections/decorations use drawing commands and StyleBoxFlat; bottom bar uses a generated 512×96 gradient in RoomThemePalette. Promotional Singularity art is excluded.

## Verification

All ten PNGs decode, retain RGBA and transparent pixels, and match target dimensions. SHA-256 checks confirm every other asset PNG and import sidecar stayed unchanged during optimization. No upscaling/cropping, paths or filenames changed.

Godot 4.7.2 isolated import generated all ten .ctex resources. The editor reported an internal singleton error and failed to exit normally; processes were stopped. Full in-game/mobile visual and frame-rate validation was not performed.
