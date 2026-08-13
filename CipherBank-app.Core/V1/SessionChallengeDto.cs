// <copyright file="SessionChallengeDto.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Text.Json.Serialization;

namespace CipherBank_app.V1;

/// <summary>
/// Server → device challenge for challenge/pass session open.
/// Ciphertext is sealed to the account public key registered at onboarding.
/// </summary>
public sealed class SessionChallengeDto
{
    [JsonPropertyName("CHALLENGE_ID")]
    public string ChallengeId { get; set; } = string.Empty;

    /// <summary>Opaque ciphertext; only the account private key can open it.</summary>
    [JsonPropertyName("CIPHERTEXT")]
    public string Ciphertext { get; set; } = string.Empty;

    /// <summary>API public key (or key id) the device must seal the pass to.</summary>
    [JsonPropertyName("API_PUBLIC_KEY")]
    public string ApiPublicKey { get; set; } = string.Empty;

    [JsonPropertyName("API_KEY_ID")]
    public string? ApiKeyId { get; set; }

    /// <summary>e.g. <c>x25519-chacha20poly1305</c>.</summary>
    [JsonPropertyName("ALGORITHM")]
    public string Algorithm { get; set; } = string.Empty;
}
