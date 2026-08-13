// <copyright file="CreateWalletRequestDto.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Text.Json.Serialization;

namespace CipherBank_app.V1;

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
