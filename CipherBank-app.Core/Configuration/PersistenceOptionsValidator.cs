// <copyright file="PersistenceOptionsValidator.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using Microsoft.Extensions.Options;

namespace CipherBank_app.Configuration;

/// <summary>Rejects a database name that is blank or not a file name, and duplicate seed recipients.</summary>
internal sealed class PersistenceOptionsValidator : IValidateOptions<PersistenceOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, PersistenceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        List<string> failures = [];
        if (string.IsNullOrWhiteSpace(options.DatabaseName))
        {
            failures.Add(OptionsValidationMessages.DatabaseNameRequired);
        }
        else if (Path.GetFileName(options.DatabaseName) != options.DatabaseName)
        {
            failures.Add(OptionsValidationMessages.DatabaseNameMustBeFileName);
        }

        if (!options.AreDefaultRecipientsValid())
        {
            failures.Add(OptionsValidationMessages.DefaultRecipientsInvalid);
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
