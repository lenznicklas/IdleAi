# Firebase regression checks

The plain .NET suite links the production session coordinator, error parser, username policy and username commit builder. No network access, Firebase credentials, Android SDK, Firestore instance or game saves are required.

```sh
dotnet run --project tests/Firebase
# If only the .NET 10 runtime is installed:
DOTNET_ROLL_FORWARD=Major dotnet run --project tests/Firebase
```

Checks cover early leaderboard requests; shared initialization; explicit states; expiry; shared refresh; 401 refresh/retry; bounded retries; stale-token protection; revoked refresh-token fallback; temporary network failure; reconnect; configuration failure; disposal; username trimming, casing, Unicode and malformed characters; atomic lowercase uniqueness reservations; JSON escaping and typed Firestore fields under English, Turkish, Arabic and Thai locales; HTTP 400 parsing; and redacted diagnostics.

The Godot integration suite also exercises the actual FirebaseAliasService API with an injected DEBUG transport:

```sh
dotnet build
dotnet build tests/Integrations/Integrations.csproj
XDG_DATA_HOME=/tmp/idle-ai-firebase-integration-tests godot-mono --headless --path tests/Integrations
```

It verifies early leaderboard loading, concurrent tabs, duplicate suppression, shared profile reads, Top 25, own rank outside Top 25, tied ranks, normalized username commits, repeated submit suppression and profile recovery after an ambiguous commit conflict. Existing integration tests run first. The transport override is compiled out of non-DEBUG game builds.

See [the investigation report](../../docs/firebase-reliability.md) for remaining on-device and Console verification.

Device-specific follow-up: the suite now also links the tagged Play Games gateway and covers delayed native/player proof, stale callbacks, empty/repeated codes, native status classification, cancellation, shared deadlines, resume/account changes and network-then-401 recovery (107 assertions). Godot Firebase integration now reports 24 assertions, including username startup/resume, redacted 400 diagnostics following 401 refresh and concurrent real HttpRequest cancellation/cleanup. The real-request fixture needs loopback sockets enabled.

See [the device authentication follow-up](../../docs/firebase-device-auth.md) for confirmed native-plugin defects, companion AAR packaging and remaining physical-device verification.
