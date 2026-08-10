// <copyright file="WaveStories.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.E2ETests.Support;

/// <summary>
/// C# mirror of <c>WAVE_STORIES</c> in <c>scripts/e2e-android.sh</c> so host tests detect bash/C# drift.
/// Use: High (harness contract tests). Scope: wave name → Story trait IDs.
/// </summary>
public static class WaveStories
{
    /// <summary>Wave name → space-separated Story trait IDs (must match bash WAVE_STORIES).</summary>
    public static readonly IReadOnlyDictionary<string, string> ByName =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["account"] = "CB-ACCOUNT-001 CB-ACCOUNT-002 CB-ACCOUNT-PIN-CHANGE US-ONB-03 US-ONB-04",
            ["market"] = "CB-MARKET-001",
            ["wallets"] = "CB-WALLET-001 CB-WALLET-002",
            ["fund"] = "CB-FUND-001",
            ["pay"] = "CB-PAY-001 CB-PAY-003",
            ["cards"] = "CB-CARD-001",
        };

    /// <summary>
    /// Splits a wave's story-id string into individual Story trait values.
    /// Use: Medium (filter contracts). Scope: single wave.
    /// </summary>
    public static IReadOnlyList<string> StoryIdsFor(string waveName)
    {
        if (!ByName.TryGetValue(waveName, out string? joined))
        {
            throw new ArgumentException($"Unknown wave '{waveName}'.", nameof(waveName));
        }

        return joined.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
