// <copyright file="IPqChannel.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

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
