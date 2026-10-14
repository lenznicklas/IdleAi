using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace IdleAi;

/// <summary>
/// Firebase-backed public Idle AI aliases.
///
/// The service deliberately does not use the Google Play display name. Google
/// Play only supplies stable player ids and leaderboard scores; Firestore maps
/// those ids to a player-chosen Idle AI username.
///
/// Authentication flow:
/// Google Play Games -> server auth code -> Firebase Auth REST API ->
/// Firebase ID token -> Firestore REST API.
///
/// No OAuth client secret is embedded in the app.
/// </summary>
public sealed partial class FirebaseAliasService : Node
{
	private const string PlayGamesPluginName =
		"GodotPlayGameServices";

	private const string PlayGamesProviderId =
		"playgames.google.com";

	private GodotObject? _playGames;
    private bool _initialized;
	private bool _profileKnownMissing;

    private string _firebaseIdToken => Session.Token;
    private string _firebaseUid => Session.Uid;

	private string _currentPlayGamesPlayerId =
		"";

	private readonly Dictionary<string, string>
		_aliasByPlayGamesPlayerId =
			new(
				StringComparer.Ordinal
			);

	private readonly HashSet<string>
		_aliasMissingPlayerIds =
			new(
				StringComparer.Ordinal
			);

	private readonly HashSet<string>
		_aliasQueryInFlightPlayerIds =
			new(
				StringComparer.Ordinal
			);

	/*
	 * The existing leaderboard service intentionally exposes only rank/score.
	 * We listen to the same native Google Play JSON signal and keep the player
	 * ids privately. This lets the UI replace Google's display names without
	 * changing the proven achievement/leaderboard submission service.
	 */
	private readonly Dictionary<
		string,
		List<string>
	> _topPlayerIdsByLeaderboard =
		new(
			StringComparer.Ordinal
		);

	private readonly Dictionary<
		string,
		string
	> _ownPlayerIdByLeaderboard =
		new(
			StringComparer.Ordinal
		);


	public event Action?
		UsernameRequired;

	public event Action<string>?
		UsernameChanged;

	public event Action<
		bool,
		string
	>? UsernameSaveFinished;

	public event Action?
		AliasDataChanged;

	public event Action<string>?
		LeaderboardIdentitiesUpdated;

	public event Action<string>?
		StatusMessage;


	public bool IsAvailable =>
		OS.GetName() == "Android"
		&& _playGames != null;

	public bool IsFirebaseAuthenticated =>
		IsFirebaseTokenFresh;

	public string CurrentUsername
	{
		get;
		private set;
	} = "";

	public string CurrentPlayGamesPlayerId =>
		_currentPlayGamesPlayerId;

	public bool HasUsername =>
		!string.IsNullOrWhiteSpace(
			CurrentUsername
		);


    public void Initialize()
    {
        if (_initialized) return;
        _initialized = true;
        DebugLog("Firebase", "Initialization started");
        if (OS.GetName() != "Android" || !Engine.HasSingleton(PlayGamesPluginName)) return;
        _playGames = Engine.GetSingleton(PlayGamesPluginName);
        ConnectPlayGamesSignals();
        InitializeNativeAuth();
        var timer = new Timer { Name = "FirebaseReconnect", WaitTime = 15, Autostart = true };
        timer.Timeout += RequestFirebaseSignIn;
        AddChild(timer);
        RequestFirebaseSignIn();
    }

	private void ConnectPlayGamesSignals()
	{
		if (_playGames == null)
			return;

		_playGames.Connect(
			"userAuthenticated",
			Callable.From<bool>(
				OnPlayGamesAuthenticated
			)
		);

		_playGames.Connect(
			"currentPlayerLoaded",
			Callable.From<string>(
				OnCurrentPlayerLoaded
			)
		);

		_playGames.Connect(
			"topScoresLoaded",
			Callable.From<string, string>(
				OnRawTopScoresLoaded
			)
		);

		_playGames.Connect(
			"scoreLoaded",
			Callable.From<string, string>(
				OnRawPlayerScoreLoaded
			)
		);
	}


	private void OnCurrentPlayerLoaded(
		string playerJson)
	{
		if (
			string.IsNullOrWhiteSpace(
				playerJson
			)
		)
		{
			return;
		}

		try
		{
			using JsonDocument document =
				JsonDocument.Parse(
					playerJson
				);

			string playerId =
				GetJsonString(
					document.RootElement,
					"playerId"
				);

			if (
				string.IsNullOrWhiteSpace(
					playerId
				)
			)
			{
				return;
			}

			_currentPlayGamesPlayerId =
				playerId;


			if (HasUsername)
			{
				_aliasByPlayGamesPlayerId[
					playerId
				] =
					CurrentUsername;
			}

			GD.Print(
				"Firebase aliases: current Play Games player id loaded."
			);

			TryLoadCurrentProfile();
		}
		catch (Exception exception)
		{
			GD.PushWarning(
				"Firebase aliases: current player JSON could not be read: "
					+ exception.Message
			);
		}
	}


    public static bool ValidateUsername(string username, out string error) =>
        UsernamePolicy.TryNormalize(username, out _, out _, out error);

    private bool _usernameSaveInFlight;
    public async void SetUsername(string username)
    {
        if (_usernameSaveInFlight) return;
        if (!UsernamePolicy.TryNormalize(username, out var normalized, out _, out var error))
        { UsernameSaveFinished?.Invoke(false, error); return; }
        _usernameSaveInFlight = true;
        try
        {
            await EnsureReadyAsync();
            await LoadCurrentProfileAsync();
            if (HasUsername) { UsernameSaveFinished?.Invoke(true, "Your username is already set."); return; }
            await EnsurePlayerLoadedAsync();
            string uid = _firebaseUid, playerId = _currentPlayGamesPlayerId;
            string body = FirestoreProfileRequests.CreateUsernameCommit(FirebaseConfig.ProjectId, uid, playerId, normalized, DateTime.UtcNow);
            DebugLog("Profile", "Reserving username");
            var response = await Session.RequestAsync((token, refreshed) => SendRawHttpAsync("ReserveUsername", refreshed, FirestoreCommitUrl,
                ["Authorization: Bearer " + token, "Content-Type: application/json"], HttpClient.Method.Post, body, _lifetime.Token), retryNetwork: false, expectedUid: uid);
            if (uid != _firebaseUid) throw new FirebaseRequestException(409, "AUTH_USER_CHANGED");
            if (response.Status is >= 200 and < 300)
            {
                _profileKnownMissing = false; CurrentUsername = normalized;
                _aliasByPlayGamesPlayerId[playerId] = normalized; _aliasMissingPlayerIds.Remove(playerId);
                UsernameSaveFinished?.Invoke(true, "Username saved.");
                UsernameChanged?.Invoke(normalized); AliasDataChanged?.Invoke();
                return;
            }
            var failure = FirebaseRequestException.Parse(response);
            bool conflict = response.Status == 409 || failure.Code is "ALREADY_EXISTS" or "FAILED_PRECONDITION";
            if (conflict || failure.Kind is FirebaseFailureKind.Network or FirebaseFailureKind.Service)
            {
                // Ambiguous commit: read the canonical profile, never replay a possibly successful create.
                _profileKnownMissing = false;
                await LoadCurrentProfileAsync();
                if (HasUsername) { UsernameSaveFinished?.Invoke(true, "Username saved."); return; }
            }
            UsernameSaveFinished?.Invoke(false, conflict ? "That username is already taken." : failure.PlayerMessage);
        }
        catch (Exception e)
        { LogFailure("CreateUsername", e); if (!_exiting) UsernameSaveFinished?.Invoke(false, PlayerError(e)); }
        finally { _usernameSaveInFlight = false; }
    }


	// ==================================================
	// LEADERBOARD PLAYER IDS
	// ==================================================

	private void OnRawTopScoresLoaded(
		string leaderboardId,
		string scoresJson)
	{
		try
		{
			List<string> playerIds =
				[];

			if (
				!string.IsNullOrWhiteSpace(
					scoresJson
				)
				&& scoresJson != "null"
			)
			{
				using JsonDocument document =
					JsonDocument.Parse(
						scoresJson
					);

				if (
					document.RootElement.ValueKind
						== JsonValueKind.Object
					&& document.RootElement.TryGetProperty(
						"scores",
						out JsonElement scores
					)
					&& scores.ValueKind
						== JsonValueKind.Array
				)
				{
					foreach (
						JsonElement score
							in scores.EnumerateArray()
					)
					{
						playerIds.Add(
							GetScoreHolderPlayerId(
								score
							)
						);
					}
				}
			}

			_topPlayerIdsByLeaderboard[
				leaderboardId
			] =
				playerIds;

			LeaderboardIdentitiesUpdated?.Invoke(
				leaderboardId
			);

			EnsureAliases(
				playerIds
			);
		}
		catch (Exception exception)
		{
			GD.PushWarning(
				"Firebase aliases: could not parse leaderboard player ids: "
					+ exception.Message
			);
		}
	}


	private void OnRawPlayerScoreLoaded(
		string leaderboardId,
		string scoreJson)
	{
		try
		{
			string playerId =
				"";

			if (
				!string.IsNullOrWhiteSpace(
					scoreJson
				)
				&& scoreJson != "null"
			)
			{
				using JsonDocument document =
					JsonDocument.Parse(
						scoreJson
					);

				if (
					document.RootElement.ValueKind
						== JsonValueKind.Object
				)
				{
					playerId =
						GetScoreHolderPlayerId(
							document.RootElement
						);
				}
			}

			if (
				string.IsNullOrWhiteSpace(
					playerId
				)
			)
			{
				playerId =
					_currentPlayGamesPlayerId;
			}

			_ownPlayerIdByLeaderboard[
				leaderboardId
			] =
				playerId;

			LeaderboardIdentitiesUpdated?.Invoke(
				leaderboardId
			);

			EnsureAliases(
				[
					playerId
				]
			);
		}
		catch (Exception exception)
		{
			GD.PushWarning(
				"Firebase aliases: could not parse own leaderboard player id: "
					+ exception.Message
			);
		}
	}


	public IReadOnlyList<string>
		GetTopPlayerIds(
			string leaderboardId)
	{
		if (
			_topPlayerIdsByLeaderboard.TryGetValue(
				leaderboardId,
				out List<string>? ids
			)
		)
		{
			return ids;
		}

		return Array.Empty<string>();
	}


	public string GetOwnPlayerId(
		string leaderboardId)
	{
		if (
			_ownPlayerIdByLeaderboard.TryGetValue(
				leaderboardId,
				out string? playerId
			)
		)
		{
			return playerId;
		}

		return _currentPlayGamesPlayerId;
	}


	public string? GetAlias(
		string playGamesPlayerId)
	{
		if (
			string.IsNullOrWhiteSpace(
				playGamesPlayerId
			)
		)
		{
			return null;
		}

		if (
			_aliasByPlayGamesPlayerId.TryGetValue(
				playGamesPlayerId,
				out string? alias
			)
		)
		{
			return alias;
		}

		return null;
	}


	public async void EnsureAliases(
		IEnumerable<string> playerIds)
	{
		List<string> unresolved =
			playerIds
				.Where(
					playerId =>
						!string.IsNullOrWhiteSpace(
							playerId
						)
				)
				.Distinct(
					StringComparer.Ordinal
				)
				.Where(
					playerId =>
						!_aliasByPlayGamesPlayerId
							.ContainsKey(
								playerId
							)
						&& !_aliasMissingPlayerIds
							.Contains(
								playerId
							)
						&& !_aliasQueryInFlightPlayerIds
							.Contains(
								playerId
							)
				)
				.Take(
					25
				)
				.ToList();

		if (unresolved.Count == 0)
			return;

        try { await EnsureReadyAsync(); }
        catch (Exception) { return; }
        // Other callers may have resolved/started these aliases while login was pending.
        unresolved.RemoveAll(id => _aliasByPlayGamesPlayerId.ContainsKey(id) || _aliasMissingPlayerIds.Contains(id) || _aliasQueryInFlightPlayerIds.Contains(id));
        if (unresolved.Count == 0) return;

		foreach (
			string playerId
				in unresolved
		)
		{
			_aliasQueryInFlightPlayerIds.Add(
				playerId
			);
		}

		object[] values =
			unresolved
				.Select(
					playerId =>
						(object)
							StringValue(
								playerId
							)
				)
				.ToArray();

		object bodyObject =
			new
			{
				structuredQuery =
					new
					{
						from =
							new[]
							{
								new
								{
									collectionId =
										"profiles"
								}
							},

						where =
							new
							{
								fieldFilter =
									new
									{
										field =
											new
											{
												fieldPath =
													"playGamesPlayerId"
											},

										op =
											"IN",

										value =
											new
											{
												arrayValue =
													new
													{
														values
													}
											}
									}
							}
					}
			};

		SendHttpRequest(
			FirestoreRunQueryUrl,
			HttpClient.Method.Post,
			JsonSerializer.Serialize(
				bodyObject
			),
			(responseCode, responseBody) =>
			{
				foreach (
					string playerId
						in unresolved
				)
				{
					_aliasQueryInFlightPlayerIds.Remove(
						playerId
					);
				}

				if (
					responseCode < 200
					|| responseCode >= 300
				)
				{
					GD.PushWarning(
						"Firebase aliases: alias query failed HTTP "
							+ responseCode
							+ ": "
							+ ReadFirebaseError(
								responseBody
							)
					);

					return;
				}

				HashSet<string> found =
					new(
						StringComparer.Ordinal
					);

				try
				{
					using JsonDocument document =
						JsonDocument.Parse(
							responseBody
						);

					if (
						document.RootElement.ValueKind
							== JsonValueKind.Array
					)
					{
						foreach (
							JsonElement result
								in document.RootElement
									.EnumerateArray()
						)
						{
							if (
								!result.TryGetProperty(
									"document",
									out JsonElement firestoreDocument
								)
								|| !firestoreDocument.TryGetProperty(
									"fields",
									out JsonElement fields
								)
							)
							{
								continue;
							}

							string playerId =
								GetFirestoreString(
									fields,
									"playGamesPlayerId"
								);

							string username =
								GetFirestoreString(
									fields,
									"username"
								);

							if (
								string.IsNullOrWhiteSpace(
									playerId
								)
								|| string.IsNullOrWhiteSpace(
									username
								)
							)
							{
								continue;
							}

							_aliasByPlayGamesPlayerId[
								playerId
							] =
								username;

							found.Add(
								playerId
							);
						}
					}

					foreach (
						string playerId
							in unresolved
					)
					{
						if (
							!found.Contains(
								playerId
							)
						)
						{
							_aliasMissingPlayerIds.Add(
								playerId
							);
						}
					}

					AliasDataChanged?.Invoke();
				}
				catch (Exception exception)
				{
					GD.PushWarning(
						"Firebase aliases: alias query response could not be parsed: "
							+ exception.Message
					);
				}
			}
		);
	}


	// ==================================================
	// HTTP / FIRESTORE HELPERS
	// ==================================================

    private bool IsFirebaseTokenFresh => Session.IsFresh;

	private static string FirestoreDocumentsBaseUrl =>
		"https://firestore.googleapis.com/v1/projects/"
			+ FirebaseConfig.ProjectId
			+ "/databases/(default)/documents";


	private static string FirestoreCommitUrl =>
		FirestoreDocumentsBaseUrl
			+ ":commit";


	private static string FirestoreRunQueryUrl =>
		FirestoreDocumentsBaseUrl
			+ ":runQuery";


	private static Dictionary<string, object>
		StringValue(
			string value)
	{
		return new Dictionary<string, object>
		{
			[
				"stringValue"
			] =
				value
		};
	}


	private static Dictionary<string, object>
		TimestampValue(
			string value)
	{
		return new Dictionary<string, object>
		{
			[
				"timestampValue"
			] =
				value
		};
	}


    private async void SendHttpRequest(string url, HttpClient.Method method,
        string requestBody, Action<long, string> completed)
    {
        FirebaseHttpResponse response;
        try
        {
            response = await Session.RequestAsync((token, refreshed) => SendRawHttpAsync(
                url == FirestoreCommitUrl ? "ReserveUsername" : method == HttpClient.Method.Patch ? "SubmitLeaderboardScores" : "QueryFirestore", refreshed, url,
                ["Authorization: Bearer " + token, "Content-Type: application/json"], method, requestBody, _lifetime.Token),
                retryNetwork: method == HttpClient.Method.Get || method == HttpClient.Method.Patch || url == FirestoreRunQueryUrl,
                expectedUid: method == HttpClient.Method.Patch ? _firebaseUid : null);
        }
        catch (Exception e)
        {
            LogFailure("Firestore request", e);
            response = e is FirebaseRequestException failure
                ? new(failure.Status, JsonSerializer.Serialize(new { error = new { status = failure.Code, message = failure.Detail } }))
                : new(0, "");
        }
        if (!_exiting && IsInsideTree()) completed(response.Status, response.Body);
    }

	private void ReportStatus(
		string message)
	{
        DebugLog("Profile", message);
        StatusMessage?.Invoke("Profile unavailable. Please try again.");
	}


	private static string GetScoreHolderPlayerId(
		JsonElement score)
	{
		if (
			!score.TryGetProperty(
				"scoreHolder",
				out JsonElement holder
			)
			|| holder.ValueKind
				!= JsonValueKind.Object
		)
		{
			return "";
		}

		return GetJsonString(
			holder,
			"playerId"
		);
	}


	private static string GetFirestoreString(
		JsonElement fields,
		string fieldName)
	{
		if (
			!fields.TryGetProperty(
				fieldName,
				out JsonElement field
			)
			|| field.ValueKind
				!= JsonValueKind.Object
			|| !field.TryGetProperty(
				"stringValue",
				out JsonElement value
			)
		)
		{
			return "";
		}

        string text = value.GetString() ?? "";
        if (fieldName == "username" && UsernamePolicy.TryNormalize(text, out var normalized, out _, out _, existing: true))
            return normalized;
        return text;
	}


	private static string GetJsonString(
		JsonElement element,
		string propertyName)
	{
		if (
			!element.TryGetProperty(
				propertyName,
				out JsonElement property
			)
			|| property.ValueKind
				== JsonValueKind.Null
		)
		{
			return "";
		}

		return property.ValueKind
				== JsonValueKind.String
			? property.GetString()
				?? ""
			: property.ToString();
	}


    private static string ReadFirebaseError(string json) => FirebaseRequestException.SafeDiagnostic(new(0, json));

	private static long CurrentUnixTime()
	{
		return (long)
			Time.GetUnixTimeFromSystem();
	}
}
