// <copyright file="NoRetiredApiNamesAnalyzerTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Threading.Tasks;
using CipherBank_app.Analyzers;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace CipherBank_app.Analyzers.Tests;

public sealed class NoRetiredApiNamesAnalyzerTests
{
    [Fact]
    public async Task ReportsIProductApi()
    {
        CSharpAnalyzerTest<NoRetiredApiNamesAnalyzer, DefaultVerifier> test = new()
        {
            CompilerDiagnostics = CompilerDiagnostics.None,
            TestCode = """
                class C
                {
                    {|CB1004:IProductApi|} api;
                }
                """,
        };
        await test.RunAsync();
    }

    [Fact]
    public async Task ReportsMockProductApiAndAppSessionDeps()
    {
        CSharpAnalyzerTest<NoRetiredApiNamesAnalyzer, DefaultVerifier> test = new()
        {
            CompilerDiagnostics = CompilerDiagnostics.None,
            TestCode = """
                class {|CB1004:MockProductApi|}
                {
                    {|CB1004:AppSessionDeps|} Deps;
                }
                """,
        };
        await test.RunAsync();
    }

    [Fact]
    public async Task IgnoresUnrelatedIdentifiers()
    {
        CSharpAnalyzerTest<NoRetiredApiNamesAnalyzer, DefaultVerifier> test = new()
        {
            CompilerDiagnostics = CompilerDiagnostics.None,
            TestCode = """
                class WalletService
                {
                    int count;
                }
                """,
        };
        await test.RunAsync();
    }

    [Fact]
    public async Task ReportsRetiredNameFromAdditionalHostFile()
    {
        CSharpAnalyzerTest<NoRetiredApiNamesAnalyzer, DefaultVerifier> test = new()
        {
            CompilerDiagnostics = CompilerDiagnostics.None,
            TestCode = "class Wallet { }",
            TestState =
            {
                AdditionalFiles =
                {
                    ("CipherBank-app/Services/Host.cs", """
                        class Host
                        {
                            {|CB1004:IProductApi|} Api;
                        }
                        """),
                    ("notes.md", "IProductApi"),
                },
            },
        };
        await test.RunAsync();
    }

    [Fact]
    public async Task DoesNotDoubleReportWhenAdditionalFileIsCompilationTree()
    {
        CSharpAnalyzerTest<NoRetiredApiNamesAnalyzer, DefaultVerifier> test = new()
        {
            CompilerDiagnostics = CompilerDiagnostics.None,
            TestState =
            {
                Sources =
                {
                    ("CipherBank-app/Services/Host.cs", """
                        class Host
                        {
                            {|CB1004:IProductApi|} Api;
                        }
                        """),
                },
                AdditionalFiles =
                {
                    ("CipherBank-app/Services/Host.cs", """
                        class Host
                        {
                            IProductApi Api;
                        }
                        """),
                },
            },
        };
        await test.RunAsync();
    }
}
