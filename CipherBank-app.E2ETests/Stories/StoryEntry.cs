// <copyright file="StoryEntry.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.E2ETests.Support;

namespace CipherBank_app.E2ETests.Stories;

/// <summary>
/// One CB-*/US-* catalog row: ids, title, runner status, Maui surface note, and optional device profile.
/// Use: High (StoryCatalog + backlog tests). Scope: E2E story inventory.
/// </summary>
/// <param name="RequiredProfile">
/// Device custody profile (<see cref="DeviceProfile"/>) the story's Fact must establish before it runs,
/// or null when no story-specific device precondition applies. Optional/trailing so existing positional
/// <c>new(...)</c> catalog entries stay valid without naming every argument.
/// </param>
public sealed record StoryEntry(
    string CbId,
    string? UsId,
    string Title,
    StoryRunnerStatus Status,
    string MauiSurface,
    DeviceProfile? RequiredProfile = null);
