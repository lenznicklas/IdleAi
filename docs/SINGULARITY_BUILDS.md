# Singularity build expansion

The eight positions form a **ring**, numbered clockwise from the north node. Each node has two local ring neighbors. Cardinal positions (0, 2, 4, 6) retain their existing reciprocal cross-sector connection. Formations use only the ring within one sector; they do not invent a central build slot or square-grid connections.

The existing facade, economy, sector/node/bot/production/persistence/offline services, unlock milestones, spiral map, core formulas, Matter investment/refunds, and SaveAndNotify ordering remain in place. New helpers calculate derived bonuses. Main-game saves, Prestige, total levels, and normal room visuals are unchanged.

## Synergies and formulas

Compute remains the direct Matter producer. Amplifier and Cooling provide local support. Quantum provides an **additive sector Compute multiplier**, rather than producing Matter independently.

For support neighbor `j`, strength is:

```
s_j = level_j × workingBotMultiplier_j × quality_j × sectorTypeMultiplier_j × (1 + formationBonus_j)
```

Support strength does **not** include that support node's incoming adjacency bonuses. This prevents feedback loops. The source sector supplies its support modifier, including for cross-sector links. Existing behavior allowing support from a locked neighboring sector is preserved.

For a Compute target:

- Each Amplifier contributes a factor `1 + 0.15 × s_j`.
- Each Cooling contributes a factor `1 + 0.10 × s_j`, representing effective cycle speed without introducing timers.
- Adjacent Quantum contributes `0.05 × s_j` to one additive local bonus.
- Adjacent Compute grants no direct synergy.

For a Quantum target:

- Each Amplifier contributes a factor `1 + 0.10 × s_j`.
- Each Cooling contributes a factor `1 + 0.20 × s_j` (twice its Compute coefficient).
- Other Quantum nodes grant no adjacency bonus.

Each combined Amplifier and Cooling factor is capped at **3×**. The additive local Quantum bonus is capped at **+30%**. These factors multiply. Existing low-level Amplifier/Cooling behavior is preserved, with new caps controlling high-level stacking.

```
nodeBonus = adjacency × (1 + formationBonus) × quality × sectorTypeMultiplier
quantumSectorMultiplier = 1 + Σ(0.12 × quantumLevel × quantumBot × quantumNodeBonus)
computeOutput = 0.25 × 1.58^(level−1) × computeBot × computeNodeBonus
                × quantumSectorMultiplier × existingComputeCoreMultiplier
sectorOutput = existingCoreOutput + Σ(computeOutput)
```

Core income does not receive quality, synergy, formation, or Quantum bonuses. Quantum contributions add, never multiply one another. Unsupported Quantum retains its original base contribution; supported Quantum becomes more effective. Offline income uses these same formulas and retains its existing efficiency, duration cap, and bot-break simulation.

## Five ring formations

Either direction and wrap from slot 7 to slot 0 are valid.

| Formation | Ring sequence | Bonus beneficiaries |
| --- | --- | --- |
| Compute Arc | Compute–Compute–Compute–Compute | +20% to all four Compute |
| Neural Bridge | Amplifier–Compute–Amplifier | +35% to center Compute |
| Quantum Matrix | Amplifier–Quantum–Cooling | +40% to center Quantum |
| Stabilized Chain | Compute–Amplifier–Quantum | +15% to all three |
| Cooling Cradle | Cooling–Quantum–Cooling | +30% to center Quantum |

Bonuses from different names add. The **same name applies once per node**, even when several overlapping instances contain that node. The total is capped at **+75%**, after the sector formation modifier. The detector deduplicates identical instances and recalculates from the layout; nothing is stored in saves. Selling a required node immediately removes its pattern.

## Ten sector modifiers

New sectors, including a fresh first sector, roll uniformly from these ten modifiers. Existing sectors retain the neutral **Stable Sector** identity.

| Modifier | Effect |
| --- | --- |
| Compute Focus | Compute output +30% |
| Quantum Focus | Quantum effect +50% |
| Amplifier Focus | Amplifier effect +35% |
| Cooling Focus | Cooling effect +35% |
| Quantum Overdrive | Quantum effect +80%; all node builds and upgrades cost +25% |
| Hot Compute | Compute output +40%; Cooling effect −20% |
| Efficient Construction | Node builds cost −20% |
| Efficient Upgrades | Node upgrades cost −20% |
| Efficient Core | Core upgrades cost −20%; core unlock costs unchanged |
| Resonant Formations | Formation bonuses +35%, subject to the +75% cap |

Only one modifier applies per sector. Costs displayed, charged, and recorded as investment use the same modifier. Bot economics and durability remain unchanged. Selling still refunds one third of actual investment; quality does not increase refunds, avoiding a profitable build/sell loop.

## Node quality

| Quality | Chance | Output/support multiplier |
| --- | --- | --- |
| Normal | 80% | 1.00× |
| Enhanced | 15% | 1.10× |
| Prototype | 4.5% | 1.25× |
| Quantum | 0.5% | 1.50× |

The successful build rolls once after payment. Failed or duplicate actions, upgrades, reads, and loading do not reroll quality. Upgrades retain quality; selling clears it. Amplifier/Cooling quality improves their outgoing support strength; Quantum quality improves its sector contribution and local Compute support. Production shares the existing Godot bot RNG; tests use the existing injectable roll seam. No RNG state or derived patterns are added to saves.

## Persistence and migration

Only the independent `singularity_save.json` schema advances from **4 to 5**:

- `SingularitySectorData.Modifier`, numeric enum; zero = Neutral.
- `SingularityNodeData.Quality`, numeric enum; zero = Normal.

Missing properties deserialize to those safe defaults, including saves without an explicit version. Existing sectors and nodes are never randomly reassigned during migration. Invalid enum values also normalize to Neutral/Normal. Newly materialized sectors receive a modifier. Pre-v3 core migration and legacy bot durability initialization remain intact. Matter, investments, levels, bot state, and timestamps retain their existing handling. The migrated schema is written on the next normal save; corrupt-save protection and atomic replacement are preserved.

## UI and gold ambience

- Sector core details show the modifier name/effect and active formation names. The sector title also has a modifier tooltip.
- A collapsible formation guide explains the five valid ring patterns and stacking rules.
- Node details show quality/multiplier, sector modifier, per-source synergy bonuses, active formation names/combined bonus, actual Compute output, or effective support strength from the gameplay layer.
- Enhanced/Prototype/Quantum nodes have a colored star beside their level; their tooltip names the quality and multiplier. Build messages announce quality.
- `GoldenAmbientParticles` is a page-owned `GpuParticles2D`, created after the background/grid and before header/map/overlays. It follows the normal room GPU approach without changing normal rooms.
- 56 particles, 20-second lifetime/preprocess, 28–60 upward velocity, upward gravity −3, 18° spread, randomized size 0.12–0.23, soft 40px radial texture, gold tint with 0.42 opacity and lifetime alpha fade. Emission starts 30px below the page and resizes with it. No physics or input handling; page visibility hides the effect. GPU rendering/readability still requires a device check.

## Changed files

- `scripts/data/SingularityData.cs`: schema fields/version.
- `scripts/game/SingularityService.cs`: content RNG wiring and new read APIs.
- `scripts/game/singularity/SingularityBuildRules.cs`: quality and modifier definitions, multipliers/costs and player guide.
- `scripts/game/singularity/SingularityFormationService.cs`: ring pattern detection and stacking.
- `scripts/game/singularity/SingularitySynergyService.cs`: neighbors and per-source result model.
- `scripts/game/singularity/SingularityProductionService.cs`: one shared path for displayed/aggregate production and support effects.
- `scripts/game/singularity/SingularitySectorService.cs`: modifier creation and core discount.
- `scripts/game/singularity/SingularityNodeService.cs`: quality rolls/reset and modified costs.
- `scripts/game/singularity/SingularityBotService.cs`: exposes its existing RNG seam internally for content rolls; bot rules unchanged.
- `scripts/game/singularity/SingularityPersistenceService.cs`: v5 normalization while preserving earlier migrations.
- `scripts/ui/room/SingularityController.cs`: sector/node feedback and quality badge.
- `scripts/ui/room/SingularityController.BuildFeedback.cs`: small UI helpers and GPU ambience.
- `tests/Singularity/Program.cs.txt`: deterministic expansion coverage.
- `docs/SINGULARITY_BUILDS.md`: formulas, content, validation and manual checklist.
- Four accompanying Godot `.cs.uid` files for the new helpers/partial controller.

## Validation

- Main `dotnet build --no-restore`: passes; existing warnings remain.
- Singularity deterministic Godot C# suite: 685 assertions passed, including previous regression coverage. New coverage exercises all five formations at every rotation and in both directions, broken layouts, overlap/caps, neighbor bonuses, supported Quantum, all modifiers and their persistence/costs, all quality intervals and persistence/output, support quality, v4 missing-field migration, invalid enums and new-sector assignment.
- Main GameSave suite: 3,416 deterministic assertions passed; main save remains version 21.
- Actual `game.tscn` smoke in an isolated `/tmp` user profile: Singularity entry, five loaded sectors, node overlay quality/synergy/formation feedback, upward GPU particle settings, radial texture, lifetime fade, sector modifier feedback, collapsible formation guide, layering and resize passed. This headless check validates scene construction, not rendered appearance. Scene teardown reports two ObjectDB instances and one resource still in use; normal-game startup/shutdown is outside this expansion.
- `git diff --check`: passes.
- Added formation-output and support-bot offline-break tests also verify that the same gameplay formula drives UI and offline/aggregate output.

## Manual checklist

These are device/editor checks, not claims of completed visual testing:

- [ ] Enter Singularity via map/navigation and return to other rooms.
- [ ] Observe gold particles rising behind content; inspect fade/drift and readability with tooltips open.
- [ ] Unlock a sector, inspect its modifier, then unlock its core.
- [ ] Build all four node types at their existing unlock milestones; inspect quality color/badge/details.
- [ ] Build/sell nodes to trigger and break each formation; inspect node and sector feedback.
- [ ] Save/reload a real v5 profile; compare layout, quality, modifier and production.
- [ ] Open a backed-up real old Singularity profile; confirm neutral sectors and Normal nodes retain progression.
- [ ] Inspect multiple sectors and all four cross-sector directions while panning/zooming.
- [ ] Test mobile frame time, battery/GPU load, safe areas, portrait/landscape resize and small-screen overlays.
- [ ] Compare readability with particles visible, especially gold/Quantum quality badges.
