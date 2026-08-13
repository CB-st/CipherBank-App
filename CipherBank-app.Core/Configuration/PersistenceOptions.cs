// <copyright file="PersistenceOptions.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Configuration;

/// <summary>Settings for the on-device EF Core database.</summary>
public sealed class PersistenceOptions
{
    public static string SectionName { get; } = "Persistence";

    public string DatabaseName { get; set; } = "cipherbank.db";
}
