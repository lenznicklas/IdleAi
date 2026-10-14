#nullable enable
using System;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace IdleAi;

public enum FirebaseFailureKind { Network, Authentication, Configuration, Permission, Request, Service }

public sealed class FirebaseRequestException(long status, string code, string detail = "") : Exception(code)
{
    public long Status { get; } = status;
    public string Code { get; } = code;
    public string Detail { get; } = detail;
    public bool IsConfigurationFailure => Code is "API_KEY_INVALID" or "INVALID_API_KEY" or "PROJECT_NOT_FOUND"
        or "CONFIGURATION_FAILURE" or "CONFIGURATION_NOT_FOUND" or "OPERATION_NOT_ALLOWED" or "INVALID_PROVIDER_ID" or "UNSUPPORTED_PLATFORM" or "NATIVE_AUTH_BRIDGE_MISSING";
    public bool CanReauthenticate => Code is "INVALID_REFRESH_TOKEN" or "TOKEN_EXPIRED" or "INVALID_GRANT" or "USER_NOT_FOUND" or "INVALID_ID_TOKEN";
    public FirebaseFailureKind Kind => IsConfigurationFailure ? FirebaseFailureKind.Configuration
        : Code is "NETWORK" or "NETWORK_ERROR" or "NETWORK_RETRY_PENDING" or "GPGS_NETWORK_ERROR" || Status == 0 && Code == "HTTP_ERROR" ? FirebaseFailureKind.Network
        : Status == 429 || Status >= 500 ? FirebaseFailureKind.Service
        : Status == 401 || Code.StartsWith("GPGS_", StringComparison.Ordinal) || Code.StartsWith("AUTH", StringComparison.Ordinal)
            || Code.Contains("TOKEN", StringComparison.Ordinal) || Code.Contains("CREDENTIAL", StringComparison.Ordinal) || Code.Contains("IDP", StringComparison.Ordinal) || Code == "INVALID_AUTH_RESPONSE" ? FirebaseFailureKind.Authentication
        : Status == 403 ? FirebaseFailureKind.Permission : FirebaseFailureKind.Request;
    public bool RetryInitialization => Kind is FirebaseFailureKind.Network or FirebaseFailureKind.Service
        || Code is "GPGS_LOGIN_TIMEOUT" or "GPGS_NOT_READY" or "GPGS_API_8" or "GPGS_API_17" or "GPGS_API_-3" or "GPGS_REUSED_AUTH_CODE" or "GPGS_PROOF_CHANGED" or "GPGS_AUTH_CODE_TIMEOUT" or "GPGS_EMPTY_AUTH_CODE" or "INVALID_IDP_RESPONSE" or "INVALID_CREDENTIAL_OR_PROVIDER_ID" or "INVALID_AUTH_RESPONSE";
    public string PlayerMessage => Kind switch
    {
        FirebaseFailureKind.Configuration => "Online services are unavailable.",
        FirebaseFailureKind.Network => "Unable to reach online services. Please check your connection.",
        FirebaseFailureKind.Service => "Online services are busy. Please try again.",
        FirebaseFailureKind.Authentication => Code == "AUTH_USER_CHANGED" ? "Your signed-in account changed. Please try again." : Code.StartsWith("GPGS_", StringComparison.Ordinal)
            ? "Could not connect to Google Play Games. Please try again." : "Could not sign in to online services. Please try again.",
        FirebaseFailureKind.Permission => "Online access was denied. Please try again later.",
        _ => "Online request failed. Please try again."
    };

    public static FirebaseRequestException Parse(FirebaseHttpResponse response)
    {
        string code = "HTTP_ERROR", detail = "";
        try
        {
            using var doc = JsonDocument.Parse(response.Body);
            if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("error", out var error))
            {
                if (error.ValueKind == JsonValueKind.String) code = error.GetString() ?? code;
                else if (error.ValueKind == JsonValueKind.Object)
                {
                    if (error.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String) detail = message.GetString() ?? "";
                    if (error.TryGetProperty("status", out var state) && state.ValueKind == JsonValueKind.String) code = state.GetString() ?? code;
                    else if (detail.Length > 0) code = detail.Split(' ', ':')[0];
                }
            }
        }
        catch (JsonException) { /* Never include arbitrary HTML/transport bodies in player errors or logs. */ }
        string reason = detail.Split(' ', ':')[0].ToUpperInvariant();
        if (reason is "INVALID_IDP_RESPONSE" or "INVALID_CREDENTIAL_OR_PROVIDER_ID" or "INVALID_REFRESH_TOKEN" or "TOKEN_EXPIRED" or "INVALID_GRANT" or "USER_NOT_FOUND" or "INVALID_ID_TOKEN" or "API_KEY_INVALID" or "OPERATION_NOT_ALLOWED") code = reason;
        return new(response.Status, code.ToUpperInvariant(), detail);
    }
    public static bool IsAuthenticationFailure(FirebaseHttpResponse response)
    {
        var error = Parse(response);
        return response.Status == 401 || error.Code is "UNAUTHENTICATED" or "INVALID_ID_TOKEN" or "TOKEN_EXPIRED" or "CREDENTIAL_TOO_OLD_LOGIN_AGAIN";
    }
    public static string SafeDiagnostic(FirebaseHttpResponse response, params string[] secrets)
    {
        var error = Parse(response);
        string text = $"firebaseStatus: {error.Code}; message: {error.Detail}";
        try
        {
            using var doc = JsonDocument.Parse(response.Body);
            if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("error", out var body))
                text += "; firebaseError: " + body.GetRawText();
        }
        catch (JsonException) { }
        foreach (var secret in secrets)
            if (!string.IsNullOrEmpty(secret))
            {
                text = text.Replace(secret, "[redacted]", StringComparison.Ordinal);
                text = text.Replace(Uri.EscapeDataString(secret), "[redacted]", StringComparison.Ordinal);
            }
        // Firebase may echo rejected credentials inside error messages. Keep structured details, redact credential forms/JWTs.
        text = Regex.Replace(text, @"(?i)(bearer\s+|(?:access_token|accessToken|id_token|idToken|refresh_token|refreshToken|authorization|oauth_token|serverAuthCode|postBody)[\s="":]+)[^\s&""<]+", "$1[redacted]");
        text = Regex.Replace(text, @"(?i)\bcode=[^\s&""<]+", "code=[redacted]");
        text = Regex.Replace(text, @"[A-Za-z0-9_-]{20,}\.[A-Za-z0-9_-]{20,}\.[A-Za-z0-9_-]+", "[redacted]");
        return text.Replace('\n', ' ').Replace('\r', ' ');
    }
}
