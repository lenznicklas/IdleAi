Run from the repository root after `dotnet build` and importing assets in Godot:

```sh
dotnet build tests/HalloweenMachinePack/HalloweenMachinePack.csproj -p:NuGetAudit=false
HALLOWEEN_REPO_ROOT="$PWD" XDG_DATA_HOME=/tmp/idle-halloween-test godot-mono --headless --path tests/HalloweenMachinePack
XDG_DATA_HOME=/tmp/idle-halloween-test-scene godot-mono --headless --path . --script tests/HalloweenMachinePack/scene_smoke.gd
```

The C# runner uses lightweight textures registered at real repository asset paths and tests ownership, purchase, partial ownership, persistence, each room's four tiers, room binding, and AI Archive integration. It requires an isolated profile and replaces its machine skin save fixture.

The scene smoke test uses imported artwork and a fresh, zero-shard profile to check adjacent cards, promo settings, portrait bounds, locked Collection cards and scrolling. For the funded purchase scenario, set `DataShards` to 100 in the isolated scene profile's `idle_ai_save.json`, ensure no machine skin save exists in that profile, and rerun with `HALLOWEEN_FUNDED=1`. This additionally verifies purchase, Collection refresh, all four equip buttons, and persisted ownership/equipment.
