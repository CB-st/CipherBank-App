// <copyright file="UserDataCryptoCatalog.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.UserData;

/// <summary>In-memory suite catalog; default active = <see cref="UserDataConstants.SuiteRsaAesGcmV1"/>.</summary>
public sealed class UserDataCryptoCatalog : IUserDataCryptoCatalog
{
    private readonly Dictionary<string, UserDataCryptoSuite> _suites;
    private string _activeSuiteId;

    /// <summary>
    /// Registers built-in RSA+AES suite (PQ suite id reserved, not registered until implemented).
    /// Use: Low (composition root). Scope: process lifetime.
    /// </summary>
    public UserDataCryptoCatalog()
        : this([UserDataCryptoSuites.CreateRsaAesGcmV1()], UserDataConstants.SuiteRsaAesGcmV1)
    {
    }

    /// <summary>
    /// Test / custom suite injection. Use: Low (tests). Scope: process lifetime.
    /// </summary>
    public UserDataCryptoCatalog(IEnumerable<UserDataCryptoSuite> suites, string activeSuiteId)
    {
        ArgumentNullException.ThrowIfNull(suites);
        ArgumentException.ThrowIfNullOrWhiteSpace(activeSuiteId);

        _suites = new Dictionary<string, UserDataCryptoSuite>(StringComparer.Ordinal);
        foreach (UserDataCryptoSuite suite in suites)
        {
            _suites[suite.SuiteId] = suite;
        }

        if (_suites.Count == 0)
        {
            throw new ArgumentException("At least one suite is required.", nameof(suites));
        }

        _activeSuiteId = activeSuiteId;
        SetActive(activeSuiteId);
    }

    public string ActiveSuiteId => _activeSuiteId;

    public UserDataCryptoSuite Active => _suites[_activeSuiteId];

    /// <inheritdoc />
    public void SetActive(string suiteId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(suiteId);
        if (!_suites.ContainsKey(suiteId))
        {
            throw new ArgumentException($"Unknown userdata crypto suite '{suiteId}'.", nameof(suiteId));
        }

        _activeSuiteId = suiteId;
    }

    /// <inheritdoc />
    public UserDataCryptoSuite GetSuite(string suiteId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(suiteId);
        if (!_suites.TryGetValue(suiteId, out UserDataCryptoSuite? suite))
        {
            throw new ArgumentException($"Unknown userdata crypto suite '{suiteId}'.", nameof(suiteId));
        }

        return suite;
    }
}
