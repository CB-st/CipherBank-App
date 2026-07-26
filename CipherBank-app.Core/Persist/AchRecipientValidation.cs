// <copyright file="AchRecipientValidation.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Persist;

/// <summary>ACH recipient field validation (Cora RecipientPickerModal parity).</summary>
public static class AchRecipientValidation
{
    public static string? Validate(
        string name,
        string holder,
        string bank,
        string routing,
        string account,
        string accountType,
        string? memo = null)
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
        if (digits.Length != 9)
        {
            return "Routing number must be 9 digits.";
        }

        if (string.IsNullOrWhiteSpace(account) || account.Trim().Length < 4)
        {
            return "Enter a valid account number.";
        }

        string type = accountType.Trim().ToLowerInvariant();
        if (type is not ("checking" or "savings"))
        {
            return "Account type must be checking or savings.";
        }

        if (memo is { Length: > 140 })
        {
            return "Memo must be 140 characters or fewer.";
        }

        return null;
    }

    public static string MaskAccount(string account)
    {
        string trimmed = account.Trim();
        if (trimmed.Length <= 4)
        {
            return "•••• " + trimmed;
        }

        return "•••• " + trimmed[^4..];
    }

    public static string MaskRouting(string routing)
    {
        string digits = new string(routing.Where(char.IsDigit).ToArray());
        if (digits.Length < 4)
        {
            return "••••";
        }

        return "•••• " + digits[^4..];
    }
}
