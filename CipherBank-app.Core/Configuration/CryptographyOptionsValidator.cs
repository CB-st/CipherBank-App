// <copyright file="CryptographyOptionsValidator.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using Microsoft.Extensions.Options;

namespace CipherBank_app.Configuration;

/// <summary>
/// Rejects cryptography settings that are unsafe or would orphan sealed custody blobs.
/// </summary>
internal sealed class CryptographyOptionsValidator : IValidateOptions<CryptographyOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, CryptographyOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.IsValid() && options.MatchesPersistedProfile())
        {
            return ValidateOptionsResult.Success;
        }

        return ValidateOptionsResult.Fail(OptionsValidationMessages.CryptographyUnsafe);
    }
}
