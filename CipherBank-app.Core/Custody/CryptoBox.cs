// <copyright file="CryptoBox.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Configuration;

namespace CipherBank_app.Custody;

/// <summary>Compatibility facade for callers that cannot yet receive <see cref="ICryptoBox"/>.</summary>
public static class CryptoBox
{
    private static readonly AesGcmCryptoBox Default = new(CryptographyOptions.Default);

    public static byte[] DeriveKey(string pin, byte[] salt) => Default.DeriveKey(pin, salt);

    public static string Seal(string plaintext, string pin) => Default.Seal(plaintext, pin);

    public static string Open(string sealedB64, string pin) => Default.Open(sealedB64, pin);
}
