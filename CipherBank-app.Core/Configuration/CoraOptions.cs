// <copyright file="CoraOptions.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Text.Json.Serialization;

namespace CipherBank_app.Configuration;

/// <summary>Localizable Cora copy keyed by stable screen identifier.</summary>
public sealed record CoraOptions : CipherBankOptions<CoraOptions>
{
    public string Fallback { get; init; } = "CipherBank.";

    [JsonInclude]
    public Dictionary<string, string> Lines { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
}
