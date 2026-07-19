// <copyright file="PqSymmetricChannel.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Security.Cryptography;
using NSec.Cryptography;

namespace CipherBank_app.ChallengePass.Hybrid;

/// <summary>PQ-established symmetric channel: ChaCha20-Poly1305 with the shared key.</summary>
public interface IPqChannel
{
    bool IsEstablished { get; }

    string? KeyShareId { get; }

    string ChannelAlgorithmId { get; }

    void SetChannelKey(byte[] channelKey32, string keyShareId);

    void Clear();

    byte[] Seal(ReadOnlySpan<byte> plaintext);

    byte[] Open(ReadOnlySpan<byte> ciphertext);
}

/// <inheritdoc />
public sealed class PqSymmetricChannel : IPqChannel, IDisposable
{
    private static readonly AeadAlgorithm Aead = AeadAlgorithm.ChaCha20Poly1305;
    private const int NonceSize = 12;

    private byte[]? _key;
    private Key? _aeadKey;

    public bool IsEstablished => _key is not null;

    public string? KeyShareId { get; private set; }

    public string ChannelAlgorithmId => HybridMlKemX25519Agreement.ChannelAlgorithmId;

    public void SetChannelKey(byte[] channelKey32, string keyShareId)
    {
        ArgumentNullException.ThrowIfNull(channelKey32);
        if (channelKey32.Length != 32)
        {
            throw new ArgumentException("Channel key must be 32 bytes.", nameof(channelKey32));
        }

        Clear();
        _key = channelKey32.ToArray();
        KeyShareId = keyShareId;
        var creation = new KeyCreationParameters { ExportPolicy = KeyExportPolicies.AllowPlaintextArchiving };
        _aeadKey = Key.Import(Aead, _key, KeyBlobFormat.RawSymmetricKey, creation);
    }

    public void Clear()
    {
        if (_key is not null)
        {
            CryptographicOperations.ZeroMemory(_key);
            _key = null;
        }

        _aeadKey?.Dispose();
        _aeadKey = null;
        KeyShareId = null;
    }

    public byte[] Seal(ReadOnlySpan<byte> plaintext)
    {
        EnsureReady();
        byte[] nonce = RandomNumberGenerator.GetBytes(NonceSize);
        byte[] cipher = Aead.Encrypt(_aeadKey!, nonce, ReadOnlySpan<byte>.Empty, plaintext);
        var result = new byte[NonceSize + cipher.Length];
        nonce.CopyTo(result.AsSpan(0, NonceSize));
        cipher.CopyTo(result.AsSpan(NonceSize));
        return result;
    }

    public byte[] Open(ReadOnlySpan<byte> ciphertext)
    {
        EnsureReady();
        if (ciphertext.Length < NonceSize + Aead.TagSize)
        {
            throw new CryptographicException("Ciphertext too short.");
        }

        ReadOnlySpan<byte> nonce = ciphertext[..NonceSize];
        ReadOnlySpan<byte> body = ciphertext[NonceSize..];
        return Aead.Decrypt(_aeadKey!, nonce, ReadOnlySpan<byte>.Empty, body)
            ?? throw new CryptographicException("Channel open failed.");
    }

    public void Dispose() => Clear();

    private void EnsureReady()
    {
        if (_aeadKey is null)
        {
            throw new InvalidOperationException("PQ channel not established. Complete hybrid key share first.");
        }
    }
}
