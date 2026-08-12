// <copyright file="AesGcmUserDataBlockCipher.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Security.Cryptography;
using System.Text;

namespace CipherBank_app.UserData;

/// <summary>Pack-block cipher: AES-GCM via <see cref="IUserDataSymmetricCipher"/> + userdata AAD.</summary>
public sealed class AesGcmUserDataBlockCipher : IUserDataBlockCipher
{
    private readonly IUserDataSymmetricCipher _symmetric;

    /// <summary>
    /// Wraps a symmetric AEAD for pack seal/open. Use: Low (suite build). Scope: userdata crypto.
    /// </summary>
    public AesGcmUserDataBlockCipher(IUserDataSymmetricCipher symmetric)
    {
        ArgumentNullException.ThrowIfNull(symmetric);
        _symmetric = symmetric;
    }

    public AesGcmUserDataBlockCipher()
        : this(new AesGcmUserDataSymmetricCipher())
    {
    }

    public string AlgorithmId => _symmetric.AlgorithmId;

    /// <inheritdoc />
    public UserDataBlockWire Seal(
        UserDataPlainBlock plain,
        ReadOnlySpan<byte> kek,
        string usernameHashHex,
        uint contentVersion)
    {
        ArgumentNullException.ThrowIfNull(plain);
        byte[] plaintext = Encoding.UTF8.GetBytes(plain.PlaintextUtf8);
        byte[] aad = UserDataAad.Build(usernameHashHex, plain.Type, plain.Id, contentVersion);
        try
        {
            UserDataSymmetricBlob blob = _symmetric.Seal(plaintext, kek, aad);
            return ToWire(plain, blob);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
            CryptographicOperations.ZeroMemory(aad);
        }
    }

    /// <inheritdoc />
    public string Open(
        UserDataBlockWire block,
        ReadOnlySpan<byte> kek,
        string usernameHashHex,
        uint contentVersion)
    {
        ArgumentNullException.ThrowIfNull(block);
        ValidateHeader(block);

        byte[] nonce = Convert.FromBase64String(block.NonceBase64);
        byte[] tag = Convert.FromBase64String(block.TagBase64);
        byte[] ciphertext = Convert.FromBase64String(block.CiphertextBase64);
        byte[] aad = UserDataAad.Build(usernameHashHex, block.Type, block.Id, contentVersion);
        UserDataSymmetricBlob blob = new UserDataSymmetricBlob(nonce, tag, ciphertext);
        byte[] plain = _symmetric.Open(blob, kek, aad);
        try
        {
            return Encoding.UTF8.GetString(plain);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plain);
            CryptographicOperations.ZeroMemory(aad);
        }
    }

    /// <summary>
    /// Seals with an injected nonce for pinned vectors. Use: Low (tests). Scope: block cipher.
    /// </summary>
    internal UserDataBlockWire SealWithNonce(
        UserDataPlainBlock plain,
        ReadOnlySpan<byte> kek,
        string usernameHashHex,
        uint contentVersion,
        ReadOnlySpan<byte> nonce12)
    {
        ArgumentNullException.ThrowIfNull(plain);
        byte[] plaintext = Encoding.UTF8.GetBytes(plain.PlaintextUtf8);
        byte[] aad = UserDataAad.Build(usernameHashHex, plain.Type, plain.Id, contentVersion);
        try
        {
            UserDataSymmetricBlob blob = _symmetric.SealWithNonce(plaintext, kek, aad, nonce12);
            return ToWire(plain, blob);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
            CryptographicOperations.ZeroMemory(aad);
        }
    }

    private static UserDataBlockWire ToWire(UserDataPlainBlock plain, UserDataSymmetricBlob blob)
        => new()
        {
            Id = plain.Id,
            Type = plain.Type,
            Seq = plain.Seq,
            Algorithm = UserDataConstants.BlockAlgorithm,
            NonceBase64 = Convert.ToBase64String(blob.Nonce),
            TagBase64 = Convert.ToBase64String(blob.Tag),
            CiphertextBase64 = Convert.ToBase64String(blob.Ciphertext),
        };

    private static void ValidateHeader(UserDataBlockWire block)
    {
        if (block.Algorithm != UserDataConstants.BlockAlgorithm)
        {
            throw new CryptographicException("Unsupported userdata block algorithm.");
        }

        if (string.IsNullOrWhiteSpace(block.Id) || string.IsNullOrWhiteSpace(block.Type))
        {
            throw new CryptographicException("Userdata block id/type missing.");
        }
    }
}
