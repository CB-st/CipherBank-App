// <copyright file="HarnessGapNoteTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.E2ETests.Support;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.E2ETests.Tests;

/// <summary>
/// Host-side harness proofs that do not need an emulator (GapNotes, filter contracts).
/// Use: High (CI / local without E2E_RUN). Scope: E2E support library.
/// </summary>
public class HarnessGapNoteTests
{
    /// <summary>
    /// Proves <see cref="StoryRunner.RunAsync"/> writes docs/tests/gaps/{story}.md before rethrowing.
    /// Use: High (Phase 1 DoD). Scope: GapNotes + StoryRunner.
    /// </summary>
    [Fact]
    public async Task RunAsync_OnFailure_WritesGapNoteThenRethrows()
    {
        string storyId = $"HARNESS-GAP-{Guid.NewGuid():N}";
        string expectedPath = Path.Combine(
            RepoPaths.ResolveFromRoot("docs/tests/gaps"),
            $"{storyId}.md");

        if (File.Exists(expectedPath))
        {
            File.Delete(expectedPath);
        }

        Func<Task> act = () => StoryRunner.RunAsync(
            storyId,
            () => throw new InvalidOperationException("deliberate harness failure"),
            fixture: null);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("deliberate harness failure");

        File.Exists(expectedPath).Should().BeTrue("GapNotes must land before the exception escapes");
        File.ReadAllText(expectedPath).Should().Contain(storyId).And.Contain("deliberate harness failure");

        File.Delete(expectedPath);
    }
}
