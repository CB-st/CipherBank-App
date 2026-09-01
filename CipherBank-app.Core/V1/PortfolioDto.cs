// <copyright file="PortfolioDto.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace CipherBank_app.V1;

/// <summary>Portfolio snapshot from GET /portfolio.</summary>
public sealed class PortfolioDto
{
    [JsonPropertyName("TOTAL_USD")]
    public string TotalUsd { get; set; } = "0";

    [JsonPropertyName("CHANGE_24H_PCT")]
    public string Change24HPct { get; set; } = "0";

    [JsonPropertyName("HOLDINGS")]
    [JsonObjectCreationHandling(JsonObjectCreationHandling.Populate)]
    public Collection<HoldingDto> Holdings { get; } = [];
}
