// <copyright file="ChannelSealAlgorithm.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.ChallengePass.Hybrid;

/// <summary>
/// Seal slot adapter: Seal/Open via established <see cref="IPqChannel"/>.
/// Recipient key arguments are ignored (channel already bound by key-share).
/// </summary>
public sealed class ChannelSealAlgorithm : ISealAlgorithm
{
    private readonly IPqChannel _channel;

    public ChannelSealAlgorithm(IPqChannel channel)
    {
        _channel = channel;
    }

    public string AlgorithmId => _channel.ChannelAlgorithmId;

    public int PublicKeySize => 0;

    public int PrivateKeySize => 0;

    public AccountKeyPair DeriveKeyPair(ReadOnlySpan<byte> seed32)
        => throw new NotSupportedException("PQ channel keys come from hybrid key-share, not DeriveKeyPair.");

    public byte[] Seal(ReadOnlySpan<byte> plaintext, ReadOnlySpan<byte> recipientPublicKey)
        => _channel.Seal(plaintext);

    public byte[] Open(ReadOnlySpan<byte> ciphertext, ReadOnlySpan<byte> recipientPrivateKey)
        => _channel.Open(ciphertext);
}
