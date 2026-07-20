// <copyright file="X25519ChaChaSealAlgorithm.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Security.Cryptography;
using CipherBank_app.ChallengePass.Crypto;

namespace CipherBank_app.ChallengePass.Algorithms;

/// <summary>
/// A1 seal slot: anonymous sealed box = ephemeral X25519 + HKDF + ChaCha20-Poly1305.
/// Wire <c>ALGORITHM</c>: <see cref="AlgorithmIdValue"/>.
/// </summary>
public sealed class X25519ChaChaSealAlgorithm : ISealAlgorithm
{
    public const string AlgorithmIdValue = "x25519-chacha20poly1305";

    private const int EphemeralPublicKeySize = 32;
    private const int NonceSize = PortableChaCha20Poly1305.NonceSize;
    private const int TagSize = PortableChaCha20Poly1305.TagSize;

    private static readonly byte[] HkdfSalt = "CipherBank-seal-v1"u8.ToArray();
    private static readonly byte[] HkdfInfo = "seal/chacha20poly1305/v1"u8.ToArray();

    public string AlgorithmId => AlgorithmIdValue;

    public int PublicKeySize => 32;

    public int PrivateKeySize => 32;

    public AccountKeyPair DeriveKeyPair(ReadOnlySpan<byte> seed32)
    {
        (byte[] publicKey, byte[] privateKey) = PortableX25519.DeriveKeyPair(seed32);
        return new AccountKeyPair(publicKey, privateKey);
    }

    public byte[] Seal(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> recipientPublicKey)
    {
        if (recipientPublicKey.Length != PublicKeySize)
        {
            throw new ArgumentException("Recipient public key must be 32 bytes.", nameof(recipientPublicKey));
        }

        (byte[] ephemeralPk, byte[] ephemeralSk) = PortableX25519.GenerateKeyPair();
        try
        {
            byte[] shared = PortableX25519.Agree(ephemeralSk, recipientPublicKey);
            try
            {
                byte[] aeadKey = DeriveAeadKey(shared);
                try
                {
                    byte[] nonce = RandomNumberGenerator.GetBytes(NonceSize);
                    byte[] cipher = PortableChaCha20Poly1305.Encrypt(aeadKey, nonce, plaintext);
                    var result = new byte[EphemeralPublicKeySize + NonceSize + cipher.Length];
                    ephemeralPk.CopyTo(result.AsSpan(0, EphemeralPublicKeySize));
                    nonce.CopyTo(result.AsSpan(EphemeralPublicKeySize, NonceSize));
                    cipher.CopyTo(result.AsSpan(EphemeralPublicKeySize + NonceSize));
                    return result;
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(aeadKey);
                }
            }
            finally
            {
                CryptographicOperations.ZeroMemory(shared);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(ephemeralSk);
        }
    }

    public byte[] Open(ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> recipientPrivateKey)
    {
        if (recipientPrivateKey.Length != PrivateKeySize)
        {
            throw new ArgumentException("Recipient private key must be 32 bytes.", nameof(recipientPrivateKey));
        }

        if (ciphertext.Length < EphemeralPublicKeySize + NonceSize + TagSize)
        {
            throw new CryptographicException("Ciphertext too short.");
        }

        ReadOnlySpan<byte> ephemeralPk = ciphertext[..EphemeralPublicKeySize];
        ReadOnlySpan<byte> nonce = ciphertext.Slice(EphemeralPublicKeySize, NonceSize);
        ReadOnlySpan<byte> cipher = ciphertext[(EphemeralPublicKeySize + NonceSize)..];

        byte[] shared = PortableX25519.Agree(recipientPrivateKey, ephemeralPk);
        try
        {
            byte[] aeadKey = DeriveAeadKey(shared);
            try
            {
                return PortableChaCha20Poly1305.Decrypt(aeadKey, nonce, cipher);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(aeadKey);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(shared);
        }
    }

    private static byte[] DeriveAeadKey(ReadOnlySpan<byte> sharedSecret)
        => HKDF.DeriveKey(
            HashAlgorithmName.SHA256,
            sharedSecret.ToArray(),
            PortableChaCha20Poly1305.KeySize,
            HkdfSalt,
            HkdfInfo);
}
