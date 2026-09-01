// <copyright file="StoryBacklogTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.E2ETests.Stories;
using Xunit;

namespace CipherBank_app.E2ETests.Tests;

/// <summary>
/// Visible backlog for CB-* / US-* stories not yet fully executable on MAUI Appium.
/// Listed as skipped Theories so <c>dotnet test --list-tests</c> shows the inventory.
/// Promote each to an executable Fact when Shell reaches Expo parity for that surface.
/// </summary>
public class StoryBacklogTests
{
    /// <summary>
    /// Returns catalog rows that remain pending MAUI Appium coverage. Use: High. Scope: StoryBacklogTests.
    /// </summary>
    public static IEnumerable<object[]> BacklogCases() =>
        StoryCatalog.Backlog.Select(s => new object[]
        {
            s.CbId,
            s.UsId ?? "(none)",
            s.Title,
            s.Status.ToString(),
            s.MauiSurface,
        });

    /// <summary>
    /// Retains the Story Pending Maui Appium legacy journey as explicit skipped inventory. Use: Low. Scope: StoryBacklogTests.
    /// </summary>
    [Theory(Skip = "MAUI Appium story backlog — implement when Expo parity lands; see docs/tests/STORY_ID_MAP.md")]
    [MemberData(nameof(BacklogCases))]
    public void Story_PendingMauiAppium(string cbId, string usId, string title, string status, string surface)
    {
        _ = (cbId, usId, title, status, surface);
    }
}
