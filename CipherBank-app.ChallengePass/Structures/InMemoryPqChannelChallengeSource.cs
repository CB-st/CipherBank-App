// <copyright file="InMemoryPqChannelChallengeSource.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Globalization;
using System.Security.Cryptography;
using CipherBank_app.ChallengePass.Templates;
using CipherBank_app.V1;

namespace CipherBank_app.ChallengePass.Hybrid;

/// <summary>Mock challenge issuer using the same channel key as <see cref="InMemoryPqKeyShareClient"/>.</summary>
public sealed class InMemoryPqChannelChallengeSource : IPqChannelChallengeSource, IDisposable
{
    private const int ChallengeIdHexLength = 16;

    private readonly InMemoryPqKeyShareClient _keyShare;
    private readonly IChallengeTemplate _template;
    private readonly PqSymmetricChannel _serverChannel = new();
    private bool _disposed;

    public InMemoryPqChannelChallengeSource(InMemoryPqKeyShareClient keyShare)
        : this(keyShare, null)
    {
    }

    public InMemoryPqChannelChallengeSource(InMemoryPqKeyShareClient keyShare, IChallengeTemplate? template)
    {
        _keyShare = keyShare;
        _template = template ?? new ChallengeIdNonceSha256Template();
    }

    public Task<SessionChallengeDto> RequestChallengeAsync()
        => RequestChallengeAsync(CancellationToken.None);

    public Task<SessionChallengeDto> RequestChallengeAsync(CancellationToken ct)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_keyShare.LastChannelKey is null || _keyShare.LastKeyShareId is null)
        {
            throw new InvalidOperationException("Key share must complete before challenge.");
        }

        _serverChannel.SetChannelKey(_keyShare.LastChannelKey, _keyShare.LastKeyShareId);

        string challengeId = "ch_" + Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)[..ChallengeIdHexLength];
        byte[] nonce = RandomNumberGenerator.GetBytes(_template.MinNonceLength);
        byte[] plaintext = _template.BuildChallengePlaintext(new ChallengeBindContext
        {
            ChallengeId = challengeId,
            Nonce = nonce,
        });
        byte[] cipher = _serverChannel.Seal(plaintext);

        return Task.FromResult(new SessionChallengeDto
        {
            ChallengeId = challengeId,
            Ciphertext = WireEncoding.ToWire(cipher),
            ApiPublicKey = string.Empty,
            ApiKeyId = _keyShare.LastKeyShareId,
            Algorithm = HybridMlKemX25519Agreement.ChannelAlgorithmId,
        });
    }

    /// <summary>
    /// Disposes the owned server-side PQ channel after challenge issuance is complete.
    /// Use: Medium (test/DI teardown). Scope: this challenge source instance.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _serverChannel.Dispose();
        _disposed = true;
    }
}
