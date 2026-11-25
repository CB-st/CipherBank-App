// <copyright file="DeviceExportNames.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Text.RegularExpressions;

namespace CipherBank_app.E2ETests.Support;

/// <summary>
/// Validates names from the device export listing and builds quoted cleanup commands.
/// Use: Medium (recovery capture and cleanup). Scope: recovery exports in shared storage.
/// </summary>
internal static partial class DeviceExportNames
{
    /// <summary>
    /// Checks whether a name exactly matches the app's generated recovery-export format.
    /// Use: Medium (each listing entry and cleanup operand). Scope: one candidate name.
    /// </summary>
    /// <param name="name">The candidate filename, which may be null.</param>
    /// <returns>True if the entire filename matches the generated format.</returns>
    public static bool IsGeneratedExportName(string? name) => name is not null && GeneratedExportName().IsMatch(name);

    /// <summary>
    /// Keeps generated recovery-export names in their original listing order.
    /// Use: Medium (each Downloads listing). Scope: one device listing.
    /// </summary>
    /// <param name="names">The device listing entries, including any whitespace in the names.</param>
    /// <returns>The matching names in listing order.</returns>
    public static IReadOnlyList<string> FilterGeneratedExportNames(IEnumerable<string> names) =>
        [.. names.Where(IsGeneratedExportName)];

    /// <summary>
    /// Quotes a value as one literal POSIX shell word.
    /// Use: Medium (recovery cleanup). Scope: one shell operand.
    /// </summary>
    /// <param name="value">The literal text to quote.</param>
    /// <returns>The shell word with embedded single quotes escaped.</returns>
    /// <exception cref="ArgumentException">The value contains a NUL character.</exception>
    public static string PosixSingleQuote(string value)
    {
        if (value.Contains('\0'))
        {
            throw new ArgumentException("A shell operand cannot contain a NUL character.", nameof(value));
        }

        return $"'{value.Replace("'", "'\\''", StringComparison.Ordinal)}'";
    }

    /// <summary>
    /// Builds the device shell command to remove one validated recovery export.
    /// Use: Medium (once per stale export). Scope: one file in device shared storage.
    /// </summary>
    /// <param name="deviceDir">The directory containing the export on the device.</param>
    /// <param name="name">The candidate export filename.</param>
    /// <returns>A removal command with a quoted path after the end-of-options marker.</returns>
    /// <exception cref="ArgumentException">The name fails validation or the path contains a NUL character.</exception>
    public static string BuildRemoveCommand(string deviceDir, string? name)
    {
        if (!IsGeneratedExportName(name))
        {
            throw new ArgumentException("The name must match the generated recovery-export format.", nameof(name));
        }

        return $"rm -f -- {PosixSingleQuote($"{deviceDir}/{name}")}";
    }

    /// <summary>
    /// Supplies the pattern for the app's timestamped recovery-export filenames.
    /// Use: Medium (each validation). Scope: recovery-export filename format.
    /// </summary>
    [GeneratedRegex(@"\Acipherbank-recovery-[0-9]{8}-[0-9]{6}\.cbr\.json\z", RegexOptions.CultureInvariant)]
    private static partial Regex GeneratedExportName();
}
