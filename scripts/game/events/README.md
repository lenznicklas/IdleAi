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

### Test Protocol balancing

Test Protocol economy values live in `TestProtocolEventDefinition.cs`. See
[`docs/event-balance.md`](../../../docs/event-balance.md) for the before/after review and
[`tests/EventBalance/README.md`](../../../tests/EventBalance/README.md) for the standalone
production/Prestige simulator, pacing assertions and JSON tuning overrides.

### Export visibility switch

`EventFeatureFlags.EventsEnabled` is currently **true**. Normal startup creates the
Event Map card, Event view and runtime through the existing bootstrap. Existing
Event progress is loaded normally, including the usual elapsed-time settlement.

For a temporary export without Events, set this one constant to `false`. Disabled
startup skips Event initialization and leaves stored progress and cosmetic ownership
in the normal save path without settling or migrating Event data.

DEBUG builds retain the explicit `--event-tools` opt-in for Event testing; export
release builds compile out this override. Existing Event smoke tests keep their
usual `-- --event-tools` command. The disabled-startup test below is only applicable
when the central flag is set to `false`:

```sh
XDG_DATA_HOME=/tmp/idle-ai-event-disabled-test godot-mono --headless --path . --script tests/Events/disabled_scene_smoke.gd
```

### Event navigation chrome

EventController derives global BottomBar visibility from the Event page's visibility,
not from the selected tab. Event uses the full page height; only ordinary content
padding remains at the bottom. Closing Event or opening Map, Shop, Lab or Singularity
restores global navigation. Deferred Singularity chrome restoration cannot reveal the
bar while Event remains open. Escape/controller back and the Android window back
signal dismiss a Prestige confirmation first, then return Event to Map. The previous
system-back quit policy is restored on exit/teardown. Resume and scene reload retain
the same visibility rule.

```sh
XDG_DATA_HOME=/tmp/idle-ai-event-navigation-test godot-mono --headless --path . --script tests/Events/navigation_scene_smoke.gd
```

This scene test covers every Event tab at three viewport sizes, full-height layout,
native input in the reclaimed bottom region, deferred chrome writes, background/resume,
close/back/global navigation, Singularity round trips and scene recreation.

### Internal Event tabs

EventView owns one cached set of tab sections. A compact single-line header shows
name/time, Event and Prestige currencies, and production; longer values are clipped
with full-value tooltips. Progress description, level and next-unlock details belong
to the scrollable Progress section. The rounded navigation grid remains two rows of
three 56-pixel-high buttons, outside the single expanding content ScrollContainer.
Selected tabs use a persistent toggle highlight, and all button states use Event
styles without resetting the global readability font adjustment.

The existing MobileScrollController supplies touch dragging/inertia. Switching tabs,
opening a modal or leaving Event stops motion. Content actions respect its post-drag
tap guard; the Prestige modal suspends underlying scroll input. There is no nested
scrolling or horizontal scrolling. Scroll offsets are cached in memory per Event ID
and tab, then restored after container layout. A revision check discards stale restores
when tabs change quickly. Tab and scroll state survive modal dismissal and reopening
the same view; no new save fields are introduced. Back still closes a modal first,
then leaves Event for Map, rather than walking through tab history.

```sh
XDG_DATA_HOME=/tmp/idle-ai-event-tabs-test godot-mono --headless --path . --script tests/Events/tabs_scene_smoke.gd -- --event-tools
```

This fixture checks portrait/window and narrow logical sizes, fixed header/content/tab
bounds, active/pressed/focus styling, touch input, independent scroll restoration,
rapid switching, modal input isolation, retained nodes/textures/signals, unchanged
progression during tab changes, long header values, and state retained on reopening.

### Reward track

Milestones retain their durable IDs, target amounts and typed rewards. Optional
Title/Description, Featured and FinalReward fields affect presentation only.
Definitions own all targets, grants and asset paths. Test Protocol has ten milestones;
the original four IDs/grants remain intact. No paid/global skins are awarded by
the sandbox and no placeholder global assets are created.

EventManager uses TotalCurrencyEarned and persisted UnlockedMilestones, independent
of the spendable wallet and Prestige checkpoint. Ended events stop unlocking rewards.
Existing earned permanent rewards remain claimable; Event currency/core rewards and
bundles containing them require the event to be active. This retains the existing
expiration policy. Claimed rewards stay visible in archived result tracks.

ClaimReward and ClaimAllRewards share one transaction. A batch snapshots eligible
milestones before granting, so currency grants can unlock but cannot automatically
claim later rewards. Internal state, global skin entitlements, external Data Shards
and claim markers commit in one main save; failure rolls them back. The action guard
blocks reentry, and saved milestone IDs prevent duplicate grants after restart.
MachineSkin/BotSkin rewards use the existing global ownership synchronization.
Custom rewards provide an ID-based future hook through grantExternalReward and must
return an undo action; unsupported hooks fail safely. No save-schema change is needed.

The existing Rewards section/scroller caches fixed 270px cards and fitted EventImageView
art. One lifetime track bar and per-card bars show progression; future cards stay visible.
Only dynamic text/progress/state is refreshed, and styling changes only when state changes.
Claim All appears for at least two eligible rewards. Texture caching, tab scroll retention,
drag guards, navigation and modal behavior remain shared with the other Event tabs.

```sh
dotnet build tests/Events/Events.csproj --no-restore
XDG_DATA_HOME=/tmp/idle-ai-event-domain-tests godot-mono --headless --path tests/Events
XDG_DATA_HOME=/tmp/idle-ai-event-rewards-test godot-mono --headless --path . --script tests/Events/rewards_scene_smoke.gd
```

RewardTrack domain regressions cover lifetime/Prestige, grant types, individual/batch
claims, one batch save/notification, rollback, duplicate prevention, save/reload,
expiration and future-hook failure. The scene fixture verifies all ten cards, portrait
bounds down to 320 logical pixels, fitted art, custom button states, real Claim All,
Data Shards/bot/core integration and reward persistence across scene restart.

### Event challenges / tasks

EventChallengeDefinition is optional catalog data on EventDefinition. It includes a
stable ID, owning Event ID, category (Daily/Event/Major), typed target, optional
generator/bot ID, existing EventReward, availability conditions, Enabled, Repeatable
and UseCurrentState. TestProtocolChallenges owns its pool (three selected Dailies),
seven Event goals and one highlighted Major goal. No production/economy tuning changes
are required to configure tasks. Future Events opt in by supplying definitions.

Supported metrics: earnings since enrollment; lifetime earnings; cumulative generator
upgrades; specific/any generator level; total unlocked generator levels; generator
unlock; Prestige count; Prestige wallet; bot ownership; Reward Track claims; shop
purchase count; production/s; and foreground Event play minutes. Reach goals preserve
high-water progress by default. UseCurrentState explicitly makes those goals live
(e.g. a level goal can fall back to ACTIVE after Prestige). Completed/claimed state
freezes on expiration. EarnLifetimeCurrency includes older saved lifetime earnings;
EarnCurrency starts at task enrollment. Daily cumulative goals use today's baseline.

EventProgressSaveData.Challenges defaults safely for older saves and is deep-copied
with all Event transactions. It stores progress/baselines, completion/claim counts,
UTC day, selected Daily IDs, retired earned Daily history, cumulative upgrades and
active play seconds. Existing PrestigeCount, ClaimedRewards and ShopPurchases are
already durable cumulative metrics and are reused rather than duplicated. Historical
upgrade counts cannot be reconstructed after old Prestiges: new telemetry starts at
zero without altering any existing generator, wallet, lifetime or reward data.

The manager groups definitions by relevant gameplay signal. Production settlement
updates earning goals; an actual unlock also updates generator/production goals.
Upgrades, Prestige, bot/economy grants, track claims and purchases update their groups
inside their existing transactions. Enrollment/activation does a single full catch-up.
The shared one-second runtime samples foreground Event play with a monotonic clock;
offline/background/normal-game time is excluded. No extra timer, per-frame evaluation,
production settlement in the UI, or per-production-tick saves are introduced.

Daily selection is deterministic from Event ID, integer UTC day and challenge ID,
then saved. Eligibility checks finite unclaimed track rewards/shop limits, known
systems and configured availability conditions. Selection never changes mid-day.
Daily IDs/baselines rotate once on a newer UTC day for the active Event. Offline
settlement splits at the saved day's end and today's boundary, crediting today's
Daily earnings accurately without generating missed days retroactively. Previous
earned/claimed Daily records are archived on rollover; unclaimed prior-day tasks
retire at that reset. The last active day's completed permanent rewards remain
claimable after Event expiration, following the Reward Track policy; Event currency
and Prestige currency rewards require an active Event. Expired Events cannot enroll
new tasks or advance existing ones through later permanent reward grants.

Claims and Claim All call the same GrantReward and atomic Transaction used by the
Reward Track. A batch snapshots eligible goals before grants, commits claims and all
typed rewards in one main save, and rolls everything back on failure. Newly completed
goals wait for another explicit claim. Repeatable goals are supported for cumulative
metrics: the claimed cycle stays visible until new progress starts, then rearms from
its new baseline. Currency rewards do not advance repeatable earning cycles, avoiding
mutually feeding reward loops. Main-save callbacks cannot recursively settle/save
while a manager persistence operation is already running.

Challenges reuse EventView's one content scroller, texture cache and per-tab scroll
state. Fixed-size cards are retained across ticks/tabs, with only affected text/bars
and changed styles refreshed. Daily rotation replaces only retired Daily cards.
A full-width Challenges button sits below the original two rows of three tabs and
shows a non-flashing ready count. Claim All appears for at least two eligible goals.
Existing back/modal handling and BottomBar ownership are unchanged.

Debug commands (Debug build AND --event-tools, through the existing Shop code box):
- event-challenges-complete / event-challenges-ready: mark selected unclaimed goals ready
- event-challenge-progress:challenge_id:10: set a selected goal's debug progress
- event-challenges-claim-all: grant currently eligible challenge rewards
- event-dailies-reset: force deterministic selection/baselines on the current UTC day
- event-day-next: advance the challenge UTC day without adding production/offline income

The simulated day offset is session-only; the selected day/claims are saved. After
restarting a simulated-day session, event-dailies-reset can restore the real UTC day.
These commands never expose normal players to challenge resets or extra reward grants.

```sh
dotnet build tests/Events/Events.csproj --no-restore
XDG_DATA_HOME=/tmp/idle-ai-event-domain-tests godot-mono --headless --path tests/Events
XDG_DATA_HOME=/tmp/idle-ai-event-challenges-test godot-mono --headless --path . --script tests/Events/challenges_scene_smoke.gd -- --event-tools
```

Tests cover all thirteen metrics, Prestige/current-state distinctions, typed rewards,
individual/batch/repeat claims, transaction and partial-grant rollback, UTC/offline
boundaries, retired history, expiration, legacy defaults, persisted selection/claims,
reentrant save callbacks, unchanged six-hour production and Test Protocol definitions.
The scene fixture covers all goal categories, badge updates, debug commands, real
reward grants/restart, active/background play, portrait bounds (320 logical px), art,
button styles, cached nodes/signals, seven-tab navigation and expired claims.
Server rotations, a historical-Daily browser and the optional Map badge are not added.

Challenge implementation file manifest (other existing workspace changes are unrelated):

Added:
- scripts/data/EventChallengeDefinition.cs
- scripts/data/EventChallengeSaveData.cs
- scripts/game/events/EventManager.Challenges.cs
- scripts/game/events/EventManager.Challenges.Debug.cs
- scripts/game/events/TestProtocolChallenges.cs
- scripts/ui/events/EventChallengeCard.cs
- tests/Events/Challenges.cs.txt
- tests/Events/challenges_scene_smoke.gd

Modified:
- scripts/data/EventDefinition.cs
- scripts/data/EventSaveData.cs
- scripts/game/events/EventDefinitionValidator.cs
- scripts/game/events/EventDeveloperTools.cs
- scripts/game/events/EventManager.cs
- scripts/game/events/EventManager.Gameplay.cs
- scripts/game/events/EventManager.Rewards.cs
- scripts/game/events/EventManager.Debug.cs
- scripts/game/events/EventRuntime.cs
- scripts/game/events/TestProtocolEventDefinition.cs
- scripts/game/events/README.md
- scripts/ui/events/EventView.cs
- scripts/ui/events/EventController.cs
- tests/EventBalance/EventBalance.csproj
- tests/Events/Events.csproj
- tests/Events/Program.cs.txt
- tests/Events/tabs_scene_smoke.gd
- tests/Events/navigation_scene_smoke.gd
- tests/Events/halloween_scene_smoke.gd

## Reusable Event Bots

See [Event Bot architecture, tuning, save compatibility, debug commands and validation](../../../docs/event-bots.md). Bots now have separate unlock/ownership/equipped states, alternative configured unlock paths, shared rarity validation, cached cards/detail UI and central reward/shop/challenge integration. Optional bot leveling remains deferred. Normal Room and Singularity bot behavior is unchanged.

## Reusable Event Shop

See [Event Shop definitions, transactions, currencies, save compatibility, commands and validation](../../../docs/event-shop.md). The existing Shop now supports optional Data Shard costs through the existing economy, persistent unlocks, explicit purchase states, global ownership checks, unlimited/finite limits, configured category grouping/sorting and cached cards. Test Protocol supplies eight offers. Temporary boosts are deferred pending correct offline timestamp-boundary settlement.

## Halloween PNG visuals

The [Halloween asset integration report](../../../docs/halloween-png-assets.md) maps all 17 supplied `assets/halloween_event` PNGs, retained generic fallbacks and validation. `HalloweenEventAssets` centralizes exact-case paths; generic widgets consume configured images, including the Prestige resource icon. No Halloween gameplay or balance changes accompany the asset mapping.
