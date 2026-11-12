// <copyright file="KeyShareRequestDto.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Text.Json.Serialization;

namespace CipherBank_app.V1;

/// <summary>Device → server hybrid identity for PQ key-share.</summary>
public sealed class KeyShareRequestDto
{
    [JsonPropertyName("X25519_PUBLIC_KEY")]
    public string X25519PublicKey { get; set; } = string.Empty;

    [JsonPropertyName("MLKEM_PUBLIC_KEY")]
    public string MlKemPublicKey { get; set; } = string.Empty;

    [JsonPropertyName("ALGORITHM")]
    public string Algorithm { get; set; } = "hybrid-mlkem768-x25519-v1";
}
