// <copyright file="RepositoryStructureTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Text.RegularExpressions;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Architecture;

public sealed partial class RepositoryStructureTests
{
    [Fact]
    public void RequiredPlatformFiles_Exist()
    {
        string root = FindRepositoryRoot();
        File.Exists(Path.Combine(root, "Directory.Packages.props")).Should().BeTrue();
        File.Exists(Path.Combine(root, "scripts", "sonar", "provision_quality_gate.py")).Should().BeTrue();
        File.Exists(Path.Combine(root, "scripts", "validate-structure.sh")).Should().BeTrue();
    }

    [Fact]
    public void PackageVersions_AreDeclaredOnlyInCentralPackageManagement()
    {
        string root = FindRepositoryRoot();
        string[] offenders = Directory
            .EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".props", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".targets", StringComparison.OrdinalIgnoreCase))
            .Where(path => !IsGenerated(path))
            .Where(path => !string.Equals(
                Path.GetFileName(path),
                "Directory.Packages.props",
                StringComparison.OrdinalIgnoreCase))
            .Where(path => PackageVersionAttribute().IsMatch(File.ReadAllText(path)))
            .Select(path => Path.GetRelativePath(root, path))
            .ToArray();

        offenders.Should().BeEmpty("all NuGet versions belong in Directory.Packages.props");
    }

    [Fact]
    public void Core_HasNoLegacyAssemblyInfoOrScatteredSql()
    {
        string root = FindRepositoryRoot();
        Directory.EnumerateFiles(root, "AssemblyInfo.cs", SearchOption.AllDirectories)
            .Where(path => path.Contains($"{Path.DirectorySeparatorChar}Properties{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(path => !IsGenerated(path))
            .Should().BeEmpty();

        string core = Path.Combine(root, "CipherBank-app.Core");
        string sqlOwner = Path.Combine(core, "Persist", "Sql", "LocalDbSql.cs");
        string[] sqlOffenders = Directory.EnumerateFiles(core, "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsGenerated(path) && !string.Equals(path, sqlOwner, StringComparison.Ordinal))
            .Where(path =>
            {
                string source = File.ReadAllText(path);
                return source.Contains("CommandText =", StringComparison.Ordinal)
                    || source.Contains("FromSqlRaw", StringComparison.Ordinal)
                    || source.Contains("ExecuteSqlRaw", StringComparison.Ordinal);
            })
            .Select(path => Path.GetRelativePath(root, path))
            .ToArray();

        sqlOffenders.Should().BeEmpty("raw SQL is owned only by LocalDbSql compatibility repair");
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "Directory.Packages.props")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate Directory.Packages.props from test output.");
    }

    [GeneratedRegex(@"<PackageReference[^>]*\sVersion=", RegexOptions.Compiled)]
    private static partial Regex PackageVersionAttribute();

    private static bool IsGenerated(string path)
        => path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            || path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal);
}
