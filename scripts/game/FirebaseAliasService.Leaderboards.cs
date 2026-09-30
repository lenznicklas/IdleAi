using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace IdleAi;


public enum IdleAiLeaderboardKind
{
	HighestTotalLevel,
	MostPrestiges
}


public sealed record IdleAiLeaderboardEntry(
	long Rank,
	string Username,
	long Score,
	bool IsOwn
);


/*
 * Firebase-backed in-game leaderboard.
 *
 * Why this exists:
 * Google Play top-score entries can legally arrive without a usable
 * scoreHolder/playerId. In that case a Google row cannot be reliably joined
 * to an Idle AI Firebase username. The old implementation replaced those
 * unknown rows with the invented label "Anonymous AI".
 *
 * The in-game leaderboard now has one authoritative identity source:
 * Firebase /profiles. Google Play leaderboards remain enabled and scores
 * continue to be submitted there separately.
 *
 * Existing profile documents are compatible. Missing score fields are read
 * as 0 until that player launches a build containing this sync code once.
 */
public sealed partial class FirebaseAliasService
{
	private const int InGameLeaderboardTopCount =
		25;

	private long _leaderboardLocalTotalLevel;

	private long _leaderboardLocalPrestiges;

	private long _leaderboardLastSyncedTotalLevel =
		long.MinValue;

	private long _leaderboardLastSyncedPrestiges =
		long.MinValue;

	private bool _leaderboardSyncInFlight;

	private bool _leaderboardLoadInFlight;


	public event Action<
		IdleAiLeaderboardKind,
		IReadOnlyList<IdleAiLeaderboardEntry>,
		IdleAiLeaderboardEntry?
	>? InGameLeaderboardLoaded;


	public event Action<
		IdleAiLeaderboardKind,
		string
	>? InGameLeaderboardLoadFailed;


	// ==================================================
	// LOCAL -> FIREBASE SCORE SYNC
	// ==================================================

	public void SyncInGameLeaderboardScores(
		long totalLevel,
		long prestigeCount)
	{
		_leaderboardLocalTotalLevel =
			Math.Max(
				0,
				totalLevel
			);

		_leaderboardLocalPrestiges =
			Math.Max(
				0,
				prestigeCount
			);

		if (
			_leaderboardSyncInFlight
			|| !IsFirebaseTokenFresh
			|| !HasUsername
			|| string.IsNullOrWhiteSpace(
				_firebaseUid
			)
		)
		{
			return;
		}

		if (
			_leaderboardLastSyncedTotalLevel
				== _leaderboardLocalTotalLevel
			&& _leaderboardLastSyncedPrestiges
				== _leaderboardLocalPrestiges
		)
		{
			return;
		}

		_leaderboardSyncInFlight =
			true;

		/*
		 * The profile document is already the canonical public mapping used by
		 * FirebaseAliasService (uid + playGamesPlayerId + username).
		 * Add/update only leaderboard fields; identity fields are untouched.
		 */
		string url =
			FirestoreDocumentsBaseUrl
			+ "/profiles/"
			+ Uri.EscapeDataString(
				_firebaseUid
			)
			+ "?updateMask.fieldPaths=highestTotalLevel"
			+ "&updateMask.fieldPaths=prestigeCount"
			+ "&updateMask.fieldPaths=leaderboardUpdatedAt";

		string now =
			DateTime.UtcNow.ToString(
				"O"
			);

		object body =
			new
			{
				fields =
					new Dictionary<string, object>
					{
						[
							"highestTotalLevel"
						] =
							IntegerValueForLeaderboard(
								_leaderboardLocalTotalLevel
							),

						[
							"prestigeCount"
						] =
							IntegerValueForLeaderboard(
								_leaderboardLocalPrestiges
							),

						[
							"leaderboardUpdatedAt"
						] =
							TimestampValue(
								now
							)
					}
			};

		long submittedLevel =
			_leaderboardLocalTotalLevel;

		long submittedPrestiges =
			_leaderboardLocalPrestiges;

		SendHttpRequest(
			url,
			FirebaseHeaders,
			HttpClient.Method.Patch,
			JsonSerializer.Serialize(
				body
			),
			(responseCode, responseBody) =>
			{
				_leaderboardSyncInFlight =
					false;

				if (
					responseCode >= 200
					&& responseCode < 300
				)
				{
					_leaderboardLastSyncedTotalLevel =
						submittedLevel;

					_leaderboardLastSyncedPrestiges =
						submittedPrestiges;

					return;
				}

				GD.PushWarning(
					"Firebase leaderboard score sync failed HTTP "
						+ responseCode
						+ ": "
						+ ReadFirebaseError(
							responseBody
						)
				);
			}
		);
	}


	// ==================================================
	// FIREBASE TOP 25
	// ==================================================

	public bool RequestInGameLeaderboard(
		IdleAiLeaderboardKind kind)
	{
		if (_leaderboardLoadInFlight)
			return true;

		if (
			!IsFirebaseTokenFresh
			|| string.IsNullOrWhiteSpace(
				_firebaseUid
			)
		)
		{
			RequestFirebaseSignIn();

			InGameLeaderboardLoadFailed?.Invoke(
				kind,
				"Firebase is still connecting. Try Refresh in a moment."
			);

			return false;
		}

		_leaderboardLoadInFlight =
			true;

		/*
		 * Read the existing public profile registry instead of Google display names.
		 *
		 * For the current closed-test scale this intentionally reads all
		 * registered profiles so:
		 * - old documents without score fields are still visible as score 0;
		 * - exact rank can be calculated even when YOU are outside Top 25;
		 * - ties receive the same competition rank.
		 *
		 * If this grows to many thousands of users, move ranking to a trusted
		 * backend / Cloud Function. The UI contract does not need to change.
		 */
		object queryBody =
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
							}
					}
			};

		SendHttpRequest(
			FirestoreRunQueryUrl,
			FirebaseHeaders,
			HttpClient.Method.Post,
			JsonSerializer.Serialize(
				queryBody
			),
			(responseCode, responseBody) =>
			{
				_leaderboardLoadInFlight =
					false;

				if (
					responseCode < 200
					|| responseCode >= 300
				)
				{
					string message =
						"Leaderboard database read failed (HTTP "
						+ responseCode
						+ "): "
						+ ReadFirebaseError(
							responseBody
						);

					GD.PushWarning(
						message
					);

					InGameLeaderboardLoadFailed?.Invoke(
						kind,
						message
					);

					return;
				}

				try
				{
					List<LeaderboardDatabaseRow> rows =
						ParseLeaderboardDatabaseRows(
							responseBody,
							kind
						);

					/*
					 * Always trust the current device's live value for YOU. The
					 * Firestore PATCH can still be in flight when the player opens
					 * the overlay.
					 */
					MergeLiveOwnScore(
						rows,
						kind
					);

					PublishLeaderboardRows(
						rows,
						kind
					);
				}
				catch (Exception exception)
				{
					string message =
						"Leaderboard database response could not be read: "
						+ exception.Message;

					GD.PushWarning(
						message
					);

					InGameLeaderboardLoadFailed?.Invoke(
						kind,
						message
					);
				}
			}
		);

		return true;
	}


	private List<LeaderboardDatabaseRow>
		ParseLeaderboardDatabaseRows(
			string json,
			IdleAiLeaderboardKind kind)
	{
		List<LeaderboardDatabaseRow> rows =
			[];

		if (
			string.IsNullOrWhiteSpace(
				json
			)
		)
		{
			return rows;
		}

		using JsonDocument document =
			JsonDocument.Parse(
				json
			);

		if (
			document.RootElement.ValueKind
				!= JsonValueKind.Array
		)
		{
			return rows;
		}

		foreach (
			JsonElement result
				in document.RootElement.EnumerateArray()
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

			string username =
				GetFirestoreString(
					fields,
					"username"
				);

			if (
				string.IsNullOrWhiteSpace(
					username
				)
			)
			{
				/*
				 * Never create a fake identity. A database row without a real
				 * Idle AI username is not a renderable leaderboard player.
				 */
				continue;
			}

			string uid =
				GetFirestoreString(
					fields,
					"uid"
				);

			long score =
				GetLeaderboardScoreFromFields(
					fields,
					kind
				);

			rows.Add(
				new LeaderboardDatabaseRow(
					uid,
					username,
					score
				)
			);
		}

		return rows;
	}


	private void MergeLiveOwnScore(
		List<LeaderboardDatabaseRow> rows,
		IdleAiLeaderboardKind kind)
	{
		if (!HasUsername)
			return;

		long localScore =
			GetLocalScore(
				kind
			);

		LeaderboardDatabaseRow? own =
			rows.FirstOrDefault(
				row =>
					(
						!string.IsNullOrWhiteSpace(
							_firebaseUid
						)
						&& string.Equals(
							row.Uid,
							_firebaseUid,
							StringComparison.Ordinal
						)
					)
					|| string.Equals(
						row.Username,
						CurrentUsername,
						StringComparison.OrdinalIgnoreCase
					)
			);

		if (own != null)
		{
			/*
			 * Never show a stale lower value for the signed-in player.
			 */
			own.Score =
				Math.Max(
					own.Score,
					localScore
				);

			return;
		}

		rows.Add(
			new LeaderboardDatabaseRow(
				_firebaseUid,
				CurrentUsername,
				localScore
			)
		);
	}


	private void PublishLeaderboardRows(
		List<LeaderboardDatabaseRow> databaseRows,
		IdleAiLeaderboardKind kind)
	{
		/*
		 * Profile documents should already be unique per Firebase uid, but
		 * defensive grouping prevents accidental duplicate visual rows if the
		 * database was edited manually during testing.
		 */
		List<LeaderboardDatabaseRow> sorted =
			databaseRows
				.GroupBy(
					row =>
						row.Username,
					StringComparer.OrdinalIgnoreCase
				)
				.Select(
					group =>
						group
							.OrderByDescending(
								row =>
									row.Score
							)
							.First()
				)
				.OrderByDescending(
					row =>
						row.Score
				)
				.ThenBy(
					row =>
						row.Username,
					StringComparer.OrdinalIgnoreCase
				)
				.ToList();

		List<IdleAiLeaderboardEntry> ranked =
			[];

		long previousScore =
			long.MinValue;

		long previousRank =
			0;

		for (
			int i = 0;
			i < sorted.Count;
			i++
		)
		{
			LeaderboardDatabaseRow row =
				sorted[
					i
				];

			long rank =
				i == 0
					? 1
					: row.Score == previousScore
						? previousRank
						: i + 1;

			bool own =
				HasUsername
				&& string.Equals(
					row.Username,
					CurrentUsername,
					StringComparison.OrdinalIgnoreCase
				);

			ranked.Add(
				new IdleAiLeaderboardEntry(
					rank,
					row.Username,
					row.Score,
					own
				)
			);

			previousScore =
				row.Score;

			previousRank =
				rank;
		}

		IReadOnlyList<IdleAiLeaderboardEntry> top =
			ranked
				.Take(
					InGameLeaderboardTopCount
				)
				.ToList();

		IdleAiLeaderboardEntry? ownEntry =
			ranked.FirstOrDefault(
				row =>
					row.IsOwn
			);

		InGameLeaderboardLoaded?.Invoke(
			kind,
			top,
			ownEntry
		);
	}


	private long GetLocalScore(
		IdleAiLeaderboardKind kind)
	{
		return kind switch
		{
			IdleAiLeaderboardKind.HighestTotalLevel =>
				Math.Max(
					0,
					_leaderboardLocalTotalLevel
				),

			IdleAiLeaderboardKind.MostPrestiges =>
				Math.Max(
					0,
					_leaderboardLocalPrestiges
				),

			_ =>
				0
		};
	}


	private static long GetLeaderboardScoreFromFields(
		JsonElement fields,
		IdleAiLeaderboardKind kind)
	{
		string fieldName =
			kind switch
			{
				IdleAiLeaderboardKind.HighestTotalLevel =>
					"highestTotalLevel",

				IdleAiLeaderboardKind.MostPrestiges =>
					"prestigeCount",

				_ =>
					""
			};

		if (
			string.IsNullOrWhiteSpace(
				fieldName
			)
			|| !fields.TryGetProperty(
				fieldName,
				out JsonElement field
			)
			|| field.ValueKind
				!= JsonValueKind.Object
			|| !field.TryGetProperty(
				"integerValue",
				out JsonElement value
			)
		)
		{
			return 0;
		}

		if (
			value.ValueKind
				== JsonValueKind.Number
			&& value.TryGetInt64(
				out long numeric
			)
		)
		{
			return Math.Max(
				0,
				numeric
			);
		}

		if (
			long.TryParse(
				value.GetString(),
				out long parsed
			)
		)
		{
			return Math.Max(
				0,
				parsed
			);
		}

		return 0;
	}


	private static Dictionary<string, object>
		IntegerValueForLeaderboard(
			long value)
	{
		return new Dictionary<string, object>
		{
			[
				"integerValue"
			] =
				Math.Max(
					0,
					value
				)
				.ToString()
		};
	}


	private sealed class LeaderboardDatabaseRow
	{
		public LeaderboardDatabaseRow(
			string uid,
			string username,
			long score)
		{
			Uid =
				uid;

			Username =
				username;

			Score =
				score;
		}

		public string Uid { get; }

		public string Username { get; }

		public long Score { get; set; }
	}
}
