# Idle AI temporary events

The existing `EventManager` owns schedules, production, upgrades, prestige, bots,
claims, shop purchases and expiration. Its shared `EventRuntime` still ticks once per
second. Event pages retain their cards across ticks, upgrades, prestige and reopen.
Countdowns/text assignments are guarded; optional textures are cached. There are no
new per-frame updates, particle systems, real-money purchases or secondary save stores.

## Configuration

`EventDefinition` now supplies `Generators`, `Prestige`, `Bots`, `Milestones`, `Shop`,
`Story` and `Theme`. `EventCatalog.Create()` retains Test Protocol and adds
`HalloweenEventDefinition.Create()`. Both catalog and Halloween factory accept optional
UTC Unix start/end overrides. New event occurrences require unique event/milestone IDs.
Only one event runs: Major > Seasonal > Weekend, then latest start, then ordinal ID.
The map follows the active event. An already open view retains its selected ended event;
completed Major Events can also be reopened from the Archive tab.

Each generator has its own stable ID, name/description/image, enabled state, starting/
maximum level, linear or exponential production curve, multiplier, exponential upgrade
cost, cycle time and unlock condition. Conditions combine earned currency (Lifetime or
CurrentRun), prestige count, and an unlocked generator's level. Unlocks remain earned
until prestige recomputes them. Currency-gated starter machines in these two protocols
use CurrentRun so prestige can reset their normal unlock progression. Lifetime is fully
supported for events which should keep those unlocks. Reward milestones always use
lifetime earnings and never reset.

Production per cycle is `(base + levels * perLevel)` for Linear, or
`base * productionGrowth^levels` for Exponential, with levels measured from StartingLevel.
It is multiplied by generator configuration, saved generator modifier, event-wide
multiplier, earned-core prestige multiplier and the one equipped event bot multiplier.
Upgrade cost is `baseCost * costGrowth^levels`. The displayed total is the sum of active
unlocked production per cycle divided by each generator's cycle time.

`EventProductionService` is the same calculator for UI, online and offline earnings.
It retains each cycle phase. Offline periods split at both schedule boundaries and
currency unlock crossings; binary searches over completed cycles avoid iterating every
cycle in long absences. No income is awarded before enrollment/start or after expiry.
Expired/inactive events report zero active production. Local time never goes backward
relative to the saved cursor; expiration stays latched.

## Event prestige

The generic prestige configuration can be disabled. For enabled events:

- Requirement = `baseRequirement * requirementGrowth^prestigeCount`.
- Run earned = `lifetimeEarned - prestigeEarnedCheckpoint`.
- New cores = `floor(rewardMultiplier * (runEarned / requirement)^rewardExponent)`.
- Permanent multiplier = `1 + totalCoresEverEarned * productionBonusPerResource`.

Test Protocol uses a 1M base requirement, growth 1, square root rewards and +25% per core.
Halloween uses a 1M base requirement, growth 10, square root rewards and +20% per core.
Cores in the wallet may be spent in the shop; permanently earned-core bonuses remain.

Prestige requires a custom confirmation overlay. The service verifies the preview's
prestige count and gain before committing. It checkpoints lifetime earnings, resets
wallet, generator levels/unlocks/phases and saved generator modifiers, and awards cores.
Lifetime history, reward claims, bot ownership/equipment, cosmetics and shop purchase
limits survive. A shared guarded transaction restores the entire event snapshot and
external currencies if saving fails. Nested actions and stale/double confirmations are
rejected. Reopening a scene cannot repeat a reset or grant.

## Halloween Protocol

ID `halloween_2026`, Major, **20 October–3 November 2026 UTC (14 days)**. Fixed dates do
not extend on restart. On earlier dates, Test Protocol stays active; use the debug force
command for immediate testing. Currency is Souls; prestige resource is Cursed Cores.

| Machine | Base Souls / 5s | First upgrade cost | Unlock |
| --- | ---: | ---: | --- |
| Abandoned Terminal | 1 | 10 | Immediate |
| Haunted Workstation | 30 | 300 | 100 earned this run |
| Possessed Server | 800 | 8,000 | 5,000 earned this run |
| Cursed Datacenter | 25,000 | 250,000 | Prestige 1 |
| Soul Processor | 800,000 | 8,000,000 | Prestige 2 |
| Nightmare AI Core | 30,000,000 | 300,000,000 | Prestige 4 |

Production grows x1.18 per level; costs grow x1.6. This is a tunable first balance pass,
not a claim of playtesting the full 14-day progression. Sixteen explicit-claim lifetime
milestones span 100 to 1B Souls. The final reward is the existing four-room Halloween
Machine Skin Pack. Rewards also include Data Shards, Souls, Cursed Cores, two permanent
event badges, the global Halloween Bot Skin Pack and bot ownership:

| Bot | Rarity | Multiplier | Claim milestone |
| --- | --- | ---: | ---: |
| Ghost Bot | Common | x1.10 | 100 Souls |
| Pumpkin Bot | Rare | x1.30 | 50K Souls |
| Skeleton Bot | Epic | x1.75 | 2M Souls |
| Reaper Bot | Legendary | x2.50 | 100M Souls |

One event bot can be equipped; no normal-room bot, research or durability state changes.
Shop items are data-driven (ID, title/image/description, cost currency/cost, reward,
availability condition, enabled flag, purchase limit). Halloween has a 50K Soul cache
for one Cursed Core (limit 3), 5 Data Shards for 100K Souls (limit 5), a network badge,
the existing bot skin pack, and the existing Garage machine skin. No real-money flow.
Story lines select the latest lifetime threshold reached; there is no dialogue system.

On expiry, upgrades, prestige, bot equipment and purchases stop. Durable unlocked
rewards (shards, bots, cosmetics) remain claimable; Soul/core rewards expire so they
cannot extend ended progression. Final Major Event results retain name/year, lifetime
earnings, highest level/production, prestige count, cores earned, rewards and cosmetics.

## Assets and cosmetics

Reused, without new skin definitions or changes to unrelated art:

- `res://assets/machines/room_{1..4}/skin_halloween/machine_{1..4}.png`
  (six selected machines, also the map's Nightmare Core image).
- `res://assets/bots/halloween_skin/{common,rare,epic,legendary,offer}.png`.
- `res://assets/shop/events/halloween_machine_skin.png`.
- `res://assets/shop/data_shard.png`, `res://assets/ui/core_level.png`, existing close icon.
- Global skin IDs: `room_1_halloween`, `room_2_halloween`, `room_3_halloween`,
  `room_4_halloween`, and bot skin ID `halloween`.

Added static code-native SVGs: `res://assets/events/halloween/souls.svg` (clean Soul icon)
and `res://assets/events/halloween/network.svg` (dark network/server backdrop with static
purple fog and orange/green signal fragments). No required asset paths are missing.
The four existing Halloween bot portraits are reused as themed placeholder character
art; bespoke Ghost/Pumpkin/Skeleton/Reaper portraits can replace their configured paths
later. Badges are stored collectible IDs using the Soul icon; a global badge display
system is intentionally deferred.

Cosmetic claims add tagged canonical skin IDs to `Events.CosmeticEntitlements` in the
same atomic main save as the claim/purchase marker. Existing `IsOwned` services recognize
these entitlements immediately; successful commits import them idempotently into the
existing global skin files without buying or auto-equipping. If a cosmetic-file write
fails, the main save remains the durable source and import replays next launch. Prestige
and either event debug reset never remove global entitlements or skin ownership.

## Save compatibility

Normal save format remains 22; nested Events schema is 2 and accepts legacy schema 1.
Per-event generator data migrates lazily to version 2: the first generator inherits the
old single-generator level/phase, others receive starting defaults. The old progression
fields remain first-generator mirrors for compatibility. Each generator stores level,
unlock, phase and modifier. Event progression additionally stores the lifetime prestige
checkpoint, current/ever-earned cores, highest production, bots/equipment, cosmetics,
shop counts and an optional final archive result. Old rewards, balances, timestamps,
unknown/retired IDs and event content state are preserved. Snapshots clone collections.

## Debug commands

Debug C# builds only: launch with `-- --event-tools` and use the existing Shop code box.
Release builds omit the tools. Commands target the current active event unless noted.

- `halloween-force`: force Halloween for seven days, until restart.
- `event-force:event_test_001`: force a specific ID; retains progress/claims.
- `event-add:1000`: add Souls / Event Data according to the active event.
- `event-level:nightmare_core:10`: set a configured generator level.
- `event-unlock-generators`, `event-unlock-bots`: grant all active-event content.
- `event-cores:3`: add Cursed Cores / Protocol Cores and permanent earned-core bonus.
- `event-ready-prestige`: top up earnings for current prestige availability.
- `event-ready-rewards`: top up lifetime earnings; does not automatically claim rewards.
- `event-ending-soon`: simulate a final 23-hour window until restart.
- `event-end`: save expiration and archive the current event.
- `halloween-reset`: reset only Halloween progression; global cosmetics survive.
- `event-reset`: reset only Test Protocol progression; global cosmetics survive.

## Checks and scope

```sh
dotnet build
dotnet build IdleAi.csproj -c ExportRelease
dotnet build tests/Events/Events.csproj
XDG_DATA_HOME=/tmp/idle-ai-event-domain-tests godot-mono --headless --path tests/Events
XDG_DATA_HOME=/tmp/idle-ai-event-scene-test godot-mono --headless --path . --script tests/Events/scene_smoke.gd -- --event-tools
XDG_DATA_HOME=/tmp/idle-ai-halloween-scene-test godot-mono --headless --path . --script tests/Events/halloween_scene_smoke.gd -- --event-tools
```

Both scene fixtures require isolated profile markers and create their own test saves.
Run existing GameSave and Singularity scene regressions as well. Intentionally deferred:
full-duration balance playtesting, dedicated bot character portraits, a global badge
collection view, challenges, elaborate story UI, reward animations and real-money shops.
Normal room/Singularity gameplay and their economies remain unchanged.

## Files in this phase

Added gameplay models in `scripts/data/EventGameplayDefinition.cs`; production, prestige,
validation and cosmetic adapters in `scripts/game/events/EventProductionService.cs`,
`EventPrestigeService.cs`, `EventDefinitionValidator.cs`, `EventCosmeticOwnership.cs`;
manager partials `EventManager.Gameplay.cs`, `.Rewards.cs`, `.Debug.cs`; Halloween data
in `HalloweenEventDefinition.cs`; reusable UI components `EventGameplayCards.cs` and
`EventPrestigePanel.cs`; the two Halloween SVG assets and their Godot imports; event
gameplay tests in `tests/Events/Gameplay.cs.txt` and `halloween_scene_smoke.gd`.

Extended existing EventDefinition/EventSaveData, EventManager/EventCatalog/
EventDeveloperTools, Game.Events integration, EventView/EventController/EventUiStyle/
EventMapEntry, BotSkinService/MachineSkinService ownership checks, the existing Shop code
input length, and Event test runner/project/scene fixtures. The normal atomic save,
Event runtime timer, map registration and Singularity gameplay were retained.
