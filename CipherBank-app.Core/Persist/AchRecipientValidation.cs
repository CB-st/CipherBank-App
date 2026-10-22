// <copyright file="AchRecipientValidation.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Resources;

namespace CipherBank_app.Persist;

/// <summary>ACH recipient field validation (Cora RecipientPickerModal parity).</summary>
public static class AchRecipientValidation
{
    private const string MaskGlyphs = "••••";
    private const string MaskPrefix = MaskGlyphs + " ";

    public static int RoutingNumberDigitCount { get; } = 9;

    public static int AccountNumberMinDigits { get; } = 4;

    public static int MaskVisibleTrailingDigits { get; } = 4;

    public static int MemoMaxLength { get; } = 140;

    public static string? Validate(
        string name,
        string holder,
        string bank,
        string routing,
        string account,
        string accountType)
        => Validate(name, holder, bank, routing, account, accountType, null);

    /// <summary>
    /// Validates ACH payee fields for create/edit; returns the first user-facing error or null
    /// when valid. Convenience head of <see cref="ValidateAll"/> for single-message callers.
    /// Use: High (every recipient save). Scope: RecipientPicker / Persist callers.
    /// </summary>
    public static string? Validate(
        string name,
        string holder,
        string bank,
        string routing,
        string account,
        string accountType,
        string? memo)
    {
        IReadOnlyList<string> errors = ValidateAll(name, holder, bank, routing, account, accountType, memo);
        return errors.Count == 0 ? null : errors[0];
    }

    /// <summary>
    /// Validates ACH payee fields and returns every user-facing error message, in stable field
    /// order: name, holder, bank, routing, account, account type, memo. Empty when valid.
    /// Message projection of <see cref="ValidateDetailed"/> for list-only consumers.
    /// Use: High (recipient form submit). Scope: RecipientPicker / Persist callers.
    /// </summary>
    public static IReadOnlyList<string> ValidateAll(
        string name,
        string holder,
        string bank,
        string routing,
        string account,
        string accountType,
        string? memo)
        => [.. ValidateDetailed(name, holder, bank, routing, account, accountType, memo)
            .Select(static e => e.Message)];

    /// <summary>
    /// Validates ACH payee fields and returns every failure with its owning field, in stable
    /// field order: name, holder, bank, routing, account, account type, memo. Empty when valid.
    /// Consumers can attach each error to its input and render the complete list.
    /// Use: High (recipient form submit). Scope: RecipientPicker / Persist callers.
    /// </summary>
    public static IReadOnlyList<AchValidationError> ValidateDetailed(
        string name,
        string holder,
        string bank,
        string routing,
        string account,
        string accountType,
        string? memo)
    {
        AchValidationError?[] findings =
        [
            Finding(AchRecipientField.Name, RequireNonBlank(name, UserFacingStrings.AchEnterPayeeName)),
            Finding(AchRecipientField.Holder, RequireNonBlank(holder, UserFacingStrings.AchEnterAccountHolderName)),
            Finding(AchRecipientField.Bank, RequireNonBlank(bank, UserFacingStrings.AchEnterBankName)),
            Finding(AchRecipientField.Routing, ValidateRouting(routing)),
            Finding(AchRecipientField.Account, ValidateAccount(account)),
            Finding(AchRecipientField.AccountType, ValidateAccountType(accountType)),
            Finding(AchRecipientField.Memo, ValidateMemo(memo)),
        ];
        return [.. findings.Where(static f => f is not null)!];
    }

    /// <summary>
    /// Masks an account number to trailing digits for display. Account masks preserve the
    /// trimmed identifier's trailing characters, including any non-digits.
    /// Use: High (recipient lists). Scope: Persist UI mapping.
    /// </summary>
    public static string MaskAccount(string account)
    {
        string trimmed = account.Trim();
        return MaskTrailing(trimmed, " " + trimmed);
    }

    /// <summary>
    /// Masks a routing number to trailing digits for display. Routing masks normalize to
    /// ASCII digits first and collapse short input to the bare mask glyphs.
    /// Use: High (recipient lists). Scope: Persist UI mapping.
    /// </summary>
    public static string MaskRouting(string routing)
        => MaskTrailing(DigitsOnly(routing), string.Empty);

    /// <summary>
    /// Requires a non-blank string; returns <paramref name="message"/> when empty.
    /// Use: High (Validate). Scope: this helper.
    /// </summary>
    private static string? RequireNonBlank(string value, string message)
        => string.IsNullOrWhiteSpace(value) ? message : null;

    /// <summary>
    /// Pairs a field with its error message, or null when the field validated clean.
    /// Use: High (ValidateDetailed). Scope: this helper.
    /// </summary>
    private static AchValidationError? Finding(AchRecipientField field, string? message)
        => message is null ? null : new AchValidationError(field, message);

    /// <summary>
    /// Ensures routing is exactly <see cref="RoutingNumberDigitCount"/> ASCII digits.
    /// Unicode decimal digits (for example Arabic-Indic) are rejected: the ABA wire
    /// format is ASCII 0-9, matching <c>PersistenceOptions</c> routing validation.
    /// Use: High (Validate). Scope: this helper.
    /// </summary>
    private static string? ValidateRouting(string routing)
    {
        string trimmed = routing.Trim();
        bool exactDigits = trimmed.Length == RoutingNumberDigitCount
            && trimmed.All(char.IsAsciiDigit);
        return exactDigits
            ? null
            : UserFacingStrings.AchRoutingNumberMustBeDigits(RoutingNumberDigitCount);
    }

    /// <summary>
    /// Ensures account has at least <see cref="AccountNumberMinDigits"/> characters after trim.
    /// Use: High (Validate). Scope: this helper.
    /// </summary>
    private static string? ValidateAccount(string account)
        => string.IsNullOrWhiteSpace(account) || account.Trim().Length < AccountNumberMinDigits
            ? UserFacingStrings.AchEnterValidAccountNumber
            : null;

    /// <summary>
    /// Ensures account type is checking or savings (case-insensitive).
    /// Use: High (Validate). Scope: this helper.
    /// </summary>
    private static string? ValidateAccountType(string accountType)
    {
        string type = accountType.Trim().ToUpperInvariant();
        return type is "CHECKING" or "SAVINGS"
            ? null
            : UserFacingStrings.AchAccountTypeMustBeCheckingOrSavings;
    }

    /// <summary>
    /// Ensures optional memo does not exceed <see cref="MemoMaxLength"/>.
    /// Use: Medium (Validate with memo). Scope: this helper.
    /// </summary>
    private static string? ValidateMemo(string? memo)
        => memo is not null && memo.Length > MemoMaxLength
            ? UserFacingStrings.AchMemoMustBeMaxLength(MemoMaxLength)
            : null;

    /// <summary>
    /// Strips non-ASCII-digit characters from a routing or similar numeric field.
    /// Use: High (Validate/Mask). Scope: this helper.
    /// </summary>
    private static string DigitsOnly(string value)
        => new(value.Where(char.IsAsciiDigit).ToArray());

    /// <summary>
    /// Shared trailing-digit mask core: shows the last <see cref="MaskVisibleTrailingDigits"/>
    /// characters of <paramref name="source"/>, or appends <paramref name="shortSuffix"/> when the
    /// source is shorter. Callers own preprocessing (trim vs digits-only) and the short-input
    /// policy; those are named invariants, not incidental differences.
    /// Use: High (MaskAccount/MaskRouting). Scope: this helper.
    /// </summary>
    private static string MaskTrailing(string source, string shortSuffix)
        => source.Length < MaskVisibleTrailingDigits
            ? MaskGlyphs + shortSuffix
            : MaskPrefix + source[^MaskVisibleTrailingDigits..];
}
