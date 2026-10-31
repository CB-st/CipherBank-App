// <copyright file="StoryRunner.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.E2ETests.Support;

/// <summary>
/// Runs an executable E2E Fact body and writes a <see cref="GapNotes"/> file on failure before rethrowing.
/// Use: High (every device Fact under E2E_RUN=1). Scope: per-Fact exception boundary.
/// </summary>
public static class StoryRunner
{
    /// <summary>
    /// Invokes <paramref name="testBody"/>; on any exception writes a gap note (with optional driver/journal
    /// diagnostics) then rethrows so xUnit still records a Failed result — never a soft-pass.
    /// Use: High. Scope: single Fact.
    /// </summary>
    public static async Task RunAsync(
        string storyId,
        Func<Task> testBody,
        AppiumFixture? fixture)
    {
        ArgumentNullException.ThrowIfNull(testBody);
        try
        {
            await testBody().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            GapNotes.Write(
                storyId,
                ex,
                fixture?.Driver,
                fixture?.Journal);
            throw;
        }
    }

    /// <summary>
    /// Synchronous counterpart of <see cref="RunAsync"/> for void Facts (e.g. sealed-device smoke).
    /// Use: High. Scope: single Fact.
    /// </summary>
    public static void Run(
        string storyId,
        Action testBody,
        AppiumFixture? fixture)
    {
        ArgumentNullException.ThrowIfNull(testBody);
        try
        {
            testBody();
        }
        catch (Exception ex)
        {
            GapNotes.Write(
                storyId,
                ex,
                fixture?.Driver,
                fixture?.Journal);
            throw;
        }
    }
}
