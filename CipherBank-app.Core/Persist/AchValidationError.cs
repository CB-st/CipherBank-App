// <copyright file="AchValidationError.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Persist;

/// <summary>
/// One ACH recipient validation failure: the field at fault plus its user-facing message.
/// Produced by <see cref="AchRecipientValidation.ValidateDetailed"/> in stable field order
/// so consumers can attach each error to its input as well as list them all.
/// </summary>
/// <param name="Field">The form field that failed validation.</param>
/// <param name="Message">The user-facing error message for that field.</param>
public sealed record AchValidationError(AchRecipientField Field, string Message);
