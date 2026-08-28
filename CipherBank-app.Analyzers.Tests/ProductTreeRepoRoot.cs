// <copyright file="ProductTreeRepoRoot.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;

namespace CipherBank_app.Analyzers.Tests;

/// <summary>
/// Resolves on-disk product files from the analyzer test assembly.
/// Use: Low (structure Facts). Scope: CipherBank-app.Analyzers.Tests.
/// </summary>
internal static class ProductTreeRepoRoot
{
    internal const string MauiHostCsproj = "CipherBank-app/CipherBank-app.csproj";
    internal const string IntegrationTestsCsproj = "CipherBank-app.IntegrationTests/CipherBank-app.IntegrationTests.csproj";
    internal const string E2ETestsCsproj = "CipherBank-app.E2ETests/CipherBank-app.E2ETests.csproj";
    internal const string MauiHostSample = "CipherBank-app/MauiProgram.cs";
    internal const string MauiHostNonSample = "CipherBank-app/App.xaml.cs";
    internal const string IntegrationTestsSample = "CipherBank-app.IntegrationTests/MockServerFixture.cs";
    internal const string E2ETestsSample = "CipherBank-app.E2ETests/PageObjects/BasePage.cs";

    private const string MauiPackageReference = "<PackageReference Include=\"CommunityToolkit.Mvvm\" />";

    private static readonly string[] UnbuiltProjectDirectories =
    {
        "CipherBank-app",
        "CipherBank-app.IntegrationTests",
        "CipherBank-app.E2ETests",
    };

    /// <summary>
    /// Walks parents of the test output directory until the solution root is found.
    /// Use: Low (once per structure Fact). Scope: analyzer tests.
    /// </summary>
    internal static string Find()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (IsRepoRoot(directory.FullName))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            "Could not locate the CipherBank-App repository root from the test assembly.");
    }

    /// <summary>
    /// Reads a repository-relative product file as UTF-8 text.
    /// Use: Low (structure Facts). Scope: analyzer tests.
    /// </summary>
    internal static string Read(string relativePath)
    {
        string fullPath = Path.Combine(Find(), relativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Product file missing at {fullPath}", fullPath);
        }

        return File.ReadAllText(fullPath);
    }

    /// <summary>
    /// Returns on-disk C# files from the MAUI, IntegrationTests, and E2ETests trees.
    /// Use: Low (structure Facts). Scope: analyzer tests.
    /// </summary>
    internal static List<(string RelativePath, string Content)> UnbuiltCsharpFiles()
    {
        string root = Find();
        List<(string RelativePath, string Content)> files = [];
        foreach (string project in UnbuiltProjectDirectories)
        {
            string projectRoot = Path.Combine(root, project);
            foreach (string fullPath in Directory.EnumerateFiles(projectRoot, "*.cs", SearchOption.AllDirectories))
            {
                string relative = Path.GetRelativePath(root, fullPath).Replace('\\', '/');
                if (IsBinOrObj(relative))
                {
                    continue;
                }

                files.Add((relative, File.ReadAllText(fullPath)));
            }
        }

        if (files.Count == 0)
        {
            throw new InvalidOperationException("No unbuilt C# files found under the MAUI, Integration, or E2E trees.");
        }

        return files;
    }

    /// <summary>
    /// Inserts a retired identifier into live MAUI App.xaml.cs text.
    /// Use: Low (injected CB1004 Fact). Scope: analyzer tests.
    /// </summary>
    internal static string InjectRetiredApiName(string source)
    {
        const string original = "    public App()";
        const string injected = "    {|CB1004:IProductApi|} Api;\n\n    public App()";
        int index = source.IndexOf(original, StringComparison.Ordinal);
        if (index < 0)
        {
            throw new InvalidOperationException("App.xaml.cs no longer declares public App().");
        }

        return string.Concat(
            source.AsSpan(0, index),
            injected,
            source.AsSpan(index + original.Length));
    }

    /// <summary>
    /// Inserts a newline-separated Version attribute on the MAUI host's first CPM PackageReference.
    /// Use: Low (injected CB1001 Fact). Scope: analyzer tests.
    /// </summary>
    internal static string InjectNewlinePackageVersion(string csprojText)
    {
        int index = csprojText.IndexOf(MauiPackageReference, StringComparison.Ordinal);
        if (index < 0)
        {
            throw new InvalidOperationException(
                "MAUI csproj no longer has a CPM CommunityToolkit.Mvvm PackageReference.");
        }

        string injected = "<PackageReference Include=\"CommunityToolkit.Mvvm\"\nVersion=\"9.9.9\" />";
        string marked = "{|CB1001:" + injected + "|}";
        return string.Concat(
            csprojText.AsSpan(0, index),
            marked,
            csprojText.AsSpan(index + MauiPackageReference.Length));
    }

    /// <summary>
    /// True when the directory holds the solution and MAUI host project.
    /// Use: Low (root walk). Scope: analyzer tests.
    /// </summary>
    private static bool IsRepoRoot(string path)
    {
        return File.Exists(Path.Combine(path, "CipherBank-app.sln"))
            && File.Exists(Path.Combine(path, "CipherBank-app", "CipherBank-app.csproj"))
            && File.Exists(Path.Combine(path, "Directory.Build.targets"));
    }

    /// <summary>
    /// True when a repository-relative path is under bin or obj.
    /// Use: Low (unbuilt tree walk). Scope: analyzer tests.
    /// </summary>
    private static bool IsBinOrObj(string relativePath)
    {
        return relativePath.Contains("/bin/", StringComparison.OrdinalIgnoreCase)
            || relativePath.Contains("/obj/", StringComparison.OrdinalIgnoreCase);
    }
}
