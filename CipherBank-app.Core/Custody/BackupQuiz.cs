// <copyright file="BackupQuiz.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Security.Cryptography;

namespace CipherBank_app.Custody;

/// <summary>Pure helper for Cora-style N-word backup quiz picking.</summary>
public static class BackupQuiz
{
    /// <summary>Pick <paramref name="count"/> distinct random word indices (sorted ascending).</summary>
    public static IReadOnlyList<(int Index, string Word)> PickRandom(string[] words, int count, Random? rng = null)
    {
        ArgumentNullException.ThrowIfNull(words);
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        if (words.Length == 0 || count == 0)
        {
            return Array.Empty<(int, string)>();
        }

        int take = Math.Min(count, words.Length);
        int[] indices = Enumerable.Range(0, words.Length).ToArray();

        // Fisher–Yates partial shuffle
        for (int i = 0; i < take; i++)
        {
            int j;
            if (rng is null)
            {
                j = RandomNumberGenerator.GetInt32(i, indices.Length);
            }
            else
            {
#pragma warning disable CA5394 // Deterministic test RNG only — production uses RandomNumberGenerator
                j = rng.Next(i, indices.Length);
#pragma warning restore CA5394
            }

            (indices[i], indices[j]) = (indices[j], indices[i]);
        }

        return indices
            .Take(take)
            .OrderBy(i => i)
            .Select(i => (i, words[i]))
            .ToList();
    }
}
