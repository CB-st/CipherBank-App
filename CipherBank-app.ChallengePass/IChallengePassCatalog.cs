// <copyright file="IChallengePassCatalog.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.ChallengePass;

/// <summary>Registry of installed suites; active suite is slot-selected by id.</summary>
public interface IChallengePassCatalog
{
    /// <summary>Gets the id of the currently active suite.</summary>
    string ActiveSuiteId { get; }

    /// <summary>Gets the currently active suite.</summary>
    ChallengePassSuite Active { get; }

    /// <summary>Ordered snapshot of installed suite ids (cached — not a live view).</summary>
    IReadOnlyList<string> AvailableSuiteIds { get; }

    /// <summary>
    /// Returns the installed suite for <paramref name="suiteId"/>; unknown ids throw rather than
    /// falling back (configuration may select suites, never invent them).
    /// </summary>
    ChallengePassSuite GetSuite(string suiteId);

    /// <summary>Slot-out / slot-in the active suite without rebuilding DI.</summary>
    void SetActive(string suiteId);
}
