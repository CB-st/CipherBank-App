// <copyright file="NfcPresentmentPayload.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CipherBank_app.Pos;

/// <summary>NDEF presentment payload (tokenRef only — no PAN).</summary>
public sealed class NfcPresentmentPayload
{
    private const int CurrentVersion = 1;

    /// <summary>Payload version this parser accepts.</summary>
    public static int SupportedVersion => CurrentVersion;

    [Range(CurrentVersion, CurrentVersion)]
    [JsonPropertyName("v")]
    public int V { get; set; } = SupportedVersion;

    [Required]
    [JsonPropertyName("sessionId")]
    public string SessionId { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("tokenRef")]
    public string TokenRef { get; set; } = string.Empty;

    [JsonPropertyName("merchantId")]
    public string? MerchantId { get; set; }

    /// <summary>
    /// Parses an NDEF presentment JSON blob into a payload, or null when malformed.
    /// Use: Medium (NFC presentment). Scope: NfcPresentmentPayload parse helper.
    /// </summary>
    public static NfcPresentmentPayload? TryParse(string json)
    {
        try
        {
            NfcPresentmentPayload? payload = JsonSerializer.Deserialize<NfcPresentmentPayload>(json);
            return payload is not null
                && Validator.TryValidateObject(payload, new ValidationContext(payload), null, validateAllProperties: true)
                ? payload
                : null;
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException or InvalidOperationException or FormatException)
        {
            // Malformed JSON or values that do not match the payload contract.
            return null;
        }
    }

    public string ToJson()
        => JsonSerializer.Serialize(this);
}
