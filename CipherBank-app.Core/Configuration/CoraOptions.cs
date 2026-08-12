// <copyright file="CoraOptions.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Configuration;

/// <summary>Localizable Cora copy keyed by stable screen identifier.</summary>
public sealed class CoraOptions
{
    public static string SectionName { get; } = "Cora";

    public string Fallback { get; set; } = "CipherBank.";

    [System.Text.Json.Serialization.JsonInclude]
    public Dictionary<string, string> Lines { get; private set; } = new(StringComparer.OrdinalIgnoreCase);
}
