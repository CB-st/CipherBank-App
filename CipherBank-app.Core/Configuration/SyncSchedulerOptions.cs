// <copyright file="SyncSchedulerOptions.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Configuration;

/// <summary>Bounded dispatch settings for synchronization work.</summary>
public sealed class SyncSchedulerOptions
{
    /// <summary>
    /// Sentinel <see cref="MaxConcurrency"/> value meaning "auto": <see cref="Resolve"/> derives the width
    /// from the processor count. This is the default when the setting is not configured.
    /// </summary>
    public const int AutoConcurrency = 0;

    /// <summary>Lower inclusive concurrency bound for validation and derived defaults.</summary>
    public static int MinConcurrency { get; } = 1;

    /// <summary>Upper inclusive concurrency bound for validation.</summary>
    public static int MaxAllowedConcurrency { get; } = 8;

    /// <summary>
    /// Dispatch width. <see cref="AutoConcurrency"/> (the default) means auto: <see cref="Resolve"/> derives half the processor count
    /// (rounded up, clamped to <see cref="MinConcurrency"/>..<see cref="MaxAllowedConcurrency"/>).
    /// Overlay <c>SyncScheduler:MaxConcurrency</c> to set an explicit value.
    /// </summary>
    public int MaxConcurrency { get; set; } = AutoConcurrency;

    /// <summary>
    /// Returns the effective dispatch width. An explicit 1–8 wins; otherwise half the CPU count.
    /// Use: High (scheduler construction). Scope: SyncSchedulerOptions.
    /// </summary>
    public int Resolve()
    {
        if (MaxConcurrency == AutoConcurrency)
        {
            int derived = (int)Math.Ceiling(Environment.ProcessorCount / 2.0);
            return Math.Clamp(derived, MinConcurrency, MaxAllowedConcurrency);
        }

        if (MaxConcurrency < MinConcurrency || MaxConcurrency > MaxAllowedConcurrency)
        {
            throw new InvalidOperationException(
                $"MaxConcurrency must be {AutoConcurrency} (auto) or between {MinConcurrency} and {MaxAllowedConcurrency}.");
        }

        return MaxConcurrency;
    }
}
