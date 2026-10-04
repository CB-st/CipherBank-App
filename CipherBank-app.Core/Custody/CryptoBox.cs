// <copyright file="CryptoBox.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Configuration;

namespace CipherBank_app.Custody;

/// <summary>
/// Helpers for tests and other callers that do not have a service provider.
/// Each call takes an explicit <see cref="CryptographyOptions"/> so this type does not
/// cache <see cref="CryptographyOptions.Default"/> and ignore a bound profile.
/// Hosts inject <see cref="AesGcmCryptoBox"/>, which is built from
/// <see cref="Microsoft.Extensions.Options.IOptions{TOptions}"/> of
/// <see cref="CryptographyOptions"/>.
/// </summary>
public static class CryptoBox
{
    public static byte[] DeriveKey(CryptographyOptions options, string pin, byte[] salt)
        => Create(options).DeriveKey(pin, salt);

    public static string Seal(CryptographyOptions options, string plaintext, string pin)
        => Create(options).Seal(plaintext, pin);

    public static string Open(CryptographyOptions options, string sealedB64, string pin)
        => Create(options).Open(sealedB64, pin);

    private static AesGcmCryptoBox Create(CryptographyOptions options) => new(options);
}
