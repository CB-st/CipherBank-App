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
        var root = FindRepositoryRoot();
        var projectFiles = Directory
            .EnumerateFiles(root, "*.*proj", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(root, "Directory.Build.props", SearchOption.TopDirectoryOnly));

        var offenders = projectFiles
            .Where(path => !IsGenerated(path))
            .SelectMany(path => XDocument.Load(path)
                .Descendants("PackageReference")
                .Where(reference => reference.Attribute("Version") is not null)
                .Select(_ => Path.GetRelativePath(root, path)))
            .ToArray();

        offenders.Should().BeEmpty("all NuGet versions belong in Directory.Packages.props");
    }

    [Fact]
    public void CentralPackageManagement_CoversEveryPackageReference()
    {
        var root = FindRepositoryRoot();
        var central = XDocument.Load(Path.Combine(root, "Directory.Packages.props"))
            .Descendants("PackageVersion")
            .Select(version => version.Attribute("Include")?.Value)
            .OfType<string>()
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var projectFiles = Directory
            .EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(root, "Directory.Build.props", SearchOption.TopDirectoryOnly));

        var missing = projectFiles
            .Where(path => !IsGenerated(path))
            .SelectMany(path => XDocument.Load(path)
                .Descendants("PackageReference")
                .Select(reference => reference.Attribute("Include")?.Value)
                .OfType<string>()
                .Where(name => !string.IsNullOrWhiteSpace(name) && !central.Contains(name))
                .Select(name => $"{Path.GetRelativePath(root, path)}: {name}"))
            .ToArray();

        missing.Should().BeEmpty("every PackageReference needs one central version declaration");
    }

    [Fact]
    public void ProductionCode_HasNoLegacyAssemblyInfoOrScatteredSql()
    {
        var root = FindRepositoryRoot();
        Directory.EnumerateFiles(root, "AssemblyInfo.cs", SearchOption.AllDirectories)
            .Where(path => path.Contains($"{Path.DirectorySeparatorChar}Properties{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(path => !IsGenerated(path))
            .Should().BeEmpty();

        var core = Path.Combine(root, "CipherBank-app.Core");
        var sqlOwner = Path.Combine(core, "Persist", "Sql", "LocalDbSql.cs");
        var sqlOffenders = Directory.EnumerateFiles(core, "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsGenerated(path) && !string.Equals(path, sqlOwner, StringComparison.Ordinal))
            .Where(path =>
            {
                var source = File.ReadAllText(path);
                return source.Contains("CommandText =", StringComparison.Ordinal)
                    || source.Contains("FromSqlRaw", StringComparison.Ordinal)
                    || source.Contains("ExecuteSqlRaw", StringComparison.Ordinal);
            });

        sqlOffenders.Should().BeEmpty("raw SQL is owned only by LocalDbSql compatibility repair");
    }

    [Fact]
    public void DesignSystem_HasSemanticTypographyAndNoViewColorLiterals()
    {
        var root = FindRepositoryRoot();
        var styles = Path.Combine(root, "CipherBank-app", "Resources", "Styles");
        var typographyPath = Path.Combine(styles, "Typography.xaml");
        File.Exists(Path.Combine(styles, "AGENTS.md")).Should().BeTrue();
        File.Exists(typographyPath).Should().BeTrue();

        var typography = XDocument.Load(typographyPath);
        var keys = typography
            .Descendants()
            .Attributes(XName.Get("Key", "http://schemas.microsoft.com/winfx/2009/xaml"))
            .Select(attribute => attribute.Value)
            .ToHashSet(StringComparer.Ordinal);
        string[] required =
        [
            "DisplayTitle",
            "PageHeader",
            "TitleMedium",
            "SectionHeader",
            "MoneyLarge",
            "MoneyMedium",
            "Body",
            "BodyStrong",
            "Caption",
            "Eyebrow",
        ];
        keys.Should().Contain(required);

        var views = Path.Combine(root, "CipherBank-app", "Views");
        var literalColors = Directory.EnumerateFiles(views, "*.xaml", SearchOption.AllDirectories)
            .Where(path => File.ReadAllText(path).Contains("=\"#", StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(root, path));
        literalColors.Should().BeEmpty("views consume semantic color resources rather than hex literals");

        var pageLocalFonts = Directory.EnumerateFiles(views, "*.xaml", SearchOption.AllDirectories)
            .Where(path => File.ReadAllText(path).Contains("FontFamily=", StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(root, path));
        pageLocalFonts.Should().BeEmpty("views consume named typography styles rather than font families");
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
