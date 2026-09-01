// <copyright file="PersistMigrationsExclusionTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace CipherBank_app.Analyzers.Tests;

public sealed class PersistMigrationsExclusionTests
{
    private const string MigrationsPath = "CipherBank-app.Core/Persist/Migrations";

    [Fact]
    public void SonarWorkflow_DoesNotExcludePersistMigrations()
    {
        FrozenSonarExclusions lists = FrozenSonarExclusions.Load();
        Assert.False(
            lists.NamesPersistMigrations(),
            $"{FrozenSonarExclusions.RelativePath} must not name Persist/Migrations.");

        string yaml = ProductTreeRepoRoot.Read(".github/workflows/sonar.yml");
        Match sources = Regex.Match(
            yaml,
            @"/d:sonar\.exclusions=""([^""]+)""");
        Match coverage = Regex.Match(
            yaml,
            @"/d:sonar\.coverage\.exclusions=""([^""]+)""");
        Assert.True(sources.Success, "sonar.exclusions property missing from sonar.yml");
        Assert.True(coverage.Success, "sonar.coverage.exclusions property missing from sonar.yml");
        Assert.Equal(lists.SourceCsv, sources.Groups[1].Value);
        Assert.Equal(lists.CoverageCsv, coverage.Groups[1].Value);
        Assert.DoesNotContain("not compiled on Linux", yaml, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SonarReadme_DoesNotExcludePersistMigrations()
    {
        string readme = ProductTreeRepoRoot.Read("config/sonar/README.md");
        Assert.DoesNotContain(
            $"migrations under `{MigrationsPath}/`",
            readme,
            StringComparison.Ordinal);
    }

    [Fact]
    public void PersistMigrations_AreNotMarkedGeneratedCode()
    {
        string editorConfig = Path.Combine(
            ProductTreeRepoRoot.Find(),
            "CipherBank-app.Core",
            "Persist",
            "Migrations",
            ".editorconfig");
        Assert.False(
            File.Exists(editorConfig),
            "Persist/Migrations/.editorconfig must not mark migrations generated_code.");
    }

    [Fact]
    public void IntegrationTests_DoNotExcludeMigrationsFromCoverage()
    {
        string csproj = ProductTreeRepoRoot.Read(ProductTreeRepoRoot.IntegrationTestsCsproj);
        Assert.DoesNotContain("ExcludeByFile", csproj, StringComparison.Ordinal);
        Assert.DoesNotContain("Migrations", csproj, StringComparison.Ordinal);
    }

    [Fact]
    public void RootEditorConfig_HasNoLegacyPersistSqlCarveOut()
    {
        string editorConfig = ProductTreeRepoRoot.Read(".editorconfig");
        Assert.DoesNotContain("Persist/Sql", editorConfig, StringComparison.Ordinal);
    }

    [Fact]
    public void AnalyzerDocs_DoNotClaimLinuxCannotCompileSiblingProjects()
    {
        string agents = ProductTreeRepoRoot.Read("CipherBank-app.Analyzers/AGENTS.md");
        string targets = ProductTreeRepoRoot.Read("Directory.Build.targets");
        string sonarAgents = ProductTreeRepoRoot.Read("config/sonar/AGENTS.md");
        Assert.DoesNotContain("cannot compile the MAUI host on Linux", agents, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("cannot compile", targets, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Linux CI does not need", sonarAgents, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReportsCommandTextInPersistMigrationsFolder()
    {
        CSharpAnalyzerTest<NoScatteredSqlAnalyzer, DefaultVerifier> test = new()
        {
            TestState =
            {
                Sources =
                {
                    ("CipherBank-app.Core/Persist/Migrations/InitialCreate.cs", """
                        class InitialCreate
                        {
                            void Run(System.Data.IDbCommand command)
                            {
                                command.{|CB1003:CommandText|} = "SELECT 1";
                            }
                        }
                        """),
                },
            },
        };
        await test.RunAsync();
    }
}
