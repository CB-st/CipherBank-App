// <copyright file="IPqChannel.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.ChallengePass.Hybrid;

/// <summary>PQ-established symmetric channel: ChaCha20-Poly1305 with the shared key.</summary>
public interface IPqChannel
{
    /// <summary>Gets a value indicating whether a channel key is currently installed.</summary>
    bool IsEstablished { get; }

    /// <summary>Gets the key-share id the current key was established under, or null when cleared.</summary>
    string? KeyShareId { get; }

    /// <summary>Gets the wire algorithm id of the channel AEAD.</summary>
    string ChannelAlgorithmId { get; }

    /// <summary>
    /// Installs a 32-byte channel key, wiping any displaced key first. The channel copies
    /// <paramref name="channelKey32"/>; the caller keeps ownership of its buffer and must zero it.
    /// Use: Low (A2 key-share establish). Scope: this channel instance.
    /// </summary>
    void SetChannelKey(byte[] channelKey32, string keyShareId);

    /// <summary>
    /// Zeroes and drops the retained channel key; idempotent.
    /// Use: Medium (lock / identity clear). Scope: this channel instance.
    /// </summary>
    void Clear();

    /// <summary>
    /// AEAD-seals <paramref name="plaintext"/> with a fresh nonce; returns nonce-prefixed
    /// ciphertext owned by the caller. Throws when no key is established.
    /// Use: Medium (A2 session traffic). Scope: this channel instance.
    /// </summary>
    byte[] Seal(ReadOnlySpan<byte> plaintext);

    /// <summary>
    /// Opens nonce-prefixed ciphertext from <see cref="Seal"/>; the returned plaintext is
    /// caller-owned and must be wiped when secret. Throws on authentication failure or no key.
    /// Use: Medium (A2 session traffic). Scope: this channel instance.
    /// </summary>
    byte[] Open(ReadOnlySpan<byte> ciphertext);
}
