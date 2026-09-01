// <copyright file="ChallengePassSessionProofBuilder.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Security.Cryptography;
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

    /// <summary>
    /// Builds the active suite's session-open body: A2 via fused identity API, A1 with private-key wipe.
    /// Use: High (every unlock). Scope: session proof path.
    /// </summary>
    public async Task<object> BuildOpenBodyAsync(CancellationToken ct)
    {
        ChallengePassSuite suite = _catalog.Active;

        if (suite.Structure is PqChannelChallengePassStructure pqStructure)
        {
            HybridPrivateIdentity hybrid = _keys.RequireHybridIdentity();
            return await pqStructure
                .BuildSessionOpenBodyWithIdentityAsync(suite.Algorithm, suite.Template, hybrid, ct)
                .ConfigureAwait(false);
        }

        AccountKeyPair a1 = _keys.RequireUnlockedKeyPair(suite.Algorithm);
        try
        {
            string a1Wire = WireEncoding.ToWire(a1.PublicKey);
            return await suite.Structure
                .BuildSessionOpenBodyAsync(suite.Algorithm, suite.Template, a1, a1Wire, ct)
                .ConfigureAwait(false);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(a1.PrivateKey);
        }
    }
}
