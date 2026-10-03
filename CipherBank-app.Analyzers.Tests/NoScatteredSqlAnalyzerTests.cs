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
    public async Task ReportsCommandTextOutsideOwnerAsync()
    {
        var test = new CSharpAnalyzerTest<NoScatteredSqlAnalyzer, DefaultVerifier>
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
    public async Task ReportsFromSqlRawOutsideOwnerAsync()
    {
        var test = new CSharpAnalyzerTest<NoScatteredSqlAnalyzer, DefaultVerifier>
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
    public async Task ReportsCommandTextInPersistSqlFolderAsync()
    {
        var test = new CSharpAnalyzerTest<NoScatteredSqlAnalyzer, DefaultVerifier>
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
    public async Task ReportsCommandTextInPersistMigrationsFolderAsync()
    {
        var test = new CSharpAnalyzerTest<NoScatteredSqlAnalyzer, DefaultVerifier>
        {
            TestState =
            {
                Sources =
                {
                    ("CipherBank-app.Core/Persist/Migrations/InitialCreate.cs", """
                        class InitialCreate
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
    public async Task IgnoresSqlOutsideCoreAsync()
    {
        var test = new CSharpAnalyzerTest<NoScatteredSqlAnalyzer, DefaultVerifier>
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

    [Fact]
    public async Task ReportsCommandTextObjectInitializerAndAddAssignmentAsync()
    {
        var test = new CSharpAnalyzerTest<NoScatteredSqlAnalyzer, DefaultVerifier>
        {
            TestState =
            {
                Sources =
                {
                    ("CipherBank-app.Core/Persist/Migrations/CommandTextShapes.cs", """
                        class CommandTextShapes
                        {
                            public string CommandText { get; set; } = "";

                            void Run(System.Data.IDbCommand command)
                            {
                                command.{|CB1003:CommandText|} += "SELECT 1";
                                {|CB1003:CommandText|} += " OR 1=1";
                                command.CommandTimeout = 1;
                                _ = new Row
                                {
                                    {|CB1003:CommandText|} = "SELECT 1",
                                };
                            }

                            sealed class Row
                            {
                                public string CommandText { get; set; } = "";
                            }
                        }
                        """),
                },
            },
        };
        await test.RunAsync();
    }

    [Fact]
    public async Task ReportsMissedRawSqlEntryPointsAsync()
    {
        var test = new CSharpAnalyzerTest<NoScatteredSqlAnalyzer, DefaultVerifier>
        {
            TestState =
            {
                Sources =
                {
                    ("CipherBank-app.Core/Persist/Migrations/RawSqlEntryPoints.cs", """
                        class RawSqlEntryPoints
                        {
                            void Up(Database database, MigrationBuilder migration)
                            {
                                {|CB1003:ExecuteSqlRawAsync("SELECT 1")|};
                                {|CB1003:ExecuteSql("SELECT 1")|};
                                {|CB1003:ExecuteSqlAsync("SELECT 1")|};
                                {|CB1003:ExecuteSqlInterpolated("SELECT 1")|};
                                {|CB1003:ExecuteSqlInterpolatedAsync("SELECT 1")|};
                                {|CB1003:FromSql("SELECT 1")|};
                                {|CB1003:FromSqlInterpolated("SELECT 1")|};
                                {|CB1003:SqlQuery<int>("SELECT 1")|};
                                {|CB1003:SqlQueryRaw<int>("SELECT 1")|};
                                {|CB1003:database.SqlQuery<int>("SELECT 1")|};
                                {|CB1003:database.SqlQueryRaw<int>("SELECT 1")|};
                                {|CB1003:migration.Sql("SELECT 1")|};
                                migration?{|CB1003:.Sql("SELECT 1")|};
                            }

                            static System.Threading.Tasks.Task ExecuteSqlRawAsync(string sql) => System.Threading.Tasks.Task.CompletedTask;

                            static int ExecuteSql(string sql) => sql.Length;

                            static System.Threading.Tasks.Task ExecuteSqlAsync(string sql) => System.Threading.Tasks.Task.CompletedTask;

                            static int ExecuteSqlInterpolated(string sql) => sql.Length;

                            static System.Threading.Tasks.Task ExecuteSqlInterpolatedAsync(string sql) => System.Threading.Tasks.Task.CompletedTask;

                            static object FromSql(string sql) => sql;

                            static object FromSqlInterpolated(string sql) => sql;

                            static object SqlQuery<T>(string sql) => sql;

                            static object SqlQueryRaw<T>(string sql) => sql;

                            sealed class Database
                            {
                                public object SqlQuery<T>(string sql) => sql;

                                public object SqlQueryRaw<T>(string sql) => sql;
                            }

                            sealed class MigrationBuilder
                            {
                                public int Sql(string sql) => sql.Length;
                            }
                        }
                        """),
                },
            },
        };
        await test.RunAsync();
    }

    [Fact]
    public async Task IgnoresSqlNamesThatAreNotInvocationsOrAssignmentsAsync()
    {
        var test = new CSharpAnalyzerTest<NoScatteredSqlAnalyzer, DefaultVerifier>
        {
            TestState =
            {
                Sources =
                {
                    ("CipherBank-app.Core/Persist/Migrations/SqlDeclaration.cs", """
                        class SqlDeclaration
                        {
                            string CommandText = "SELECT 1";

                            void Sql(string sql)
                            {
                                _ = sql;
                                _ = CommandText;
                                _ = nameof(Sql);
                                _ = nameof(CommandText);
                                SaveChanges();
                            }

                            void SaveChanges()
                            {
                            }
                        }
                        """),
                },
            },
        };
        await test.RunAsync();
    }
}
