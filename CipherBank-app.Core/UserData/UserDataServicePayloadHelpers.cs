// <copyright file="UserDataServicePayloadHelpers.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.UserData;

/// <summary>Payload field helpers for <see cref="UserDataServiceLogic"/>.</summary>
public static class UserDataServicePayloadHelpers
{
    /// <summary>
    /// Requires a non-blank payload field. Use: High (HandleRequest). Scope: service logic.
    /// </summary>
    public static string GetRequired(IReadOnlyDictionary<string, string> payload, string key)
        => payload.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new ArgumentException($"Missing required field '{key}'.");

    /// <summary>
    /// Optional payload field. Use: High (HandleRequest). Scope: service logic.
    /// </summary>
    public static string? GetOptional(IReadOnlyDictionary<string, string> payload, string key)
        => payload.TryGetValue(key, out string? value) ? value : null;

    /// <summary>
    /// Base64-decodes CHALLENGE_RESPONSE_BLOB; empty on bad input. Use: High. Scope: service logic.
    /// </summary>
    public static byte[] DecodeChallengeResponse(string base64)
    {
        try
        {
            return Convert.FromBase64String(base64);
        }
        catch (FormatException)
        {
            return [];
        }
    }

    /// <summary>
    /// Parses OVERWRITE / AREYOUSURE flags. Use: High. Scope: service logic.
    /// </summary>
    public static bool ParseBool(string? value)
        => bool.TryParse(value, out bool parsed) && parsed;

    /// <summary>
    /// Maps preferred 2FA to an effective method or INVALID. Use: High. Scope: service logic.
    /// </summary>
    public static string Normalize2Fa(string preferred)
    {
        string method = string.IsNullOrWhiteSpace(preferred)
            ? UserDataWireNames.TwoFaUnspecified
            : preferred.Trim().ToUpperInvariant();

        return method switch
        {
            UserDataWireNames.TwoFaUnspecified => UserDataWireNames.TwoFaEmail,
            UserDataWireNames.TwoFaEmail => UserDataWireNames.TwoFaEmail,
            UserDataWireNames.TwoFaSms => UserDataWireNames.TwoFaSms,
            UserDataWireNames.TwoFaAuthenticator => UserDataWireNames.TwoFaAuthenticator,
            _ => "INVALID",
        };
    }

    /// <summary>
    /// Cheap PEM shape check (full parse happens at RSA encrypt time). Use: Medium. Scope: enroll.
    /// </summary>
    public static bool LooksLikePublicKeyPem(string pem)
        => pem.Contains("BEGIN PUBLIC KEY", StringComparison.Ordinal)
            || pem.Contains("BEGIN RSA PUBLIC KEY", StringComparison.Ordinal);
}
