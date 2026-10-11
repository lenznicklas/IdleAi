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

## Normal offline extraction regression matrix

Automated: build then `dotnet run --project tests/OfflineProgress/OfflineProgress.csproj`.

- [x] Zero and negative elapsed; short/base income; year-long interval (normal game has NO time cap).
- [x] Offline upgrade levels 0, 4, 15 and efficiency clamp.
- [x] Boost entire interval, halfway expiry, already expired; research before/at/after boost expiry.
- [x] Research incomplete, due at/before save, one second after save, halfway, near end, exactly end.
- [x] Production multiplier and offline-efficiency research changes; invalid research clearing; locked Lab and no research.
- [x] Pre-v15/pre-v19/current income selection and retained legacy boost normalization helper.
- [x] Equivalent cold-load/resume snapshots; duplicate pause, repeated resume, resume without pause, backwards resume.
- [x] Exact Tokens, RunEarnedTokens, TotalEarned and OfflineEarned.

Required manual checks (pending):

- [ ] Fresh launch, current save, older save; verify startup failure/research/earnings/default message precedence and resume earnings/research precedence.
- [ ] Close/reopen after short and long periods; verify full uncapped normal earnings (Singularity's separate cap unchanged).
- [ ] Android screen off/background/resume, repeated cycles; iOS lifecycle; desktop does not use mobile path.
- [ ] Background with production boost; return before/after expiration.
- [ ] Research incomplete/completed while away; completion while boost active and after boost expires.
- [ ] Offline upgrade level 0 and upgraded efficiency; exact offline Token amount and all four counters.
- [ ] Save immediately after resume; restart immediately after resume does not double-pay.
- [ ] Prestige followed by offline period; Lab locked; no active research.
- [ ] Production bots/cycle state prepared after load/resume; Singularity offline income unchanged.

## Normal save mapping / migration extraction

Automated commands and fixture design are documented in REFACTOR_SERVICES.md.

- [x] Current SaveVersion remains 21; DTO schema/defaults and serializer unchanged.
- [x] Full current roundtrip and save -> load -> save compares every persisted DTO property.
- [x] Exact Tokens, RunEarnedTokens, Data Shards, stats and spending dictionary.
- [x] Lab unlock/points/completed/active research ID and timestamp; Prestige/AiCores/count/lifetime base.
- [x] All four rooms, eight slots each, unlock/reward flags; every slot field, every bot rarity, price, healthy/broken/legacy durability, running cycles and milestones.
- [x] Pipeline levels/buffers/cycles; Infrastructure levels; Quantum stability/energy/overclock/levels/recovery.
- [x] Level reward claims, shop upgrade levels and both temporary boost timestamps.
- [x] Current room index clamp and locked-room fallback.
- [x] Missing/new/null fields, missing/excess rooms/slots, special-room null resets and existing leaf clamps/runtime-only clearing.
- [x] Version fixtures 0, 1, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21 cover every hydration migration branch.
- [x] Prestige/claim lifetime migration lower bound (positive, zero and negative outcomes); current negative clamps.
- [x] Loading/reloading creates no earned/spend stats, duplicate rewards, Prestige or offline settlement; active research stays active until offline step.
- [x] Separate offline settlement integration for historical/current saves; existing offline suite unchanged (663 assertions).
- [x] Main creation/hydration source-equivalence comparison with pre-refactor code; searches classify remaining persistence references.

Manual regression required on Godot/device (pending):

- [ ] Fresh install, fresh game save and reload: first machine/default rooms/slots unchanged.
- [ ] Existing current-version save and an older save if available (use copies).
- [ ] Rooms remain unlocked correctly; slot states and machine tiers/levels remain correct.
- [ ] Bots remain present; rarity/price and bot durability survive restart, including broken bots.
- [ ] Tokens, Data Shards and all stats exact after accounting for expected offline settlement.
- [ ] Active research survives restart; completed research remains completed.
- [ ] Pipeline, Infrastructure and Quantum state survive restart.
- [ ] Prestige count and lifetime level exact; level rewards cannot be reclaimed.
- [ ] Shop boost timers, permanent production upgrade and offline upgrade survive restart.
- [ ] Close/reopen offline earnings remain correct, including boost expiry/research completion.
- [ ] Android pause/resume saves and settles once; initial post-load save still occurs.
- [ ] Corrupted main + good backup recovers (isolated profile).
- [ ] Corrupted main + corrupted backup blocks overwrite and preserves files (isolated profile).
- [ ] Autosave still runs every 10 seconds; exit saves; action-triggered saves persist.
- [ ] Rewarded-ad grant saves successfully.
- [ ] IAP grant/save/acknowledge/consume/finalization sequence still works and deduplicates.
- [ ] Singularity save and offline progression remain completely independent; settings/cosmetics/machine skins/IAP ledger remain independent.

- [x] Final `dotnet build`: 0 errors, 353 existing CS8632 warnings; test project: 0 warnings/errors.
- [x] Final headless persistence run: 3,416 assertions passed; offline run: 663 assertions passed.
- [x] Full diff review and `git diff --check` passed; SaveManager, DTO/data classes, independent stores, formulas and SDK callbacks unchanged.

## Singularity domain extraction

Automated regression harness and isolated-profile commands are documented in REFACTOR_SERVICES.md.

- [x] Default locked state, installed first Sector/Core, eight empty nodes and zero aggregate output.
- [x] Normal progression prerequisite, first unlock, duplicate unlock, insufficient Tokens, exact normal spend/stats and unchanged Matter.
- [x] Later Core unlock, upgrade, legacy level mirror, unlock/upgrade cost progression and output multipliers.
- [x] Compute/Amplifier builds; Cooling at exactly 3 Cores and Quantum at exactly 5; all four node types build.
- [x] Global node growth, sector scaling, node upgrade/investment, sale restrictions and exact one-third refund, legacy investment estimate.
- [x] Failed Matter purchases leave balance/state unchanged; successful transactions spend exactly once.
- [x] Compute/Core output, local Amplifier, Cooling, Quantum, total production and formula order.
- [x] Cross-sector Amplifier in all four cardinal directions; reciprocal neighbor/index lookup and locked-neighbor compatibility.
- [x] Bot purchase/rarity intervals/research, price storage/scaling, repair/sale/refunds, durability consumption/legacy initialization, broken bonus and locked-Core no-wear.
- [x] Research Bot-power multiplier; bots on every output/support type; broken support bots retain base node effects.
- [x] Deterministic spiral positions, grid index rebuilding and current-sector navigation/select semantics.
- [x] Full DTO file roundtrip, current version/timestamp, case-insensitive JSON, pre-v3 Cores, legacy bot durability, broken clamp and corrupt fallback.
- [x] 50% offline Matter, seven-day cap, invalid/backwards timestamps, exact bot-break settlement, pause/resume and duplicate resume.
- [x] Constructor-load settlement/save, pending reward consumed once, immediate restart never double-credits.
- [x] Changed after mutation/save, once per successful action, no events for failures/queries/frame Update/SelectSector; positive resume once.
- [x] Matter actions leave normal Tokens untouched; normal purchases leave Matter untouched.
- [x] All original public facade members retained and callers unchanged; original formulas/action bodies compared.

Manual checks still required (Godot/device, using copied saves):

- [ ] Unlock Singularity and enter it; inspect first Sector/Core and map locked/unlocked state.
- [ ] Build and upgrade every node type; sell a node; exercise every insufficient-Matter path.
- [ ] Upgrade Cores and open additional sectors; verify frontier/Core prerequisites.
- [ ] Sector scrolling/navigation and cross-sector links; Amplifier across a sector boundary.
- [ ] Buy bots; verify rarity/output, durability wear/break, repair and sale.
- [ ] Cold-load offline Matter and Android pause/resume; repeated resume/restart never double-credits.
- [ ] Save/restart with current save and a copied old Singularity save; verify migrations and separate store.
- [ ] Shop/Lab remain accessible from Singularity; map locked/unlocked state still reflects ownership.
- [ ] Normal Tokens unchanged by Matter purchases; Singularity Matter unchanged by normal purchases.
- [ ] Performance with many sectors; compare against pre-refactor gameplay.

- [x] Singularity suite: 197 deterministic assertions passed.
- [x] Existing normal offline suite: 663 assertions passed; existing GameSave suite: 3,416 assertions passed.
- [x] `dotnet build` succeeds with zero errors; public declarations, ownership searches and `git diff --check` verified.
