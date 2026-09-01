// <copyright file="SonarCoverageExclusionTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Text.RegularExpressions;
using Xunit;

namespace CipherBank_app.Analyzers.Tests;

public sealed class SonarCoverageExclusionTests
{
    [Fact]
    public void SonarWorkflow_CoverageExclusions_MatchDedicatedFile()
    {
        FrozenSonarExclusions lists = FrozenSonarExclusions.Load();
        string yaml = ProductTreeRepoRoot.Read(".github/workflows/sonar.yml");
        Match coverage = Regex.Match(
            yaml,
            @"/d:sonar\.coverage\.exclusions=""([^""]+)""");
        Assert.True(coverage.Success, "sonar.coverage.exclusions property missing from sonar.yml");
        Assert.Equal(lists.CoverageCsv, coverage.Groups[1].Value);
        Assert.Equal(5, lists.CoverageExclusions.Count);

        Match sources = Regex.Match(
            yaml,
            @"/d:sonar\.exclusions=""([^""]+)""");
        Assert.True(sources.Success, "sonar.exclusions property missing from sonar.yml");
        Assert.Equal(lists.SourceCsv, sources.Groups[1].Value);
    }
}
