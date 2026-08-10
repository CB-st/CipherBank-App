// <copyright file="LocalDbSql.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

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
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $name";
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
        command.CommandText = "SELECT COUNT(*) FROM pragma_table_info('recipients') WHERE name = $column";
        AddParameter(command, "$column", columnName);
        return Convert.ToInt64(
            await command.ExecuteScalarAsync(ct).ConfigureAwait(false),
            CultureInfo.InvariantCulture) > 0;
    }

    /// <summary>
    /// Executes a compatibility DDL/DML statement selected from a closed enum allowlist of literals.
    /// </summary>
    [SuppressMessage(
        "Security",
        "CA2100:Review SQL queries for security vulnerabilities",
        Justification = "CommandText is assigned only from CompatibilityStatement switch arms that return compile-time SQL literals; no user input reaches the query.")] // NOSONAR
    private static async Task ExecuteConstantAsync(
        DbConnection connection,
        CompatibilityStatement statement,
        CancellationToken ct)
    {
        await using DbCommand command = connection.CreateCommand();
        command.CommandText = ResolveCompatibilitySql(statement);
        await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    private static string ResolveCompatibilitySql(CompatibilityStatement statement)
        => statement switch
        {
            CompatibilityStatement.AddRecipientHolder => "ALTER TABLE recipients ADD COLUMN holder TEXT",
            CompatibilityStatement.AddRecipientBank => "ALTER TABLE recipients ADD COLUMN bank TEXT",
            CompatibilityStatement.AddRecipientAccountType => "ALTER TABLE recipients ADD COLUMN account_type TEXT NOT NULL DEFAULT 'checking'",
            CompatibilityStatement.AddRecipientMemo => "ALTER TABLE recipients ADD COLUMN memo TEXT",
            CompatibilityStatement.AddRecipientAccountMask => "ALTER TABLE recipients ADD COLUMN account_mask TEXT",
            CompatibilityStatement.AddRecipientRoutingMask => "ALTER TABLE recipients ADD COLUMN routing_mask TEXT",
            CompatibilityStatement.ScrubRecipientAccount => "UPDATE recipients SET account = NULL WHERE account IS NOT NULL",
            CompatibilityStatement.ScrubRecipientRouting => "UPDATE recipients SET routing = NULL WHERE routing IS NOT NULL",
            CompatibilityStatement.ScrubRecipientAccountFull => "UPDATE recipients SET account_full = NULL WHERE account_full IS NOT NULL",
            _ => throw new ArgumentOutOfRangeException(nameof(statement)),
        };

    private static void AddParameter(DbCommand command, string name, object value)
    {
        DbParameter parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private sealed record ColumnUpgrade(string ColumnName, CompatibilityStatement Statement);

    private sealed record SensitiveColumnScrub(string ColumnName, CompatibilityStatement Statement);
}
