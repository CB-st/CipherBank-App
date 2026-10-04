// <copyright file="CryptoBox.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Configuration;

namespace CipherBank_app.Custody;

/// <summary>
/// Frozen-profile helpers for tests and other callers that do not have a service provider.
/// Hosts inject <see cref="AesGcmCryptoBox"/>, which is built from
/// <see cref="Microsoft.Extensions.Options.IOptions{TOptions}"/> of
/// <see cref="CryptographyOptions"/>. Startup validation rejects any profile other than
/// <see cref="CryptographyOptions.Default"/>, so this helper cannot diverge from a running host.
/// </summary>
public static class CryptoBox
{
    private static readonly AesGcmCryptoBox Default = new(CryptographyOptions.Default);

    public static byte[] DeriveKey(string pin, byte[] salt) => Default.DeriveKey(pin, salt);

    public static string Seal(string plaintext, string pin) => Default.Seal(plaintext, pin);

    public static string Open(string sealedB64, string pin) => Default.Open(sealedB64, pin);
}
