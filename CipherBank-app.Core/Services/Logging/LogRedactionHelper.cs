// <copyright file="LogRedactionHelper.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System;

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

        if (username.Length <= 2)
        {
            return RedactionMarker;
        }

        return $"{username[0]}{new string('*', username.Length - 2)}{username[^1]}";
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

        if (walletId.Length <= 8)
        {
            return $"{walletId[..2]}{Ellipsis}";
        }

        return $"{walletId[..4]}{Ellipsis}{walletId[^4..]}";
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

        if (address.Length <= 10)
        {
            return $"{address[..3]}{Ellipsis}";
        }

        return $"{address[..6]}{Ellipsis}{address[^4..]}";
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

        if (token.Length <= 8)
        {
            return RedactionMarker;
        }

        return $"{token[..8]}{Ellipsis}";
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

        var atIndex = email.IndexOf('@', StringComparison.Ordinal);
        if (atIndex <= 0)
        {
            return RedactionMarker;
        }

        var localPart = email[..atIndex];
        var domain = email[atIndex..];

        if (localPart.Length <= 2)
        {
            return $"{localPart[0]}{RedactionMarker}{domain}";
        }

        return $"{localPart[..2]}{RedactionMarker}{domain}";
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

        if (transactionId.Length <= 12)
        {
            return $"{transactionId[..4]}{Ellipsis}";
        }

        return $"{transactionId[..8]}{Ellipsis}{transactionId[^4..]}";
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

        if (value.Length <= showChars * 2)
        {
            return RedactionMarker;
        }

        return $"{value[..showChars]}{Ellipsis}{value[^showChars..]}";
    }
}
