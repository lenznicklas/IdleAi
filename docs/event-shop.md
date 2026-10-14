# Reusable Event Shop

The Event Shop extends the existing EventManager transaction and reward grant implementation. It does not add another currency, global Shop, IAP, ad placement or event-specific purchase controller.

## Definition and tuning

`EventShopItem` in `scripts/data/EventGameplayDefinition.cs` remains the reusable immutable item definition. It now includes event association, display name/description/icon, configurable category, cost/currency/icon, reward, purchase limit, enabled/featured flags, sort order and permanent-unlock policy. Runtime purchased count and state come from `EventShopSnapshot`; mutable player data is not stored in catalog definitions.

An empty EventId inherits the containing Event for old definitions. Explicit cross-event IDs, duplicate IDs, unknown currency enum values, nonfinite/nonpositive prices, negative limits, fractional/overflowing Prestige prices and invalid unlock references are rejected during catalog validation. Existing currency enum ordinals are unchanged; DataShards is appended. Normal Tokens are not a supported cost currency.

`PurchaseLimit = 0` means unlimited; 1 means once; larger positive values mean a maximum count. Unlimited purchases still stop at the safe integer counter ceiling.

`Availability` retains existing lifetime/current-run earnings, Prestige count and generator level/unlock requirements. The additional `Unlock` condition supports Prestige currency balance, unlocked Event Bot, completed challenge (including saved daily history), and earned Reward Track milestone. Populated requirements are ANDed. IDs are resolved to names/amounts for player-facing text.

Reached requirements latch into `UnlockedShopItems` by default and survive spending and Prestige. `PermanentUnlock = false` uses current-state requirements unless an explicit permanent unlock already exists. Existing successful purchase history reconstructs permanent eligibility when loading a save without the new field. Expired events do not latch new unlocks from post-event rewards.

## Central purchase flow

`EventManager.Shop.cs` owns sorted catalog access, balances, availability, ownership checks, state snapshots and purchasing:

1. Settle legitimate elapsed Event progress and verify active/writable Event.
2. Check enabled/known item and reward availability.
3. Revalidate the UI's expected purchase count, ownership, limit, unlock requirements and balance.
4. Deduct the configured currency and invoke the existing GrantReward path.
5. Increment the count, notify affected challenge metrics and latch new permanent unlocks.
6. Commit the existing atomic main save once; otherwise compensate external currency changes and restore the Event snapshot and entitlements.

The existing transaction reentrancy guard rejects purchases during a save/grant. UI cards send an expected count, so a stale/replayed quote fails after a successful purchase, including after restart. A local 300 ms monotonic input debounce prevents normal double taps without timers/tasks surviving a scene change. Legitimate subsequent purchases use the updated count. Finite limits and permanent ownership are persisted before UI success is shown.

Event currency and Prestige currency use existing Event wallets. Data Shard costs require explicit balance/spend adapters, wired in Game.Events to EconomyService. Spending returns an undo action, just like external Data Shard reward grants. Missing adapters make offers unavailable. Data Shard rewards reuse the existing external grant callback, with a finite-balance guard in the game adapter.

Bot and cosmetic duplicates are rejected before charging, including inside reward bundles. Global MachineSkinService/BotSkinService ownership is checked as well as saved Event entitlements; a skin already bought outside the Event cannot be bought again here. New skins use existing entitlement/save/synchronization paths and remain globally owned after Prestige/expiration. Permanent reward grants from other sources remain idempotent.

Reward Track requirements only read earned milestone state; Shop has no dependency on Reward Track UI. Shop, Bot and reward signals update existing challenge metrics. Optional `ShopItemId` on a PurchaseShopItems challenge selects one item's persistent count; empty means the existing total-count metric. Daily eligibility understands unlimited offers.

## Presentation

Cached fixed-height cards show bounded item and currency images, name, category, description, reward, formatted price, requirements, purchased count/limit, status and Buy button. Featured offers have a small accent change and the same card size. Sorting is featured first, category ordinal, configured sort order, then stable ID; ownership and purchases never reorder the list.

Categories use section headings within the existing vertical Event scroller. No extra category navigation or nested Shop scrolling is introduced. A single-category shop needs no category navigation/header overhead unless it explicitly features offers.

Cards use shared EventImageView/EventTextureCache and Idle AI button styling. Values are clipped within fixed text slots, with complete requirement/price tooltips. Refresh changes state text/enabled flags only; it does not reload images, recreate cards or connect signals. The existing Event runtime handles updates; no new per-frame processing or periodic timer is added.

After expiration all buttons show EVENT ENDED, history stays visible, and an owned bot/cosmetic keeps an OWNED badge. The tab explains that purchases are no longer available. Normal Event back/navigation and global BottomBar ownership remain unchanged.

## Test Protocol offers

| Item ID | Cost | Reward | Limit / requirement |
| --- | --- | --- | --- |
| optimizer_license | 5K Event Data | Optimizer Bot | Once; existing ID/price preserved |
| helper_license | 200 Event Data | Helper Drone | Once; normally already owned |
| small_data | 1 Protocol Core | 10K Event Data | 3 |
| large_data | 2 Protocol Cores | 40K Event Data | 2; Prestige 1 |
| protocol_core_offer | 50K Event Data | 1 Protocol Core | 2; 25K lifetime data |
| shard_pack | 10K Event Data | 3 Data Shards | 3; earned 10K track milestone |
| training_data | 1 Data Shard | 2K Event Data | Unlimited |
| nature_bot_skin | 50 Data Shards | Existing global Nature Overgrown Bot Skin | Once; complete the first-Prestige challenge |

All tuning lives in TestProtocolShop, not UI code. Resource conversions lose value and finite core/cache offers cannot create an unlimited currency loop. The cosmetic reuses an existing non-Halloween asset/catalog ID at its existing global Shard price. No new/fake cosmetic assets or Halloween content were added.

## Save compatibility

Existing `ShopPurchases` remains the source of purchased counts. The only new Shop save field is `UnlockedShopItems`, initialized empty when missing and deeply copied for rollback. Unknown/deleted item IDs and their counts remain in the save but cannot be purchased. Event save version remains 2; existing bots, claimed rewards, lifetime progress, cosmetics and global data remain untouched. No boost state is serialized because temporary boosts are deferred.

## Debug commands

Only Debug builds launched with `--event-tools`, through the existing Shop code box:

- `event-shop-unlock`: persist unlocks for all enabled offers.
- `event-shop-reset`: reset purchase counters, retaining permanent rewards and unlocks.
- `event-shop-count:item_id:2`: set one known item's count (including test values above its cap).
- `event-shop-fund:item_id`: grant the shortfall in that offer's actual cost currency through existing reward adapters.
- `event-shop-expire`: expire the Event using the existing expiration tool.
- `event-shop-print`: log IDs, states, prices/currencies, balances, counts and limits.

## Deferred temporary boosts

Applying a temporary multiplier to the entire offline interval would overpay after expiry. Correct implementation requires production settlement to split at UTC activation/expiration boundaries and preserve that state through load/Prestige. This is intentionally deferred rather than presenting a partially working boost. The existing EventRewardType.Custom ID and external grant/undo callback are the extension point; unsupported custom grants fail and roll back without charging. No boost offers are configured.

## Files changed

Added:

- `scripts/game/events/EventManager.Shop.cs`
- `scripts/game/events/EventManager.Shop.Debug.cs`
- `scripts/game/events/TestProtocolShop.cs`
- `scripts/ui/events/EventShopCard.cs`
- `tests/Events/Shop.cs.txt`
- `tests/Events/shop_scene_smoke.gd`
- `docs/event-shop.md`

Modified:

- `scripts/data/EventGameplayDefinition.cs`
- `scripts/data/EventSaveData.cs`
- `scripts/data/EventChallengeDefinition.cs`
- `scripts/game/Game.Events.cs`
- `scripts/game/events/EventManager.cs`
- `scripts/game/events/EventManager.Gameplay.cs`
- `scripts/game/events/EventManager.Rewards.cs`
- `scripts/game/events/EventManager.Challenges.cs`
- `scripts/game/events/EventBotRules.cs`
- `scripts/game/events/EventDefinitionValidator.cs`
- `scripts/game/events/EventDeveloperTools.cs`
- `scripts/game/events/TestProtocolEventDefinition.cs`
- `scripts/game/events/README.md`
- `scripts/ui/events/EventView.cs`
- `scripts/ui/events/EventController.cs`
- `tests/Events/Events.csproj`
- `tests/Events/Program.cs.txt`

Unrelated pre-existing workspace edits remain in place.

## Validation results

- Debug: zero errors; 255 pre-existing repository warnings.
- ExportRelease: zero errors; 255 pre-existing repository warnings.
- Event suite: 385 assertions passed, including all prior progression/Reward Track/challenge/Bot tests and new Shop regressions.
- GameSave suite: 3,645 deterministic assertions passed.
- New Shop scene: 910 checks, zero failures; clean process exit.
- Existing Event tabs: 2,203 checks, zero failures.
- Existing Challenges scene: 998 checks, zero failures.
- Existing Reward Track scene: 840 checks, zero failures.
- Existing Event Bots scene: 67 checks, zero failures.
- Existing navigation scene: 122 checks, zero failures (including normal navigation and Singularity).
- Existing Halloween scene compatibility fixture: zero failures; its content/configuration was not modified.
- `git diff --check`: passed.

Domain regressions cover purchase success/insufficient balance, all cost currencies, once/finite/unlimited limits, stale/replayed counts, reentrant purchases, sold-out/owned/locked/expired states, unlock sources, save/reward failure compensation, global cosmetic ownership, bot bundles, reward/challenge integration, specific-item goals, play-time challenge unlocks without production polling, Prestige preservation, restart, old missing fields, unknown/retired IDs, overflow and invalid catalog configuration.

The scene fixture exercises all eight Test Protocol offers, real shared Data Shard and cosmetic services, double taps, immediate full-scene reload after a purchase, restart persistence, debug commands, expired ownership/history, stable card order/identities/signals, price/item images, modern button states and portrait 1080×2400 / 720×1600 / 540×960 with logical widths 320 / 360 / 720. Checks were headless; physical Android visual/touch testing remains outside this validation.
