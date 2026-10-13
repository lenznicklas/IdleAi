# Idle AI recording sandbox

## Common → Legendary bot reel

Open `BotUpgradeReel.tscn` in **this promo project** and press F6, or run:

```sh
dotnet build promo/PromoRecording.csproj
godot-mono --path promo res://BotUpgradeReel.tscn
```

One fixed Server machine produces automatically. Wait briefly at Common, then tap
**UPGRADE BOT** three times to reach Rare, Epic, and Legendary. Each upgrade changes
only the bot. Promo income is $10 → $1K → $100K → $10M per second (×100 at each step).
The counter integrates income each frame and uses Idle AI's shared `NumberFormatter`.
Rarity changes give the bot a brief pop/glow; holding the button does not animate it.
Legendary disables the button and keeps a stronger glow. After a 2.5-second hold,
a gold **COMING SOON** ending card appears above the darkening screen: shade fades
over 0.45 seconds, text fades over 0.55 seconds with a 0.2-second delay. The message
stays visible until the scene is stopped; production and money effects continue.
Stop/run the scene to reset.

The scene shares the other reels' background texture/dark blue tint and existing
tap ripple. Floating money rises/fades over 0.65 seconds, spawning every 0.9 / 0.55 /
0.32 / 0.2 seconds as rarity increases, with at most six live popups. Bot and button
sit above an expandable bottom recording-safe area. Physical mouse/touch presses
show one ripple on the button; synthesized duplicate input is ignored.

Machine and bot artwork gently hover (4–5 logical pixels, reduced on short screens)
inside fixed container frames. Successful rarity changes add a 5% bot pop settling
over 0.28 seconds and a brief glow. Neither animation changes card/button bounds.
The ending Label keeps its natural minimum width, so centering cannot clip it away.

The scene uses the existing server/font assets and copies of the game's four bot
textures. It never initializes game state, reads saves, or writes saves. The shared
formatter is compiled directly from its original source; there is no game assembly
dependency. Neither project's default scene nor any normal game balance changes.

Headless regression check (after importing the promo project):

```sh
godot-mono --headless --path promo res://ReelCheck.tscn
```

For an Android recording, run this scene with F6 using the promo project's existing
remote deployment workflow. Its normal export/default scene remains unchanged.

This folder is a **separate Godot 4.7.2 Mono project**, deliberately not a scene launched from the production game. Its `res://` root is this folder. Open `promo/project.godot`, not the repository's `project.godot`, to record or export it.

This is the alternative to changing the production main scene or disabling its Play Games autoload. The promo project has no autoloads, addons, external game references, service initialization, save reads/writes, or production assembly dependency. Even F6 from the production project would initialize its autoload, so the promo folder is hidden there with `.gdignore`.

## Run and record

1. Import `promo/project.godot` into the Godot **Mono/.NET** project manager.
2. Build the C# solution using the editor's Build button.
3. Press F5, or open `AdDemo.tscn` and press F6.
4. Tap the large button, or use Space. Five upgrade taps fill the bar; the sixth tap evolves. Evolution takes approximately 0.62 seconds. Tap every 0.4–0.7 seconds for a roughly 15–25 second trip through all five machines.

Controls: **Space** upgrade, **E** force evolution, **R** reset, **1–5** select stage, **F** toggle desktop fullscreen. The final Mystery PC is a terminal teaser: upgrades and evolution stop immediately, the lower UI fades out over 0.65 seconds, and a gold COMING SOON fades in over 0.65 seconds after a 0.20-second delay. Money/income and the machine remain visible. Reset and stage selection cancel active evolution, teaser fades and effects, restoring the normal UI. Selecting stage 5 directly also enters the teaser state. Touch uses the same native Godot Button as desktop input.

Every Android touch press or desktop left-click produces a white touch ripple at the pointer position, including on the upgrade button and teaser screen. `TapIndicator.tscn` / `TapIndicator.cs` are reusable visual components: two translucent rings and a soft center expand and fade over 0.34 seconds, then free themselves. A separate canvas layer renders them above UI and flashes without following screen shake or consuming input. Synthesized touch/mouse events are ignored to avoid duplicate indicators; simultaneous physical touches each get their own ripple. Releases, drags, right-clicks and keyboard controls do not spawn ripples.

The portrait composition uses 720×1280 with aspect kept and black bars on taller/wider screens. Important UI stays within x=60–600 and y=136–1102, reserving the right edge and the top/bottom for recording overlays. Currency is sponsored locally on taps so recording never stalls. No money is saved. Numbers are capped at large finite values for very long sessions.

The sandbox is intentionally silent; `score.wav` is music rather than an upgrade effect and is not included. There are no placeholder images. The artwork is existing game art used under promotional names; the Basic Processor is the existing laptop rather than a newly drawn old computer.

## Choose Your Machine (standalone second ad)

Open `ChooseMachineAd.tscn` in this **promo project** and press F6 (Run Current Scene).
F5 still runs the original AdDemo. Click/tap either large artwork card, or press **1** / **2**;
**R** resets at any point. Physical touch and left-click use the existing exact-position
TapIndicator, with synthesized duplicate events filtered. Selection locks immediately.

The laptop (`basic.png`) and quantum machine (`quantum.png`) idle side by side. One tap
highlights the choice and dims the other card. After 0.35 seconds both produce for three
seconds: +25 every second versus +500 every 0.2 seconds (100× output), with floating
numbers, particles, glow pulses and totals. The strong choice adds restrained screen
shake. A brief SMART CHOICE / THINK BIGGER payoff holds for 1.1 seconds, then fades out
over 0.2 seconds and resets all values/effects. No game economy or saves are involved.

For Android scene testing, enable **Deploy with Remote Debug** and **Small Deploy with
Network Filesystem** in the promo editor, connect an ARM64 phone via USB/ADB, select
the Android device in the run target, then press F6 with `ChooseMachineAd.tscn` open.
Use the existing Android Promo preset and this project's own Android build template.
Test both choices, simultaneous touches, touches during comparison, and several automatic
replays. The normal exported promo APK still opens AdDemo; remote F6 tests this scene
without changing either project's main scene or export configuration.

## Export the promo APK (AdDemo)

In the **promo project**:

1. Use the same installed Godot **4.7.2 Mono export templates**, Android SDK/JDK and .NET dependencies as the game.
2. On a fresh checkout choose **Project → Setup Android Build** (install the Gradle build template into this project's own `android/` folder). Never point this preset at the production `android/build` folder. Generated Android files are ignored by Git.
3. Choose **Project → Export → Android Promo → Export Project**.
4. Leave **Export With Debug** enabled for a locally installed recording APK. The existing global Godot debug keystore configuration is used. For a release APK, configure signing in this promo preset separately.
5. Export `IdleAi-Promo.apk`, install it on an ARM64 Android phone, and launch **Idle AI Promo**.

The preset uses APK format, ARM64, a `promo` feature tag and package ID **`com.lenznicklas.idleai.promo`**. It uses a separate assembly (`PromoRecording`) and user directory (`IdleAiPromo`). Both apps can be installed together. There are no ad, billing, Firebase or Play Games export plugins in this project. Gradle is enabled for the Mono Android runtime. Do not exclude `*.cs` from resource export: Godot needs to see C# resources to package the assembly; `dotnet/include_scripts_content=false` handles source stripping.

Equivalent commands from the repository root:

```sh
dotnet build promo/PromoRecording.csproj
godot-mono --headless --editor --path promo --import
godot-mono --headless --path promo --export-debug "Android Promo" /absolute/path/IdleAi-Promo.apk
```

## Export the normal game

Open the **repository root** `project.godot` and export the existing **Android** preset as before. It still starts `game.tscn`, retains the existing Gradle setup, package ID `com.lenznicklas.idleai`, version, plugins, permissions, signing behavior and default AAB format. For a production APK, use the normal preset's existing format option as you ordinarily would.

Only two existing files changed:

| File | Reason |
| --- | --- |
| `IdleAi.csproj` | Excludes `promo/**/*.cs` so production cannot compile the sandbox or its generated C# files. |
| `export_presets.cfg` | Adds `promo/*` to the production Android resource exclusion. Godot matches full resource paths, including descendants. |

No existing scene, save schema, gameplay value, startup path, or Android integration was modified. Future production export presets should preserve the promo exclusion. The `.gdignore` and nested project boundary also keep the promo out of the production resource scan.

## Files and copied assets

Core implementation: `AdDemo.tscn`, `AdDemo.cs`, `PromoMachine.cs`, `PromoMachineData.cs`, `TapIndicator.tscn`, `TapIndicator.cs`. Standalone configuration: `project.godot`, `PromoRecording.csproj`, `PromoRecording.sln`, `export_presets.cfg`, `.gdignore`, `.gitignore`. Godot-generated `.uid` and `.import` metadata accompany the scripts/artwork.

Machine/background/font/icon assets are **unchanged copies**, except the supplied Mystery PC teaser artwork. Originals remain untouched:

| Promo asset | Original source from repository root |
| --- | --- |
| `basic.png` | `assets/machines/room_1/machine1.png` |
| `server.png` | `assets/machines/room_2/machine4.png` |
| `supercomputer.png` | `assets/machines/room_3/machine4.png` |
| `quantum.png` | `assets/machines/room_4/machine4.png` |
| `mystery_pc.png` | Supplied teaser artwork in `promo_assets/` (final stage) |
| `singularity.png` | `assets/singularity/core.png` (retained unused copy) |
| `background.png` | `assets/background/bg_room4.png` |
| `Orbitron-Regular.ttf` | `assets/fonts/Orbitron-Regular.ttf` |
| `icon.png` | `assets/ui/app_icon.png` |

No images are placeholders. Visual effects are procedural Control drawing and short-lived tweens. No new external packages are used. The remaining deployment dependency is the local Mono Android toolchain and signing configuration; actual phone performance and Reel overlay placement should be checked on the recording device.

## Validation performed

- Promo C# build: zero compiler errors and warnings in the final check.
- Production C# build: zero errors; existing dependency audit/network and addon warnings are unrelated to the promo.
- Mono Godot runtime smoke check: all five stages, five-tap evolution thresholds, input lock during evolution, terminal Mystery PC input lock, smooth lower UI/COMING SOON fades, stage selection and reset during both evolution and teaser fading.
- Tap feedback runtime check: mouse/touch positions, overlay transform conversion, upgrade button pass-through, expansion/fading and automatic cleanup, simultaneous touches, synthesized-event filtering, and feedback on the teaser screen.
- Portrait window rendered and inspected; the machine platform uses a flattened ellipse below the artwork.
- Debug APK exported and signature verified. APK inspection confirmed `PromoRecording.dll` and the ARM64 Mono runtime, package ID `com.lenznicklas.idleai.promo`, and no production assembly, gameplay scene or addon resources.
- Evaluated production compile inputs: no files under `promo/`.

The local APK is `promo/IdleAi-Promo.apk` (ignored by Git). The installed Godot editor emitted an `EditorSettings not instantiated ... shutdown_adb_on_exit` message on headless export shutdown after completing export; APK signature verification passed. No phone installation or real-device recording was performed.
