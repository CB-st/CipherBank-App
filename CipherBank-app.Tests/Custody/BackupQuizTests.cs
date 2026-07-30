// <copyright file="BackupQuizTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Custody;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Custody;

public class BackupQuizTests
{
    [Fact]
    public void PickRandom_ReturnsUniqueSortedIndices()
    {
        var words = Enumerable.Range(0, 12).Select(i => $"w{i}").ToArray();
        IReadOnlyList<(int Index, string Word)> picks = BackupQuiz.PickRandom(words, 3, new Random(42));
        picks.Should().HaveCount(3);
        picks.Select(p => p.Index).Should().OnlyHaveUniqueItems();
        picks.Select(p => p.Index).Should().BeInAscendingOrder();
        foreach ((var index, var word) in picks)
        {
            word.Should().Be(words[index]);
        }
    }

    [Fact]
    public void PickRandom_IsDeterministicForSeed()
    {
        var words = Enumerable.Range(0, 12).Select(i => $"w{i}").ToArray();
        var a = BackupQuiz.PickRandom(words, 3, new Random(7)).Select(p => p.Index).ToArray();
        var b = BackupQuiz.PickRandom(words, 3, new Random(7)).Select(p => p.Index).ToArray();
        a.Should().Equal(b);
    }
}
