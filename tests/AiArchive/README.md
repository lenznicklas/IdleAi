# AI Archive regression checks

From the repository root:

```sh
dotnet build IdleAi.csproj
dotnet build tests/AiArchive/AiArchive.csproj
ARCHIVE_REPO_ROOT="$PWD" XDG_DATA_HOME=/tmp/idle-archive-ui-check-data godot-mono --headless --path tests/AiArchive
```

The runner uses real catalogs/progress services with in-memory art and the game's
Orbitron font. It bypasses Game startup and initializes no save or external services.
Existing progress checks remain intact. UI checks open every category, navigate back,
repeat openings, switch pages within one frame, and close/reopen at portrait sizes
720×1600 (the 1080×2400 aspect ratio) and 360×800.

Every displayed Control's rectangle, scale, and font size is checked after native
deferred layout and across eight subsequent frames/readability scans. This catches
delayed text-metric changes and opening layout/scale movement. Hover/pressed styles,
scrolling, card borders, and unrelated overlays retain their existing implementation.
Headless checks do not replace an Android rendering check.
