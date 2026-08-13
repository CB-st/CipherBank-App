// <copyright file="SyncSchedulerOptions.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
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

    /// <summary>
    /// Default dispatch width from the host CPU, capped for mobile.
    /// Overlay <c>SyncScheduler:MaxConcurrency</c> to override.
    /// </summary>
    public int MaxConcurrency { get; set; } = DeriveDefaultMaxConcurrency();

    /// <summary>Clamps <see cref="Environment.ProcessorCount"/> into [1, 2] for on-device sync.</summary>
    public static int DeriveDefaultMaxConcurrency()
    {
        int processors = Environment.ProcessorCount;
        if (processors < MinConcurrency)
        {
            return MinConcurrency;
        }

        return Math.Min(2, processors);
    }
}
