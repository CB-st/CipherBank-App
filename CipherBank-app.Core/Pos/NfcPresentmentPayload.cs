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
            JsonElement root = doc.RootElement;
            int version = root.TryGetProperty("v", out JsonElement v) ? v.GetInt32() : SupportedVersion;
            string sessionId = root.TryGetProperty("sessionId", out JsonElement s)
                ? s.GetString() ?? string.Empty
                : string.Empty;
            string tokenRef = root.TryGetProperty("tokenRef", out JsonElement t)
                ? t.GetString() ?? string.Empty
                : string.Empty;
            if (version != SupportedVersion
                || string.IsNullOrWhiteSpace(sessionId)
                || string.IsNullOrWhiteSpace(tokenRef))
            {
                return null;
            }

            return new NfcPresentmentPayload
            {
                V = version,
                SessionId = sessionId,
                TokenRef = tokenRef,
                MerchantId = root.TryGetProperty("merchantId", out JsonElement m) ? m.GetString() : null,
            };
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
}
