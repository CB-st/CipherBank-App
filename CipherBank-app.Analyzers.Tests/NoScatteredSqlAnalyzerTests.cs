// <copyright file="NoScatteredSqlAnalyzerTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace CipherBank_app.Analyzers.Tests;

public sealed class NoScatteredSqlAnalyzerTests
{
    [Fact]
    public async Task ReportsCommandTextOutsideOwner()
    {
        CSharpAnalyzerTest<NoScatteredSqlAnalyzer, DefaultVerifier> test = new()
        {
            TestState =
            {
                Sources =
                {
                    ("CipherBank-app.Core/Services/Query.cs", """
                        class Query
                        {
                            void Run(System.Data.IDbCommand command)
                            {
                                command.{|CB1003:CommandText|} = "SELECT 1";
                            }
                        }
                        """),
                },
            },
        };
        await test.RunAsync();
    }

    [Fact]
    public async Task ReportsFromSqlRawOutsideOwner()
    {
        CSharpAnalyzerTest<NoScatteredSqlAnalyzer, DefaultVerifier> test = new()
        {
            TestState =
            {
                Sources =
                {
                    ("CipherBank-app.Core/Services/Query.cs", """
                        class Query
                        {
                            void Run()
                            {
                                {|CB1003:FromSqlRaw("SELECT 1")|};
                            }

                            static object FromSqlRaw(string sql) => sql;
                        }
                        """),
                },
            },
        };
        await test.RunAsync();
    }

    [Fact]
    public async Task ReportsCommandTextInPersistSqlFolder()
    {
        CSharpAnalyzerTest<NoScatteredSqlAnalyzer, DefaultVerifier> test = new()
        {
            TestState =
            {
                Sources =
                {
                    ("CipherBank-app.Core/Persist/Sql/LocalDbSql.cs", """
                        class LocalDbSql
                        {
                            void Run(System.Data.IDbCommand command)
                            {
                                command.{|CB1003:CommandText|} = "SELECT 1";
                                {|CB1003:ExecuteSqlRaw("SELECT 1")|};
                            }

                            static void ExecuteSqlRaw(string sql) { }
                        }
                        """),
                },
            },
        };
        await test.RunAsync();
    }

    [Fact]
    public async Task IgnoresSqlOutsideCore()
    {
        CSharpAnalyzerTest<NoScatteredSqlAnalyzer, DefaultVerifier> test = new()
        {
            TestState =
            {
                Sources =
                {
                    ("CipherBank-app.Tests/QueryTests.cs", """
                        class QueryTests
                        {
                            void Run(System.Data.IDbCommand command)
                            {
                                command.CommandText = "SELECT 1";
                            }
                        }
                        """),
                },
            },
        };
        await test.RunAsync();
    }
}
