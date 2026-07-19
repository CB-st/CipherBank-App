// <copyright file="X25519ChaChaSealAlgorithm.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Security.Cryptography;
using NSec.Cryptography;

namespace CipherBank_app.ChallengePass.Algorithms;

/// <summary>
/// A1 seal slot: anonymous sealed box = ephemeral X25519 + HKDF + ChaCha20-Poly1305.
/// Wire <c>ALGORITHM</c>: <see cref="AlgorithmIdValue"/>.
/// </summary>
public sealed class X25519ChaChaSealAlgorithm : ISealAlgorithm
{
    public const string AlgorithmIdValue = "x25519-chacha20poly1305";

    private const int EphemeralPublicKeySize = 32;
    private const int NonceSize = 12;

    private static readonly KeyAgreementAlgorithm X25519 = KeyAgreementAlgorithm.X25519;
    private static readonly AeadAlgorithm Aead = AeadAlgorithm.ChaCha20Poly1305;
    private static readonly byte[] HkdfSalt = "CipherBank-seal-v1"u8.ToArray();
    private static readonly byte[] HkdfInfo = "seal/chacha20poly1305/v1"u8.ToArray();

    public string AlgorithmId => AlgorithmIdValue;

    public int PublicKeySize => 32;

    public int PrivateKeySize => 32;

    public AccountKeyPair DeriveKeyPair(ReadOnlySpan<byte> seed32)
    {
        if (seed32.Length != 32)
        {
            throw new ArgumentException("Seed must be 32 bytes.", nameof(seed32));
        }

        var creation = new KeyCreationParameters { ExportPolicy = KeyExportPolicies.AllowPlaintextExport };
        using var key = Key.Import(X25519, seed32, KeyBlobFormat.RawPrivateKey, creation);
        byte[] privateKey = key.Export(KeyBlobFormat.RawPrivateKey);
        byte[] publicKey = key.PublicKey.Export(KeyBlobFormat.RawPublicKey);
        return new AccountKeyPair(publicKey, privateKey);
    }

    public byte[] Seal(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> recipientPublicKey)
    {
        if (recipientPublicKey.Length != PublicKeySize)
        {
            throw new ArgumentException("Recipient public key must be 32 bytes.", nameof(recipientPublicKey));
        }

        var creation = new KeyCreationParameters { ExportPolicy = KeyExportPolicies.AllowPlaintextExport };
        using var ephemeral = Key.Create(X25519, creation);
        byte[] ephemeralPk = ephemeral.PublicKey.Export(KeyBlobFormat.RawPublicKey);

        PublicKey recipient = PublicKey.Import(X25519, recipientPublicKey, KeyBlobFormat.RawPublicKey);
        var sharedParams = new SharedSecretCreationParameters { ExportPolicy = KeyExportPolicies.AllowPlaintextExport };
        using SharedSecret shared = X25519.Agree(ephemeral, recipient, ref sharedParams)
            ?? throw new CryptographicException("X25519 key agreement failed.");

        using Key aeadKey = ImportAeadKey(shared);
        byte[] nonce = RandomNumberGenerator.GetBytes(NonceSize);
        byte[] cipher = Aead.Encrypt(aeadKey, nonce, ReadOnlySpan<byte>.Empty, plaintext);

        var result = new byte[EphemeralPublicKeySize + NonceSize + cipher.Length];
        ephemeralPk.CopyTo(result.AsSpan(0, EphemeralPublicKeySize));
        nonce.CopyTo(result.AsSpan(EphemeralPublicKeySize, NonceSize));
        cipher.CopyTo(result.AsSpan(EphemeralPublicKeySize + NonceSize));
        return result;
    }

    public byte[] Open(ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> recipientPrivateKey)
    {
        if (recipientPrivateKey.Length != PrivateKeySize)
        {
            throw new ArgumentException("Recipient private key must be 32 bytes.", nameof(recipientPrivateKey));
        }

        if (ciphertext.Length < EphemeralPublicKeySize + NonceSize + Aead.TagSize)
        {
            throw new CryptographicException("Ciphertext too short.");
        }

        ReadOnlySpan<byte> ephemeralPk = ciphertext[..EphemeralPublicKeySize];
        ReadOnlySpan<byte> nonce = ciphertext.Slice(EphemeralPublicKeySize, NonceSize);
        ReadOnlySpan<byte> cipher = ciphertext[(EphemeralPublicKeySize + NonceSize)..];

        var creation = new KeyCreationParameters { ExportPolicy = KeyExportPolicies.AllowPlaintextExport };
        using var recipient = Key.Import(X25519, recipientPrivateKey, KeyBlobFormat.RawPrivateKey, creation);
        PublicKey ephemeral = PublicKey.Import(X25519, ephemeralPk, KeyBlobFormat.RawPublicKey);
        var sharedParams = new SharedSecretCreationParameters { ExportPolicy = KeyExportPolicies.AllowPlaintextExport };
        using SharedSecret shared = X25519.Agree(recipient, ephemeral, ref sharedParams)
            ?? throw new CryptographicException("X25519 key agreement failed.");

        using Key aeadKey = ImportAeadKey(shared);
        return Aead.Decrypt(aeadKey, nonce, ReadOnlySpan<byte>.Empty, cipher)
            ?? throw new CryptographicException("Challenge open failed (auth tag).");
    }

    private static Key ImportAeadKey(SharedSecret shared)
    {
        byte[] ikm = shared.Export(SharedSecretBlobFormat.RawSharedSecret);
        try
        {
            byte[] keyBytes = HKDF.DeriveKey(
                HashAlgorithmName.SHA256,
                ikm,
                Aead.KeySize,
                HkdfSalt,
                HkdfInfo);
            try
            {
                var creation = new KeyCreationParameters { ExportPolicy = KeyExportPolicies.AllowPlaintextArchiving };
                return Key.Import(Aead, keyBytes, KeyBlobFormat.RawSymmetricKey, creation);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(keyBytes);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(ikm);
        }
    }
}
