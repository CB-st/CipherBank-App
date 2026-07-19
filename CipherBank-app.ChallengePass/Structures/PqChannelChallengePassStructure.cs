// <copyright file="PqChannelChallengePassStructure.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Security.Cryptography;
using CipherBank_app.ChallengePass.Templates;
using CipherBank_app.V1;

namespace CipherBank_app.ChallengePass.Hybrid;

/// <summary>
/// Structure slot: ensure hybrid key-share → open challenge with channel key → seal pass with channel key.
/// </summary>
public sealed class PqChannelChallengePassStructure : IChallengePassStructure
{
    public const string StructureIdValue = "pq-channel-challenge-pass-v1";

    private readonly IPqKeyShareClient _keyShare;
    private readonly IPqChannel _channel;
    private readonly IPqChannelChallengeSource _challenges;
    private readonly HybridMlKemX25519Agreement _agreement = new();
    private HybridPrivateIdentity? _identity;

    public PqChannelChallengePassStructure(
        IPqKeyShareClient keyShare,
        IPqChannel channel,
        IPqChannelChallengeSource challenges)
    {
        _keyShare = keyShare;
        _channel = channel;
        _challenges = challenges;
    }

    public string StructureId => StructureIdValue;

    /// <summary>Supply hybrid identity (from custody entropy) before session open.</summary>
    public void SetDeviceIdentity(HybridPrivateIdentity identity) => _identity = identity;

    public async Task<object> BuildSessionOpenBodyAsync(
        ISealAlgorithm algorithm,
        IChallengeTemplate challengeTemplate,
        AccountKeyPair accountKey,
        string accountPublicKeyWire,
        CancellationToken ct = default)
    {
        HybridPrivateIdentity identity = _identity
            ?? throw new InvalidOperationException("Hybrid device identity not set on PQ structure.");

        if (!_channel.IsEstablished)
        {
            PqKeyShareResponse share = await _keyShare.EstablishAsync(identity.ToPublic(), ct).ConfigureAwait(false);
            byte[] channelKey = _agreement.CompleteAsDevice(identity, share);
            _channel.SetChannelKey(channelKey, share.KeyShareId);
            CryptographicOperations.ZeroMemory(channelKey);
        }

        SessionChallengeDto challenge = await _challenges.RequestChallengeAsync(ct).ConfigureAwait(false);
        byte[] ciphertext = WireEncoding.FromWire(challenge.Ciphertext);
        byte[] plaintext = _channel.Open(ciphertext);
        ParsedChallenge parsed = challengeTemplate.ParseChallengePlaintext(plaintext);

        if (!parsed.ChallengeId.Equals(challenge.ChallengeId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Opened challenge id mismatch.");
        }

        byte[] passPayload = challengeTemplate.BuildPassPayload(parsed);
        byte[] passCipher = _channel.Seal(passPayload);

        return new SessionPassDto
        {
            ChallengeId = challenge.ChallengeId,
            PassCiphertext = WireEncoding.ToWire(passCipher),
            AccountPublicKey = accountPublicKeyWire,
            ApiKeyId = challenge.ApiKeyId ?? _channel.KeyShareId,
            Algorithm = _channel.ChannelAlgorithmId,
        };
    }
}

/// <summary>Issues challenges sealed with the shared PQ channel key (server side).</summary>
public interface IPqChannelChallengeSource
{
    Task<SessionChallengeDto> RequestChallengeAsync(CancellationToken ct = default);
}

/// <summary>Mock challenge issuer using the same channel key as <see cref="InMemoryPqKeyShareClient"/>.</summary>
public sealed class InMemoryPqChannelChallengeSource : IPqChannelChallengeSource
{
    private readonly InMemoryPqKeyShareClient _keyShare;
    private readonly IChallengeTemplate _template;
    private readonly PqSymmetricChannel _serverChannel = new();

    public InMemoryPqChannelChallengeSource(InMemoryPqKeyShareClient keyShare, IChallengeTemplate? template = null)
    {
        _keyShare = keyShare;
        _template = template ?? new ChallengeIdNonceSha256Template();
    }

    public Task<SessionChallengeDto> RequestChallengeAsync(CancellationToken ct = default)
    {
        if (_keyShare.LastChannelKey is null || _keyShare.LastKeyShareId is null)
        {
            throw new InvalidOperationException("Key share must complete before challenge.");
        }

        _serverChannel.SetChannelKey(_keyShare.LastChannelKey, _keyShare.LastKeyShareId);

        string challengeId = "ch_" + Guid.NewGuid().ToString("N")[..16];
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
}
