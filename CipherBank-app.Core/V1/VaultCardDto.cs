// <copyright file="VaultCardDto.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Text.Json.Serialization;

namespace CipherBank_app.V1;

/// <summary>Vault card metadata (never PAN).</summary>
public sealed class VaultCardDto
{
    [JsonPropertyName("CARD_ID")]
    public string CardId { get; set; } = string.Empty;

    [JsonPropertyName("LAST4")]
    public string Last4 { get; set; } = "4242";

    [JsonPropertyName("BRAND")]
    public string Brand { get; set; } = "visa";

    [JsonPropertyName("LABEL")]
    public string Label { get; set; } = "Hardware test";

    [JsonPropertyName("HARDWARE_TEST")]
    public bool HardwareTest { get; set; } = true;
}
