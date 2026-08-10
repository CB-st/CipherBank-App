// <copyright file="RepositoryStructureTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Xml.Linq;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Architecture;

public sealed class RepositoryStructureTests
{
    [Fact]
    public void PackageVersions_AreDeclaredOnlyInCentralPackageManagement()
    {
        string root = FindRepositoryRoot();
        IEnumerable<string> projectFiles = Directory
            .EnumerateFiles(root, "*.*proj", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(root, "Directory.Build.props", SearchOption.TopDirectoryOnly));

        string[] offenders = projectFiles
            .Where(path => !IsGenerated(path))
            .SelectMany(path => XDocument.Load(path)
                .Descendants("PackageReference")
                .Where(reference => reference.Attribute("Version") is not null)
                .Select(_ => Path.GetRelativePath(root, path)))
            .ToArray();

        offenders.Should().BeEmpty("all NuGet versions belong in Directory.Packages.props");
    }

    [Fact]
    public void ProductionCode_HasNoLegacyAssemblyInfoOrScatteredSql()
    {
        string root = FindRepositoryRoot();
        Directory.EnumerateFiles(root, "AssemblyInfo.cs", SearchOption.AllDirectories)
            .Where(path => path.Contains($"{Path.DirectorySeparatorChar}Properties{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(path => !IsGenerated(path))
            .Should().BeEmpty();

        string core = Path.Combine(root, "CipherBank-app.Core");
        string sqlOwner = Path.Combine(core, "Persist", "Sql", "LocalDbSql.cs");
        IEnumerable<string> sqlOffenders = Directory.EnumerateFiles(core, "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsGenerated(path) && !string.Equals(path, sqlOwner, StringComparison.Ordinal))
            .Where(path =>
            {
                string source = File.ReadAllText(path);
                return source.Contains("CommandText =", StringComparison.Ordinal)
                    || source.Contains("FromSqlRaw", StringComparison.Ordinal)
                    || source.Contains("ExecuteSqlRaw", StringComparison.Ordinal);
            });

        sqlOffenders.Should().BeEmpty("raw SQL is owned only by LocalDbSql compatibility repair");
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current = new(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "CipherBank-app.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate CipherBank-app.sln from test output.");
    }

    private static bool IsGenerated(string path)
        => path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            || path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal);
}
