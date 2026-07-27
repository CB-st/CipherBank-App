// <copyright file="HoldingDto.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Text.Json.Serialization;

namespace CipherBank_app.V1;

/// <summary>Single holding line.</summary>
public sealed class HoldingDto
{
    [JsonPropertyName("SYMBOL")]
    public string Symbol { get; set; } = string.Empty;

    [JsonPropertyName("NAME")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("BALANCE")]
    public string Balance { get; set; } = "0";

    [JsonPropertyName("USD_VALUE")]
    public string UsdValue { get; set; } = "0";

    [JsonPropertyName("CHANGE_24H_PCT")]
    public string Change24HPct { get; set; } = "0";
}
