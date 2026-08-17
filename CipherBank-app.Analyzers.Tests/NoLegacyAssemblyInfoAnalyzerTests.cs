// <copyright file="NoLegacyAssemblyInfoAnalyzerTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Threading.Tasks;
using CipherBank_app.Analyzers;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace CipherBank_app.Analyzers.Tests;

public sealed class NoLegacyAssemblyInfoAnalyzerTests
{
    [Fact]
    public async Task ReportsPropertiesAssemblyInfo()
    {
        CSharpAnalyzerTest<NoLegacyAssemblyInfoAnalyzer, DefaultVerifier> test = new()
        {
            TestState =
            {
                Sources =
                {
                    ("Properties/AssemblyInfo.cs", """
                        {|CB1002:class AssemblyInfo
                        {
                        }|}
                        """),
                },
            },
        };
        await test.RunAsync();
    }

    [Fact]
    public async Task IgnoresOrdinarySource()
    {
        CSharpAnalyzerTest<NoLegacyAssemblyInfoAnalyzer, DefaultVerifier> test = new()
        {
            TestCode = """
                class Wallet
                {
                }
                """,
        };
        await test.RunAsync();
    }

    [Fact]
    public async Task ReportsAssemblyInfoFromAdditionalFile()
    {
        CSharpAnalyzerTest<NoLegacyAssemblyInfoAnalyzer, DefaultVerifier> test = new()
        {
            TestCode = "class Wallet { }",
            TestState =
            {
                AdditionalFiles =
                {
                    ("CipherBank-app/Properties/AssemblyInfo.cs", """
                        {|CB1002:class AssemblyInfo
                        {
                        }|}
                        """),
                },
            },
        };
        await test.RunAsync();
    }

    [Fact]
    public async Task DoesNotDoubleReportWhenAdditionalAssemblyInfoIsCompilationTree()
    {
        CSharpAnalyzerTest<NoLegacyAssemblyInfoAnalyzer, DefaultVerifier> test = new()
        {
            TestState =
            {
                Sources =
                {
                    ("CipherBank-app/Properties/AssemblyInfo.cs", """
                        {|CB1002:class AssemblyInfo
                        {
                        }|}
                        """),
                },
                AdditionalFiles =
                {
                    ("CipherBank-app/Properties/AssemblyInfo.cs", """
                        class AssemblyInfo
                        {
                        }
                        """),
                },
            },
        };
        await test.RunAsync();
    }
}
