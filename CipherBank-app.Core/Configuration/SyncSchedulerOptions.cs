// <copyright file="SyncSchedulerOptions.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Configuration;

/// <summary>Bounded dispatch settings for synchronization work.</summary>
public sealed class SyncSchedulerOptions
{
    /// <summary>Lower inclusive concurrency bound for validation and derived defaults.</summary>
    public static int MinConcurrency { get; } = 1;

    /// <summary>Upper inclusive concurrency bound for validation.</summary>
    public static int MaxAllowedConcurrency { get; } = 8;

    public static string SectionName { get; } = "SyncScheduler";

    /// <summary>
    /// Dispatch width. Zero means unset: <see cref="Resolve"/> derives half the processor count
    /// (rounded up, clamped to <see cref="MinConcurrency"/>..<see cref="MaxAllowedConcurrency"/>).
    /// Overlay <c>SyncScheduler:MaxConcurrency</c> to set an explicit value.
    /// </summary>
    public int MaxConcurrency { get; set; }

    /// <summary>
    /// Returns the effective dispatch width. An explicit 1–8 wins; otherwise half the CPU count.
    /// Use: High (scheduler construction). Scope: SyncSchedulerOptions.
    /// </summary>
    public int Resolve()
    {
        if (MaxConcurrency == 0)
        {
            int derived = (int)Math.Ceiling(Environment.ProcessorCount / 2.0);
            return Math.Clamp(derived, MinConcurrency, MaxAllowedConcurrency);
        }

        if (MaxConcurrency < MinConcurrency || MaxConcurrency > MaxAllowedConcurrency)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MaxConcurrency),
                $"MaxConcurrency must be 0 (unset) or between {MinConcurrency} and {MaxAllowedConcurrency}.");
        }

        return MaxConcurrency;
    }
}
