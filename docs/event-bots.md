# Reusable Event Bots

Event Bots extend the existing EventManager, Event reward transaction and production service. They do not use Room/Singularity bot production, durability, repair, skin selection or save slots.

## Definitions and unlocks

`EventBotDefinition` remains in `scripts/data/EventGameplayDefinition.cs`. It supplies ID, event association, name, description, icon, shared BotRarity-compatible rarity, enabled state, production multiplier and optional generator-upgrade discount. Empty EventId inherits the containing Event for existing definitions; a mismatched explicit ID is rejected.

`AvailableFromStart` grants early availability. `UnlockPaths` is an OR list; requirements populated in one `EventBotUnlockCondition` are ANDed. Supported requirements are lifetime Event earnings, Prestige count, total Prestige currency earned, a claimed Reward Track milestone, completed challenge (including recorded daily history), and a purchased Event Shop item. Total Prestige currency earned prevents spending from re-locking a permanent unlock. Direct reward grants bypass requirements through the existing reward path.

By default a reached path both unlocks and grants ownership. `GrantOnUnlock = false` leaves a distinct unlocked-but-unowned state, with an explicit free Acquire action using the same reward grant transaction. Ownership never silently equips a bot. Legacy `UnlockMilestoneId` remains a presentation/source reference: reaching that milestone alone does not grant its unclaimed reward.

Locked enabled bots remain visible. Player-facing requirements resolve challenge titles, shop names and milestone amounts rather than printing internal IDs. Disabled definitions are excluded from new grants/equipping/UI while their saved ownership is retained.

## Production and persistence

The single saved `ActiveBotId` selects one owned, enabled bot. Re-equipping the same bot is a no-op, rather than toggling it off. Switching replaces the prior selection atomically. Both live and offline income use EventProductionService:

`generator output × generator modifiers × configured Event multiplier × Prestige multiplier × equipped Event Bot multiplier`

The optional discount affects the manager's centralized generator upgrade quote and spend. Neither effect applies to other Events or normal gameplay. Expired Events produce zero and reject bot acquire/equip/shop actions; ownership and the last selection remain visible as history.

`UnlockedBots` is an additive save field with an empty default. Existing `OwnedBots` and `ActiveBotId` fields retain their names and semantics. Normalization mirrors old ownership into unlocks, retains retired/unknown ownership IDs, and clears an invalid/disabled/unowned equipped reference without rejecting the whole save. No save version migration, currency reset or ownership reset is needed. Archive results add unlocked bot count and Legendary ownership.

Equip/acquire/reward/shop actions use the existing transactional save/rollback and reentrancy guard. Rewards remain explicit and claim IDs prevent duplicate grants. Repeated bot grants are idempotent; shops reject an already-owned bot, including inside a bundle, before charging or incrementing purchase counts. Prestige keeps unlocks, ownership and selection.

## Integrations and performance

Reward Track, challenge reward and shop grants all use EventManager.GrantReward. Unlock/own/equip challenge metrics are distinct; OwnBot and EquipBot enum values are appended to preserve existing enum numbers. Bot progression hooks run on relevant economy/actions and enrollment. A bounded dependency closure allows an unlock-bot challenge to complete and unlock another bot without per-frame polling. Expired permanent claims cannot advance new objectives.

Cards, textures and one detail modal are reused. Card slots have fixed bounds, numeric changes do not resize cards, and the shared EventImageView fits assets without overflow. Details reuse the room bot image preview (SkinPreviewOverlay), including its near-fullscreen frame, large graphic, opening animation and tap-anywhere dismissal. EventBotDetailPanel only binds Event name, rarity, description, bonus, requirements and status. Equip/acquire stays on the cards. The preview suspends underlying list gestures, closes before Event on system back, and preserves the selected tab/scroll. Card widgets skip unchanged state updates; there are no new timers or per-frame listeners and no saves per production tick.

## Test Protocol configuration

| Bot (stable ID) | Rarity | Production | Default acquisition |
| --- | --- | --- | --- |
| Helper Drone (`protocol_helper`) | Common | +10% | Available from start; legacy track reward still grants the same ID |
| Optimizer Bot (`protocol_optimizer`) | Rare | +25% | 25K lifetime Event Data, or one 5K Event Data shop license |
| Neural Assistant (`protocol_neural`) | Epic | +50% | First Event Prestige |
| Prototype Intelligence (`protocol_prototype`) | Legendary | +100% | Complete the existing Complete Test Protocol Major Challenge |

Existing reward IDs, amounts and claimed states are preserved. Existing art is reused; no Halloween art/content was added or changed.

## Development commands

Debug builds only, with `--event-tools`, through the existing Shop code box:

- `event-unlock-bots`: grant all enabled bots via the central reward path.
- `event-bot-grant:bot_id`: grant one configured bot.
- `event-bot-equip:bot_id`: equip one owned bot.
- `event-bots-reset`: clear ownership and equipment while retaining permanent unlock eligibility/history. Unlocked bots can be acquired again for testing; existing reward claims/purchase counts are not reset.

Optional bot leveling, level costs/max levels and level reset rules are intentionally deferred. They are not present as nonfunctional configuration/UI. All four Test Protocol bots are fixed-strength; future leveling can extend this ownership/production architecture without a second reward system.

## Validation

Build Debug, then build/run the existing Events and GameSave projects. The Event suite includes Bots.cs.txt alongside all prior progression, Reward Track, challenge and Halloween regression checks.

```sh
dotnet build IdleAi.csproj -c Debug
dotnet build IdleAi.csproj -c ExportRelease
dotnet build tests/Events/Events.csproj
XDG_DATA_HOME=/tmp/idle-ai-event-bots-domain godot-mono --headless --path tests/Events
dotnet build tests/GameSave/GameSave.csproj
XDG_DATA_HOME=/tmp/idle-ai-event-bots-save godot-mono --headless --path tests/GameSave
XDG_DATA_HOME=/tmp/idle-ai-event-bots-test godot-mono --headless --path . --script res://tests/Events/bots_scene_smoke.gd -- --event-tools
```

The new scene fixture checks 1080×2400, 720×1600 and 540×960 portrait layouts, all four bot cards, image bounds, stable modal geometry, system back, immediate equip/save, shop duplicate prevention, Prestige, restart, expired read-only state, retained card identities and single signal subscriptions. Additional existing navigation/tab/reward/challenge scene fixtures cover surrounding Event UI. Headless layout checks do not substitute for a physical Android visual/touch review.

## Files in this change

Added:

- `scripts/game/events/EventBotRules.cs`
- `scripts/game/events/EventManager.Bots.cs`
- `scripts/game/events/EventManager.Bots.Debug.cs`
- `scripts/ui/events/EventBotCard.cs`
- `scripts/ui/events/EventBotDetailPanel.cs`
- `tests/Events/Bots.cs.txt`
- `tests/Events/bots_scene_smoke.gd`
- `docs/event-bots.md`

Modified:

- `scripts/data/EventGameplayDefinition.cs`
- `scripts/data/EventSaveData.cs`
- `scripts/data/EventChallengeDefinition.cs`
- `scripts/game/events/EventManager.cs`
- `scripts/game/events/EventManager.Gameplay.cs`
- `scripts/game/events/EventManager.Rewards.cs`
- `scripts/game/events/EventManager.Challenges.cs`
- `scripts/game/events/EventProductionService.cs`
- `scripts/game/events/EventDefinitionValidator.cs`
- `scripts/game/events/EventDeveloperTools.cs`
- `scripts/game/events/TestProtocolEventDefinition.cs`
- `scripts/game/events/README.md`
- `scripts/ui/events/EventView.cs`
- `scripts/ui/events/EventController.cs`
- `scripts/ui/events/EventImageView.cs`
- `tests/Events/Events.csproj`
- `tests/Events/Program.cs.txt`
- `tests/Events/tabs_scene_smoke.gd`
- `tests/Events/challenges_scene_smoke.gd`

Validation results for this change:

- Debug and ExportRelease: zero errors (existing 255 repository warnings remain).
- Events: 325 assertions passed.
- GameSave: 3,645 deterministic assertions passed.
- Event Bots scene: 67 checks, zero failures.
- Existing navigation scene: 122 checks, zero failures.
- Existing tabs scene: 2,203 checks, zero failures.
- Existing Reward Track scene: 840 checks, zero failures.
- Existing Challenges scene: 998 checks, zero failures.

The existing tab/challenge scene checks were adjusted to count visible scrollers and reject nested tab scrolling: the shared bot preview has no nested scroller, and the underlying mobile list gesture handler is disabled while it is open.

## Missing bot images: initial layout fix

The Halloween paths and resource loads were correct, and both cards/details read `EventBotDefinition.IconPath` through the shared texture cache. However, `EventImageView` fitted its texture in its constructor while its size was still zero. Bot cards assign the fixed 88×112 slot size before entering the scene tree, when Godot does not deliver the resize notification. The inner TextureRect consequently kept a 0×0 minimum/drawing size. The four Halloween card checks reproduced this even with valid, non-null textures; detail images already sized correctly in the reproduction.

`EventImageView._Ready()` now refits the texture after attachment. Existing resize/texture-change fitting, native-size caps, aspect-preserving centered drawing, cached resources and fixed card dimensions remain in use. No per-frame work, UI-specific paths, asset changes or gameplay/save changes were added.

Regression checks now reject zero-size visible images, verify drawable/centered/bounded images in all four Halloween cards/details, and verify every Test Protocol card/detail while switching events repeatedly. Exact-case `ghost_bot.png`, `pumpkin_bot.png`, `skeleton_bot.png` and `reaper_bot.png` paths/imports were checked. The Android preset already exports all resources with only `promo/*` excluded; no export configuration change is necessary.

Changed files: `scripts/ui/events/EventImageView.cs`, `tests/Events/halloween_assets_scene_smoke.gd`, `tests/Events/image_layout_checks.gd` and this document. Debug and ExportRelease builds pass with zero errors (existing warnings remain). The expanded Halloween/Test Protocol asset scene passes 7,047 checks, Event Bots 67 and Event Shop 910. Tests run headless on Linux with isolated saves, not on a physical Android device.

The additional tab regression passes all 2,203 checks, but Godot reports two ObjectDB instances/one resource still held during that fixture's shutdown. The dedicated asset, Bot and Shop scene fixtures exit successfully without that warning.
