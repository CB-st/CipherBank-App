// <copyright file="FrozenSonarExclusions.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Text.Json;

namespace CipherBank_app.Analyzers.Tests;

/// <summary>
/// Loads the frozen Sonar exclusion lists from <c>config/sonar/exclusions.json</c>.
/// Use: Low (structure Facts). Scope: CipherBank-app.Analyzers.Tests.
/// </summary>
internal sealed class FrozenSonarExclusions
{
    internal const string RelativePath = "config/sonar/exclusions.json";

    private FrozenSonarExclusions(
        IReadOnlyList<string> sourceExclusions,
        IReadOnlyList<string> coverageExclusions)
    {
        SourceExclusions = sourceExclusions;
        CoverageExclusions = coverageExclusions;
    }

    internal IReadOnlyList<string> SourceExclusions { get; }

    internal IReadOnlyList<string> CoverageExclusions { get; }

    internal string SourceCsv => string.Join(',', SourceExclusions);

    internal string CoverageCsv => string.Join(',', CoverageExclusions);

    /// <summary>
    /// Reads the dedicated exclusion file from the repository root.
    /// Use: Low (once per structure Fact). Scope: analyzer tests.
    /// </summary>
    internal static FrozenSonarExclusions Load()
    {
        FileShape? shape = JsonSerializer.Deserialize<FileShape>(ProductTreeRepoRoot.Read(RelativePath));
        if (shape?.SourceExclusions is null || shape.SourceExclusions.Length == 0)
        {
            throw new InvalidOperationException($"{RelativePath} is missing SourceExclusions.");
        }

        if (shape.CoverageExclusions is null || shape.CoverageExclusions.Length == 0)
        {
            throw new InvalidOperationException($"{RelativePath} is missing CoverageExclusions.");
        }

        return new FrozenSonarExclusions(shape.SourceExclusions, shape.CoverageExclusions);
    }

    /// <summary>
    /// True when either frozen list names a Persist/Migrations path fragment.
    /// Use: Low (structure Facts). Scope: analyzer tests.
    /// </summary>
    internal bool NamesPersistMigrations()
    {
        return NamesPersistMigrations(SourceExclusions) || NamesPersistMigrations(CoverageExclusions);
    }

    private static bool NamesPersistMigrations(IReadOnlyList<string> entries)
    {
        return entries.Any(entry =>
            entry.Contains("Persist/Migrations", StringComparison.OrdinalIgnoreCase)
            || entry.Contains("Persist\\Migrations", StringComparison.OrdinalIgnoreCase));
    }

    private sealed record FileShape(string[]? SourceExclusions, string[]? CoverageExclusions);
}
