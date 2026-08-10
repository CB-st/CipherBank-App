// <copyright file="PqChannelChallengePassStructure.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Security.Cryptography;
using CipherBank_app.V1;

namespace CipherBank_app.ChallengePass.Hybrid;

/// <summary>
/// Structure slot: ensure hybrid key-share → open challenge with channel key → seal pass with channel key.
/// </summary>
public sealed class PqChannelChallengePassStructure : IChallengePassStructure, IDisposable
{
    private readonly IPqKeyShareClient _keyShare;
    private readonly IPqChannel _channel;
    private readonly IPqChannelChallengeSource _challenges;
    private readonly SemaphoreSlim _buildGate = new(1, 1);
    private readonly object _identityGate = new();
    private HybridPrivateIdentity? _identity;
    private byte[]? _channelIdentityPublicKey;

    public PqChannelChallengePassStructure(
        IPqKeyShareClient keyShare,
        IPqChannel channel,
        IPqChannelChallengeSource challenges)
    {
        _keyShare = keyShare;
        _channel = channel;
        _challenges = challenges;
    }

    public static string StructureIdValue => "pq-channel-challenge-pass-v1";

    public string StructureId => StructureIdValue;

    /// <summary>
    /// Clears cached hybrid identity and channel key material (custody lock / dispose).
    /// Use: High (every custody lock). Scope: singleton structure lifetime.
    /// </summary>
    public void ClearDeviceIdentity()
    {
        _buildGate.Wait();
        try
        {
            ClearDeviceIdentityUnlocked();
        }
        finally
        {
            _buildGate.Release();
        }
    }

    /// <summary>
    /// Releases the build gate and zeroes cached identity / channel material.
    /// Use: Low (app shutdown). Scope: singleton structure lifetime.
    /// </summary>
    public void Dispose()
    {
        ClearDeviceIdentity();
        _buildGate.Dispose();
    }

    /// <summary>
    /// Supply hybrid identity (from custody entropy) for cache / lock clearing.
    /// Production A2 builds should use <see cref="BuildSessionOpenBodyWithIdentityAsync"/> so
    /// set-and-build share one <c>_buildGate</c> hold. Clears an established channel when the
    /// public X25519 key changes so a mnemonic swap cannot seal with the previous account's key share.
    /// Use: Medium (tests / explicit cache). Scope: singleton structure / per-device identity.
    /// </summary>
    public void SetDeviceIdentity(HybridPrivateIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);
        _buildGate.Wait();
        try
        {
            ApplyIdentityUnlocked(identity);
        }
        finally
        {
            _buildGate.Release();
        }
    }

    /// <summary>
    /// Sets device identity and builds the A2 body under one gate hold so channel + account wire
    /// always come from the same custody snapshot (no Set/Build interleave).
    /// Use: High (every A2 unlock via ChallengePassSessionProofBuilder). Scope: singleton structure.
    /// </summary>
    public Task<object> BuildSessionOpenBodyWithIdentityAsync(
        ISealAlgorithm algorithm,
        IChallengeTemplate challengeTemplate,
        HybridPrivateIdentity identity,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(identity);
        return BuildSessionOpenBodyWithIdentityCoreAsync(algorithm, challengeTemplate, identity, ct);
    }

    /// <summary>
    /// Rejects the legacy Set-then-Build path; A2 callers must use the fused identity API.
    /// Use: Low (miswired IChallengePassStructure callers). Scope: interface surface.
    /// </summary>
    public Task<object> BuildSessionOpenBodyAsync(
        ISealAlgorithm algorithm,
        IChallengeTemplate challengeTemplate,
        AccountKeyPair accountKey,
        string accountPublicKeyWire)
        => BuildSessionOpenBodyAsync(algorithm, challengeTemplate, accountKey, accountPublicKeyWire, CancellationToken.None);

    /// <summary>
    /// Rejects sourcing device identity from mutable <c>_identity</c> after a prior
    /// <see cref="SetDeviceIdentity"/>; channel + account wire must bind under one gate via
    /// <see cref="BuildSessionOpenBodyWithIdentityAsync"/>.
    /// Use: Low (miswired IChallengePassStructure callers). Scope: interface surface.
    /// </summary>
    public Task<object> BuildSessionOpenBodyAsync(
        ISealAlgorithm algorithm,
        IChallengeTemplate challengeTemplate,
        AccountKeyPair accountKey,
        string accountPublicKeyWire,
        CancellationToken ct)
    {
        _ = algorithm;
        _ = challengeTemplate;
        _ = accountKey;
        _ = accountPublicKeyWire;
        _ = ct;
        return Task.FromException<object>(new InvalidOperationException(
            "A2 PQ builds must use BuildSessionOpenBodyWithIdentityAsync so identity and channel bind under one _buildGate hold."));
    }

    private async Task<object> BuildSessionOpenBodyWithIdentityCoreAsync(
        ISealAlgorithm algorithm,
        IChallengeTemplate challengeTemplate,
        HybridPrivateIdentity identity,
        CancellationToken ct)
    {
        // Caller-owned identity is only adopted after the gate is held. If WaitAsync is cancelled
        // first, wipe here so custody-fresh material cannot linger unadopted on the heap.
        try
        {
            await _buildGate.WaitAsync(ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            HybridIdentityBuffers.Zero(identity);
            throw;
        }

        try
        {
            ApplyIdentityUnlocked(identity);
            AccountKeyPair pair = new AccountKeyPair(identity.X25519PublicKey, identity.X25519PrivateKey);
            string wire = WireEncoding.ToWire(identity.X25519PublicKey);
            return await BuildBodyCoreAsync(algorithm, challengeTemplate, pair, wire, identity, ct)
                .ConfigureAwait(false);
        }
        finally
        {
            _buildGate.Release();
        }
    }

    private async Task<object> BuildBodyCoreAsync(
        ISealAlgorithm algorithm,
        IChallengeTemplate challengeTemplate,
        AccountKeyPair accountKey,
        string accountPublicKeyWire,
        HybridPrivateIdentity identity,
        CancellationToken ct)
    {
        _ = algorithm;
        _ = accountKey;

        await EnsureChannelEstablishedAsync(identity, ct).ConfigureAwait(false);

        SessionChallengeDto challenge = await _challenges.RequestChallengeAsync(ct).ConfigureAwait(false);
        if (!string.IsNullOrEmpty(challenge.Algorithm)
            && !challenge.Algorithm.Equals(HybridMlKemX25519Agreement.ChannelAlgorithmId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Channel challenge ALGORITHM '{challenge.Algorithm}' does not match '{HybridMlKemX25519Agreement.ChannelAlgorithmId}'.");
        }

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

    /// <summary>
    /// Establishes or re-establishes the PQ channel for the captured identity.
    /// Use: Medium (inside serialized build). Scope: shared singleton IPqChannel.
    /// </summary>
    private async Task EnsureChannelEstablishedAsync(HybridPrivateIdentity identity, CancellationToken ct)
    {
        bool needsEstablish = !_channel.IsEstablished
            || _channelIdentityPublicKey is null
            || _channelIdentityPublicKey.Length != identity.X25519PublicKey.Length
            || !CryptographicOperations.FixedTimeEquals(_channelIdentityPublicKey, identity.X25519PublicKey);

        if (!needsEstablish)
        {
            return;
        }

        _channel.Clear();
        PqKeyShareResponse share = await _keyShare.EstablishAsync(identity.ToPublic(), ct).ConfigureAwait(false);
        if (!string.IsNullOrEmpty(share.Algorithm)
            && !share.Algorithm.Equals(HybridMlKemX25519Agreement.KeyShareAlgorithmId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Key-share ALGORITHM '{share.Algorithm}' does not match '{HybridMlKemX25519Agreement.KeyShareAlgorithmId}'.");
        }

        byte[] channelKey = HybridMlKemX25519Agreement.CompleteAsDevice(identity, share);
        _channel.SetChannelKey(channelKey, share.KeyShareId);
        CryptographicOperations.ZeroMemory(channelKey);
        _channelIdentityPublicKey = identity.X25519PublicKey.ToArray();
    }

    private void ApplyIdentityUnlocked(HybridPrivateIdentity identity)
    {
        lock (_identityGate)
        {
            if (_identity is not null && !ReferenceEquals(_identity, identity))
            {
                bool pubkeyChanged =
                    _identity.X25519PublicKey.Length != identity.X25519PublicKey.Length
                    || !CryptographicOperations.FixedTimeEquals(_identity.X25519PublicKey, identity.X25519PublicKey);

                HybridIdentityBuffers.Zero(_identity);

                // Only drop the channel-key binding when the public X25519 identity actually changes.
                // Custody hands a new HybridPrivateIdentity object every unlock with the same pubkey;
                // nulling here would force a redundant key-share on every fused A2 build.
                if (pubkeyChanged)
                {
                    if (_channelIdentityPublicKey is not null)
                    {
                        CryptographicOperations.ZeroMemory(_channelIdentityPublicKey);
                        _channelIdentityPublicKey = null;
                    }

                    _channel.Clear();
                }
            }

            _identity = identity;
        }
    }

    private void ClearDeviceIdentityUnlocked()
    {
        lock (_identityGate)
        {
            if (_identity is not null)
            {
                HybridIdentityBuffers.Zero(_identity);
                _identity = null;
            }

            if (_channelIdentityPublicKey is not null)
            {
                CryptographicOperations.ZeroMemory(_channelIdentityPublicKey);
                _channelIdentityPublicKey = null;
            }
        }

        _channel.Clear();
    }
}
