// <copyright file="ChallengePassSessionProofBuilder.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.V1;

namespace CipherBank_app.ChallengePass;

/// <summary>
/// <see cref="ISessionProofBuilder"/> that delegates to the active challenge/pass suite.
/// Account keypair is supplied by <see cref="IAccountKeySource"/> (custody-backed in app).
/// </summary>
public sealed class ChallengePassSessionProofBuilder : ISessionProofBuilder
{
    private readonly IChallengePassCatalog _catalog;
    private readonly IAccountKeySource _keys;

    public ChallengePassSessionProofBuilder(IChallengePassCatalog catalog, IAccountKeySource keys)
    {
        _catalog = catalog;
        _keys = keys;
    }

    public async Task<object> BuildOpenBodyAsync(CancellationToken ct = default)
    {
        ChallengePassSuite suite = _catalog.GetActive();
        AccountKeyPair pair = _keys.RequireUnlockedKeyPair(suite.Algorithm);
        string wire = WireEncoding.ToWire(pair.PublicKey);
        return await suite.Structure
            .BuildSessionOpenBodyAsync(suite.Algorithm, suite.Template, pair, wire, ct)
            .ConfigureAwait(false);
    }
}

/// <summary>Provides unlocked account key material to the proof builder.</summary>
public interface IAccountKeySource
{
    AccountKeyPair RequireUnlockedKeyPair(ISealAlgorithm algorithm);
}
