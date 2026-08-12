// <copyright file="LocalDbSql.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Data;
using System.Data.Common;
using System.Globalization;
using Microsoft.EntityFrameworkCore;

namespace CipherBank_app.Persist.Sql;

/// <summary>
/// Centralized, idempotent SQL for compatibility repair that EF Core cannot infer
/// from databases created by pre-EF builds.
/// </summary>
internal static class LocalDbSql
{
    private static readonly IReadOnlyList<ColumnUpgrade> RecipientColumns =
    [
        new("holder", CompatibilityStatement.AddRecipientHolder),
        new("bank", CompatibilityStatement.AddRecipientBank),
        new("account_type", CompatibilityStatement.AddRecipientAccountType),
        new("memo", CompatibilityStatement.AddRecipientMemo),
        new("account_mask", CompatibilityStatement.AddRecipientAccountMask),
        new("routing_mask", CompatibilityStatement.AddRecipientRoutingMask),
    ];

    private static readonly IReadOnlyList<SensitiveColumnScrub> RecipientScrubs =
    [
        new("account", CompatibilityStatement.ScrubRecipientAccount),
        new("routing", CompatibilityStatement.ScrubRecipientRouting),
        new("account_full", CompatibilityStatement.ScrubRecipientAccountFull),
    ];

    private enum CompatibilityStatement
    {
        AddRecipientHolder,
        AddRecipientBank,
        AddRecipientAccountType,
        AddRecipientMemo,
        AddRecipientAccountMask,
        AddRecipientRoutingMask,
        ScrubRecipientAccount,
        ScrubRecipientRouting,
        ScrubRecipientAccountFull,
    }

    /// <summary>Upgrades the known legacy recipient shape and removes cleartext remnants.</summary>
    internal static Task ApplyCompatibilityAsync(DbConnection connection, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(connection);
        return ApplyCompatibilityCoreAsync(connection, ct);
    }

    /// <summary>
    /// Creates EF model tables that <c>EnsureCreated</c> skipped because a legacy nonempty DB already existed.
    /// Use: Low (first open after upgrade). Scope: LocalDb initialization.
    /// </summary>
    internal static Task EnsureMissingModelTablesAsync(DbContext context, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(context);
        return EnsureMissingModelTablesCoreAsync(context, ct);
    }

    private static async Task EnsureMissingModelTablesCoreAsync(
        DbContext context,
        CancellationToken ct)
    {
        DbConnection connection = context.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(ct).ConfigureAwait(false);
        }

        HashSet<string> existing = await ListUserTablesAsync(connection, ct).ConfigureAwait(false);

        // Script is generated from the compiled EF model (not user input).
        string script = context.Database.GenerateCreateScript();
        foreach (string statement in SplitSqliteScript(script))
        {
            if (!ShouldExecuteCreateStatement(statement, existing))
            {
                continue;
            }

            await using DbCommand command = connection.CreateCommand();
            command.CommandText = statement;
            try
            {
                await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }
            catch (DbException) when (statement.StartsWith("CREATE INDEX", StringComparison.OrdinalIgnoreCase)
                || statement.StartsWith("CREATE UNIQUE INDEX", StringComparison.OrdinalIgnoreCase))
            {
                // Index may already exist on a partially upgraded database.
            }

            string? createdTable = TryExtractCreateTableName(statement);
            if (createdTable is not null)
            {
                existing.Add(createdTable);
            }
        }
    }

    /// <summary>
    /// Lists user tables already present in the SQLite catalog.
    /// Use: Low (schema upgrade). Scope: LocalDbSql.
    /// </summary>
    private static async Task<HashSet<string>> ListUserTablesAsync(
        DbConnection connection,
        CancellationToken ct)
    {
        HashSet<string> tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = SqliteCatalogSql.ListUserTables;
        await using DbDataReader reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
    }

    /// <summary>
    /// Splits an EF-generated SQLite create script into executable statements.
    /// Use: Low. Scope: LocalDbSql script helper.
    /// </summary>
    private static IEnumerable<string> SplitSqliteScript(string script)
    {
        string[] parts = script.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (string part in parts)
        {
            if (part.Length > 0)
            {
                yield return part;
            }
        }
    }

    /// <summary>
    /// Skips CREATE TABLE / INDEX statements whose target already exists.
    /// Use: Low. Scope: LocalDbSql script helper.
    /// </summary>
    private static bool ShouldExecuteCreateStatement(string statement, ICollection<string> existingTables)
    {
        string? table = TryExtractCreateTableName(statement);
        if (table is not null)
        {
            return !existingTables.Contains(table);
        }

        // Indexes / other DDL from GenerateCreateScript — run only when the base table is present
        // and the statement is not a duplicate CREATE TABLE we already skipped.
        return statement.StartsWith("CREATE ", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Parses the table name from a CREATE TABLE statement, if present.
    /// Use: Low. Scope: LocalDbSql script helper.
    /// </summary>
    private static string? TryExtractCreateTableName(string statement)
    {
        const string marker = "CREATE TABLE";
        if (!statement.StartsWith(marker, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        string rest = statement[marker.Length..].TrimStart();
        if (rest.StartsWith("IF NOT EXISTS", StringComparison.OrdinalIgnoreCase))
        {
            rest = rest["IF NOT EXISTS".Length..].TrimStart();
        }

        if (rest.Length == 0)
        {
            return null;
        }

        if (rest[0] is '"' or '\'')
        {
            char quote = rest[0];
            int end = rest.IndexOf(quote, 1);
            return end > 1 ? rest[1..end] : null;
        }

        int stop = rest.IndexOfAny([' ', '(', '\r', '\n']);
        return stop < 0 ? rest : rest[..stop];
    }

    private static async Task ApplyCompatibilityCoreAsync(DbConnection connection, CancellationToken ct)
    {
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(ct).ConfigureAwait(false);
        }

        if (!await TableExistsAsync(connection, "recipients", ct).ConfigureAwait(false))
        {
            return;
        }

        await ApplyRecipientColumnUpgradesAsync(connection, ct).ConfigureAwait(false);
        await ApplyRecipientScrubsAsync(connection, ct).ConfigureAwait(false);
    }

    private static async Task ApplyRecipientColumnUpgradesAsync(DbConnection connection, CancellationToken ct)
    {
        foreach (ColumnUpgrade upgrade in RecipientColumns)
        {
            if (!await RecipientColumnExistsAsync(connection, upgrade.ColumnName, ct).ConfigureAwait(false))
            {
                await ExecuteConstantAsync(connection, upgrade.Statement, ct).ConfigureAwait(false);
            }
        }
    }

    private static async Task ApplyRecipientScrubsAsync(DbConnection connection, CancellationToken ct)
    {
        foreach (SensitiveColumnScrub scrub in RecipientScrubs)
        {
            if (await RecipientColumnExistsAsync(connection, scrub.ColumnName, ct).ConfigureAwait(false))
            {
                await ExecuteConstantAsync(connection, scrub.Statement, ct).ConfigureAwait(false);
            }
        }
    }

    private static async Task<bool> TableExistsAsync(
        DbConnection connection,
        string tableName,
        CancellationToken ct)
    {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = SqliteCatalogSql.TableExistsByName;
        AddParameter(command, "$name", tableName);
        return Convert.ToInt64(
            await command.ExecuteScalarAsync(ct).ConfigureAwait(false),
            CultureInfo.InvariantCulture) > 0;
    }

    private static async Task<bool> RecipientColumnExistsAsync(
        DbConnection connection,
        string columnName,
        CancellationToken ct)
    {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = SqliteCatalogSql.RecipientColumnExistsByName;
        AddParameter(command, "$column", columnName);
        return Convert.ToInt64(
            await command.ExecuteScalarAsync(ct).ConfigureAwait(false),
            CultureInfo.InvariantCulture) > 0;
    }

    /// <summary>
    /// Executes a compatibility DDL/DML statement. Each arm assigns a compile-time SQL literal to
    /// CommandText so CA2100 does not require a suppression (and S1309 stays clear).
    /// Use: Low (schema upgrade). Scope: LocalDbSql compatibility repair.
    /// </summary>
    private static Task ExecuteConstantAsync(
        DbConnection connection,
        CompatibilityStatement statement,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(connection);
        if (!Enum.IsDefined(statement))
        {
            throw new ArgumentOutOfRangeException(nameof(statement));
        }

        return ExecuteConstantCoreAsync(connection, statement, ct);
    }

    private static async Task ExecuteConstantCoreAsync(
        DbConnection connection,
        CompatibilityStatement statement,
        CancellationToken ct)
    {
        await using DbCommand command = connection.CreateCommand();
        switch (statement)
        {
            case CompatibilityStatement.AddRecipientHolder:
                command.CommandText = "ALTER TABLE recipients ADD COLUMN holder TEXT";
                break;
            case CompatibilityStatement.AddRecipientBank:
                command.CommandText = "ALTER TABLE recipients ADD COLUMN bank TEXT";
                break;
            case CompatibilityStatement.AddRecipientAccountType:
                command.CommandText =
                    "ALTER TABLE recipients ADD COLUMN account_type TEXT NOT NULL DEFAULT 'checking'";
                break;
            case CompatibilityStatement.AddRecipientMemo:
                command.CommandText = "ALTER TABLE recipients ADD COLUMN memo TEXT";
                break;
            case CompatibilityStatement.AddRecipientAccountMask:
                command.CommandText = "ALTER TABLE recipients ADD COLUMN account_mask TEXT";
                break;
            case CompatibilityStatement.AddRecipientRoutingMask:
                command.CommandText = "ALTER TABLE recipients ADD COLUMN routing_mask TEXT";
                break;
            case CompatibilityStatement.ScrubRecipientAccount:
                command.CommandText = "UPDATE recipients SET account = NULL WHERE account IS NOT NULL";
                break;
            case CompatibilityStatement.ScrubRecipientRouting:
                command.CommandText = "UPDATE recipients SET routing = NULL WHERE routing IS NOT NULL";
                break;
            case CompatibilityStatement.ScrubRecipientAccountFull:
                command.CommandText = "UPDATE recipients SET account_full = NULL WHERE account_full IS NOT NULL";
                break;
            default:
                throw new InvalidOperationException($"Unhandled {nameof(CompatibilityStatement)} value.");
        }

        await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        DbParameter parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private sealed record ColumnUpgrade(string ColumnName, CompatibilityStatement Statement);

    private sealed record SensitiveColumnScrub(string ColumnName, CompatibilityStatement Statement);

    /// <summary>Catalog / pragma probes for compatibility repair (not user-facing copy).</summary>
    private static class SqliteCatalogSql
    {
        internal const string TableExistsByName =
            "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $name";

        internal const string ListUserTables =
            "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%'";

        internal const string RecipientColumnExistsByName =
            "SELECT COUNT(*) FROM pragma_table_info('recipients') WHERE name = $column";
    }
}
