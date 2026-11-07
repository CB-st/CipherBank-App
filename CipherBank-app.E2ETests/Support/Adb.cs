// <copyright file="Adb.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Diagnostics;

namespace CipherBank_app.E2ETests.Support;

/// <summary>
/// Single owner of adb process invocation for the harness. Every device-side shell/pull/push in the E2E
/// suite goes through here so process plumbing (arguments, redirection, exit handling) lives in one place
/// instead of being re-implemented per helper.
/// Use: Medium (a handful of calls per device story). Scope: process-wide, against the attached device.
/// </summary>
public static class Adb
{
    private static readonly TimeSpan CommandTimeout = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Runs an adb command and returns combined stdout+stderr, throwing when adb cannot be started or
    /// does not finish inside <see cref="CommandTimeout"/> — a stuck adb must fail loudly, not hang a story.
    /// Stdout/stderr are drained asynchronously so a full pipe buffer cannot deadlock WaitForExit.
    /// Use: Medium (every device-side helper call). Scope: one adb process.
    /// </summary>
    public static string Run(string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "adb",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Failed to start adb (arguments: {arguments}).");

        Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync();
        Task<string> stderrTask = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit((int)CommandTimeout.TotalMilliseconds))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException($"adb {arguments} did not finish within {CommandTimeout.TotalSeconds:0}s.");
        }

        string stdout = stdoutTask.GetAwaiter().GetResult();
        string stderr = stderrTask.GetAwaiter().GetResult();
        return stdout + stderr;
    }

    /// <summary>
    /// Runs `adb shell &lt;command&gt;` and returns its output with CR characters stripped, so callers can
    /// split device output on '\n' without tripping over Windows-style line endings from the shell.
    /// Use: Medium (device queries such as ls). Scope: one adb process.
    /// </summary>
    public static string Shell(string command) => Run($"shell {command}").Replace("\r", string.Empty);

    /// <summary>
    /// Splits shell output into non-empty trimmed lines — the common shape for `ls`-style queries.
    /// Use: Medium (device file listings). Scope: one adb process.
    /// </summary>
    public static IReadOnlyList<string> ShellLines(string command) =>
        Shell(command)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();
}
