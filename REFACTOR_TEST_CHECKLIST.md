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

## Currency ownership phase

- [x] `dotnet build`: 0 errors, 353 CS8632 warnings.
- [x] Audit all first-party Token/shard writes and monetary Stats calls; classify remaining hydration/default/DTO writes in REFACTOR_SERVICES.md.
- [x] Review full diff, exact message/category literals, unchanged constants/save files/SDK callbacks; `git diff --check`.
- [x] Temporary built-assembly checks: earned/offline/grant/refund semantics, failed spends, category stats, bulk floating-point aggregation, fractional shards and prestige reset retention.
- [ ] Normal machine production gives correct Tokens, lifetime TotalEarned and RunEarnedTokens; verify pipeline income too.
- [ ] Slot unlock deducts exact cost and updates TotalSpent/SlotUnlockSpent.
- [ ] Machine upgrades: 1x, 5x, 10x, MAX, partial affordability and tier; compare exact balance and named/total spending, including large balances and milestone behavior.
- [ ] Bot purchase, repair and sale/refund: exact costs/refund, Bots/Bot Repairs stats; refunds leave earned/run/spent totals unchanged.
- [ ] Room unlock: exact Token cost, slot spending, eligibility and existing shard reward behavior (current UnlockRoom does not grant shards).
- [ ] Shard rewards and level reward claims: exact grant, claim marker, duplicate prevention and reload.
- [ ] Laboratory unlock, Research Point purchase and research purchases: exact balances and existing categories; RP remains independent.
- [ ] Pipeline, Infrastructure and Quantum upgrades: exact cost and stage/system/category stats.
- [ ] Shop production boost, bot luck, permanent production/offline upgrades: unchanged whole-shard ceiling, duration/stacking and effects.
- [ ] Instant production success: exact shard cost and Tokens/run/lifetime earnings; material/RP behavior unchanged.
- [ ] Instant production failure/refund: no completed cycles restores exact charged shards, no Token credit.
- [ ] Bot/machine skins and seasonal offers: exact quoted shard costs, ownership/equipment/save behavior.
- [ ] Offline earnings: exact balance, run/lifetime/offline stats; cold load and suspend/resume, boost expiry and research completion splits.
- [ ] Prestige: Tokens/run earnings zero; lifetime/offline/spending stats and shards retained; no new currency reward.
- [ ] Secret/debug codes: same grants/messages/validation; Token grants leave run/lifetime/offline earnings unchanged.
- [ ] Rewarded ads: one exact shard grant, same callback/save timing, failure grants nothing.
- [ ] IAP grants where available: same shard amount, deduplication and grant -> save -> acknowledge/consume/finalize ordering.
- [ ] Existing saves load identically; save/reload retains exact balances and stats, including legacy migration fixtures.
- [ ] Singularity unlock: exact normal Token cost and slot-spending stats, failure/duplicate checks.
- [ ] Singularity Matter production, purchases, refunds, offline settlement and independent saves remain unchanged.


## Progression / room eligibility phase

- [x] Build: 0 errors, 353 existing CS8632 warnings.
- [x] Direct built-assembly checks: invalid/bypass/stale-preview failures, insufficient balance, sequential unlocks, exact and duplicate spending, slot/reward initialization, legacy ownership, shared Singularity boundary.
- [x] Search and classify every normal-room ownership write; only UnlockRoom performs a new gameplay unlock. Removed integration guard symbols are absent.
- [x] Diff review: costs, slot/upgrade messages and algorithms, level/reward rules, save schema/migrations, scenes/node paths and integration internals unchanged.
- [ ] Room 1 remains initially unlocked.
- [ ] Locked Room 2 cannot unlock before every Room 1 slot is unlocked.
- [ ] Room 2 unlocks with every Room 1 slot unlocked and enough Tokens.
- [ ] Insufficient Tokens leaves ownership, balance and spending stats unchanged.
- [ ] Exact room cost is deducted once; repeated selection never charges again; TotalSpent / SlotUnlockSpent match baseline.
- [ ] Already-owned rooms always select, including older saves whose previous rooms/slots do not meet newer requirements.
- [ ] Sequential Room 2 -> Room 3 -> Room 4 progression works.
- [ ] Map locked/unlocked visuals and links remain correct; locked-room taps/tooltips show the same or reasonable prerequisite/cost message.
- [ ] Selecting the current room works; Lab hiding/page closing and room-unlock animation remain correct.
- [ ] Slot unlock flow and single/bulk/MAX/tier machine upgrades work with unchanged costs, messages and milestones.
- [ ] Total level matches baseline; level rewards keep 150-level cadence, 10-shard amount, ordered claims, duplicate prevention and reload persistence.
- [ ] Prestige preserves lifetime-level behavior; Play Games level leaderboard scores and achievements remain unchanged.
- [ ] Singularity availability/unlock matches the existing Room 4/all-slots map prerequisite, with unchanged Token price/spending and existing-owned access. Check before/after completing Room 4 slots and with insufficient Tokens; separate Matter/save behavior remains unchanged.
- [ ] Save/reload retains room states, first-slot initialization and reward flags, including historical saves.
