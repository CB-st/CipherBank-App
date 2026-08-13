// <copyright file="HistoryPointDto.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Text.Json.Serialization;

namespace CipherBank_app.V1;

/// <summary>OHLC / history point.</summary>
public sealed class HistoryPointDto
{
    [JsonPropertyName("T")]
    public long T { get; set; }

    [JsonPropertyName("V")]
    public double V { get; set; }
}
