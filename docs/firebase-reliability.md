# Firebase / profile / leaderboard reliability investigation

## Confirmed causes in the previous implementation

- `SetUsername` checked token freshness, Firebase UID and Play Games player ID immediately and returned `Firebase is not connected yet. Wait for the connection message, then try again.` if any were missing. The leaderboard similarly returned `Firebase is still connecting. Try Refresh in a moment.` when its token/UID check failed. Neither retained the caller's request. GPGS login is asynchronous and does not imply Firebase exchange or player loading has completed.
- `_firebaseSignInRequested` covered only Play Games code acquisition. `OnServerSideAccessRequested` cleared it **before** sending `accounts:signInWithIdp`. Another GPGS authentication notification or consumer could start another exchange while the first HTTP request was still in flight. Repeated authenticated signals also started exchanges even when the Firebase token was fresh. Out-of-order success/failure callbacks could replace or clear credentials.
- Firebase `refreshToken` was discarded. A one-shot refresh timer requested another Play Games code; failures had no recurring recovery. HTTP authentication failures did not invalidate/refresh/retry tokens. Suspend/resume had no Firebase recovery hook.
- HTTPRequest nodes were already separate per request, which was correct. However, they had no timeout, the Godot transport result was ignored, and callbacks could prevent cleanup by throwing. There was no explicit completion/cancellation of pending operations or native signal disconnection on service teardown.
- `_leaderboardLoadInFlight` was one global flag: a second tab request returned success without recording its requested kind. That tab could remain loading when the older tab's response arrived.
- Profile loading unnecessarily waited for a Play Games player ID, even though `/profiles/{Firebase UID}` only needs Firebase identity. Username creation did not first resolve an existing profile, so an early/manual picker could submit an `exists=false` commit against an already-owned profile. A lost successful commit response was also subsequently mislabeled as a taken username.

The startup `CallDeferred` schedules feature composition; it is not authentication synchronization. It is retained. The username modal's deferred presentation is likewise UI scheduling only. The existing GooglePlayGamesAchievementService remains the sole owner of initial interactive GPGS sign-in; Firebase never starts a competing interactive login.

## HTTP 400: evidence and limits

There are no affected-device Firebase response bodies or reproducible authenticated Android sessions in this workspace. **The exact cause of the reported device HTTP 400 cannot be established from the status alone.** No live requests were issued with player credentials, and no Console settings were changed. The exchange race above can produce redundant/stale credential exchanges, but attributing the observed 400 to that race without its response would be speculation.

Inspected operations:

| Operation | Method / endpoint | Findings |
|---|---|---|
| GPGS credential exchange | POST `identitytoolkit.googleapis.com/v1/accounts:signInWithIdp` | URL-encoded `code` and `providerId=playgames.google.com` inside a JSON `postBody`; secure-token response requested. One full exchange at a time now. |
| Token refresh (added) | POST `securetoken.googleapis.com/v1/token` | Form-encoded `grant_type=refresh_token` and refresh token, JSON response parsed with refresh-specific field names. |
| Own profile load | GET `firestore.googleapis.com/v1/projects/idle-ai-empire/databases/(default)/documents/profiles/{escaped UID}` | Token acquired at dispatch, not assumed ready from GPGS. |
| Username uniqueness and save | POST `.../databases/(default)/documents:commit` | Two typed documents, both `currentDocument.exists=false`, one atomic reservation/profile write. No check-then-write uniqueness race. |
| Username/alias lookup | POST `.../documents:runQuery` | Typed `arrayValue.values[].stringValue` IN filter on playGamesPlayerId; remains capped at 25 IDs. |
| In-game leaderboard | POST `.../documents:runQuery` | Reads existing profiles; existing Top 25, own entry and competition-rank calculation retained. |
| Score submission | PATCH `.../documents/profiles/{escaped UID}?updateMask.fieldPaths=...` | Only existing score fields updated. integerValue is a string; timestamps explicitly invariant RFC3339. |

The original username payload already used `JsonSerializer`, `stringValue`, `timestampValue` and fully qualified document names correctly. Its `O` timestamp format is not evidence of a locale bug. Serialization is now extracted and regression-tested across four locales; this is hardening, not a claim that a specific malformed field caused the 400. The original commit/runQuery endpoint shapes were correct and remain unchanged.

DEBUG logs identify method, endpoint, HTTP status, parsed Firebase code/message and the redacted Firebase error object (including error details). Example:

```text
[Profile] Reserving username
[Firebase HTTP] Post https://firestore.googleapis.com/v1/projects/idle-ai-empire/databases/(default)/documents:commit; HTTP=400; firebaseStatus: INVALID_ARGUMENT; message: ...; firebaseError: {...}
```

Auth endpoint API-key query parameters, Authorization headers, request bodies, ID tokens, refresh tokens and Play Games auth codes are never logged. Error echoes are redacted using known credentials and credential-field/JWT patterns. Successful auth response bodies are never logged. Transport failures log their Godot result or request-start error. Player UI receives a short connection/authentication/unavailability message rather than raw Firebase internals.

## New ownership and recovery

`FirebaseAliasService` remains the public integration owner. Its `FirebaseSession` owns credentials, state and the single shared initialization/refresh task. Concurrent Godot callers use `EnsureReadyAsync()` and join the same task through GPGS authentication, code acquisition and the entire Firebase exchange. The service uses Godot's main synchronization context; worker threads must not call Godot APIs directly.

States: NotStarted -> Initializing -> Authenticating -> Ready, or TemporaryFailure / ConfigurationFailure. Known permanent configuration failures do not spin authentication attempts. Temporary auth failures back off up to 15 seconds. The service retries on its 15-second reconnect timer, on app resume, and on caller intent. UI waits up to 60 seconds for readiness; native login/code waits are bounded, player-ID loading has a 20-second timeout, and each HTTPRequest has a 20-second timeout. Timers/backoff schedule retries only; they do not infer readiness.

Every Firestore request obtains the current token from the shared session at dispatch. Tokens refresh when locally expired (with a 60-second safety margin); a rejected token causes one shared refresh and one replay. A stale request cannot invalidate a newer token. Rejected replacement tokens are invalidated for subsequent recovery. Revoked/expired refresh credentials fall back to a fresh GPGS exchange. Rules-related PERMISSION_DENIED is not treated as an expired token. No infinite retry loop is added.

Safe reads, queries and score PATCH operations retry a temporary transport/429/server failure once. An ambiguous username create commit is not blindly replayed: the service re-reads the own profile after a transport failure or conflict. Each HTTP operation owns its own temporary node and completion source. Tree exit cancels requests and waiters and disconnects only this service's native callbacks. Tokens remain memory-only; reopening the process performs a new coordinated login without changing saved game data.

The leaderboard waits for both authentication and the shared own-profile read, so the own row can be merged immediately. Each leaderboard kind has its own pending flag. A visible leaderboard that failed temporarily retries automatically on the next ready state. Profile/score recovery continues without reopening the app. Username save waits for authentication/profile discovery and requests the Play Games player ID only when a new profile actually needs it. Repeated submit attempts are coalesced.

UsernamePolicy uses trim + Unicode NFC + invariant lowercase. The existing 3–16 ASCII letter/number/underscore restriction and letter-first requirement are retained; accented/unsupported characters are rejected clearly rather than silently mapped to a different unique name. Null, malformed Unicode and embedded control characters are rejected. The same policy handles validation, reservation construction and loaded usernames. Existing reserved names remain readable. IDs, profile collections, fields, uniqueness semantics and saves remain compatible; no schema migration is required.

## Configuration verification

Local FirebaseConfig exactly matches the supplied google-services.json for project ID/number, web API key, Android package, mobile app ID and Web OAuth client ID. All Firestore requests use `(default)`; no named database configuration exists locally.

No Firebase Console, Firestore Rules or Play Games configuration changes are required by this patch. Remote Console settings and signing certificates cannot be verified from these files. If DEBUG logs show a genuine configuration error, verify:

- Firebase Authentication has the Play Games provider enabled, with the same Web/server OAuth client and server-side client secret configured in the Console (never in the app).
- Play Console Game server credential uses that same Web client; Android credentials match the installed package and its actual signing SHA certificate (including Play App Signing vs local/debug builds), and relevant testers/releases have access.
- The default Firestore database exists. Existing rules permit the same authenticated profile read/query, atomic username reservation/profile create and score-field update operations. Do not loosen rules to hide an error.
- API restrictions permit the required Identity Toolkit, Secure Token and Firestore operations for the intended Android credentials/project.

Capture the first failing `[Firebase HTTP]` line and preceding startup stages on an affected Android DEBUG build to distinguish auth 400 (provider/credential/configuration) from Firestore 400 (request validation). This is the remaining evidence needed to name the original device-specific 400 exactly.

## Validation and changed files

- `FirebaseAliasService.cs`: startup, username/alias callers and authenticated dispatch use coordination; old callback auth implementation removed.
- `FirebaseAliasService.Connection.cs`: GPGS bridge, refresh transport, profile loading, lifecycle, diagnostics and resume/reconnect.
- `FirebaseSession.cs`: shared readiness, credential lifetime, retry/backoff and state.
- `FirebaseRequestException.cs`: structured errors, classifications and diagnostic redaction.
- `FirestoreProfileRequests.cs`, `UsernamePolicy.cs`: typed atomic payloads and canonical username policy.
- `FirebaseAliasService.Leaderboards.cs`: readiness/profile waiting, per-kind loads and score dispatch.
- `OnlineProfileService.cs`: stable readiness/state boundary for UI.
- `LeaderboardOverlayController.cs`, `UsernameSetupOverlayController.cs`: centralized connection status and reconnect continuation.
- `tests/Firebase/*`: network-free session/payload/error regressions.
- `tests/Integrations/FirebaseTests.cs.txt`, `Runner.cs`, `Integrations.csproj`: actual Godot service API regressions with a DEBUG fake transport.

Prior Event-balance work in this workspace is unrelated and preserved.

The protocol was checked against the official [Firebase Auth REST reference](https://firebase.google.com/docs/reference/rest/auth), [Firestore commit reference](https://firebase.google.com/docs/firestore/reference/rest/v1/projects.databases.documents/commit), and [Firestore typed Value reference](https://firebase.google.com/docs/firestore/reference/rest/v1/Value).

On-device follow-up: cold-start and immediately open both leaderboard tabs; submit a name while login is pending; background/resume across token expiry; disable internet during login/read/save and restore it; close/reopen; test both a Play-distributed build and locally signed build. These require Android/GPGS credentials and are not replaced by the simulated transport tests.

Completed validation: Debug and ExportRelease builds pass with zero errors (254 existing repository warnings); 66 Firebase core assertions, 14 Firebase Godot service assertions and 206 existing integration assertions pass. Godot service coverage also includes scene teardown during authentication, verifying that late completions do not publish to disposed UI. The integration suite deliberately exercises damaged-save failures and therefore emits expected save-recovery error logs while passing.

The username picker also ignores delayed automatic-open requests while it is already visible, so a late profile-not-found response cannot clear typed input or reset an in-progress save.

## Device-specific follow-up

See [firebase-device-auth.md](firebase-device-auth.md) for subsequent native callback/startup/resume findings and fixes. It distinguishes confirmed code defects from backend failures that still require an affected-device log capture.
