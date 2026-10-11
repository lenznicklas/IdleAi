# Idle AI service refactor

## Scope and baseline

Analysis performed on branch `refactor/services`, with a clean initial working tree. This is an incremental refactor of the working Godot 4.x C# game. Inspection covered the first-party C# inventory in `Game.cs`, `scripts/game`, `scripts/data`, `scripts/persistence`, and `scripts/ui`, including partial classes, constructor dependencies, state mutations, persistence entry points, and scene/plugin configuration. Third-party AdMob, billing and Play Games implementations are integration boundaries, not refactor targets. Runtime behavior still requires Godot/device verification.

## Current architecture

`Game` is the scene root (`game.tscn`), composition root, frame-loop coordinator, save mapper/migrator, and normal-game offline settlement coordinator. There is no first-party service locator. The project autoloads the Play Games plugin.

| Component | Current responsibility and dependencies |
| --- | --- |
| GameState | Shared mutable normal-game state: rooms/slots, Tokens, run earnings, lifetime level, stats, prestige, lab, shop. Catalog definitions are separate from runtime room states. |
| EconomyService | Price quotes and production estimates; reads GameState, catalogs, research/prestige/shop and special-room data. Does not own all currency mutations. |
| ProgressionService | Room/slot unlocks, machine upgrades, milestone shards and total-level calculations; depends on state and economy. |
| ProductionService | Machine cycles, manual advancement animation bookkeeping, bot durability and research-point awards; depends on state, economy and pipeline. Returns earned Tokens to Game. |
| PipelineService | Pipeline simulation/upgrades and machine input; depends on state and economy. |
| InfrastructureService / QuantumService | Specialized room simulation, quotes and upgrades; depend on state. |
| BotService | Bot purchase/repair/sale, rarity RNG and cycle adjustments; depends on state and economy. |
| LabService / LabData / ResearchCatalog | Research requirements, purchases, timers and completion; LabData computes bonuses from completed research. |
| PrestigeService | Eligibility, normal-game reset and lifetime-level preservation; state plus static ProgressionService calculations. |
| ShopService | Shard spending/rewards, timed boosts, permanent upgrades and instant production; depends on state, economy and production. |
| LevelRewardService | Reward-road eligibility/claims; state and progression. Initially created inside GameUiController. |
| BotSkinService / MachineSkinService | Ownership, equipment, purchases, independent JSON saves and catalog texture application. Initially created inside GameUiController. Bot skins also grant secret-code debug Tokens. |
| GameSettingsService | Independent JSON preferences, global vibration gate, music player and Master bus mute. Initially created inside GameUiController. |
| GameUiController | Creates/wires pages and overlays, refreshes presentation, handles navigation and owns four non-UI services at baseline. |
| Game.PlayGamesAchievements partial | Deferred bootstrapping of Firebase, Play Games, Singularity and UI helpers; one-second achievement/leaderboard sync; installs sequential-room selection guard. |
| FirebaseAliasService + Leaderboards partial | Play Games server-code exchange, Firebase authentication/token refresh, usernames/Firestore alias operations and custom leaderboards. Godot Node with HTTP/timer lifecycle. |
| GooglePlayGamesAchievementService | Native authentication, achievement eligibility/pending confirmations and official leaderboard synchronization. |
| GooglePlayBillingService | Native billing lifecycle, purchase-token deduplication and persistent consumable ledger. Created by IapShopController. |
| ShopController / IapShopController | Rewarded-ad lifecycle and purchase grant/save/finalization sequencing respectively, mixed into presentation. |
| SingularityService | Separate Matter economy, grid/sector/node/core rules, bots, simulation, pause/resume/offline settlement and independent JSON persistence. |
| SingularityController partials | Dynamic UI, navigation, pan physics, bot presentation; owns runtime update/lifecycle bridges calling SingularityService. |

Startup order is significant: service construction (including cosmetic loads and settings/music), default room-state creation, normal UI construction/reparenting, lab/mobile UI, save timer, main load/migration/offline/research, production preparation, UI refresh, initial save. Deferred features initialize afterward. Shop prewarming waits two process frames; rewarded ads initialize on explicit video request.

## Problems and boundaries

- Service ownership is split between Game, UI controllers and deferred feature bootstraps. Cosmetic/settings constructors have side effects, making initialization order part of the contract.
- Game combines orchestration, JSON DTO mapping, version migrations and offline research/boost settlement. Keep these intact until their behavior has regression coverage.
- Currency writes occur in Game, progression, bots, lab, shop, rewards, skins and special-room upgrade services. Stats bookkeeping differs intentionally: secret-code Tokens do not count as normal earned/run Tokens. A generic grant operation must not erase those distinctions.
- Settings mixes persistence/preferences with Godot audio. `IdleAi.Input` shadows Godot.Input to gate vibration via static `GameSettingsService.IsVibrationEnabled`. Retain that API during incremental cleanup.
- Skin services duplicate temporary-file persistence and modify shared catalog textures. Combining them wholesale would risk distinct room/equipment semantics and separate save ownership.
- Large services include EconomyService, ProgressionService, SingularityService and external integrations. Size alone does not justify splitting tightly related formulas or rules.
- UI contains integration/business policy: ShopController chooses the ad reward and guards duplicate grants; IapShopController coordinates grant, main save and billing completion; secret-code policy lives in SecretKorpoSkinController; seasonal pricing in HalloweenOfferShopController; SingularityMapExtension checks Room 4 eligibility; MachineDetailsOverlay selects level versus tier upgrade operations. Extract these individually after service ownership is stable.
- Room unlock gating is enforced both in presentation/RoomUnlockRules and through a deferred Game event-wrapper replacement. Audit all entry points before consolidating it.
- Pure constructor dependencies have no identified service-to-service cycle: state -> economy -> pipeline/progression/bots -> production -> shop; level rewards -> progression. There is lifecycle coupling back through root Nodes, shared state, events and static catalogs. Avoid introducing a container or event bus to solve that coupling.
- LabData, ShopData and PrestigeData already contain domain calculations. Keep their public APIs; do not duplicate those formulas in newly named services.

Do not refactor scene hierarchy/reparenting, node paths, animation/pan/touch helpers, catalogs/GameConfig, bot RNG, production arithmetic, native plugin code, deferred startup, sync intervals, or purchase/ad callbacks in phase 1. Preserve separate Singularity update and offline paths.

## Target architecture

Game remains the explicit composition/lifecycle root. UI receives existing service instances and emits the same events. Add abstractions only for concrete boundaries, without a DI framework or global service registry.

```text
Game (composition, lifecycle, frame ordering, UI event coordination)
  GameState + existing catalogs/data
  EconomyService (quotes; later explicit currency operations)
  ProgressionService -> EconomyService
  LevelRewardService -> ProgressionService
  PipelineService -> EconomyService
  ProductionService -> EconomyService + PipelineService
  BotService -> EconomyService
  InfrastructureService / QuantumService
  LabService (existing research service; retain name)
  PrestigeService
  ShopService -> EconomyService + ProductionService
  BotSkinService / MachineSkinService (retain separate equipment models)
  GameSettingsService -> future AudioService
  future OfflineEarningsService -> state + EconomyService + LabService
  future SaveService (mapping/migration) -> existing SaveManager (Node/file adapter)
  deferred integration adapters (existing names/APIs)
  deferred SingularityService (separate state/save/simulation)
  UI controllers (explicit dependencies)
```

Do not create duplicate ResearchService, AchievementService or SkinService solely to match a generic list. Later isolate Firebase authentication and leaderboard operations behind its existing facade; retain the native achievement/billing services. A small rewarded-ad adapter is justified later because UI currently owns the SDK lifecycle.

## Phased migration

Each phase is a separate reviewable patch, must compile, and uses REFACTOR_TEST_CHECKLIST.md. Do not proceed automatically beyond phase 1.

1. **Composition ownership:** create cosmetic/settings/reward services in Game and inject them into GameUiController. Preserve old public constructor for compatibility, service creation order, scene children and all service bodies. No container needed.
2. **Settings/audio:** move player/bus operations into AudioService behind the existing settings facade. Preserve settings DTO/path, default booleans, music asset, -8 dB volume, Finished looping, mute/stop/restart behavior and vibration gate.
3. **Currency:** incrementally introduce explicit earned/offline/grant/spend operations through economy; migrate one caller group at a time. Preserve amounts, rounding, stats categories and run-earnings distinctions.
4. **Progression/rewards:** consolidate room eligibility entry points without changing unlock/claim rules or total-level formulas; keep LevelRewardService separate.
5. **Research:** separate time access from LabService only where needed for reproducible tests; keep catalog, bonuses and completion order unchanged.
6. **Prestige:** clarify reset ownership using existing reset methods; preserve all retained/reset fields and lifetime-level offset.
7. **Offline:** extract normal-game settlement verbatim, covering boost expiration, research completion split, cold load and in-process mobile resume. Keep Singularity settlement separate initially.
8. **Save:** extract DTO mapping and existing migrations behind a SaveService only if still useful. Leave SaveManager's file recovery/overwrite lock intact. No schema change planned.
9. **Integrations:** small separate steps for rewarded ads, billing grant coordination, Firebase auth/aliases/leaderboards and achievement polling. Device verification required for each. Singularity decomposition is optional later work, not bundled with integration changes.

## Save compatibility inventory (before implementation)

Main save uses reflection-based System.Text.Json serialization/deserialization with PascalCase property names and indented output. `SaveJsonContext` exists but SaveManager does not use it; switching serializers is outside phase 1. Main version remains **21**.

| Store | Owner / contents / write behavior |
| --- | --- |
| user://idle_ai_save.json | Game maps state; SaveManager reads/writes. Ten-second autosave plus action saves, initial save, exit and mobile pause/resume. Temp file flushed to disk before move; previous readable main copied to idle_ai_save.bak.json. |
| user://idle_ai_save.bak.json | Backup fallback restores main when possible. If both existing files fail to load, LastLoadFailed blocks overwrite until DeleteSave. Temporary file idle_ai_save.tmp.json is not a load fallback. |
| user://idle_ai_settings.json | GameSettingsService version 1: SaveVersion, VibrationEnabled=true, SoundEnabled=true. Temp idle_ai_settings.tmp.json then overwrite move. Survives prestige. |
| user://idle_ai_cosmetics.json | BotSkinService version 1: SaveVersion, OwnedSkinIds, EquippedBotSkinId. Temp idle_ai_cosmetics.tmp.json. Case-insensitive runtime ownership; only known catalog IDs restored, default skin fallback. |
| user://idle_ai_machine_skins.json | MachineSkinService version 1: SaveVersion, OwnedSkinIds, EquippedSkinByRoom (string room-index keys). Temp idle_ai_machine_skins.tmp.json. Known/owned/complete matching-room equipment restored. |
| user://idle_ai_iap_ledger.json | GooglePlayBillingService: ProcessedPurchaseTokens list (sorted on write), no version field. Temp idle_ai_iap_ledger.tmp.json flushed before move. Preserve purchase deduplication and grant/save/finalization ordering. |
| user://singularity_save.json | SingularityService version 4: separate JSON, case-insensitive read, direct Godot.FileAccess write. No main-save backup/lock. |

Firebase auth credentials/tokens and alias caches are runtime state in FirebaseAliasService; usernames/aliases and custom scores are remote Firestore data, not main-save fields. Play Games auth/achievements/official scores are native/remote state. Do not copy those into gameplay DTOs.

### Complete main persisted DTO properties

- `SaveGameData`: `SaveVersion` (int), `Tokens` (double), `RunEarnedTokens` (double), `LifetimeLevelBase` (int), `LastSaveUnix` (long), `IncomePerSecond` (double), `CurrentRoomIndex` (int), `AiCores` (long), `PrestigeCount` (int), `LabUnlocked` (bool), `ResearchPoints` (double), `CompletedResearch` (List<string>), `ActiveResearchId` (string?), `ActiveResearchEndUnix` (long), `DataShards` (double), `HighestClaimedLevelReward` (int), `ShopProductionBoostEndUnix` (long), `ShopBotLuckBoostEndUnix` (long), `ShopProductionUpgradeLevel` (int), `ShopOfflineUpgradeLevel` (int), `Rooms` (List<RoomSaveData>), `Stats` (StatsSaveData).
- `RoomSaveData`: `Unlocked` (bool), `DataShardUnlockRewardClaimed` (bool), `Pipeline` (PipelineSaveData?), `Infrastructure` (InfrastructureSaveData?), `Quantum` (QuantumSaveData?), `Slots` (List<SlotSaveData>).
- `PipelineSaveData`: `ComputeLevel` (int), `DataLevel` (int), `ModelLevel` (int), `OutputLevel` (int), `RawInputBuffer` (double), `ComputeBuffer` (double), `DataBuffer` (double), `ModelBuffer` (double), `ComputeCycleRemaining` (double), `DataCycleRemaining` (double), `ModelCycleRemaining` (double), `OutputCycleRemaining` (double).
- `InfrastructureSaveData`: `PowerLevel` (int), `CoolingLevel` (int), `StorageLevel` (int).
- `QuantumSaveData`: `Stability` (double), `Energy` (double), `OverclockIndex` (int), `StabilizerLevel` (int), `EnergyCoreLevel` (int), `AmplifierLevel` (int), `RecoveryMode` (bool).
- `SlotSaveData`: `Unlocked` (bool), `MachineTier` (int), `MachineLevel` (int), `ClaimedDataShardMilestones` (List<int>), `BotRarity` (BotRarity?), `BotPurchasePrice` (double), `BotDurabilityInitialized` (bool), `BotDurabilitySecondsRemaining` (double), `IsRunning` (bool), `CycleRemaining` (double).
- `StatsSaveData`: `TotalEarned` (double), `OfflineEarned` (double), `TotalSpent` (double), `SlotUnlockSpent` (double), `MachineSpending` (Dictionary<string, double>).

Lists/dictionaries preserve the existing DTO defaults. Stage/system/core levels default to 1, Quantum stability/energy use GameConfig defaults. BotRarity is the existing nullable numeric enum. RuntimeCycleDuration and manual advancement animations are not persisted. SlotData restores cycle state and initializes old bot durability through BotDurabilityInitialized, retaining save version 21. PipelineData, InfrastructureData, QuantumData and StatsData own their existing ToSaveData/LoadFromSaveData mappings and clamps.

### Singularity persisted properties

- SingularitySaveData: SaveVersion, Unlocked, Matter, CoreLevel (legacy), CurrentSectorIndex, LastSaveUnix, Sectors.
- SingularitySectorData: Nodes, CoreUnlocked, CoreLevel.
- SingularityNodeData: Type (numeric enum), Level, InvestedMatter, BotRarity, BotPurchasePriceMatter, BotDurabilityInitialized, BotDurabilitySecondsRemaining. Default serializer also emits public computed getters HasBot, BotBroken and HasWorkingBot; they have no setters and are derived on load.
- Pre-v3 saves map legacy CoreLevel to sector 0, initialize other core states, and ensure sectors/nodes exist. Bot durability is initialized when missing or clamped when present. Loaded data is set to version 4. Separate offline efficiency/cap and mobile lifecycle stay unchanged.

### Main load migrations and dependent classes

Game.LoadGame preserves existing branches: before v13 initial shards; before v14 previously unlocked rooms marked reward-claimed; v16 pipeline, v18 infrastructure and v19 Quantum restoration gates (older versions reset these systems); before v20 reward-road claim default; v21 lifetime level and pre-v21 prestige/reward lower-bound migration. Pre-v19 offline base income is recomputed; newer versions use saved base income. GetMigratedOfflineIncomePerSecond retains its legacy pre-v15 boost branch, even though current caller gating makes that branch unreachable for pre-v19 saves. Do not simplify it in this phase.

Direct schema dependencies: Game, SaveManager, SaveJsonContext, SlotData, PipelineData, InfrastructureData, QuantumData, StatsData and the independent store owners above. Indirect consumers include every service/UI reading GameState, achievement/leaderboard level calculations, catalog texture users, and Singularity research bonuses.

No save fields, defaults, versions, file paths, serialization settings or migrations change in phase 1. No migration is needed. Future persistence changes must retain unknown/legacy values where relevant rather than silently discard them, and use copies of historical saves for fixtures.

## Existing risks recorded separately (no behavior fixes)

- Main read success means non-null deserialization rather than complete structural validation. Malformed nested values may fail later in mapping. Keep recovery semantics unchanged here.
- Independent cosmetic/settings stores do not share the main corruption lock; cosmetic loading filters unknown catalog IDs. Singularity catches load failures by replacing runtime data with defaults and uses direct writes. Review data-loss protection separately before any save cleanup.
- Purchase and ad workflows span UI and persistence. Moving callbacks or changing event order could duplicate or lose grants; postpone them.

## Completed phases / remaining work

Phase 1 implementation: Game constructs BotSkinService, MachineSkinService, LevelRewardService and GameSettingsService immediately before creating GameUiController, in the existing order. An internal injection constructor accepts those instances. The existing public constructor remains a compatibility entry point with its original signature and construction behavior. UI presentation, subscriptions and service algorithms are unchanged. This intentionally retains the legacy constructor's construction responsibilities for compatibility; the production Game path uses explicit injection.

Phases 2–9 remain proposed work. Godot runtime, existing-save and Android checks remain manual until checked. Build and diff results are recorded after verification below.

## Phase 1 verification results

- Baseline `dotnet build`: succeeded, 0 errors, 351 existing CS8632 nullable-annotation warnings.
- Post-change `dotnet build`: succeeded, 0 errors, same 351 CS8632 warnings. No warning-policy/project changes made.
- `git diff --check`: passed. Constructor-reference search found the production Game call and retained public/new internal constructors; no moved/renamed types.
- Compared against HEAD: Game is byte-for-byte unchanged outside CreateGameSystems; GameUiController is byte-for-byte unchanged outside its constructors. All existing service implementations, persistence/data definitions, scenes, assets, plugin configuration and gameplay constants are unchanged.
- Manual Godot, historical-save, mobile integration and Android export checks have not been run; use REFACTOR_TEST_CHECKLIST.md. No automated gameplay tests added for this construction-only phase.
- Changes remain uncommitted on `refactor/services`: two code files plus these two documentation files form a single separable phase-1 patch. No main-branch changes or unrelated user-file edits.
