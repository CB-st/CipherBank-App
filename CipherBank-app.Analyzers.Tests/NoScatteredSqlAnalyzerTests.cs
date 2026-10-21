// <copyright file="NoScatteredSqlAnalyzerTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace CipherBank_app.Analyzers.Tests;

public sealed class NoScatteredSqlAnalyzerTests
{
    // 10.0.12 matches Directory.Packages.props (Sqlite.Core pulls this Relational package).
    private static readonly ReferenceAssemblies _efCoreReferences =
        ReferenceAssemblies.Net.Net100.AddPackages(
            ImmutableArray.Create(new PackageIdentity(
                "Microsoft.EntityFrameworkCore.Relational",
                "10.0.12")));

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
    public async Task IgnoresUnrelatedMethodsNamedLikeRawSqlAsync()
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
                            void Run(Database database, MigrationBuilder migration)
                            {
                                FromSqlRaw("SELECT 1");
                                ExecuteSqlRaw("SELECT 1");
                                ExecuteSqlRawAsync("SELECT 1");
                                ExecuteSql("SELECT 1");
                                ExecuteSqlAsync("SELECT 1");
                                ExecuteSqlInterpolated("SELECT 1");
                                ExecuteSqlInterpolatedAsync("SELECT 1");
                                FromSql("SELECT 1");
                                FromSqlInterpolated("SELECT 1");
                                SqlQuery<int>("SELECT 1");
                                SqlQueryRaw<int>("SELECT 1");
                                database.SqlQuery<int>("SELECT 1");
                                database.SqlQueryRaw<int>("SELECT 1");
                                migration.Sql("SELECT 1");
                                migration?.Sql("SELECT 1");

                                System.Func<string, int> execute = ExecuteSqlRaw;
                                execute("SELECT 1");
                                var alias = execute;
                                alias("SELECT 1");
                            }

                            static object FromSqlRaw(string sql) => sql;

                            static System.Threading.Tasks.Task ExecuteSqlRawAsync(string sql) => System.Threading.Tasks.Task.CompletedTask;

                            static int ExecuteSql(string sql) => sql.Length;

                            static int ExecuteSqlRaw(string sql) => sql.Length;

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
                                ExecuteSqlRaw("SELECT 1");
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
    public async Task ReportsNullConditionalCommandTextAssignmentAsync()
    {
        var test = new CSharpAnalyzerTest<NoScatteredSqlAnalyzer, DefaultVerifier>
        {
            TestState =
            {
                Sources =
                {
                    ("CipherBank-app.Core/Persist/Migrations/NullConditionalCommandText.cs", """
                        class NullConditionalCommandText
                        {
                            void Run(System.Data.IDbCommand command, Holder outer)
                            {
                                command?.{|CB1003:CommandText|} = "SELECT 1";
                                command?.{|CB1003:CommandText|} += " OR 1=1";
                                command?.{|CB1003:CommandText|} ??= "SELECT 1";
                                command?.CommandTimeout = 1;
                                outer?.Command?.{|CB1003:CommandText|} = "SELECT 1";
                            }

                            sealed class Holder
                            {
                                public System.Data.IDbCommand Command { get; set; }
                            }
                        }
                        """),
                },
            },
        };
        await test.RunAsync();
    }

    [Fact]
    public async Task ReportsEfRelationalRawSqlEntryPointsAsync()
    {
        var test = new CSharpAnalyzerTest<NoScatteredSqlAnalyzer, DefaultVerifier>
        {
            TestState =
            {
                ReferenceAssemblies = _efCoreReferences,
                Sources =
                {
                    ("CipherBank-app.Core/Persist/Migrations/RawSqlEntryPoints.cs", """
                        using Microsoft.EntityFrameworkCore;
                        using Microsoft.EntityFrameworkCore.Infrastructure;
                        using Microsoft.EntityFrameworkCore.Metadata;
                        using Microsoft.EntityFrameworkCore.Migrations;

                        sealed class Item
                        {
                            public int Id { get; set; }
                        }

                        sealed class AppDb : DbContext
                        {
                            public DbSet<Item> Items => Set<Item>();
                        }

                        sealed class RawSqlEntryPoints
                        {
                            void Up(AppDb db, DatabaseFacade database, DbSet<Item> set, MigrationBuilder migration)
                            {
                                {|CB1003:set.FromSqlRaw("SELECT 1")|};
                                {|CB1003:set.FromSqlInterpolated($"SELECT 1")|};
                                {|CB1003:set.FromSql($"SELECT 1")|};
                                {|CB1003:database.ExecuteSqlRaw("SELECT 1")|};
                                {|CB1003:database.ExecuteSqlRawAsync("SELECT 1")|};
                                {|CB1003:database.ExecuteSql($"SELECT 1")|};
                                {|CB1003:database.ExecuteSqlAsync($"SELECT 1")|};
                                {|CB1003:database.ExecuteSqlInterpolated($"SELECT 1")|};
                                {|CB1003:database.ExecuteSqlInterpolatedAsync($"SELECT 1")|};
                                {|CB1003:database.SqlQuery<int>($"SELECT 1")|};
                                {|CB1003:database.SqlQueryRaw<int>("SELECT 1")|};
                                {|CB1003:db.Database.ExecuteSqlRaw("SELECT 1")|};
                                {|CB1003:migration.Sql("SELECT 1")|};
                                migration?{|CB1003:.Sql("SELECT 1")|};
                                database?{|CB1003:.ExecuteSqlRaw("SELECT 1")|};
                                _ = StoreObjectIdentifier.SqlQuery("Orders");
                            }
                        }
                        """),
                },
            },
        };
        await test.RunAsync();
    }

    [Fact]
    public async Task ReportsMethodGroupAndDelegateAliasesAsync()
    {
        var test = new CSharpAnalyzerTest<NoScatteredSqlAnalyzer, DefaultVerifier>
        {
            TestState =
            {
                ReferenceAssemblies = _efCoreReferences,
                Sources =
                {
                    ("CipherBank-app.Core/Persist/Migrations/RawSqlAliases.cs", """
                        using Microsoft.EntityFrameworkCore;
                        using Microsoft.EntityFrameworkCore.Infrastructure;

                        sealed class Item
                        {
                            public int Id { get; set; }
                        }

                        sealed class RawSqlAliases
                        {
                            private System.Func<System.FormattableString, int> _execute = static _ => 0;

                            void Alias(DatabaseFacade database, Microsoft.EntityFrameworkCore.DbSet<Item> set)
                            {
                                var execute = {|CB1003:database.ExecuteSqlInterpolated|};
                                {|CB1003:execute($"SELECT 1")|};
                                var alias = execute;
                                {|CB1003:alias($"SELECT 1")|};

                                System.Func<string, object[], int> raw = {|CB1003:database.ExecuteSqlRaw|};
                                {|CB1003:raw("SELECT 1", System.Array.Empty<object>())|};
                                System.Func<string, object[], int> copied = raw;
                                {|CB1003:copied("SELECT 1", System.Array.Empty<object>())|};

                                void Fire(System.Func<System.FormattableString, int> command) => {|CB1003:command($"SELECT 1")|};
                                Fire({|CB1003:database.ExecuteSqlInterpolated|});

                                System.Func<System.FormattableString, int> created =
                                    new System.Func<System.FormattableString, int>({|CB1003:database.ExecuteSqlInterpolated|});
                                {|CB1003:created($"SELECT 1")|};

                                {|CB1003:((System.Func<System.FormattableString, int>){|CB1003:database.ExecuteSqlInterpolated|})($"SELECT 1")|};

                                System.Func<System.FormattableString, int> chosen = true
                                    ? {|CB1003:database.ExecuteSqlInterpolated|}
                                    : static _ => 0;
                                {|CB1003:chosen($"SELECT 1")|};

                                System.Func<System.FormattableString, System.Linq.IQueryable<int>> query = {|CB1003:database.SqlQuery<int>|};
                                {|CB1003:query($"SELECT 1")|};

                                System.Func<System.FormattableString, int> returned = Get(database);
                                {|CB1003:returned($"SELECT 1")|};
                                {|CB1003:Get(database)($"SELECT 1")|};
                            }

                            void Arm(DatabaseFacade database)
                            {
                                _execute = {|CB1003:database.ExecuteSqlInterpolated|};
                            }

                            void FireField()
                            {
                                {|CB1003:_execute($"SELECT 1")|};
                            }

                            System.Func<System.FormattableString, int> Get(DatabaseFacade database)
                                => {|CB1003:database.ExecuteSqlInterpolated|};
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
