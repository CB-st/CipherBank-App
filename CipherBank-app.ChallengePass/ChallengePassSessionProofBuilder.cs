// <copyright file="ChallengePassSessionProofBuilder.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.ChallengePass.Hybrid;
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

        if (suite.Structure is PqChannelChallengePassStructure pqStructure)
        {
            HybridPrivateIdentity hybrid = _keys.RequireHybridIdentity();
            pqStructure.SetDeviceIdentity(hybrid);
            var pair = new AccountKeyPair(hybrid.X25519PublicKey, hybrid.X25519PrivateKey);
            string wire = WireEncoding.ToWire(hybrid.X25519PublicKey);
            return await suite.Structure
                .BuildSessionOpenBodyAsync(suite.Algorithm, suite.Template, pair, wire, ct)
                .ConfigureAwait(false);
        }

        AccountKeyPair a1 = _keys.RequireUnlockedKeyPair(suite.Algorithm);
        string a1Wire = WireEncoding.ToWire(a1.PublicKey);
        return await suite.Structure
            .BuildSessionOpenBodyAsync(suite.Algorithm, suite.Template, a1, a1Wire, ct)
            .ConfigureAwait(false);
    }
}
