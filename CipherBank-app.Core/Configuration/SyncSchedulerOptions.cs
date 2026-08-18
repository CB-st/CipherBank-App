// <copyright file="SyncSchedulerOptions.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Configuration;

/// <summary>Bounded dispatch settings for synchronization work.</summary>
public sealed class SyncSchedulerOptions
{
    /// <summary>Lower inclusive concurrency bound for validation and the unset default.</summary>
    public static int MinConcurrency { get; } = 1;

    /// <summary>Upper inclusive concurrency bound for validation.</summary>
    public static int MaxAllowedConcurrency { get; } = 8;

    public static string SectionName { get; } = "SyncScheduler";

    /// <summary>
    /// Dispatch width. Defaults to <see cref="MinConcurrency"/>; overlay
    /// <c>SyncScheduler:MaxConcurrency</c> to raise it (up to <see cref="MaxAllowedConcurrency"/>).
    /// </summary>
    public int MaxConcurrency { get; set; } = MinConcurrency;
}
