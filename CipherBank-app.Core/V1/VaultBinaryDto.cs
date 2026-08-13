// <copyright file="VaultBinaryDto.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Text.Json.Serialization;

namespace CipherBank_app.V1;

/// <summary>Server-held vault binary metadata (never mnemonic).</summary>
public sealed class VaultBinaryDto
{
    [JsonPropertyName("BINARY_ID")]
    public string BinaryId { get; set; } = string.Empty;

    [JsonPropertyName("LABEL")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("KIND")]
    public string Kind { get; set; } = "wallet_rpc";
}
