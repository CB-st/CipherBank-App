// <copyright file="ChallengePassCatalog.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.ChallengePass;

/// <summary>In-memory suite registry with runtime active-suite swap.</summary>
public sealed class ChallengePassCatalog : IChallengePassCatalog
{
    private readonly Dictionary<string, ChallengePassSuite> _suites;
    private string _activeSuiteId;

    public ChallengePassCatalog(IEnumerable<ChallengePassSuite> suites, string activeSuiteId)
    {
        _suites = suites.ToDictionary(s => s.SuiteId, StringComparer.OrdinalIgnoreCase);
        if (_suites.Count == 0)
        {
            throw new ArgumentException("At least one suite required.", nameof(suites));
        }

        if (!_suites.ContainsKey(activeSuiteId))
        {
            throw new ArgumentException($"Unknown active suite '{activeSuiteId}'.", nameof(activeSuiteId));
        }

        _activeSuiteId = activeSuiteId;
    }

    public string ActiveSuiteId => _activeSuiteId;

    public IReadOnlyList<string> AvailableSuiteIds => _suites.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();

    public ChallengePassSuite GetActive() => GetSuite(_activeSuiteId);

    public ChallengePassSuite GetSuite(string suiteId)
    {
        if (!_suites.TryGetValue(suiteId, out ChallengePassSuite? suite))
        {
            throw new KeyNotFoundException($"Challenge/pass suite '{suiteId}' is not installed.");
        }

        return suite;
    }

    public void SetActive(string suiteId)
    {
        if (!_suites.ContainsKey(suiteId))
        {
            throw new KeyNotFoundException($"Challenge/pass suite '{suiteId}' is not installed.");
        }

        _activeSuiteId = suiteId;
    }
}
