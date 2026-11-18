// <copyright file="LogRedactionHelper.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Services.Logging;

/// <summary>
/// Helper class for redacting sensitive information in log messages.
/// Provides consistent redaction across all logging throughout the application.
/// </summary>
public static class LogRedactionHelper
{
    private const string RedactionMarker = "***";
    private const string Ellipsis = "...";
    private const int DefaultShowChars = 4;
    private const int BothEndsMultiplier = 2;
    private const int UsernameMinLengthForPartial = 2;
    private const int WalletIdShortMaxLength = 8;
    private const int WalletIdShortPrefixLength = 2;
    private const int WalletIdPrefixLength = 4;
    private const int WalletIdSuffixLength = 4;
    private const int AddressShortMaxLength = 10;
    private const int AddressShortPrefixLength = 3;
    private const int AddressPrefixLength = 6;
    private const int AddressSuffixLength = 4;
    private const int TokenPrefixLength = 8;
    private const int EmailLocalPrefixLength = 2;
    private const int TransactionIdShortMaxLength = 12;
    private const int TransactionIdShortPrefixLength = 4;
    private const int TransactionIdPrefixLength = 8;
    private const int TransactionIdSuffixLength = 4;

    /// <summary>
    /// Redacts a username, showing only the first and last characters.
    /// Example: "testuser123" becomes "t*********3"
    /// </summary>
    /// <param name="username">The username to redact</param>
    /// <returns>Redacted username</returns>
    public static string RedactUsername(string? username)
    {
        if (string.IsNullOrEmpty(username))
        {
            return "[empty]";
        }

        if (username.Length <= UsernameMinLengthForPartial)
        {
            return RedactionMarker;
        }

        return $"{username[0]}{new string('*', username.Length - UsernameMinLengthForPartial)}{username[^1]}";
    }

    /// <summary>
    /// Redacts a wallet ID, showing only the first 4 and last 4 characters.
    /// Example: "wallet1234567890abcdef" becomes "wall...cdef"
    /// </summary>
    /// <param name="walletId">The wallet ID to redact</param>
    /// <returns>Redacted wallet ID</returns>
    public static string RedactWalletId(string? walletId)
    {
        if (string.IsNullOrEmpty(walletId))
        {
            return "[empty]";
        }

        if (walletId.Length <= WalletIdShortMaxLength)
        {
            return $"{walletId[..WalletIdShortPrefixLength]}{Ellipsis}";
        }

        return $"{walletId[..WalletIdPrefixLength]}{Ellipsis}{walletId[^WalletIdSuffixLength..]}";
    }

    /// <summary>
    /// Redacts a cryptocurrency address, showing only the first 6 and last 4 characters.
    /// Example: "1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa" becomes "1A1zP1...fNa"
    /// </summary>
    /// <param name="address">The address to redact</param>
    /// <returns>Redacted address</returns>
    public static string RedactAddress(string? address)
    {
        if (string.IsNullOrEmpty(address))
        {
            return "[empty]";
        }

        if (address.Length <= AddressShortMaxLength)
        {
            return $"{address[..AddressShortPrefixLength]}{Ellipsis}";
        }

        return $"{address[..AddressPrefixLength]}{Ellipsis}{address[^AddressSuffixLength..]}";
    }

    /// <summary>
    /// Redacts an authentication token, showing only the first 8 characters.
    /// Example: "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..." becomes "eyJhbGci..."
    /// </summary>
    /// <param name="token">The token to redact</param>
    /// <returns>Redacted token</returns>
    public static string RedactToken(string? token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return "[empty]";
        }

        if (token.Length <= TokenPrefixLength)
        {
            return RedactionMarker;
        }

        return $"{token[..TokenPrefixLength]}{Ellipsis}";
    }

    /// <summary>
    /// Redacts an email address, showing only the first 2 characters and domain.
    /// Example: "user@example.com" becomes "us***@example.com"
    /// </summary>
    /// <param name="email">The email to redact</param>
    /// <returns>Redacted email</returns>
    public static string RedactEmail(string? email)
    {
        if (string.IsNullOrEmpty(email))
        {
            return "[empty]";
        }

        int atIndex = email.IndexOf('@', StringComparison.Ordinal);
        if (atIndex <= 0)
        {
            return RedactionMarker;
        }

        string localPart = email[..atIndex];
        string domain = email[atIndex..];

        if (localPart.Length <= EmailLocalPrefixLength)
        {
            return $"{localPart[0]}{RedactionMarker}{domain}";
        }

        return $"{localPart[..EmailLocalPrefixLength]}{RedactionMarker}{domain}";
    }

    /// <summary>
    /// Redacts a transaction ID, showing only the first 8 and last 4 characters.
    /// Example: "tx_1234567890abcdef" becomes "tx_12345...cdef"
    /// </summary>
    /// <param name="transactionId">The transaction ID to redact</param>
    /// <returns>Redacted transaction ID</returns>
    public static string RedactTransactionId(string? transactionId)
    {
        if (string.IsNullOrEmpty(transactionId))
        {
            return "[empty]";
        }

        if (transactionId.Length <= TransactionIdShortMaxLength)
        {
            return $"{transactionId[..TransactionIdShortPrefixLength]}{Ellipsis}";
        }

        return $"{transactionId[..TransactionIdPrefixLength]}{Ellipsis}{transactionId[^TransactionIdSuffixLength..]}";
    }

    /// <summary>Redacts sensitive data from a generic string based on its apparent type.</summary>
    /// <param name="value">The value to redact</param>
    /// <returns>Redacted value</returns>
    public static string Redact(string? value)
        => Redact(value, DefaultShowChars);

    /// <summary>Redacts sensitive data from a generic string based on its apparent type.</summary>
    /// <param name="value">The value to redact</param>
    /// <param name="showChars">Number of characters to show at start and end</param>
    /// <returns>Redacted value</returns>
    public static string Redact(string? value, int showChars)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "[empty]";
        }

        if (value.Length <= showChars * BothEndsMultiplier)
        {
            return RedactionMarker;
        }

        return $"{value[..showChars]}{Ellipsis}{value[^showChars..]}";
    }
}
