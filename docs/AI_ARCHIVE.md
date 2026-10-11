# AI Archive

The Stats list retains its Leaderboards section first, followed by the AI Archive entry and Statistics. There is no new navigation-bar entry. Archive is a lazy full-screen overlay over Stats; closing it leaves Stats visible at its existing scroll position. Categories rebuild simple cards only on navigation/open, with existing mobile scrolling and close-button helpers.

`AiArchiveProgressService` takes read-only ownership/equipment queries from the session's existing cosmetic services, the existing Lab and Progression services, GameState, and a read-only Singularity unlock callback. It owns no progress and writes no saves. `AiArchiveOverlayController` renders its snapshot; Stats only renders a summary and opens Archive.

## Completion rules

- **Bots:** the four BotCatalog rarities. An assigned normal-room bot proves that rarity, including broken bots. Selling the last bot of a rarity or Prestige can remove this evidence. No lifetime rarity ledger exists. Unproven entries show a silhouette and `???`, without rarity details.
- **Machines:** actual room machine catalogs. An unlocked slot at tier N proves tiers 0 through N in that room in the current run. Prestige resets this evidence. Unproven entries hide their names/details.
- **Skins:** public bot skin packs and complete machine skin packs from existing catalogs. Ownership/equipped status uses the existing services. Default artwork is not a purchasable collection pack. Korpo is entirely excluded, including the denominator, until the existing service reports ownership; it then appears owned. Incomplete machine packs are excluded as unavailable content.
- **Research:** all existing ResearchCatalog definitions, with completion from LabData and availability from LabService. The existing research tree already shows these definitions; Archive introduces no hidden-research disclosure. Completed research counts; researching/available/locked research does not.
- **Achievements:** seven existing local Play Games conditions. `IsLocallyEarned` centralizes the previous predicates, also used by normal online synchronization. Archive makes no authentication or Google Play calls. Conditions and online behavior are unchanged. Local earned state is not a claim of confirmed online synchronization.
- **Rooms:** normal room catalog plus The Singularity. Normal rooms count when unlocked or when their retained room reward history proves an earlier unlock. Singularity uses the already initialized service's unlock flag; Archive never constructs or loads a separate Singularity service. Normal room accent colors are preserved.

Overall completion is the sum of completed entries divided by the sum of visible/available entries, rounded down to a whole percentage; an empty archive reports zero. Category completion uses the same entries. Secret content does not create unknowable missing percentage points.

There are no save-version, migration, cosmetic-save, gameplay, balance, or offline changes.

## Verification

```sh
dotnet build
# Build the main assembly before the tests.
dotnet build tests/AiArchive/AiArchive.csproj
ARCHIVE_REPO_ROOT="$PWD" XDG_DATA_HOME=/tmp/idle-ai-archive-tests godot-mono --headless --path tests/AiArchive
XDG_DATA_HOME=/tmp/idle-ai-archive-smoke godot-mono --headless --path . --script tests/AiArchive/scene_smoke.gd
```

The deterministic test runner uses real catalogs and read-only ownership delegates with lightweight in-memory textures. It covers zero progress, current tiers/rarities, room history, research, achievements, owned/unowned and secret skins, aggregate counts, late game and Singularity. The scene smoke check exercises actual game initialization, Stats ordering, all category pages, portrait text bounds, back/close navigation, and Stats scroll retention. Profiles are isolated from real player saves.

Remaining hands-on checks: portrait devices at different aspect ratios/safe areas, touch scrolling and momentum, image/glow/text readability, long names, fresh/mid/late/Prestige saves, Korpo locked/unlocked, and Singularity locked/unlocked.
