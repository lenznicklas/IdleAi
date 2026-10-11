# Service refactor verification checklist

Unchecked items require manual verification; compilation does not prove gameplay equivalence. Use copies of real save files and an isolated test profile for destructive/corruption scenarios. Compare against the pre-refactor build with the same starting data. Do not edit production saves.

## Automated verification

- [x] `dotnet build` after phase 1: 0 errors; same 351 baseline CS8632 warnings.
- [x] `git diff --check` and review constructor references, initialization order and changed file scope.
- [x] Verify service logic, DTOs, save versions, scenes, assets, node/resource paths and gameplay constants are unchanged.

## Godot / device verification

- [ ] **Game starts:** Cold start, initial message, first machine, deferred overlays and shop prewarm; compare baseline visuals.
- [ ] **Existing save loads:** Use a COPY of a pre-refactor save plus its independent stores. Check rooms, balances, research, boosts and cycle progress; retain originals.
- [ ] **New save works:** Use a separate test user-data directory/profile; start, play, quit and reload.
- [ ] **Tokens load/save correctly:** Compare exact balance, run earnings and stats before/after reload.
- [ ] **Data Shards load/save correctly:** Compare exact balance; buy/claim once, reload and verify no duplicate grants.
- [ ] **Rooms unlock correctly:** Check prices, sequential gating, requirements, shard rewards and navigation.
- [ ] **Machines produce correctly:** Manual START and automated cycles; compare durations, output and research-point awards.
- [ ] **Bots work correctly:** Buy, automate, wear out, repair/service and sell; reload durability.
- [ ] **Machine upgrades work:** Single, bulk/MAX and tier upgrades; verify costs, caps and milestone shards.
- [ ] **Bot upgrades work:** Exercise available bot power research/luck/repair operations; verify effects and costs (no new upgrade mechanic added).
- [ ] **Research works:** Unlock Lab, buy points, start/finish normal and endless research; reload an active timer.
- [ ] **Prestige works:** Check eligibility, multiplier and exact reset/retained state.
- [ ] **Total level remains correct:** Compare before/after upgrades, prestige and reload; include pre-v21 migration fixture.
- [ ] **Offline earnings work:** Cold reload and Android/iOS suspend/resume; include boost expiry, research completion and future/equal save timestamp.
- [ ] **Level rewards work:** Top-bar indicator, ordered 150-level claims, 10-shard award and claim persistence.
- [ ] **Skins remain unlocked correctly:** Owned/equipped bot and per-room machine skins appear on FIRST render and after reload/prestige; previews/defaults work.
- [ ] **Secret codes work:** Known valid/invalid codes, repeated redemption and existing debug-token stats behavior.
- [ ] **Settings persist:** Toggle vibration and sound; restart and verify values survive prestige.
- [ ] **Music/sound settings work:** Music starts exactly once, loops, mute stops playback, re-enable restarts; Master bus and vibration gate behave as before.
- [ ] **Firebase authentication works:** Android sign-in/server-code exchange/token refresh and reconnect.
- [ ] **Usernames work:** Existing alias load, valid save, duplicate/invalid input and restart.
- [ ] **Leaderboard works:** Custom level/prestige Top 25, own rank/username and official Play Games scores.
- [ ] **Google Play Games achievements work:** Sign in, trigger eligibility, sync and reopen native UI; no duplicate/unexpected unlocks.
- [ ] **Rewarded ads work:** Explicit request, loading/failure/dismissal, one reward per completed video, persistence.
- [ ] **Singularity loads:** Separate existing save, sectors/core levels/nodes/bots and map navigation unchanged.
- [ ] **Singularity production works:** Matter output, core/node build/upgrade/sell, bot durability/repair, cooling/Quantum and pan/touch behavior.
- [ ] **Singularity offline earnings work:** Cold reload and mobile suspend/resume; verify efficiency/cap and one-time payout.
- [ ] **Android build still succeeds:** Godot Android export with existing presets, signing and plugins; install and smoke-test.
- [ ] **IAP consumables remain correct:** Purchase/restore/retry, exactly-once shards, persisted purchase ledger and completion after save.
- [ ] **Main save recovery remains correct:** In isolated test copies only: invalid main + valid backup recovers; both invalid block autosave and preserve files.
