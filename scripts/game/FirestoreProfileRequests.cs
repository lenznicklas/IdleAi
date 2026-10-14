#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;

namespace IdleAi;

/// <summary>Pure, culture-independent construction of the existing atomic username reservation.</summary>
public static class FirestoreProfileRequests
{
    public static string CreateUsernameCommit(string project, string uid, string playGamesPlayerId, string input, DateTime now)
    {
        if (!UsernamePolicy.TryNormalize(input, out var username, out var lower, out var error))
            throw new ArgumentException(error, nameof(input));
        if (string.IsNullOrWhiteSpace(project) || string.IsNullOrWhiteSpace(uid) || uid.Contains('/')
            || string.IsNullOrWhiteSpace(playGamesPlayerId)) throw new ArgumentException("Missing or invalid profile identity.");
        object String(string value) => new { stringValue = value };
        var profile = new Dictionary<string, object>
        {
            ["uid"] = String(uid), ["playGamesPlayerId"] = String(playGamesPlayerId),
            ["username"] = String(username), ["usernameLower"] = String(lower),
            ["createdAt"] = new { timestampValue = now.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture) }
        };
        var reservation = new Dictionary<string, object>
        { ["uid"] = String(uid), ["username"] = String(username), ["usernameLower"] = String(lower) };
        string Name(string collection, string id) => $"projects/{project}/databases/(default)/documents/{collection}/{id}";
        object Write(string name, Dictionary<string, object> fields) => new
        { update = new { name, fields }, currentDocument = new { exists = false } };
        return JsonSerializer.Serialize(new { writes = new[]
        { Write(Name("usernames", lower), reservation), Write(Name("profiles", uid), profile) } });
    }
}
