// <copyright file="SyncSchedulerOptions.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Configuration;

/// <summary>Bounded dispatch settings for synchronization work.</summary>
public sealed class SyncSchedulerOptions
{
    /// <summary>Lower inclusive concurrency bound for validation.</summary>
    public static int MinConcurrency { get; } = 1;

    /// <summary>Upper inclusive concurrency bound for validation.</summary>
    public static int MaxAllowedConcurrency { get; } = 8;

    public static string SectionName { get; } = "SyncScheduler";

    public int MaxConcurrency { get; set; } = 2;
}
