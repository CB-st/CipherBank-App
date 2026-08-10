// <copyright file="PqSymmetricChannel.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Security.Cryptography;
using CipherBank_app.ChallengePass.Crypto;

namespace CipherBank_app.ChallengePass.Hybrid;

/// <inheritdoc />
public sealed class PqSymmetricChannel : IPqChannel, IDisposable
{
    private const int ChannelKeySizeBytes = 32;
    private const int NonceSize = PortableChaCha20Poly1305.NonceSize;
    private const int TagSize = PortableChaCha20Poly1305.TagSize;

    private byte[]? _key;

    public bool IsEstablished => _key is not null;

    public string? KeyShareId { get; private set; }

    public string ChannelAlgorithmId => HybridMlKemX25519Agreement.ChannelAlgorithmId;

    public void SetChannelKey(byte[] channelKey32, string keyShareId)
    {
        ArgumentNullException.ThrowIfNull(channelKey32);
        if (channelKey32.Length != ChannelKeySizeBytes)
        {
            throw new ArgumentException("Channel key must be 32 bytes.", nameof(channelKey32));
        }

        Clear();
        _key = channelKey32.ToArray();
        KeyShareId = keyShareId;
    }

    public void Clear()
    {
        if (_key is not null)
        {
            CryptographicOperations.ZeroMemory(_key);
            _key = null;
        }

        KeyShareId = null;
    }

    public byte[] Seal(ReadOnlySpan<byte> plaintext)
    {
        byte[] key = RequireKey();
        byte[] nonce = RandomNumberGenerator.GetBytes(NonceSize);
        byte[] cipher = PortableChaCha20Poly1305.Encrypt(key, nonce, plaintext);
        byte[] result = new byte[NonceSize + cipher.Length];
        nonce.CopyTo(result.AsSpan(0, NonceSize));
        cipher.CopyTo(result.AsSpan(NonceSize));
        return result;
    }

    public byte[] Open(ReadOnlySpan<byte> ciphertext)
    {
        byte[] key = RequireKey();
        if (ciphertext.Length < NonceSize + TagSize)
        {
            throw new CryptographicException("Ciphertext too short.");
        }

        ReadOnlySpan<byte> nonce = ciphertext[..NonceSize];
        ReadOnlySpan<byte> body = ciphertext[NonceSize..];
        return PortableChaCha20Poly1305.Decrypt(key, nonce, body);
    }

    public void Dispose() => Clear();

    private byte[] RequireKey()
        => _key ?? throw new InvalidOperationException("PQ channel not established. Complete hybrid key share first.");
}
