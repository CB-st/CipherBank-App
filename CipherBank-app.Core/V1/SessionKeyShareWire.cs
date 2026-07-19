// <copyright file="SessionKeyShareWire.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
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

/// <summary>Managed / server wallet create (XMR).</summary>
public sealed class CreateWalletRequestDto
{
    [JsonPropertyName("SYMBOL")]
    public string Symbol { get; set; } = "XMR";

    [JsonPropertyName("LABEL")]
    public string? Label { get; set; }

    [JsonPropertyName("MODE")]
    public string Mode { get; set; } = "managed";

    [JsonPropertyName("ADDRESS")]
    public string? Address { get; set; }
}

/// <summary>Server wallet create result — never includes spend key.</summary>
public sealed class CreateWalletResultDto
{
    [JsonPropertyName("WALLET_ID")]
    public string WalletId { get; set; } = string.Empty;

    [JsonPropertyName("SYMBOL")]
    public string Symbol { get; set; } = string.Empty;

    [JsonPropertyName("LABEL")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("MODE")]
    public string Mode { get; set; } = string.Empty;

    [JsonPropertyName("ADDRESS")]
    public string? Address { get; set; }
}
