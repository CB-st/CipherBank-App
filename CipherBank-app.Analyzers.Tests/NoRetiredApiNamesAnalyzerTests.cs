// <copyright file="NoRetiredApiNamesAnalyzerTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace CipherBank_app.Analyzers.Tests;

public sealed class NoRetiredApiNamesAnalyzerTests
{
    [Fact]
    public async Task ReportsIProductApiAsync()
    {
        var test = new CSharpAnalyzerTest<NoRetiredApiNamesAnalyzer, DefaultVerifier>
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
    public async Task ReportsMockProductApiAndAppSessionDepsAsync()
    {
        var test = new CSharpAnalyzerTest<NoRetiredApiNamesAnalyzer, DefaultVerifier>
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
    public async Task IgnoresUnrelatedIdentifiersAsync()
    {
        var test = new CSharpAnalyzerTest<NoRetiredApiNamesAnalyzer, DefaultVerifier>
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
    public async Task ReportsRetiredNameFromAdditionalHostFileAsync()
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
    public async Task DoesNotDoubleReportWhenAdditionalFileIsCompilationTreeAsync()
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
