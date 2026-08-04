// <copyright file="UserDataKeyDerivation.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Security.Cryptography;
using System.Text;
using CipherBank_app.Custody;

namespace CipherBank_app.UserData;

/// <summary>
/// Derives pack KEK and enroll-seed from a BIP39 mnemonic (HKDF-SHA256).
/// Does not derive deterministic RSA PEM yet — enroll-seed is the stable input for that follow-up.
/// </summary>
public static class UserDataKeyDerivation
{
    private static readonly byte[] HkdfSalt = Encoding.UTF8.GetBytes(UserDataConstants.KdfFamily);

    /// <summary>
    /// Rematerializes session key material from an unlocked mnemonic (empty BIP39 passphrase).
    /// Use: High (unlock / pack seal). Scope: userdata Core.
    /// </summary>
    public static UserDataKeyMaterial Derive(string mnemonic)
        => Derive(mnemonic, bip39Passphrase: string.Empty);

    /// <summary>
    /// Rematerializes session key material with an optional BIP39 passphrase.
    /// Use: Low (passphrase wallets). Scope: userdata Core.
    /// </summary>
    public static UserDataKeyMaterial Derive(string mnemonic, string bip39Passphrase)
    {
        ArgumentNullException.ThrowIfNull(mnemonic);
        ArgumentNullException.ThrowIfNull(bip39Passphrase);

        if (!MnemonicHelper.Validate(mnemonic))
        {
            throw new ArgumentException("Mnemonic is invalid.", nameof(mnemonic));
        }

        byte[] seed = DeriveBip39Seed(MnemonicHelper.Normalize(mnemonic), bip39Passphrase);
        try
        {
            byte[] kek = HkdfExpand(seed, UserDataConstants.KekInfoLabel, UserDataConstants.KekLength);
            byte[] enrollSeed = HkdfExpand(
                seed,
                UserDataConstants.EnrollSeedInfoLabel,
                UserDataConstants.EnrollSeedLength);
            try
            {
                return new UserDataKeyMaterial(kek, enrollSeed);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(kek);
                CryptographicOperations.ZeroMemory(enrollSeed);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(seed);
        }
    }

    /// <summary>
    /// BIP39 seed: PBKDF2-HMAC-SHA512(mnemonic, "mnemonic"+passphrase, 2048) → 64 bytes.
    /// Use: High (Derive). Scope: UserDataKeyDerivation.
    /// </summary>
    internal static byte[] DeriveBip39Seed(string normalizedMnemonic, string bip39Passphrase)
    {
        byte[] password = Encoding.UTF8.GetBytes(normalizedMnemonic);
        byte[] salt = Encoding.UTF8.GetBytes("mnemonic" + bip39Passphrase);
        try
        {
            return Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                UserDataConstants.Bip39SeedIterations,
                HashAlgorithmName.SHA512,
                UserDataConstants.Bip39SeedLength);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(password);
            CryptographicOperations.ZeroMemory(salt);
        }
    }

    /// <summary>
    /// HKDF-SHA256 expand with fixed salt <see cref="UserDataConstants.KdfFamily"/>.
    /// Use: High (Derive). Scope: UserDataKeyDerivation.
    /// </summary>
    internal static byte[] HkdfExpand(byte[] ikm, string infoLabel, int length)
    {
        byte[] info = Encoding.UTF8.GetBytes(infoLabel);
        try
        {
            return HKDF.DeriveKey(HashAlgorithmName.SHA256, ikm, length, HkdfSalt, info);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(info);
        }
    }
}
