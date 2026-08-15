// <copyright file="ProductTreeStructureAnalyzerTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Threading.Tasks;
using CipherBank_app.Analyzers;
using Microsoft.CodeAnalysis.CSharp.Testing;
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
    public async Task LiveUnbuiltCsharpSamples_HaveNoRetiredApiNames()
    {
        CSharpAnalyzerTest<NoRetiredApiNamesAnalyzer, DefaultVerifier> test = new()
        {
            CompilerDiagnostics = CompilerDiagnostics.None,
            TestCode = "class Wallet { }",
        };
        Attach(test, ProductTreeRepoRoot.MauiHostSample);
        Attach(test, ProductTreeRepoRoot.IntegrationTestsSample);
        Attach(test, ProductTreeRepoRoot.E2ETestsSample);
        await test.RunAsync();
    }

    [Fact]
    public async Task LiveUnbuiltCsharpSamples_HaveNoLegacyAssemblyInfo()
    {
        CSharpAnalyzerTest<NoLegacyAssemblyInfoAnalyzer, DefaultVerifier> test = new()
        {
            CompilerDiagnostics = CompilerDiagnostics.None,
            TestCode = "class Wallet { }",
        };
        Attach(test, ProductTreeRepoRoot.MauiHostSample);
        Attach(test, ProductTreeRepoRoot.IntegrationTestsSample);
        Attach(test, ProductTreeRepoRoot.E2ETestsSample);
        await test.RunAsync();
    }

    /// <summary>
    /// Attaches one on-disk product file as an additional analyzer input.
    /// Use: Low (structure Facts). Scope: this test class.
    /// </summary>
    private static void Attach<TAnalyzer>(CSharpAnalyzerTest<TAnalyzer, DefaultVerifier> test, string relativePath)
        where TAnalyzer : Microsoft.CodeAnalysis.Diagnostics.DiagnosticAnalyzer, new()
    {
        test.TestState.AdditionalFiles.Add((relativePath, ProductTreeRepoRoot.Read(relativePath)));
    }
}
