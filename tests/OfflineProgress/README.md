# Normal-room offline regression tests

Run from the repository root after building the game:

```sh
dotnet build
dotnet build tests/OfflineProgress
godot-mono --headless --path tests/OfflineProgress
```

The tests use Godot C# to load the real Bot catalog and exercise the real online
production service. The runner supplies in-memory cosmetic textures.

Coverage includes fractional and exact Bot breaks, independent Bots and rooms,
locked rooms/slots, machine upgrades, pipeline bottlenecks, research and temporary
boost boundaries, legacy initialization, JSON save hydration, uncapped long
absences, and consumed pause snapshots. Online/offline comparisons use cycle
boundaries: normal offline income deliberately retains the existing averaged
cycle-rate calculation and offline earning percentage. Between cycle boundaries,
that existing averaging can differ from discrete online cycle rewards.

Mobile notification delivery, minimized desktop runtime, process termination,
and the repair/earnings UI still require device checks.
