# Event Prestige UI

The prior tab's clipped, auto-wrapped labels collapsed to a 1px minimum height in Godot, leaving only its title, icons and button visible. Its existing text also gave little explanation of Prestige's effects.

`EventPrestigePanel` now uses four compact, cached cards and the existing Prestige button:

- Progress: earned/required currency, themed bar, percentage, total lifetime earnings and eligibility message.
- Reward: expected gain with the configured Prestige icon, current wallet and total earned resources.
- Bonus: current production bonus and projected bonus after Prestige.
- Reset/keep: currency, generator levels and run unlocks reset; Prestige resources, lifetime earnings, reward claims, bots, cosmetics, cumulative challenge progress and shop limits remain. Current-state challenges are re-evaluated.

Text wraps with a real minimum line height instead of clipping away. The confirmation heading also wraps to fit narrow screens. The button highlights when available, shows the quoted reward and remains disabled when ineligible/expired. Active-definition names, currency icons and accent colors work for Halloween and Test Protocol. Cards/resources are reused; normal manager changes update existing labels/bar immediately.

All numeric values come from `GetPrestigeQuote`, `GetProgress`, `GetTotalPrestigeResourceEarned` and the active definition. Eligibility still uses lifetime earnings **since the last Prestige** (`TotalCurrencyEarned - PrestigeEarnedCheckpoint`), with total lifetime earnings displayed separately. Spending currency does not reduce progress. No economy formula, save schema or reset behavior changed.

Files changed:

- `scripts/ui/events/EventPrestigePanel.cs`
- `tests/Events/prestige_ui_scene_smoke.gd`
- This document.

Validation:

- Debug and ExportRelease builds pass with zero errors; existing warnings remain.
- Dedicated real-scene fixture: 329 checks, zero failures, covering both event definitions, values/eligibility updates, spending, Prestige/confirmation, post-reset values and requirement growth, expired state, cached nodes and 320/360/720px portrait widths.
- Existing tab scene: 2,595 checks, zero failures; existing Halloween gameplay scene also passes.
- Existing Halloween/Test Protocol asset scene: 7,267 checks, zero failures. Normal/Event/Singularity navigation: 155 checks, zero failures.
- Rendered OpenGL portrait previews inspect both events before eligibility, when ready and after Prestige. Screenshots are in `/tmp/idle-ai-prestige-ui/`. The fixture can generate them with `-- --event-tools --capture-prestige` using an isolated `event-prestige-test` profile.
- No physical Android run.

Existing workspace changes from earlier Event tasks were preserved.
