# Test Protocol balancing sandbox

Run from the repository root (plain .NET; no Godot runtime or player saves):

```sh
dotnet run --project tests/EventBalance -- --check
dotnet run --project tests/EventBalance -- --minutes 60 --visit-seconds 5
dotnet run --project tests/EventBalance -- --minutes 60 --visit-seconds 120
dotnet run --project tests/EventBalance -- --minutes 0 --offline-hours 6
dotnet run --project tests/EventBalance -- --minutes 1440 --offline-hours 6
dotnet run --project tests/EventBalance -- --no-prestige
dotnet run --project tests/EventBalance -- --export /tmp/event-balance.json
# Edit the JSON, then run a variant without changing game configuration:
dotnet run --project tests/EventBalance -- --config /tmp/event-balance.json --minutes 120
```

With only .NET 10 installed, prefix commands with `DOTNET_ROLL_FORWARD=Major`.
The project targets net9.0 to match the game. Production runs still use the game's normal runtime.

The executable links the actual definition, validator, save model, production service and Prestige service. It never copies economy formulas, edits game saves or grants permanent rewards. The small catalog shim supplies only the Test Protocol ID, avoiding Godot asset dependencies.

Default scenario: a fresh save, check in every 30 seconds, buy affordable upgrades by best marginal production per currency, immediately Prestige when eligible. Every visit can make multiple purchases. Unlock and milestone log times are observations rounded up to the visit interval. First upgrade is **affordable at 5 seconds**; a 30-second visitor buys it at 30 seconds. This strategy is reproducible, not an optimal player model. `--no-prestige` allows testing a longer first run. Test Protocol has no Bots/Shop or production-affecting milestone rewards; the simulator does not claim rewards, equip bots or buy shop items, so JSON variants adding those effects need explicit modeling before trusting their pacing.

After the requested minutes, production settles for `--offline-hours` (default 6), with automatic unlocks but no upgrades or Prestige. It then checks that one large settlement agrees with minute-by-minute settlement, including cycle phases and unlock states, and validates the resulting save. Offline unlocks appear in the return snapshot; they are not incorrectly logged as unlocking at the return time.

`--check` additionally verifies first-upgrade affordability, second-generator pacing, first and second Prestige pacing, all five generator unlocks, continued early-generator contribution, and first-hour runaway limits. Use default 60 minutes, Prestige enabled, visits <=30 seconds and no JSON override for these baseline checks. Production and Prestige still use all values from the definition; check thresholds are assertions of pacing goals only.

See [the balance report](../../docs/event-balance.md) for old/new values and measured results.
