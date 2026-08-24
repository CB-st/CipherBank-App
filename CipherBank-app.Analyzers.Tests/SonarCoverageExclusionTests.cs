// <copyright file="SonarCoverageExclusionTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Text.RegularExpressions;
using Xunit;

namespace CipherBank_app.Analyzers.Tests;

public sealed class SonarCoverageExclusionTests
{
    // Frozen allowlist. Do not grow this list; shrink only with an explicit policy change.
    private const string FrozenCoverageExclusions =
        "**/Platforms/**,**/Resources/**,**/*Tests*/**,scripts/**,design_handoff_cipherbank/**";

    [Fact]
    public void SonarWorkflow_CoverageExclusions_AreFrozen()
    {
        string yaml = ProductTreeRepoRoot.Read(".github/workflows/sonar.yml");
        Match match = Regex.Match(
            yaml,
            @"/d:sonar\.coverage\.exclusions=""([^""]+)""");
        Assert.True(match.Success, "sonar.coverage.exclusions property missing from sonar.yml");
        Assert.Equal(FrozenCoverageExclusions, match.Groups[1].Value);

        string[] entries = match.Groups[1].Value.Split(',', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(5, entries.Length);
    }
}
