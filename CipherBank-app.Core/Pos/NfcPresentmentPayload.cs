// <copyright file="NfcPresentmentPayload.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Text.Json;

namespace CipherBank_app.Pos;

/// <summary>NDEF presentment payload (tokenRef only — no PAN).</summary>
public sealed class NfcPresentmentPayload
{
    /// <summary>Payload version this parser accepts.</summary>
    public static int SupportedVersion => 1;

    public int V { get; set; } = SupportedVersion;

    public string SessionId { get; set; } = string.Empty;

    public string TokenRef { get; set; } = string.Empty;

    public string? MerchantId { get; set; }

    /// <summary>
    /// Parses an NDEF presentment JSON blob into a payload, or null when malformed.
    /// Use: Medium (NFC presentment). Scope: NfcPresentmentPayload parse helper.
    /// </summary>
    public static NfcPresentmentPayload? TryParse(string json)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(json);
            return ReadPayload(doc.RootElement);
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException or InvalidOperationException or FormatException)
        {
            // InvalidOperationException: wrong JSON value kinds (e.g. string where int expected).
            return null;
        }
    }

    public string ToJson()
        => JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["v"] = V,
            ["sessionId"] = SessionId,
            ["tokenRef"] = TokenRef,
            ["merchantId"] = MerchantId,
        });

    private static NfcPresentmentPayload? ReadPayload(JsonElement root)
    {
        if (!TryReadVersion(root, out int version)
            || !TryReadIdentifiers(root, out string sessionId, out string tokenRef))
        {
            return null;
        }

        return new NfcPresentmentPayload
        {
            V = version,
            SessionId = sessionId,
            TokenRef = tokenRef,
            MerchantId = ReadOptionalString(root, "merchantId"),
        };
    }

    private static bool TryReadVersion(JsonElement root, out int version)
    {
        if (!root.TryGetProperty("v", out JsonElement value))
        {
            version = SupportedVersion;
            return true;
        }

        version = value.GetInt32();
        return version == SupportedVersion;
    }

    private static bool TryReadIdentifiers(JsonElement root, out string sessionId, out string tokenRef)
    {
        sessionId = ReadRequiredString(root, "sessionId");
        tokenRef = ReadRequiredString(root, "tokenRef");
        return !string.IsNullOrWhiteSpace(sessionId)
            && !string.IsNullOrWhiteSpace(tokenRef);
    }

    private static string ReadRequiredString(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out JsonElement value))
        {
            return string.Empty;
        }

        return value.GetString() ?? string.Empty;
    }

    private static string? ReadOptionalString(JsonElement root, string name)
        => root.TryGetProperty(name, out JsonElement value) ? value.GetString() : null;
}
