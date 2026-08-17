// <copyright file="ConfigurationValidationMessages.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Globalization;
using System.Resources;

namespace CipherBank_app.Configuration;

/// <summary>
/// Options DataAnnotation / IValidateOptions copy (resource file, not appsettings keys).
/// </summary>
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
