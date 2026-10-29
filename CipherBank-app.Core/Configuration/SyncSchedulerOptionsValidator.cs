// <copyright file="SyncSchedulerOptionsValidator.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using Microsoft.Extensions.Options;

namespace CipherBank_app.Configuration;

/// <summary>Rejects a sync dispatch width outside the unset or 1–8 range.</summary>
internal sealed class SyncSchedulerOptionsValidator : IValidateOptions<SyncSchedulerOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, SyncSchedulerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.MaxConcurrency == 0
            || (options.MaxConcurrency >= SyncSchedulerOptions.MinConcurrency
                && options.MaxConcurrency <= SyncSchedulerOptions.MaxAllowedConcurrency))
        {
            return ValidateOptionsResult.Success;
        }

        return ValidateOptionsResult.Fail(OptionsValidationMessages.SyncConcurrencyOutOfRange);
    }
}
