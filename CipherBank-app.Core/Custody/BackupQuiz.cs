// <copyright file="BackupQuiz.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Security.Cryptography;

namespace CipherBank_app.Custody;

/// <summary>Pure helper for Cora-style N-word backup quiz picking.</summary>
public static class BackupQuiz
{
    /// <summary>Pick <paramref name="count"/> distinct random word indices (sorted ascending).</summary>
    public static IReadOnlyList<(int Index, string Word)> PickRandom(string[] words, int count)
        => PickRandom(words, count, nextInclusiveExclusive: null);

    /// <summary>
    /// Pick <paramref name="count"/> distinct word indices; optional <see cref="Random"/> for deterministic tests.
    /// Production path uses <see cref="RandomNumberGenerator"/>.
    /// Use: High (backup quiz). Scope: BackupQuiz.
    /// </summary>
    public static IReadOnlyList<(int Index, string Word)> PickRandom(string[] words, int count, Random? rng)
        => PickRandom(
            words,
            count,
            rng is null ? null : rng.Next);

    /// <summary>
    /// Pick <paramref name="count"/> distinct word indices using an optional next(minInclusive, maxExclusive) callback.
    /// Use: High (backup quiz). Scope: BackupQuiz.
    /// </summary>
    public static IReadOnlyList<(int Index, string Word)> PickRandom(
        string[] words,
        int count,
        Func<int, int, int>? nextInclusiveExclusive)
    {
        ArgumentNullException.ThrowIfNull(words);
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        if (words.Length == 0 || count == 0)
        {
            return Array.Empty<(int, string)>();
        }

        var take = Math.Min(count, words.Length);
        var indices = Enumerable.Range(0, words.Length).ToArray();

        for (var i = 0; i < take; i++)
        {
            var j = nextInclusiveExclusive?.Invoke(i, indices.Length)
                ?? RandomNumberGenerator.GetInt32(i, indices.Length);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }

        return indices
            .Take(take)
            .OrderBy(i => i)
            .Select(i => (i, words[i]))
            .ToList();
    }
}
