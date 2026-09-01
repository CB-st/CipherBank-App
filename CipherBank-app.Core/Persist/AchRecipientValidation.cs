// <copyright file="AchRecipientValidation.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Resources;

namespace CipherBank_app.Persist;

/// <summary>ACH recipient field validation (Cora RecipientPickerModal parity).</summary>
public static class AchRecipientValidation
{
    private static readonly int RoutingNumberDigitCountValue = 9;
    private static readonly int AccountNumberMinDigitsValue = 4;
    private static readonly int MaskVisibleTrailingDigitsValue = 4;
    private static readonly int MemoMaxLengthValue = 140;

    public static int RoutingNumberDigitCount => RoutingNumberDigitCountValue;

    public static int AccountNumberMinDigits => AccountNumberMinDigitsValue;

    public static int MaskVisibleTrailingDigits => MaskVisibleTrailingDigitsValue;

    public static int MemoMaxLength => MemoMaxLengthValue;

    public static string? Validate(
        string name,
        string holder,
        string bank,
        string routing,
        string account,
        string accountType)
        => Validate(name, holder, bank, routing, account, accountType, null);

    /// <summary>
    /// Validates ACH payee fields for create/edit; returns the first user-facing error or null when valid.
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
        string?[] errors =
        [
            RequireNonBlank(name, Strings.AchEnterPayeeName),
            RequireNonBlank(holder, Strings.AchEnterAccountHolderName),
            RequireNonBlank(bank, Strings.AchEnterBankName),
            ValidateRouting(routing),
            ValidateAccount(account),
            ValidateAccountType(accountType),
            ValidateMemo(memo),
        ];
        return Array.Find(errors, static e => e is not null);
    }

    /// <summary>
    /// Masks an account number to trailing digits for display.
    /// Use: High (recipient lists). Scope: Persist UI mapping.
    /// </summary>
    public static string MaskAccount(string account)
    {
        string trimmed = account.Trim();
        if (trimmed.Length <= MaskVisibleTrailingDigits)
        {
            return "•••• " + trimmed;
        }

        return "•••• " + trimmed[^MaskVisibleTrailingDigits..];
    }

    /// <summary>
    /// Masks a routing number to trailing digits for display.
    /// Use: High (recipient lists). Scope: Persist UI mapping.
    /// </summary>
    public static string MaskRouting(string routing)
    {
        string digits = DigitsOnly(routing);
        if (digits.Length < MaskVisibleTrailingDigits)
        {
            return "••••";
        }

        return "•••• " + digits[^MaskVisibleTrailingDigits..];
    }

    /// <summary>
    /// Requires a non-blank string; returns <paramref name="message"/> when empty.
    /// Use: High (Validate). Scope: this helper.
    /// </summary>
    private static string? RequireNonBlank(string value, string message)
        => string.IsNullOrWhiteSpace(value) ? message : null;

    /// <summary>
    /// Ensures routing is exactly <see cref="RoutingNumberDigitCount"/> digits.
    /// Use: High (Validate). Scope: this helper.
    /// </summary>
    private static string? ValidateRouting(string routing)
    {
        string trimmed = routing.Trim();
        bool exactDigits = trimmed.Length == RoutingNumberDigitCount
            && trimmed.All(char.IsDigit);
        return exactDigits
            ? null
            : Strings.AchRoutingNumberMustBeDigits(RoutingNumberDigitCount);
    }

    /// <summary>
    /// Ensures account has at least <see cref="AccountNumberMinDigits"/> characters after trim.
    /// Use: High (Validate). Scope: this helper.
    /// </summary>
    private static string? ValidateAccount(string account)
        => string.IsNullOrWhiteSpace(account) || account.Trim().Length < AccountNumberMinDigits
            ? Strings.AchEnterValidAccountNumber
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
            : Strings.AchAccountTypeMustBeCheckingOrSavings;
    }

    /// <summary>
    /// Ensures optional memo does not exceed <see cref="MemoMaxLength"/>.
    /// Use: Medium (Validate with memo). Scope: this helper.
    /// </summary>
    private static string? ValidateMemo(string? memo)
        => memo is not null && memo.Length > MemoMaxLength
            ? Strings.AchMemoMustBeMaxLength(MemoMaxLength)
            : null;

    /// <summary>
    /// Strips non-digit characters from a routing or similar numeric field.
    /// Use: High (Validate/Mask). Scope: this helper.
    /// </summary>
    private static string DigitsOnly(string value)
        => new(value.Where(char.IsDigit).ToArray());
}
