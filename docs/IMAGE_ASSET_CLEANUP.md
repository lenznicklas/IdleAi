# Image asset cleanup audit

## SAFE TO DELETE

- `assets/background/stat_box.png` (831,648 bytes): no filename, path, or UID references in non-generated repository content. TopBarController.ApplyTheme assigns procedural CardNormal/CardHover/CardPressed textures from RoomThemePalette to the stat card. No dynamic background-file loader targets this image.
- `assets/background/topbar.png` (1,092,801 bytes): no filename, path, or UID references in non-generated repository content. TopBarController.ApplyTheme assigns the procedural RoomThemePalette.Bar texture. No dynamic background-file loader targets this image.

## POSSIBLY UNUSED — intentionally kept

- `addons/admob/assets/android.png` — Unreferenced addon artwork; retained conservatively as part of the third-party plugin distribution.
- `addons/admob/assets/format-app-open.svg` — Unreferenced addon artwork; retained conservatively as part of the third-party plugin distribution.
- `addons/admob/assets/format-banner.svg` — Unreferenced addon artwork; retained conservatively as part of the third-party plugin distribution.
- `addons/admob/assets/format-native.svg` — Unreferenced addon artwork; retained conservatively as part of the third-party plugin distribution.
- `addons/admob/assets/icon-1024.png` — Unreferenced addon artwork; retained conservatively as part of the third-party plugin distribution.
- `addons/admob/assets/icon-15.png` — Unreferenced addon artwork; retained conservatively as part of the third-party plugin distribution.
- `addons/admob/assets/icon-500.png` — Unreferenced addon artwork; retained conservatively as part of the third-party plugin distribution.
- `addons/admob/assets/ios.png` — Unreferenced addon artwork; retained conservatively as part of the third-party plugin distribution.
- `addons/admob/assets/poing.jpeg` — Unreferenced addon artwork; retained conservatively as part of the third-party plugin distribution.
- `android/build/assetPackInstallTime/src/main/assets/assets/ui/app_icon.png` — Generated Android/export copy; not a source-asset cleanup target.
- `android/build/assetPackInstallTime/src/main/assets/assets/ui/loading_screen.png` — Generated Android/export copy; not a source-asset cleanup target.
- `android/build/src/instrumented/assets/icon.svg` — Generated Android/export copy; not a source-asset cleanup target.
- `assets/map/route_node.png` — No static reference found; retained conservatively because it is map/pipeline/UI artwork and its intended role is uncertain.
- `assets/pipeline/pipeline_bottleneck.png` — No static reference found; retained conservatively because it is map/pipeline/UI artwork and its intended role is uncertain.
- `assets/ui/quer_bild.png` — No static reference found; retained conservatively because it is map/pipeline/UI artwork and its intended role is uncertain.
- `promo/android/build/build/intermediates/assets/monoDebug/mergeMonoDebugAssets/promo_assets/icon.png` — Generated Android/export copy; not a source-asset cleanup target.
- `promo/android/build/src/instrumented/assets/icon.svg` — Generated Android/export copy; not a source-asset cleanup target.

## USED — important indirect references

- MachineSkinCatalog constructs all 60 retro, lab, and Halloween texture paths for rooms 1–4. Retro uses machine1–machine4; lab and Halloween use machine_1–machine_4; every pack includes empty.png. All retained. Lab packs are the experimental/prototype machine artwork.
- BotSkinCatalog constructs 24 rarity paths across nature_overgrown_skin, steampunk_skin, apocalypse_skin, military_skin, halloween_skin, and korpo_skin. The secret Korpo definition is outside GetAll but reachable through TryGet after unlock; all retained.
- Skin saves persist IDs resolved against the catalogs, rather than arbitrary texture filenames. Shop previews and machine/bot rendering use catalog Texture2D objects.
- project.godot identifies assets/ui/app_icon.png by UID (44mag1j18nrj). Import metadata was used only to resolve UID identity, never as evidence of usage.
- IapCatalog supplies product image paths to IapShopController. Halloween offers and startup content reference bot offer artwork and the machine-pack offer graphic.
- SingularityController and SingularityMapExtension load node/core textures and map icons. Node quality effects, including Prototype, are generated in code.
- Promo has its own resource root; res://promo_assets/... resolves inside promo/. Its artwork was retained.
- AiArchive and HalloweenMachinePack test runners enumerate every assets PNG to register lightweight in-memory GradientTexture2D mocks. They do not read the image bytes or require the two legacy filenames; this enumeration was reviewed rather than mistaken for gameplay loading.

## Scope and method

- Enumerated the complete working repository, including ignored Android build output and untracked promo. Excluded .git internals, Godot caches/import directories, *.import as usage evidence, and generated bin/obj directories. Scanned 292 image files and read 2202 non-image text files across source, scenes, resources, settings, JSON, docs, addons, tests, and export definitions.
- Reviewed GD.Load/ResourceLoader/load/preload calls, path concatenation/interpolation, directory enumeration, catalogs, save IDs, autoloads, themes, StyleBoxes, scenes, animation/particle/material references, and filename conventions. No source .res or shader files were present. Binary deployment archives, native libraries, audio/fonts and Gradle artifacts were inventoried; they are generated/third-party artifacts, not authoritative current gameplay-reference sources.
- 84 images have confirmed constructed catalog paths; 189 have textual path matches (some matches are documentation or generated export records, so retained conservatively); 19 initially lacked matches, of which 2 are safe and 17 are retained.
- Existing changes: IdleAi.csproj and export_presets.cfg modified; promo/ untracked. These are preserved. No code or gameplay changes, placeholders, or manual cache/import deletion.

## Results

- Images scanned: 292, including Android source/build/export copies (Godot cache/import files excluded).
- Deleted: 2 images; saved 1,924,449 bytes (approximately 1.84 MiB). No cache/import files manually deleted.
- Deleted files: assets/background/stat_box.png; assets/background/topbar.png.
- Intentionally kept: all 17 POSSIBLY UNUSED files listed above, plus all directly/indirectly referenced or uncertain artwork.
- Post-deletion repository filename scan: zero references outside this report and generated import/cache metadata.
- All 37 .tscn/.tres files checked: zero broken ext_resource paths, accounting for nested project resource roots.
- dotnet build IdleAi.csproj --nologo -p:NuGetAudit=false: succeeded, zero errors and warnings. An earlier normal build also succeeded but NuGet vulnerability lookup reported NU1900 due to unavailable network access; the final build disabled that lookup.
- Godot 4.7.2 Mono headless editor/import on an isolated /tmp project copy: exit 0, no reported errors/warnings on the final run. Initial sandbox run reported editor socket permission failures and missing assembly in the fresh copy; copied the built assembly and reran outside the sandbox successfully. Gameplay was not started. This is import/editor validation, not a complete runtime exercise of every unlock/shop path.
- Existing modified project/export file hashes unchanged. Existing promo files were read and left unchanged.
- Godot validation logs: /tmp/idle_godot_validation_unsandboxed.log (final), /tmp/idle_godot_validation.log (initial).

### git diff --stat

```text
 IdleAi.csproj                  |   2 ++
 assets/background/stat_box.png | Bin 831648 -> 0 bytes
 assets/background/topbar.png   | Bin 1092801 -> 0 bytes
 export_presets.cfg             |   2 +-
 4 files changed, 3 insertions(+), 1 deletion(-)
```

### git status --short

```text
 M IdleAi.csproj
 D assets/background/stat_box.png
 D assets/background/topbar.png
 M export_presets.cfg
?? docs/IMAGE_ASSET_CLEANUP.md
?? promo/
```

The stat includes the user's pre-existing project/export changes and excludes the untracked report and promo directory.

