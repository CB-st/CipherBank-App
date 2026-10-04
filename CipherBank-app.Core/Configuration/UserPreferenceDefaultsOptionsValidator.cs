// <copyright file="UserPreferenceDefaultsOptionsValidator.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using Microsoft.Extensions.Options;

namespace CipherBank_app.Configuration;

/// <summary>Rejects first-run preference defaults that the prefs store cannot apply.</summary>
internal sealed class UserPreferenceDefaultsOptionsValidator : IValidateOptions<UserPreferenceDefaultsOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, UserPreferenceDefaultsOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.IsValid())
        {
            return ValidateOptionsResult.Success;
        }

        return ValidateOptionsResult.Fail(OptionsValidationMessages.UserPreferenceDefaultsInvalid);
    }
}
