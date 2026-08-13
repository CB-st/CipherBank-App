// <copyright file="ConfigurationValidationMessages.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Globalization;
using System.Resources;

namespace CipherBank_app.Configuration;

/// <summary>Non-UI validation strings surfaced through <see cref="ResourceManager"/>.</summary>
internal static class ConfigurationValidationMessages
{
    private static readonly ResourceManager Manager = new(
        "CipherBank_app.Configuration.ConfigurationValidationMessages",
        typeof(ConfigurationValidationMessages).Assembly);

    internal static string CryptographyUnsafe => Require(nameof(CryptographyUnsafe));

    internal static string SyncConcurrencyOutOfRange => Require(nameof(SyncConcurrencyOutOfRange));

    internal static string DatabaseNameRequired => Require(nameof(DatabaseNameRequired));

    internal static string DatabaseNameMustBeFileName => Require(nameof(DatabaseNameMustBeFileName));

    private static string Require(string name)
        => Manager.GetString(name, CultureInfo.InvariantCulture)
           ?? throw new InvalidOperationException($"Missing resource string '{name}'.");
}
