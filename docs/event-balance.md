# Test Protocol balance review

All tuning is in `scripts/game/events/TestProtocolEventDefinition.cs`, referenced by EventCatalog. Shared Event formulas, UI, Halloween, Bots, Shop, schedules and non-Event gameplay are unchanged. The existing Halloween definition was inspected; its balance remains provisional for the final event. Test Protocol currently defines no Bots or Shop inventory, so there is no bot/shop multiplier in these estimates.

## Generator tuning

Production is **per 5-second cycle**, before multipliers; generators unlock at level 1 for free. Costs below are for the first upgrade, not purchase/unlock prices. All generators retain max level 1000, generator multiplier 1, starting level 1 and 5-second cycles.

| Generator | Old base output | New base output | Old upgrade cost | New upgrade cost | Old unlock | New unlock |
|---|---:|---:|---:|---:|---|---|
| Data Terminal | 1 | 5 | 10 | 5 | Immediate | Immediate |
| Compute Rack | 25 | 20 | 250 | 20 | 100 run Data | 350 run Data |
| Neural Server | 500 | 80 | 5,000 | 80 | 5,000 run Data + Rack level 5 | 8,000 run Data |
| Quantum Cluster | 15,000 | 320 | 150,000 | 320 | Prestige 1, immediately after reset | Prestige 1 + 100,000 run Data |
| Prototype Core | 500,000 | 1,280 | 5,000,000 | 1,280 | Prestige 2, immediately after reset | Prestige 2 + 500,000 run Data |

| Parameter | Old | New |
|---|---|---|
| Terminal production scaling | Linear, +1 per level | Exponential, x1.18 per level |
| Other generator production scaling | x1.15 per level | x1.18 per level |
| All upgrade cost scaling | x1.6 per level | x1.4 per level |
| Prestige requirement | 1,000,000 every run | 500,000 x 1.3^completed Prestiges |
| Prestige reward | floor(sqrt(run earnings / requirement)) | Same; denominator uses the growing requirement |
| Production multiplier | 1 + 0.25 x total cores earned | 1 + 1.0 x total cores earned |
| Milestone thresholds (stable IDs, in original order) | 100 / 500 / 2,500 / 10,000 | 100 / 2,500 / 150,000 / 1,500,000 |
| Milestone rewards | 1 / 2 / 5 / 10 Developer Credits | Unchanged |
| Event-wide multiplier | 1 | 1 |
| Offline efficiency | 100%, cycle based, through event end | Unchanged |

Milestone IDs retain their old numeric names for save compatibility. Existing claimed/unlocked milestones are preserved; rebalance does not retract rewards, reset saves, or retroactively relock unlocked generators. For a fresh benchmark, start a new simulator run or use the existing debug `event-reset` command in the game.

## Measured pacing

Fresh save; no shop spending, bots, external rewards or manual currency. Simulator uses real game production, unlock and Prestige services. Visits purchase affordable upgrades by marginal production per cost and Prestige at the first opportunity. Timings are estimates of this policy, not guaranteed player behavior.

| First occurrence, elapsed minutes | Old, 5-second visits | New, 5-second visits | New, 30-second visits | New, 2-minute visits |
|---|---:|---:|---:|---:|
| First upgrade purchased | 0.83 | 0.08 | 0.50 | 2.00 |
| Data Terminal | 0 | 0 | 0 | 0 |
| Compute Rack | 3.25 | 2.08 | 2.50 | 4.00 |
| Neural Server | 12.67 | 6.25 | 7.00 | 10.00 |
| First Prestige | 81.75 | 25.67 | 26.00 | 30.00 |
| Quantum Cluster | 81.75 | 32.42 | 33.50 | 42.00 |
| Second Prestige | 85.83 | 36.50 | 37.50 | 46.00 |
| Prototype Core | 85.83 | 43.08 | 45.00 | 58.00 |
| Duration of second run | 4.08 | 10.83 | 11.50 | 16.00 |

First upgrade becomes affordable in **5 seconds**, regardless of how often the player visits. With 2-minute visits, the second-generator target slips to 4 minutes due to deferred purchases. First Prestige stays within the 20–40-minute target across all three policies. Later generators require increasingly large earned targets (350 -> 8,000 -> 100,000 -> 500,000), plus the existing Prestige gates.

At one hour:

- Old frequent-play run: 600,445 lifetime Data, 266/s, no Prestige.
- New 30-second visits: 5,956,692 lifetime Data, five Prestiges, sixfold multiplier. A reset at minute 59 leaves only 138/s at minute 60; production rebuilds during the run. Quantum and Prototype have both been reached, then relocked by subsequent resets.
- New frequent play: 6,607,551 lifetime Data, six Prestiges, sevenfold multiplier, 1,111/s at minute 60.
- New 2-minute visits: about 3.59 million lifetime Data, three Prestiges, fourfold multiplier; the third reset happens at minute 60.
- New no-Prestige policy with 30-second upgrades: 3.19 million Data and 1,767/s, but later generators remain Prestige gated.

At minute 20 in the frequent-play run, Terminal/Rack/Server contribute about 74/180/315 Data per second: earlier machines still supply **45%** of production. In the 30-second run their combined share is 37%. New generators do not erase previous investment.

Milestones with 30-second visits arrive at about 1.5, 4.5, 16 and 44 minutes, spanning early upgrades through the third run. The permanent reward budget remains 18 credits.

## Offline impact and longer runs

Offline time uses the same production and unlock rules as online time, with no automatic spending, reward claiming or Prestige. No new cap or efficiency system was added.

- **Fresh save, immediately offline for six hours:** 422,120 Data, all first three generators at level 1, 21/s. Rack unlocks at 5m50s and Server at 31m20s even while offline. First Prestige is still slightly out of reach (about 7h02m without upgrades); returning upgrades accelerate it.
- **One hour with 30-second visits, then six hours offline:** adds 44,665,644 Data; lifetime total 50,622,336, wallet 44,665,649. All generators automatically reunlock, return production is 2,154/s. Four cores are available for a single return Prestige, raising the multiplier from x6 to x10. It cannot repeat immediately because current-run earnings reset to zero.
- **One hour with 5-second visits, then six hours offline:** adds 71.74 million Data; lifetime total 78.35 million, five cores available on return.
- **24 hours of continuous 30-second purchasing/Prestige:** 54 Prestiges, x55 production, 2.57 trillion lifetime Data, 82.5 million/s. The shortest runs are about 4.5 minutes; later runs grow again, reaching 132.5 minutes for Prestige 54. This is an intensive-play upper scenario, not passive offline growth. Large late numbers remain a watchpoint for the eventual Halloween duration/reward budget.

The old balance exhibits a concrete runaway: Prestige 3 takes ten seconds, then later resets take five seconds. The new exponential requirement growth eventually outpaces the linear core multiplier. Production per upgrade grows more slowly than its price (1.18 vs 1.4), so marginal returns decline; holding a fixed generator roster does not yield unbounded exponential growth over time. Later generators have moderate fourfold base-output steps and repeatable earned gates, avoiding free post-reset production spikes.

The square-root reward formula is retained deliberately: four times the current requirement grants two cores, nine times grants three. Overnight earnings therefore help strongly without converting linearly into a huge permanent multiplier. The first core doubles production, and the second raises x2 to x3; spending cores still preserves the existing lifetime-earned production bonus.

## Validation

The standalone simulator checks balance definitions, save validity, online/offline equivalence, fresh first-upgrade affordability, first/second Prestige pacing, early-generator relevance and generator unlock pacing. JSON export/import supports fast parameter sweeps without touching game configuration. See `tests/EventBalance/README.md` for commands and assumptions.

Validation completed: Debug game build passed (existing repository warnings); Event domain regression suite passed all 173 assertions; Event scene smoke reported zero failures. Simulator baseline checks passed at 5- and 30-second visits. Also checked 2-minute visits, no-Prestige play, fresh six-hour offline settlement, a 24-hour active run, and JSON export/import. All offline comparisons and simulated save validation passed. Existing migration and scene assertions were updated for the new Terminal output and upgrade price.
