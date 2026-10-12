# Machine Detail stability regression tests

Run from the repository root:

```sh
dotnet build
dotnet build tests/MachineDetails
godot-mono --headless --path tests/MachineDetails
```

The runner exercises the shared overlay and authoritative progression service
using the real four-room catalog, Orbitron font, and readability pass. Cosmetic
textures are supplied in memory; tall and wide previews are tested separately.
The runner isolates the detail window from unrelated game startup services.

The previous maximum measured panel size was 580 × 956 with the game font.
Tests compare exact global rectangles for the panel, text/preview regions,
quantity row, main Upgrade button, and close button across all tiers and 1x,
5x, 10x, MAX. They include Laptop → PC and every subsequent available tier
transition, normal quantity quotes/costs, one transition per tier click,
selection retention, no extra level upgrades, affordability/invalid targets,
Bot state changes, long content, reopen, slot/room switching, and JSON save/load.
Tier clicks also target a displayed room different from the current game room.

The existing open animation is stopped in tests to compare settled geometry.
Rendered text legibility, image appearance, and touch behavior still need a
visual/device check.
