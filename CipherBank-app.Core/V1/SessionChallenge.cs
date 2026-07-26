// <copyright file="SessionChallenge.cs" company="CipherBank">
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

/// <summary>
/// Builds the JSON body for <c>POST /v1/session</c>.
/// Lab stub today; challenge/pass implementation will decrypt with custody-derived
/// account key and seal the pass to the API public key — mnemonic never leaves the device.
/// </summary>
public interface ISessionProofBuilder
{
    Task<object> BuildOpenBodyAsync(CancellationToken ct);
}

/// <summary>Current stub: <c>{ DEVICE_ATTESTATION: "lab" }</c>.</summary>
public sealed class LabSessionProofBuilder : ISessionProofBuilder
{
    public static readonly string LabAttestation = "lab";

    public Task<object> BuildOpenBodyAsync(CancellationToken ct)
        => Task.FromResult<object>(new Dictionary<string, string>
        {
            ["DEVICE_ATTESTATION"] = LabAttestation,
        });
}
