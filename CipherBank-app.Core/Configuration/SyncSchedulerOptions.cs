// <copyright file="SyncSchedulerOptions.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Configuration;

/// <summary>Bounded dispatch settings for synchronization work.</summary>
public sealed class SyncSchedulerOptions
{
    public const string SectionName = "SyncScheduler";

    public int MaxConcurrency { get; set; } = 2;
}
