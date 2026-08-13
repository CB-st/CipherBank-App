// <copyright file="ChallengePassCatalog.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.ChallengePass;

/// <summary>In-memory suite registry with runtime active-suite swap.</summary>
public sealed class ChallengePassCatalog : IChallengePassCatalog
{
    private readonly Dictionary<string, ChallengePassSuite> _suites;
    private readonly IReadOnlyList<string> _availableSuiteIds;
    private readonly object _activeGate = new();
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

        _availableSuiteIds = _suites.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();
        _activeSuiteId = activeSuiteId;
    }

    /// <summary>
    /// Returns the currently active suite id.
    /// Use: Medium (diagnostics / settings). Scope: catalog singleton.
    /// </summary>
    public string ActiveSuiteId
    {
        get
        {
            lock (_activeGate)
            {
                return _activeSuiteId;
            }
        }
    }

    /// <summary>
    /// Ordered snapshot of installed suite ids (built once at construction — no per-get copy).
    /// Use: Low (settings / diagnostics). Scope: catalog singleton.
    /// </summary>
    public IReadOnlyList<string> AvailableSuiteIds => _availableSuiteIds;

    /// <summary>
    /// Returns the active challenge/pass suite.
    /// Use: High (every proof build). Scope: catalog singleton.
    /// </summary>
    public ChallengePassSuite Active
    {
        get
        {
            lock (_activeGate)
            {
                return GetSuite(_activeSuiteId);
            }
        }
    }

    public ChallengePassSuite GetSuite(string suiteId)
    {
        if (!_suites.TryGetValue(suiteId, out ChallengePassSuite? suite))
        {
            throw new KeyNotFoundException($"Challenge/pass suite '{suiteId}' is not installed.");
        }

        return suite;
    }

    /// <summary>
    /// Swaps the active suite at runtime (e.g. A1 → A2 in lab settings).
    /// Use: Low (manual suite switch). Scope: catalog singleton.
    /// </summary>
    public void SetActive(string suiteId)
    {
        if (!_suites.ContainsKey(suiteId))
        {
            throw new KeyNotFoundException($"Challenge/pass suite '{suiteId}' is not installed.");
        }

        lock (_activeGate)
        {
            _activeSuiteId = suiteId;
        }
    }
}
