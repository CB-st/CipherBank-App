// <copyright file="RecipientRepository.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Globalization;
using Microsoft.Data.Sqlite;

namespace CipherBank_app.Persist;

/// <inheritdoc />
public sealed class RecipientRepository : IRecipientRepository
{
    private const string DefaultAccountType = "checking";

    private readonly ILocalDb _db;
    private readonly TimeProvider _timeProvider;
    private bool _schemaReady;

    public RecipientRepository(ILocalDb db)
        : this(db, TimeProvider.System)
    {
    }

    public RecipientRepository(ILocalDb db, TimeProvider timeProvider)
    {
        _db = db;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task EnsureSchemaAsync()
    {
        if (_schemaReady)
        {
            return;
        }

        await using SqliteConnection conn = _db.Open();
        await conn.OpenAsync().ConfigureAwait(false);
        await TryAddRecipientColumnAsync(conn, "ALTER TABLE recipients ADD COLUMN holder TEXT").ConfigureAwait(false);
        await TryAddRecipientColumnAsync(conn, "ALTER TABLE recipients ADD COLUMN bank TEXT").ConfigureAwait(false);
        await TryAddRecipientColumnAsync(conn, "ALTER TABLE recipients ADD COLUMN routing TEXT").ConfigureAwait(false);
        await TryAddRecipientColumnAsync(conn, "ALTER TABLE recipients ADD COLUMN account TEXT").ConfigureAwait(false);
        await TryAddRecipientColumnAsync(conn, "ALTER TABLE recipients ADD COLUMN account_type TEXT").ConfigureAwait(false);
        await TryAddRecipientColumnAsync(conn, "ALTER TABLE recipients ADD COLUMN memo TEXT").ConfigureAwait(false);
        await ClearSensitiveRecipientColumnsAsync(conn).ConfigureAwait(false);
        _schemaReady = true;
    }

    public async Task<IReadOnlyList<AchRecipientRow>> ListAsync()
    {
        await EnsureSchemaAsync().ConfigureAwait(false);
        await using SqliteConnection conn = _db.Open();
        await conn.OpenAsync().ConfigureAwait(false);
        await using SqliteCommand cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT id, name, holder, bank, account_type, memo,
                   account_mask, routing_mask, created_at
            FROM recipients ORDER BY name
            """;
        var list = new List<AchRecipientRow>();
        await using SqliteDataReader reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
        int ordId = reader.GetOrdinal("id");
        int ordName = reader.GetOrdinal("name");
        int ordHolder = reader.GetOrdinal("holder");
        int ordBank = reader.GetOrdinal("bank");
        int ordAccountType = reader.GetOrdinal("account_type");
        int ordMemo = reader.GetOrdinal("memo");
        int ordAccountMask = reader.GetOrdinal("account_mask");
        int ordRoutingMask = reader.GetOrdinal("routing_mask");
        int ordCreatedAt = reader.GetOrdinal("created_at");
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            list.Add(new AchRecipientRow(
                reader.GetString(ordId),
                reader.GetString(ordName),
                await ReadOptionalStringAsync(reader, ordHolder).ConfigureAwait(false),
                await ReadOptionalStringAsync(reader, ordBank).ConfigureAwait(false),
                Routing: null,
                Account: null,
                await ReadAccountTypeAsync(reader, ordAccountType).ConfigureAwait(false),
                await ReadOptionalStringAsync(reader, ordMemo).ConfigureAwait(false),
                await ReadOptionalStringAsync(reader, ordAccountMask).ConfigureAwait(false),
                await ReadOptionalStringAsync(reader, ordRoutingMask).ConfigureAwait(false),
                DateTimeOffset.Parse(reader.GetString(ordCreatedAt), System.Globalization.CultureInfo.InvariantCulture)));
        }

        return list;
    }

    /// <summary>
    /// Upserts payee metadata and masks only — never binds cleartext account/routing into SQLite.
    /// Use: Medium (bootstrap / picker save). Scope: recipients table row.
    /// </summary>
    public async Task UpsertAsync(AchRecipientRow row)
    {
        await EnsureSchemaAsync().ConfigureAwait(false);
        string? accountMask = row.AccountMask
            ?? (string.IsNullOrWhiteSpace(row.Account) ? null : AchRecipientValidation.MaskAccount(row.Account));
        string? routingMask = row.RoutingMask
            ?? (string.IsNullOrWhiteSpace(row.Routing) ? null : AchRecipientValidation.MaskRouting(row.Routing));

        await using SqliteConnection conn = _db.Open();
        await conn.OpenAsync().ConfigureAwait(false);
        await using SqliteCommand cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO recipients (
              id, name, holder, bank, routing, account, account_type, memo,
              account_mask, routing_mask, created_at)
            VALUES ($id, $name, $holder, $bank, NULL, NULL, $type, $memo, $am, $rm, $created)
            ON CONFLICT(id) DO UPDATE SET
              name=$name, holder=$holder, bank=$bank, routing=NULL, account=NULL,
              account_type=$type, memo=$memo, account_mask=$am, routing_mask=$rm
            """;
        cmd.Parameters.AddWithValue("$id", row.Id);
        cmd.Parameters.AddWithValue("$name", row.Name);
        cmd.Parameters.AddWithValue("$holder", (object?)row.Holder ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$bank", (object?)row.Bank ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$type", row.AccountType);
        cmd.Parameters.AddWithValue("$memo", (object?)row.Memo ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$am", (object?)accountMask ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$rm", (object?)routingMask ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$created", row.CreatedAt.ToString("O", CultureInfo.InvariantCulture));
        await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    public async Task DeleteAsync(string id)
    {
        await EnsureSchemaAsync().ConfigureAwait(false);
        await using SqliteConnection conn = _db.Open();
        await conn.OpenAsync().ConfigureAwait(false);
        await using SqliteCommand cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM recipients WHERE id=$id";
        cmd.Parameters.AddWithValue("$id", id);
        await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    public async Task SeedDefaultsIfEmptyAsync()
    {
        IReadOnlyList<AchRecipientRow> existing = await ListAsync().ConfigureAwait(false);
        if (existing.Count > 0)
        {
            return;
        }

        await UpsertAsync(new AchRecipientRow(
            Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture),
            "Rent — 4th St LLC",
            "4th St LLC",
            "Demo Bank",
            "021000021",
            "88210001",
            DefaultAccountType,
            "Rent",
            null,
            null,
            _timeProvider.GetUtcNow())).ConfigureAwait(false);
        await UpsertAsync(new AchRecipientRow(
            Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture),
            "Utilities Co",
            "Utilities Co",
            "City Credit Union",
            "110000000",
            "44102222",
            DefaultAccountType,
            null,
            null,
            null,
            _timeProvider.GetUtcNow())).ConfigureAwait(false);
    }

#pragma warning disable CA2100 // Constant DDL strings only
    /// <summary>
    /// Adds a column when missing; rethrows non-duplicate failures so schema-ready is not latched.
    /// Use: Low (first open). Scope: recipients table migration.
    /// </summary>
    private static async Task TryAddRecipientColumnAsync(SqliteConnection conn, string ddl)
    {
        try
        {
            await using SqliteCommand alter = conn.CreateCommand();
            alter.CommandText = ddl;
            await alter.ExecuteNonQueryAsync().ConfigureAwait(false);
        }
        catch (SqliteException ex) when (IsDuplicateColumn(ex))
        {
            // Column already exists.
        }
    }

    /// <summary>
    /// Nulls any legacy cleartext account/routing columns left from older builds.
    /// Use: Low (schema ensure). Scope: recipients table scrub.
    /// </summary>
    private static async Task ClearSensitiveRecipientColumnsAsync(SqliteConnection conn)
    {
        foreach (string sql in new[]
                 {
                     "UPDATE recipients SET account = NULL WHERE account IS NOT NULL",
                     "UPDATE recipients SET routing = NULL WHERE routing IS NOT NULL",
                     "UPDATE recipients SET account_full = NULL WHERE account_full IS NOT NULL",
                 })
        {
            try
            {
                await using SqliteCommand cmd = conn.CreateCommand();
                cmd.CommandText = sql;
                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
            catch (SqliteException)
            {
                // Column absent on fresh schemas that never had the legacy column.
            }
        }
    }
#pragma warning restore CA2100

    /// <summary>
    /// SQLite reports duplicate-column ALTERs as SqliteException (message contains "duplicate column").
    /// Use: Low. Scope: schema migration catch filter.
    /// </summary>
    private static bool IsDuplicateColumn(SqliteException ex)
        => ex.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Reads a nullable TEXT column without sync IsDBNull.
    /// Use: High (list paths). Scope: RecipientRepository row hydrate.
    /// </summary>
    private static async Task<string?> ReadOptionalStringAsync(Microsoft.Data.Sqlite.SqliteDataReader reader, int ordinal)
        => await reader.IsDBNullAsync(ordinal).ConfigureAwait(false) ? null : reader.GetString(ordinal);

    /// <summary>
    /// Reads account_type with checking default when null/empty.
    /// Use: High (list paths). Scope: RecipientRepository row hydrate.
    /// </summary>
    private static async Task<string> ReadAccountTypeAsync(Microsoft.Data.Sqlite.SqliteDataReader reader, int ordinal)
    {
        if (await reader.IsDBNullAsync(ordinal).ConfigureAwait(false))
        {
            return DefaultAccountType;
        }

        string value = reader.GetString(ordinal);
        return string.IsNullOrEmpty(value) ? DefaultAccountType : value;
    }
}
