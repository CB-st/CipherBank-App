// <copyright file="SessionPassDto.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Text.Json.Serialization;

namespace CipherBank_app.V1;

/// <summary>
/// Device → server pass: solved challenge, re-sealed to the API public key.
/// Replaces <c>DEVICE_ATTESTATION=lab</c> once live.
/// </summary>
public sealed class SessionPassDto
{
    [JsonPropertyName("CHALLENGE_ID")]
    public string ChallengeId { get; set; } = string.Empty;

    [JsonPropertyName("PASS_CIPHERTEXT")]
    public string PassCiphertext { get; set; } = string.Empty;

    [JsonPropertyName("ACCOUNT_PUBLIC_KEY")]
    public string AccountPublicKey { get; set; } = string.Empty;

    [JsonPropertyName("API_KEY_ID")]
    public string? ApiKeyId { get; set; }

    [JsonPropertyName("ALGORITHM")]
    public string Algorithm { get; set; } = string.Empty;
}
