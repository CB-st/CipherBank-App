// <copyright file="AesGcmUserDataBlockCipher.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Security.Cryptography;
using System.Text;

namespace CipherBank_app.UserData;

/// <summary>
/// Pack-block cipher: AES-GCM via <see cref="IUserDataSymmetricCipher"/> + userdata AAD.
/// Use: Low (suite build). Scope: userdata crypto.
/// </summary>
/// <param name="symmetric">Symmetric AEAD used to seal and open pack blocks.</param>
public sealed class AesGcmUserDataBlockCipher(IUserDataSymmetricCipher symmetric) : IUserDataBlockCipher
{
    private readonly IUserDataSymmetricCipher _symmetric = symmetric;

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
        (string id, string type) = ValidateHeader(block);

        byte[] nonce = DecodeBase64(block.NonceBase64);
        byte[] tag = DecodeBase64(block.TagBase64);
        byte[] ciphertext = DecodeBase64(block.CiphertextBase64);
        byte[] aad = UserDataAad.Build(usernameHashHex, type, id, contentVersion);
        UserDataSymmetricBlob blob = new(nonce, tag, ciphertext);
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

    private static (string Id, string Type) ValidateHeader(UserDataBlockWire block)
    {
        if (block.Algorithm != UserDataConstants.BlockAlgorithm)
        {
            throw new CryptographicException("Unsupported userdata block algorithm.");
        }

        string? id = block.Id;
        string? type = block.Type;
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(type))
        {
            throw new CryptographicException("Userdata block id/type missing.");
        }

        return (id, type);
    }

    private static byte[] DecodeBase64(string? s)
    {
        // Preserve the exception contract of Convert.FromBase64String for a null field.
        ArgumentNullException.ThrowIfNull(s);
        return Convert.FromBase64String(s);
    }
}
