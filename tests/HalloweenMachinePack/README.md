Run from the repository root after `dotnet build` and importing assets in Godot:

```sh
dotnet build tests/HalloweenMachinePack/HalloweenMachinePack.csproj -p:NuGetAudit=false
HALLOWEEN_REPO_ROOT="$PWD" XDG_DATA_HOME=/tmp/idle-halloween-test godot-mono --headless --path tests/HalloweenMachinePack
XDG_DATA_HOME=/tmp/idle-halloween-test-scene godot-mono --headless --path . --script tests/HalloweenMachinePack/scene_smoke.gd
```

The C# runner uses lightweight textures registered at real repository asset paths and tests ownership, purchase, partial ownership, persistence, each room's four tiers, room binding, and AI Archive integration. It requires an isolated profile and replaces its machine skin save fixture.

The scene smoke test uses imported artwork and a fresh, zero-shard profile to check adjacent cards, promo settings, portrait bounds, locked Collection cards and scrolling. For the funded purchase scenario, set `DataShards` to 100 in the isolated scene profile's `idle_ai_save.json`, ensure no machine skin save exists in that profile, and rerun with `HALLOWEEN_FUNDED=1`. This additionally verifies purchase, Collection refresh, all four equip buttons, and persisted ownership/equipment.

The shared seasonal offer regression checks equal card/slot dimensions at three portrait sizes, synchronous hiding after each purchase, reopening, existing ownership saves, and Event skin offers. Run each pair with a new isolated profile; the second invocation starts a separate game process using the first invocation's saves:

```sh
XDG_DATA_HOME=/tmp/halloween-offers-test-shop godot-mono --headless --path . --script tests/HalloweenMachinePack/offers_scene_smoke.gd -- --event-tools
XDG_DATA_HOME=/tmp/halloween-offers-test-shop godot-mono --headless --path . --script tests/HalloweenMachinePack/offers_scene_smoke.gd -- --event-tools --verify-reload
XDG_DATA_HOME=/tmp/halloween-offers-test-event godot-mono --headless --path . --script tests/HalloweenMachinePack/offers_scene_smoke.gd -- --event-tools --event-purchase
XDG_DATA_HOME=/tmp/halloween-offers-test-event godot-mono --headless --path . --script tests/HalloweenMachinePack/offers_scene_smoke.gd -- --event-tools --verify-event-reload
```
