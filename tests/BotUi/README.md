# Bot UI regression checks

Run from the repository root after building the game:

```sh
dotnet build IdleAi.csproj
dotnet build tests/BotUi/BotUi.csproj
BOT_UI_REPO_ROOT="$PWD" XDG_DATA_HOME=/tmp/idleai-bot-ui-tests godot-mono --headless --path tests/BotUi
```

This isolated Godot project uses real state, production, bot services and UI controls, with lightweight in-memory textures. It never opens the game's normal save profile.

Checks cover:

- Working, broken, manually running, repaired and removed bots in each of rooms 1–4.
- A bot breaking through `ProductionService.Update`, failed repair, and successful repair.
- Status and manual-button state immediately after each runtime, full-view and periodic durability refresh, in both update orders across 30 cycles per room.
- Singularity purchase, service, repair, sale and node upgrade with an installed bot, including repeated actions and exact costs/refunds.
- Held mouse input: unchanged button rectangle, scale, modulation, text color and idle/pressed StyleBoxes; actions execute on release.
- Full-durability disabled styling and restoration of ordinary node-upgrade feedback after bot removal.

There is no separate bot-upgrade action in the current game. The upgrade check exercises the existing node upgrade with a bot installed. Headless checks validate state and geometry; Android rendering still needs device verification.
