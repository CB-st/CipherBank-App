// <copyright file="UserDataUsernameHash.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace CipherBank_app.UserData;

/// <summary>Username normalization and SHA-256 helpers for AAD / pack prefix.</summary>
public static class UserDataUsernameHash
{
    /// <summary>ASCII uppercase → lowercase delta ('a' - 'A').</summary>
    private const int AsciiUpperToLowerDelta = 32;

    /// <summary>
    /// Trims and lowercases a username for stable hashing (ASCII product handles).
    /// Use: High (every pack seal/open). Scope: userdata client identity binding.
    /// </summary>
    public static string NormalizeUsername(string username)
    {
        ArgumentNullException.ThrowIfNull(username);
        ReadOnlySpan<char> trimmed = username.AsSpan().Trim();
        return string.Create(
            trimmed.Length,
            trimmed,
            static (destination, source) =>
            {
                for (int i = 0; i < source.Length; i++)
                {
                    char c = source[i];
                    destination[i] = c is >= 'A' and <= 'Z'
                        ? (char)(c + AsciiUpperToLowerDelta)
                        : c;
                }
            });
    }

    /// <summary>
    /// Full lowercase hex SHA-256 of the normalized username.
    /// Use: High (AAD binding). Scope: userdata pack codec.
    /// </summary>
    public static string HashHex(string username)
    {
        string normalized = NormalizeUsername(username);
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexStringLower(hash);
    }

    /// <summary>
    /// First <see cref="UserDataConstants.UsernameHashPrefixLength"/> hex chars of <see cref="HashHex"/>.
    /// Use: Medium (pack envelope hint). Scope: userdata pack wire.
    /// </summary>
    public static string HashPrefix(string username)
    {
        string hex = HashHex(username);
        return hex[..UserDataConstants.UsernameHashPrefixLength];
    }

    /// <summary>
    /// Formats content_version as a decimal string for AAD (invariant culture).
    /// Use: High (seal/open AAD). Scope: userdata pack codec.
    /// </summary>
    public static string FormatVersion(uint contentVersion)
        => contentVersion.ToString(CultureInfo.InvariantCulture);
}
