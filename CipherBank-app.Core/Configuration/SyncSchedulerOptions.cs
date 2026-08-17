// <copyright file="SyncSchedulerOptions.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Configuration;

/// <summary>Bounded dispatch settings for synchronization work.</summary>
public sealed class SyncSchedulerOptions
{
    /// <summary>Lower inclusive concurrency bound for validation.</summary>
    public static int MinConcurrency { get; } = 1;

    /// <summary>CPU-derived default cap for on-device sync (overlay may go higher).</summary>
    public static int DefaultMaxConcurrencyCap { get; } = 2;

    /// <summary>Upper inclusive concurrency bound for validation.</summary>
    public static int MaxAllowedConcurrency { get; } = 8;

    public static string SectionName { get; } = "SyncScheduler";

    /// <summary>
    /// Default dispatch width from the host CPU, capped for mobile.
    /// Overlay <c>SyncScheduler:MaxConcurrency</c> to override.
    /// </summary>
    public int MaxConcurrency { get; set; } = DeriveDefaultMaxConcurrency();

    /// <summary>Clamps <see cref="Environment.ProcessorCount"/> into the mobile default range.</summary>
    public static int DeriveDefaultMaxConcurrency()
        => Math.Clamp(Environment.ProcessorCount, MinConcurrency, DefaultMaxConcurrencyCap);
}
