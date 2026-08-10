// <copyright file="NfcPresentmentPayload.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Text.Json;

namespace CipherBank_app.Pos;

/// <summary>NDEF presentment payload (tokenRef only — no PAN).</summary>
public sealed class NfcPresentmentPayload
{
    public int V { get; set; } = 1;

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
            return new NfcPresentmentPayload
            {
                V = root.TryGetProperty("v", out JsonElement v) ? v.GetInt32() : 1,
                SessionId = root.TryGetProperty("sessionId", out JsonElement s) ? s.GetString() ?? string.Empty : string.Empty,
                TokenRef = root.TryGetProperty("tokenRef", out JsonElement t) ? t.GetString() ?? string.Empty : string.Empty,
                MerchantId = root.TryGetProperty("merchantId", out JsonElement m) ? m.GetString() : null,
            };
        }
        catch (JsonException)
        {
            return null;
        }
        catch (ArgumentException)
        {
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
