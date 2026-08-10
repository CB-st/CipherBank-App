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
    public void CentralPackageManagement_CoversEveryPackageReference()
    {
        string root = FindRepositoryRoot();
        HashSet<string> central = XDocument.Load(Path.Combine(root, "Directory.Packages.props"))
            .Descendants("PackageVersion")
            .Select(version => version.Attribute("Include")?.Value)
            .OfType<string>()
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        IEnumerable<string> projectFiles = Directory
            .EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(root, "Directory.Build.props", SearchOption.TopDirectoryOnly));

        string[] missing = projectFiles
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

    [Fact]
    public void DesignSystem_HasSemanticTypographyAndNoViewColorLiterals()
    {
        string root = FindRepositoryRoot();
        string styles = Path.Combine(root, "CipherBank-app", "Resources", "Styles");
        string typographyPath = Path.Combine(styles, "Typography.xaml");
        File.Exists(Path.Combine(styles, "AGENTS.md")).Should().BeTrue();
        File.Exists(typographyPath).Should().BeTrue();

        XDocument typography = XDocument.Load(typographyPath);
        IEnumerable<string> keys = typography
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
            "PinEntry",
            "MonoCaption",
        ];
        keys.Should().Contain(required);

        string views = Path.Combine(root, "CipherBank-app", "Views");
        IEnumerable<string> literalColors = Directory.EnumerateFiles(views, "*.xaml", SearchOption.AllDirectories)
            .Where(path => File.ReadAllText(path).Contains("=\"#", StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(root, path));
        literalColors.Should().BeEmpty("views consume semantic color resources rather than hex literals");

        IEnumerable<string> pageLocalFonts = Directory.EnumerateFiles(views, "*.xaml", SearchOption.AllDirectories)
            .Where(path => File.ReadAllText(path).Contains("FontFamily=", StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(root, path));
        pageLocalFonts.Should().BeEmpty("views consume named typography styles rather than font families");

        string controls = Path.Combine(root, "CipherBank-app", "Controls");
        IEnumerable<string> codeTokenOffenders = Directory.EnumerateFiles(controls, "*.cs", SearchOption.AllDirectories)
            .Where(path =>
            {
                string source = File.ReadAllText(path);
                return source.Contains("Color.FromArgb(\"#", StringComparison.Ordinal)
                    || source.Contains("FontFamily =", StringComparison.Ordinal);
            })
            .Select(path => Path.GetRelativePath(root, path));
        codeTokenOffenders.Should().BeEmpty("code-created controls also consume semantic resources");
    }

    [Fact]
    public void RuntimeConfigurationThemes_AreDocumentedAndRetiredCompositionNamesStayRemoved()
    {
        string root = FindRepositoryRoot();
        string[] themes = ["security", "challenge-pass", "dispatch", "network", "persistence", "ui"];
        foreach (string theme in themes)
        {
            string directory = Path.Combine(root, "config", theme);
            File.Exists(Path.Combine(directory, "README.md")).Should().BeTrue($"{theme} configuration needs ownership documentation");
            Directory.EnumerateFiles(directory, "*.json", SearchOption.TopDirectoryOnly)
                .Should().NotBeEmpty($"{theme} configuration needs a defaults file");
        }

        string[] retiredNames = ["IProductApi", "MockProductApi", "MockPublicQuoteService", "AppSessionDeps"];
        string[] sourceRoots = ["CipherBank-app.Core", "CipherBank-app.ChallengePass", "CipherBank-app"];
        IEnumerable<string> offenders = sourceRoots
            .SelectMany(directory => Directory.EnumerateFiles(
                Path.Combine(root, directory),
                "*.cs",
                SearchOption.AllDirectories))
            .Where(path => !IsGenerated(path))
            .SelectMany(path => retiredNames
                .Where(name => File.ReadAllText(path).Contains(name, StringComparison.Ordinal))
                .Select(name => $"{Path.GetRelativePath(root, path)}: {name}"));

        offenders.Should().BeEmpty("M3 must consume the reviewed client and focused-coordinator contracts");
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
