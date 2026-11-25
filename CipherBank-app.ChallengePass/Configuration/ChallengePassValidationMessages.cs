// <copyright file="ChallengePassValidationMessages.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Globalization;
using System.Resources;

namespace CipherBank_app.ChallengePass.Configuration;

/// <summary>Non-UI validation strings surfaced through <see cref="ResourceManager"/>.</summary>
internal static class ChallengePassValidationMessages
{
    private static readonly ResourceManager Manager = new(
        "CipherBank_app.ChallengePass.Configuration.ChallengePassValidationMessages",
        typeof(ChallengePassValidationMessages).Assembly);

    internal static string ActiveSuiteNotInstalled => Require(nameof(ActiveSuiteNotInstalled));

    private static string Require(string name)
        => Manager.GetString(name, CultureInfo.InvariantCulture)
           ?? throw new InvalidOperationException($"Missing resource string '{name}'.");
}
