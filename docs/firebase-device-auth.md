# Device-specific Firebase authentication follow-up

## Confirmed defects

- Startup initialized FirebaseAliasService before the Google Play Games owner initialized the Android SDK. Fast/already-signed-in devices could mask this ordering assumption.
- Inspection of the shipped Play Games release AAR's SignInProxy bytecode found that failed server-side-access tasks only wrote Android Logcat errors. Its Godot serverSideAccessRequested signal was success-only and had no request identifier. Managed code consequently waited for a timeout without the native failure; a late success could satisfy a subsequent request's untagged waiter.
- Cached Firebase readiness did not require fresh proof of the currently signed-in Play Games player. Resume could use a stale token/account while the native check was still pending.
- A shorter outer timeout could reject consumers while shared authentication continued. Generic status-zero failures mislabeled native/authentication timeouts as offline. A network retry followed by HTTP 401 bypassed refresh. Identity-bound requests could retain the old UID after account-changing reauthentication.

These are verified code defects, not proof of one specific backend rejection on an affected handset. No affected-device HTTP response or Logcat capture was provided. Synthetic regression responses (including HTTP 400/401 and native status 10) must not be interpreted as production evidence.

## Flow after the fix

The existing Play Games owner initializes first and remains responsible for interactive login. A small native companion uses the same SDK and reports tagged authentication/player-proof and server-code success **and failure**, including native status. It does not log credentials or introduce another login owner. Both Android export variants automatically include its AAR; the original vendor AAR is unchanged.

FirebaseSession owns one bounded initialization/refresh task shared by username, profile, scores and leaderboard consumers. Ready requires verified native player identity, Firebase UID, nonempty ID token and unexpired token (with expiry margin). Native work has bounded waits, stale callbacks are ignored, empty/repeated server codes are rejected, and cancellation cleans up request state. Shared initialization has a 100-second ceiling and one transient retry; later reconnect attempts use backoff.

Before Firestore dispatch, consumers await readiness. HTTP 401/UNAUTHENTICATED invalidates only the rejected token, refreshes once and retries once. Revoked refresh credentials fall back to fresh Play Games/Firebase sign-in. Network retries are separately bounded; ambiguous username commits are reconciled by reading the canonical profile rather than blindly replayed. UID-bound writes abort if reauthentication changes the Firebase user.

On pause/resume, cached native readiness is invalidated and the player is verified again (without disrupting an in-flight native consent/login operation). The same player can reuse a fresh Firebase token or refresh an expired one; a different player requires a new sign-in. Consumers wait for this shared check.

Username creation retains existing normalization and the existing atomic Firestore username/profile reservation. It awaits readiness and shared profile loading, captures verified identity and prevents concurrent submit attempts. Existing usernames, profiles, fields, rules, save data and Firebase configuration are unchanged.

Each HTTP attempt owns its own HttpRequest node. Completion, cancellation, scene teardown and independent deadlines clean it up. DEBUG failures include operation, safe endpoint, HTTP status, redacted Firebase response, UID/token presence, readiness at dispatch and refresh-attempt status. Auth/configuration/permission/request/server errors are distinct from genuine transport failures; UI shows Connecting while initialization is pending.

## Validation and limits

- Debug and ExportRelease builds: passed (existing repository warnings remain).
- Standalone Firebase regressions: 107 assertions passed.
- Godot Firebase integration: 24 assertions passed, alongside the existing integration/save regression suite. Coverage includes startup delays, concurrent consumers, stale/empty/repeated native codes, native errors/timeouts, identity changes, expiry/401 refresh, bounded retries, malformed responses, resume, username commits, error redaction and concurrent real HttpRequest cleanup/cancellation.
- Native companion compiled against the locally cached Godot/Android/Play Games dependencies; source, manifest and AAR are included.

No Firebase Console, Firestore schema/rules or Google Play Games configuration change is required by this implementation. Actual phone authentication, background/resume, Play Games consent and APK delivery still require an Android device test. If native status 10 or a backend configuration error occurs, inspect that concrete error against the device's account/build signing certificate/project settings; do not assume a global misconfiguration from a generic offline label.

Rebuild the native companion after Java changes with:

```sh
python3 addons/GodotPlayGameServices/firebase_bridge/build.py
```

The script uses the existing Android export dependency cache; it downloads nothing and leaves vendor binaries unchanged.

Protocol references: [Play Games server access](https://developer.android.com/games/pgs/android/server-access), [Firebase Play Games authentication](https://firebase.google.com/docs/auth/android/play-games).

## Files changed in this follow-up

Added:

- `scripts/game/FirebasePlayGamesGateway.cs`
- `addons/GodotPlayGameServices/firebase_bridge/src/com/idleai/firebase/IdleAiFirebaseAuth.java`
- `addons/GodotPlayGameServices/firebase_bridge/AndroidManifest.xml`
- `addons/GodotPlayGameServices/firebase_bridge/build.py`
- `addons/GodotPlayGameServices/bin/IdleAiFirebaseAuth.aar`
- `tests/Firebase/DeviceScenarios.cs`
- `docs/firebase-device-auth.md`

Updated:

- `addons/GodotPlayGameServices/export_plugin.gd`
- `scripts/game/FirebaseAliasService.cs`
- `scripts/game/FirebaseAliasService.Connection.cs`
- `scripts/game/FirebaseSession.cs`
- `scripts/game/FirebaseRequestException.cs`
- `scripts/game/OnlineIntegrationCoordinator.cs`
- `scripts/ui/overlays/LeaderboardOverlayController.cs`
- `scripts/ui/overlays/UsernameSetupOverlayController.cs`
- `tests/Firebase/Firebase.csproj`
- `tests/Firebase/Program.cs`
- `tests/Firebase/README.md`
- `tests/Integrations/FirebaseTests.cs.txt`
- `docs/firebase-reliability.md`

Other existing workspace edits were left in place; Event systems, bot lifetimes, FirebaseConfig and production database schema were not changed by this follow-up.
