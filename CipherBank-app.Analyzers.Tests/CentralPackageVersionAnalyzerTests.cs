// <copyright file="CentralPackageVersionAnalyzerTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Threading.Tasks;
using CipherBank_app.Analyzers;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace CipherBank_app.Analyzers.Tests;

public sealed class CentralPackageVersionAnalyzerTests
{
    [Fact]
    public async Task ReportsVersionOnCsprojPackageReference()
    {
        var test = new CSharpAnalyzerTest<CentralPackageVersionAnalyzer, DefaultVerifier>
        {
            TestCode = "class C { }",
            TestState =
            {
                AdditionalFiles =
                {
                    ("CipherBank-app.Core/CipherBank-app.Core.csproj", """
                        <Project Sdk="Microsoft.NET.Sdk">
                          <ItemGroup>
                            {|CB1001:<PackageReference Include="NBitcoin" Version="8.0.13" />|}
                          </ItemGroup>
                        </Project>
                        """),
                },
            },
        };
        await test.RunAsync();
    }

    [Fact]
    public async Task IgnoresDirectoryPackagesProps()
    {
        var test = new CSharpAnalyzerTest<CentralPackageVersionAnalyzer, DefaultVerifier>
        {
            TestCode = "class C { }",
            TestState =
            {
                AdditionalFiles =
                {
                    ("Directory.Packages.props", """
                        <Project>
                          <ItemGroup>
                            <PackageVersion Include="NBitcoin" Version="8.0.13" />
                          </ItemGroup>
                        </Project>
                        """),
                    ("CipherBank-app.Core/CipherBank-app.Core.csproj", """
                        <Project Sdk="Microsoft.NET.Sdk">
                          <ItemGroup>
                            <PackageReference Include="NBitcoin" />
                          </ItemGroup>
                        </Project>
                        """),
                },
            },
        };
        await test.RunAsync();
    }
}
