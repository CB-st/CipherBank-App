// <copyright file="ProductTreeStructureAnalyzerTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CipherBank_app.Analyzers;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace CipherBank_app.Analyzers.Tests;

public sealed class ProductTreeStructureAnalyzerTests
{
    [Fact]
    public async Task LiveUnbuiltCsprojs_HaveNoPackageReferenceVersion()
    {
        CSharpAnalyzerTest<CentralPackageVersionAnalyzer, DefaultVerifier> test = new()
        {
            TestCode = "class C { }",
        };
        Attach(test, ProductTreeRepoRoot.MauiHostCsproj);
        Attach(test, ProductTreeRepoRoot.IntegrationTestsCsproj);
        Attach(test, ProductTreeRepoRoot.E2ETestsCsproj);
        await test.RunAsync();
    }

    [Fact]
    public async Task ReportsVersionWhenInjectedIntoLiveMauiCsproj()
    {
        CSharpAnalyzerTest<CentralPackageVersionAnalyzer, DefaultVerifier> test = new()
        {
            TestCode = "class C { }",
            TestState =
            {
                AdditionalFiles =
                {
                    (ProductTreeRepoRoot.MauiHostCsproj,
                     ProductTreeRepoRoot.InjectNewlinePackageVersion(
                         ProductTreeRepoRoot.Read(ProductTreeRepoRoot.MauiHostCsproj))),
                },
            },
        };
        await test.RunAsync();
    }

    [Fact]
    public void UnbuiltCsharpFiles_IncludesEveryTreeFile()
    {
        HashSet<string> paths = new(StringComparer.Ordinal);
        foreach ((string relativePath, string _) in ProductTreeRepoRoot.UnbuiltCsharpFiles())
        {
            paths.Add(relativePath);
        }

        Assert.Contains(ProductTreeRepoRoot.MauiHostSample, paths);
        Assert.Contains(ProductTreeRepoRoot.MauiHostNonSample, paths);
        Assert.Contains(ProductTreeRepoRoot.IntegrationTestsSample, paths);
        Assert.Contains("CipherBank-app.IntegrationTests/SecurityTests.cs", paths);
        Assert.Contains(ProductTreeRepoRoot.E2ETestsSample, paths);
        Assert.Contains("CipherBank-app.E2ETests/Tests/CriticalUserJourneyTests.cs", paths);
        Assert.True(paths.Count > 3, $"Expected the full unbuilt trees, found {paths.Count} files.");
    }

    [Fact]
    public async Task LiveUnbuiltCsharpFiles_HaveNoRetiredApiNames()
    {
        CSharpAnalyzerTest<NoRetiredApiNamesAnalyzer, DefaultVerifier> test = new()
        {
            CompilerDiagnostics = CompilerDiagnostics.None,
            TestCode = "class Wallet { }",
        };
        AttachAll(test, ProductTreeRepoRoot.UnbuiltCsharpFiles());
        await test.RunAsync();
    }

    [Fact]
    public async Task ReportsRetiredNameWhenInjectedIntoNonSampleUnbuiltFile()
    {
        CSharpAnalyzerTest<NoRetiredApiNamesAnalyzer, DefaultVerifier> test = new()
        {
            CompilerDiagnostics = CompilerDiagnostics.None,
            TestCode = "class Wallet { }",
        };
        AttachAll(test, WithInjectedRetiredName(ProductTreeRepoRoot.UnbuiltCsharpFiles()));
        await test.RunAsync();
    }

    [Fact]
    public async Task LiveUnbuiltCsharpFiles_HaveNoLegacyAssemblyInfo()
    {
        CSharpAnalyzerTest<NoLegacyAssemblyInfoAnalyzer, DefaultVerifier> test = new()
        {
            CompilerDiagnostics = CompilerDiagnostics.None,
            TestCode = "class Wallet { }",
        };
        AttachAll(test, ProductTreeRepoRoot.UnbuiltCsharpFiles());
        await test.RunAsync();
    }

    [Fact]
    public async Task LiveUnbuiltCsharpFiles_HaveNoScatteredSql()
    {
        CSharpAnalyzerTest<NoScatteredSqlAnalyzer, DefaultVerifier> test = new()
        {
            CompilerDiagnostics = CompilerDiagnostics.None,
            TestCode = "class Wallet { }",
        };
        AttachAll(test, ProductTreeRepoRoot.UnbuiltCsharpFiles());
        await test.RunAsync();
    }

    /// <summary>
    /// Attaches one on-disk product file as an additional analyzer input.
    /// Use: Low (structure Facts). Scope: this test class.
    /// </summary>
    private static void Attach<TAnalyzer>(CSharpAnalyzerTest<TAnalyzer, DefaultVerifier> test, string relativePath)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        test.TestState.AdditionalFiles.Add((relativePath, ProductTreeRepoRoot.Read(relativePath)));
    }

    /// <summary>
    /// Attaches every enumerated unbuilt C# file as additional analyzer input.
    /// Use: Low (structure Facts). Scope: this test class.
    /// </summary>
    private static void AttachAll<TAnalyzer>(
        CSharpAnalyzerTest<TAnalyzer, DefaultVerifier> test,
        List<(string RelativePath, string Content)> files)
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        foreach ((string relativePath, string content) in files)
        {
            test.TestState.AdditionalFiles.Add((relativePath, content));
        }
    }

    /// <summary>
    /// Replaces App.xaml.cs content with a retired-name injection.
    /// Use: Low (injected CB1004 Fact). Scope: this test class.
    /// </summary>
    private static List<(string RelativePath, string Content)> WithInjectedRetiredName(
        List<(string RelativePath, string Content)> files)
    {
        List<(string RelativePath, string Content)> mutated = new(files.Count);
        bool found = false;
        foreach ((string relativePath, string content) in files)
        {
            if (relativePath == ProductTreeRepoRoot.MauiHostNonSample)
            {
                mutated.Add((relativePath, ProductTreeRepoRoot.InjectRetiredApiName(content)));
                found = true;
                continue;
            }

            mutated.Add((relativePath, content));
        }

        if (!found)
        {
            throw new InvalidOperationException(
                $"{ProductTreeRepoRoot.MauiHostNonSample} was not in the unbuilt C# enumeration.");
        }

        return mutated;
    }
}
