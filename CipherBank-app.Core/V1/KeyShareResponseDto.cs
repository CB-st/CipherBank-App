// <copyright file="KeyShareResponseDto.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Text.Json.Serialization;

namespace CipherBank_app.V1;

/// <summary>Server → device encapsulation for PQ channel establishment.</summary>
public sealed class KeyShareResponseDto
{
    [JsonPropertyName("KEY_SHARE_ID")]
    public string KeyShareId { get; set; } = string.Empty;

    [JsonPropertyName("MLKEM_CIPHERTEXT")]
    public string MlKemCiphertext { get; set; } = string.Empty;

    [JsonPropertyName("SERVER_X25519_PUBLIC_KEY")]
    public string ServerX25519PublicKey { get; set; } = string.Empty;

    [JsonPropertyName("ALGORITHM")]
    public string Algorithm { get; set; } = "hybrid-mlkem768-x25519-v1";
}
