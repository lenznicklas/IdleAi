#nullable enable
using System;
using System.Text;
using System.Text.RegularExpressions;

namespace IdleAi;

public static class UsernamePolicy
{
    private static readonly Regex Pattern = new(@"\A[A-Za-z][A-Za-z0-9_]{2,15}\z", RegexOptions.CultureInvariant);
    private static readonly string[] Reserved = ["admin", "administrator", "moderator", "official", "support", "developer", "idleai", "idle_ai", "firebase", "google", "anonymous"];
    public static bool TryNormalize(string? input, out string username, out string lower, out string error, bool existing = false)
    {
        username = ""; lower = ""; error = "";
        try { username = (input ?? "").Trim().Normalize(NormalizationForm.FormC); }
        catch (ArgumentException) { error = "Invalid username characters."; return false; }
        if (username.Length is < 3 or > 16) { error = "Use 3-16 characters."; return false; }
        if (!Pattern.IsMatch(username)) { error = "Start with a letter. Use only letters, numbers and _."; return false; }
        lower = username.ToLowerInvariant();
        // Existing names keep their ownership if the reserved-name policy expands later.
        if (!existing)
            foreach (var prefix in Reserved)
                if (lower.StartsWith(prefix, StringComparison.Ordinal)) { error = "This username is reserved."; return false; }
        return true;
    }
}
