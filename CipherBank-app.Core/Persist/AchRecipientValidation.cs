// <copyright file="AchRecipientValidation.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Persist;

/// <summary>ACH recipient field validation (Cora RecipientPickerModal parity).</summary>
public static class AchRecipientValidation
{
    public const int RoutingNumberDigitCount = 9;
    public const int AccountNumberMinDigits = 4;
    public const int MaskVisibleTrailingDigits = 4;
    public const int MemoMaxLength = 140;

    public static string? Validate(
        string name,
        string holder,
        string bank,
        string routing,
        string account,
        string accountType)
        => Validate(name, holder, bank, routing, account, accountType, null);

    public static string? Validate(
        string name,
        string holder,
        string bank,
        string routing,
        string account,
        string accountType,
        string? memo)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Enter a payee name.";
        }

        if (string.IsNullOrWhiteSpace(holder))
        {
            return "Enter the account holder name.";
        }

        if (string.IsNullOrWhiteSpace(bank))
        {
            return "Enter the bank name.";
        }

        string digits = new string(routing.Where(char.IsDigit).ToArray());
        if (digits.Length != RoutingNumberDigitCount)
        {
            return "Routing number must be 9 digits.";
        }

        if (string.IsNullOrWhiteSpace(account) || account.Trim().Length < AccountNumberMinDigits)
        {
            return "Enter a valid account number.";
        }

        string type = accountType.Trim().ToLowerInvariant();
        if (type is not ("checking" or "savings"))
        {
            return "Account type must be checking or savings.";
        }

        if (memo is { Length: > MemoMaxLength })
        {
            return "Memo must be 140 characters or fewer.";
        }

        return null;
    }

    public static string MaskAccount(string account)
    {
        string trimmed = account.Trim();
        if (trimmed.Length <= MaskVisibleTrailingDigits)
        {
            return "•••• " + trimmed;
        }

        return "•••• " + trimmed[^MaskVisibleTrailingDigits..];
    }

    public static string MaskRouting(string routing)
    {
        string digits = new string(routing.Where(char.IsDigit).ToArray());
        if (digits.Length < MaskVisibleTrailingDigits)
        {
            return "••••";
        }

        return "•••• " + digits[^MaskVisibleTrailingDigits..];
    }
}
