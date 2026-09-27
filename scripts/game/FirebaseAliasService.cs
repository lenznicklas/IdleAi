using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

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

	private const double ServerAuthTimeoutSeconds =
		15.0;

	private const int MinimumUsernameLength =
		3;

	private const int MaximumUsernameLength =
		16;

	private static readonly Regex UsernamePattern =
		new(
			"^[A-Za-z][A-Za-z0-9_]{2,15}$",
			RegexOptions.CultureInvariant
		);

	private static readonly string[] ReservedPrefixes =
	[
		"admin",
		"administrator",
		"moderator",
		"official",
		"support",
		"developer",
		"idleai",
		"idle_ai",
		"firebase",
		"google",
		"anonymous"
	];

	private GodotObject? _playGames;

	private Timer? _tokenRefreshTimer;

	private Timer? _serverAuthTimeoutTimer;

	private bool _initialized;

	private bool _playGamesAuthenticated;

	private bool _firebaseSignInRequested;

	private bool _profileRequestInFlight;

	private bool _profileKnownMissing;

	private string _firebaseIdToken =
		"";

	private string _firebaseUid =
		"";

	private long _firebaseTokenExpiresAtUnix;

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
		if (_initialized)
			return;

		_initialized =
			true;

		if (OS.GetName() != "Android")
		{
			GD.Print(
				"Firebase aliases: Android-only integration disabled in editor/desktop."
			);

			return;
		}

		if (!Engine.HasSingleton(PlayGamesPluginName))
		{
			GD.PushWarning(
				"Firebase aliases: GodotPlayGameServices singleton was not found."
			);

			return;
		}

		_playGames =
			Engine.GetSingleton(
				PlayGamesPluginName
			);

		if (_playGames == null)
			return;

		GD.Print(
			"Firebase aliases: project=",
			FirebaseConfig.ProjectId,
			" | projectNumber=",
			FirebaseConfig.ProjectNumber,
			" | PlayGamesWebClient=",
			FirebaseConfig.PlayGamesWebClientId
		);

		ConnectPlayGamesSignals();

		_tokenRefreshTimer =
			new Timer
			{
				Name =
					"FirebaseAliasTokenRefresh",

				OneShot =
					true,

				WaitTime =
					3000.0
			};

		_tokenRefreshTimer.Timeout +=
			RequestFirebaseSignIn;

		AddChild(
			_tokenRefreshTimer
		);

		_serverAuthTimeoutTimer =
			new Timer
			{
				Name =
					"FirebaseServerAuthTimeout",

				OneShot =
					true,

				WaitTime =
					ServerAuthTimeoutSeconds
			};

		_serverAuthTimeoutTimer.Timeout +=
			OnServerAuthTimeout;

		AddChild(
			_serverAuthTimeoutTimer
		);

		/*
		 * GooglePlayGamesAchievementService also checks authentication. It is
		 * fine for both consumers to listen to the same native singleton.
		 */
		_playGames.Call(
			"isAuthenticated"
		);
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
			"serverSideAccessRequested",
			Callable.From<string>(
				OnServerSideAccessRequested
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


	private void OnPlayGamesAuthenticated(
		bool authenticated)
	{
		_playGamesAuthenticated =
			authenticated;

		if (!authenticated)
		{
			GD.Print(
				"Firebase aliases: waiting for Google Play Games authentication."
			);

			return;
		}

		GD.Print(
			"Firebase aliases: Google Play Games authenticated."
		);

		try
		{
			_playGames?.Call(
				"loadCurrentPlayer",
				true
			);
		}
		catch (Exception exception)
		{
			GD.PushWarning(
				"Firebase aliases: could not request current Play Games player: "
					+ exception.Message
			);
		}

		RequestFirebaseSignIn();
	}


	private void RequestFirebaseSignIn()
	{
		if (
			!_playGamesAuthenticated
			|| _playGames == null
			|| _firebaseSignInRequested
		)
		{
			return;
		}

		_firebaseSignInRequested =
			true;

		try
		{
			/*
			 * The Play Games plugin returns an OAuth 2.0 server auth code via
			 * serverSideAccessRequested. This is the code Firebase's
			 * PlayGamesAuthProvider expects.
			 */
			_playGames.Call(
				"requestServerSideAccess",
				FirebaseConfig.PlayGamesWebClientId,
				false
			);

			_serverAuthTimeoutTimer?.Start();

			GD.Print(
				"Firebase aliases: requested Play Games server auth code."
			);
		}
		catch (Exception exception)
		{
			_serverAuthTimeoutTimer?.Stop();

			_firebaseSignInRequested =
				false;

			ReportStatus(
				"Firebase sign-in could not start: "
					+ exception.Message
			);
		}
	}


	private void OnServerAuthTimeout()
	{
		if (!_firebaseSignInRequested)
		{
			return;
		}

		_firebaseSignInRequested =
			false;

		ReportStatus(
			"Firebase sign-in timed out while requesting Play Games server access. "
			+ "Check the Play Games Game server credential/Web client ID and try again."
		);
	}


	private void OnServerSideAccessRequested(
		string serverAuthCode)
	{
		_serverAuthTimeoutTimer?.Stop();

		_firebaseSignInRequested =
			false;

		if (
			string.IsNullOrWhiteSpace(
				serverAuthCode
			)
		)
		{
			ReportStatus(
				"Firebase sign-in failed: Play Games returned no server auth code."
			);

			return;
		}

		string postBody =
			"code="
			+ Uri.EscapeDataString(
				serverAuthCode
			)
			+ "&providerId="
			+ PlayGamesProviderId;

		string requestBody =
			JsonSerializer.Serialize(
				new
				{
					postBody,
					requestUri =
						FirebaseConfig.AuthRequestUri,
					returnIdpCredential =
						true,
					returnSecureToken =
						true
				}
			);

		string url =
			"https://identitytoolkit.googleapis.com/v1/accounts:signInWithIdp?key="
			+ Uri.EscapeDataString(
				FirebaseConfig.WebApiKey
			);

		SendHttpRequest(
			url,
			[
				"Content-Type: application/json"
			],
			HttpClient.Method.Post,
			requestBody,
			OnFirebaseSignInCompleted
		);
	}


	private void OnFirebaseSignInCompleted(
		long responseCode,
		string responseBody)
	{
		if (
			responseCode < 200
			|| responseCode >= 300
		)
		{
			ReportStatus(
				"Firebase sign-in failed (HTTP "
					+ responseCode
					+ "): "
					+ ReadFirebaseError(
						responseBody
					)
			);

			return;
		}

		try
		{
			using JsonDocument document =
				JsonDocument.Parse(
					responseBody
				);

			JsonElement root =
				document.RootElement;

			_firebaseIdToken =
				GetJsonString(
					root,
					"idToken"
				);

			_firebaseUid =
				GetJsonString(
					root,
					"localId"
				);

			long expiresInSeconds =
				3600;

			string expiresText =
				GetJsonString(
					root,
					"expiresIn"
				);

			if (
				long.TryParse(
					expiresText,
					out long parsedExpires
				)
				&& parsedExpires > 0
			)
			{
				expiresInSeconds =
					parsedExpires;
			}

			_firebaseTokenExpiresAtUnix =
				CurrentUnixTime()
				+ Math.Max(
					60,
					expiresInSeconds - 60
				);

			if (
				string.IsNullOrWhiteSpace(
					_firebaseIdToken
				)
				|| string.IsNullOrWhiteSpace(
					_firebaseUid
				)
			)
			{
				throw new InvalidOperationException(
					"Firebase response did not contain idToken/localId."
				);
			}

			_tokenRefreshTimer?.Start(
				Math.Max(
					60,
					expiresInSeconds - 300
				)
			);

			GD.Print(
				"Firebase aliases: Firebase Authentication succeeded."
			);

			AliasDataChanged?.Invoke();

			TryLoadCurrentProfile();
		}
		catch (Exception exception)
		{
			_firebaseIdToken =
				"";

			_firebaseUid =
				"";

			ReportStatus(
				"Firebase sign-in response could not be read: "
					+ exception.Message
			);
		}
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


	private void TryLoadCurrentProfile()
	{
		if (
			_profileRequestInFlight
			|| _profileKnownMissing
			|| HasUsername
			|| !IsFirebaseTokenFresh
			|| string.IsNullOrWhiteSpace(
				_firebaseUid
			)
			|| string.IsNullOrWhiteSpace(
				_currentPlayGamesPlayerId
			)
		)
		{
			return;
		}

		_profileRequestInFlight =
			true;

		string url =
			FirestoreDocumentsBaseUrl
			+ "/profiles/"
			+ Uri.EscapeDataString(
				_firebaseUid
			);

		SendHttpRequest(
			url,
			FirebaseHeaders,
			HttpClient.Method.Get,
			"",
			(responseCode, body) =>
			{
				_profileRequestInFlight =
					false;

				if (responseCode == 404)
				{
					_profileKnownMissing =
						true;

					UsernameRequired?.Invoke();

					return;
				}

				if (
					responseCode < 200
					|| responseCode >= 300
				)
				{
					ReportStatus(
						"Could not load Idle AI profile (HTTP "
							+ responseCode
							+ "): "
							+ ReadFirebaseError(
								body
							)
					);

					return;
				}

				try
				{
					using JsonDocument document =
						JsonDocument.Parse(
							body
						);

					JsonElement fields =
						document.RootElement
							.GetProperty(
								"fields"
							);

					string username =
						GetFirestoreString(
							fields,
							"username"
						);

					string playGamesPlayerId =
						GetFirestoreString(
							fields,
							"playGamesPlayerId"
						);

					if (
						string.IsNullOrWhiteSpace(
							username
						)
					)
					{
						_profileKnownMissing =
							true;

						UsernameRequired?.Invoke();

						return;
					}

					CurrentUsername =
						username;

					if (
						!string.IsNullOrWhiteSpace(
							playGamesPlayerId
						)
					)
					{
						_aliasByPlayGamesPlayerId[
							playGamesPlayerId
						] =
							username;
					}

					_aliasByPlayGamesPlayerId[
						_currentPlayGamesPlayerId
					] =
						username;

					UsernameChanged?.Invoke(
						username
					);

					AliasDataChanged?.Invoke();

					GD.Print(
						"Firebase aliases: Idle AI username loaded: ",
						username
					);
				}
				catch (Exception exception)
				{
					ReportStatus(
						"Idle AI profile could not be parsed: "
							+ exception.Message
					);
				}
			}
		);
	}


	public static bool ValidateUsername(
		string username,
		out string error)
	{
		string trimmed =
			username.Trim();

		if (
			trimmed.Length
				< MinimumUsernameLength
			|| trimmed.Length
				> MaximumUsernameLength
		)
		{
			error =
				"Use 3-16 characters.";

			return false;
		}

		if (
			!UsernamePattern.IsMatch(
				trimmed
			)
		)
		{
			error =
				"Start with a letter. Use only letters, numbers and _.";

			return false;
		}

		string lower =
			trimmed.ToLowerInvariant();

		foreach (
			string reserved
				in ReservedPrefixes
		)
		{
			if (
				lower.StartsWith(
					reserved,
					StringComparison.Ordinal
				)
			)
			{
				error =
					"This username is reserved.";

				return false;
			}
		}

		error =
			"";

		return true;
	}


	public void SetUsername(
		string username)
	{
		string trimmed =
			username.Trim();

		if (
			!ValidateUsername(
				trimmed,
				out string validationError
			)
		)
		{
			UsernameSaveFinished?.Invoke(
				false,
				validationError
			);

			return;
		}

		if (HasUsername)
		{
			UsernameSaveFinished?.Invoke(
				false,
				"Your Idle AI username is already set."
			);

			return;
		}

		if (
			!IsFirebaseTokenFresh
			|| string.IsNullOrWhiteSpace(
				_firebaseUid
			)
			|| string.IsNullOrWhiteSpace(
				_currentPlayGamesPlayerId
			)
		)
		{
			RequestFirebaseSignIn();

			UsernameSaveFinished?.Invoke(
				false,
				"Firebase is not connected yet. Wait for the connection message, then try again."
			);

			return;
		}

		string lower =
			trimmed.ToLowerInvariant();

		string profileDocumentName =
			FirestoreDocumentName(
				"profiles",
				_firebaseUid
			);

		string usernameDocumentName =
			FirestoreDocumentName(
				"usernames",
				lower
			);

		string timestamp =
			DateTime.UtcNow
				.ToString(
					"O"
				);

		Dictionary<string, object>
			profileFields =
				new()
				{
					[
						"uid"
					] =
						StringValue(
							_firebaseUid
						),

					[
						"playGamesPlayerId"
					] =
						StringValue(
							_currentPlayGamesPlayerId
						),

					[
						"username"
					] =
						StringValue(
							trimmed
						),

					[
						"usernameLower"
					] =
						StringValue(
							lower
						),

					[
						"createdAt"
					] =
						TimestampValue(
							timestamp
						)
				};

		Dictionary<string, object>
			usernameFields =
				new()
				{
					[
						"uid"
					] =
						StringValue(
							_firebaseUid
						),

					[
						"username"
					] =
						StringValue(
							trimmed
						),

					[
						"usernameLower"
					] =
						StringValue(
							lower
						)
				};

		/*
		 * Both documents are created in one Firestore commit and both use
		 * exists=false preconditions. The username document therefore acts as
		 * a case-insensitive unique reservation without a check-then-write
		 * race condition.
		 */
		object request =
			new
			{
				writes =
					new object[]
					{
						new
						{
							update =
								new
								{
									name =
										usernameDocumentName,

									fields =
										usernameFields
								},

							currentDocument =
								new
								{
									exists =
										false
								}
						},

						new
						{
							update =
								new
								{
									name =
										profileDocumentName,

									fields =
										profileFields
								},

							currentDocument =
								new
								{
									exists =
										false
								}
						}
					}
			};

		string body =
			JsonSerializer.Serialize(
				request
			);

		SendHttpRequest(
			FirestoreCommitUrl,
			FirebaseHeaders,
			HttpClient.Method.Post,
			body,
			(responseCode, responseBody) =>
			{
				if (
					responseCode >= 200
					&& responseCode < 300
				)
				{
					_profileKnownMissing =
						false;

					CurrentUsername =
						trimmed;

					_aliasByPlayGamesPlayerId[
						_currentPlayGamesPlayerId
					] =
						trimmed;

					_aliasMissingPlayerIds.Remove(
						_currentPlayGamesPlayerId
					);

					UsernameSaveFinished?.Invoke(
						true,
						"Username saved."
					);

					UsernameChanged?.Invoke(
						trimmed
					);

					AliasDataChanged?.Invoke();

					GD.Print(
						"Firebase aliases: username created: ",
						trimmed
					);

					return;
				}

				string firebaseError =
					ReadFirebaseError(
						responseBody
					);

				bool alreadyExists =
					responseBody.Contains(
						"ALREADY_EXISTS",
						StringComparison.OrdinalIgnoreCase
					)
					|| responseBody.Contains(
						"FAILED_PRECONDITION",
						StringComparison.OrdinalIgnoreCase
					)
					|| responseCode == 409;

				UsernameSaveFinished?.Invoke(
					false,
					alreadyExists
						? "That username is already taken."
						: "Could not save username: "
							+ firebaseError
				);
			}
		);
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


	public void EnsureAliases(
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

		if (!IsFirebaseTokenFresh)
		{
			RequestFirebaseSignIn();
			return;
		}

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
			FirebaseHeaders,
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

	private bool IsFirebaseTokenFresh =>
		!string.IsNullOrWhiteSpace(
			_firebaseIdToken
		)
		&& CurrentUnixTime()
			< _firebaseTokenExpiresAtUnix;


	private string[] FirebaseHeaders =>
	[
		"Authorization: Bearer "
			+ _firebaseIdToken,

		"Content-Type: application/json"
	];


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


	private static string FirestoreDocumentName(
		string collection,
		string documentId)
	{
		return "projects/"
			+ FirebaseConfig.ProjectId
			+ "/databases/(default)/documents/"
			+ collection
			+ "/"
			+ documentId;
	}


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


	private void SendHttpRequest(
		string url,
		string[] headers,
		HttpClient.Method method,
		string requestBody,
		Action<long, string> completed)
	{
		HttpRequest request =
			new()
			{
				UseThreads =
					true
			};

		AddChild(
			request
		);

		request.RequestCompleted +=
			(
				long result,
				long responseCode,
				string[] responseHeaders,
				byte[] body
			) =>
			{
				string text =
					Encoding.UTF8.GetString(
						body
					);

				completed(
					responseCode,
					text
				);

				request.QueueFree();
			};

		Error error =
			request.Request(
				url,
				headers,
				method,
				requestBody
			);

		if (error == Error.Ok)
			return;

		request.QueueFree();

		completed(
			0,
			"Godot HTTPRequest error: "
				+ error
		);
	}


	private void ReportStatus(
		string message)
	{
		GD.PushWarning(
			message
		);

		StatusMessage?.Invoke(
			message
		);
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

		return value.GetString()
			?? "";
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


	private static string ReadFirebaseError(
		string json)
	{
		if (
			string.IsNullOrWhiteSpace(
				json
			)
		)
		{
			return "Unknown Firebase error.";
		}

		try
		{
			using JsonDocument document =
				JsonDocument.Parse(
					json
				);

			if (
				document.RootElement.TryGetProperty(
					"error",
					out JsonElement error
				)
			)
			{
				if (
					error.ValueKind
						== JsonValueKind.Object
					&& error.TryGetProperty(
						"message",
						out JsonElement message
					)
				)
				{
					return message.GetString()
						?? message.ToString();
				}

				return error.ToString();
			}
		}
		catch
		{
			// Fall through to a bounded plain-text error below.
		}

		return json.Length <= 240
			? json
			: json[
				..240
			];
	}


	private static long CurrentUnixTime()
	{
		return (long)
			Time.GetUnixTimeFromSystem();
	}
}
