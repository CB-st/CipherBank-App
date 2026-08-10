// <copyright file="CoraOptions.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Configuration;

/// <summary>Localizable Cora copy keyed by stable screen identifier.</summary>
public sealed class CoraOptions
{
    public const string SectionName = "Cora";

    public string Fallback { get; set; } = "CipherBank.";

    public Dictionary<string, string> Lines { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
