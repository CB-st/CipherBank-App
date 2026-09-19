// <copyright file="OptionsValidationMessages.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Globalization;
using System.Resources;

namespace CipherBank_app.Configuration;

/// <summary>
/// Options DataAnnotation / IValidateOptions copy (resource file, not appsettings keys).
/// </summary>
internal static class OptionsValidationMessages
{
    private static readonly ResourceManager _manager = new(
        "CipherBank_app.Configuration.OptionsValidationMessages",
        typeof(OptionsValidationMessages).Assembly);

    internal static string CryptographyUnsafe => Require(nameof(CryptographyUnsafe));

    internal static string SyncConcurrencyOutOfRange => Require(nameof(SyncConcurrencyOutOfRange));

    internal static string DatabaseNameRequired => Require(nameof(DatabaseNameRequired));

    internal static string DatabaseNameMustBeFileName => Require(nameof(DatabaseNameMustBeFileName));

    internal static string DefaultRecipientsInvalid => Require(nameof(DefaultRecipientsInvalid));

    private static string Require(string name)
        => _manager.GetString(name, CultureInfo.InvariantCulture)
           ?? throw new InvalidOperationException($"Missing resource string '{name}'.");
}
