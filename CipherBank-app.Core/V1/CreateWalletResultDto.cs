// <copyright file="CreateWalletResultDto.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Text.Json.Serialization;

namespace CipherBank_app.V1;

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
