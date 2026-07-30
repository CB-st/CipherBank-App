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
        await TryAddHolderColumnAsync(conn).ConfigureAwait(false);
        await TryAddBankColumnAsync(conn).ConfigureAwait(false);
        await TryAddRoutingColumnAsync(conn).ConfigureAwait(false);
        await TryAddAccountColumnAsync(conn).ConfigureAwait(false);
        await TryAddAccountTypeColumnAsync(conn).ConfigureAwait(false);
        await TryAddMemoColumnAsync(conn).ConfigureAwait(false);
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
        var ordId = reader.GetOrdinal("id");
        var ordName = reader.GetOrdinal("name");
        var ordHolder = reader.GetOrdinal("holder");
        var ordBank = reader.GetOrdinal("bank");
        var ordAccountType = reader.GetOrdinal("account_type");
        var ordMemo = reader.GetOrdinal("memo");
        var ordAccountMask = reader.GetOrdinal("account_mask");
        var ordRoutingMask = reader.GetOrdinal("routing_mask");
        var ordCreatedAt = reader.GetOrdinal("created_at");
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
        var accountMask = row.AccountMask
            ?? (string.IsNullOrWhiteSpace(row.Account) ? null : AchRecipientValidation.MaskAccount(row.Account));
        var routingMask = row.RoutingMask
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

    /// <summary>
    /// Adds holder column when missing.
    /// Use: Low (first open). Scope: recipients schema.
    /// </summary>
    private static async Task TryAddHolderColumnAsync(SqliteConnection conn)
    {
        await using SqliteCommand alter = conn.CreateCommand();
        alter.CommandText = "ALTER TABLE recipients ADD COLUMN holder TEXT";
        await TryExecuteSchemaAsync(alter).ConfigureAwait(false);
    }

    /// <summary>
    /// Adds bank column when missing.
    /// Use: Low (first open). Scope: recipients schema.
    /// </summary>
    private static async Task TryAddBankColumnAsync(SqliteConnection conn)
    {
        await using SqliteCommand alter = conn.CreateCommand();
        alter.CommandText = "ALTER TABLE recipients ADD COLUMN bank TEXT";
        await TryExecuteSchemaAsync(alter).ConfigureAwait(false);
    }

    /// <summary>
    /// Adds routing column when missing.
    /// Use: Low (first open). Scope: recipients schema.
    /// </summary>
    private static async Task TryAddRoutingColumnAsync(SqliteConnection conn)
    {
        await using SqliteCommand alter = conn.CreateCommand();
        alter.CommandText = "ALTER TABLE recipients ADD COLUMN routing TEXT";
        await TryExecuteSchemaAsync(alter).ConfigureAwait(false);
    }

    /// <summary>
    /// Adds account column when missing.
    /// Use: Low (first open). Scope: recipients schema.
    /// </summary>
    private static async Task TryAddAccountColumnAsync(SqliteConnection conn)
    {
        await using SqliteCommand alter = conn.CreateCommand();
        alter.CommandText = "ALTER TABLE recipients ADD COLUMN account TEXT";
        await TryExecuteSchemaAsync(alter).ConfigureAwait(false);
    }

    /// <summary>
    /// Adds account_type column when missing.
    /// Use: Low (first open). Scope: recipients schema.
    /// </summary>
    private static async Task TryAddAccountTypeColumnAsync(SqliteConnection conn)
    {
        await using SqliteCommand alter = conn.CreateCommand();
        alter.CommandText = "ALTER TABLE recipients ADD COLUMN account_type TEXT";
        await TryExecuteSchemaAsync(alter).ConfigureAwait(false);
    }

    /// <summary>
    /// Adds memo column when missing.
    /// Use: Low (first open). Scope: recipients schema.
    /// </summary>
    private static async Task TryAddMemoColumnAsync(SqliteConnection conn)
    {
        await using SqliteCommand alter = conn.CreateCommand();
        alter.CommandText = "ALTER TABLE recipients ADD COLUMN memo TEXT";
        await TryExecuteSchemaAsync(alter).ConfigureAwait(false);
    }

    /// <summary>
    /// Runs schema DDL, ignoring duplicate-column failures.
    /// Use: Low (migration). Scope: recipients schema helpers.
    /// </summary>
    private static async Task TryExecuteSchemaAsync(SqliteCommand cmd)
    {
        try
        {
            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
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
        await TryClearAccountAsync(conn).ConfigureAwait(false);
        await TryClearRoutingAsync(conn).ConfigureAwait(false);
        await TryClearAccountFullAsync(conn).ConfigureAwait(false);
    }

    /// <summary>
    /// Clears legacy account cleartext when the column exists.
    /// Use: Low. Scope: recipients scrub.
    /// </summary>
    private static async Task TryClearAccountAsync(SqliteConnection conn)
    {
        await using SqliteCommand cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE recipients SET account = NULL WHERE account IS NOT NULL";
        await TryExecuteOptionalColumnAsync(cmd).ConfigureAwait(false);
    }

    /// <summary>
    /// Clears legacy routing cleartext when the column exists.
    /// Use: Low. Scope: recipients scrub.
    /// </summary>
    private static async Task TryClearRoutingAsync(SqliteConnection conn)
    {
        await using SqliteCommand cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE recipients SET routing = NULL WHERE routing IS NOT NULL";
        await TryExecuteOptionalColumnAsync(cmd).ConfigureAwait(false);
    }

    /// <summary>
    /// Clears legacy account_full cleartext when the column exists.
    /// Use: Low. Scope: recipients scrub.
    /// </summary>
    private static async Task TryClearAccountFullAsync(SqliteConnection conn)
    {
        await using SqliteCommand cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE recipients SET account_full = NULL WHERE account_full IS NOT NULL";
        await TryExecuteOptionalColumnAsync(cmd).ConfigureAwait(false);
    }

    /// <summary>
    /// Runs scrub SQL, ignoring missing-column failures on fresh schemas.
    /// Use: Low. Scope: recipients scrub helpers.
    /// </summary>
    private static async Task TryExecuteOptionalColumnAsync(SqliteCommand cmd)
    {
        try
        {
            await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
        }
        catch (SqliteException)
        {
            // Column absent on fresh schemas that never had the legacy column.
        }
    }

    /// <summary>
    /// SQLite reports duplicate-column ALTERs as SqliteException (message contains "duplicate column").
    /// Use: Low. Scope: schema migration catch filter.
    /// </summary>
    private static bool IsDuplicateColumn(Exception ex)
        => ex.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Reads a nullable TEXT column without sync IsDBNull.
    /// Use: High (list paths). Scope: RecipientRepository row hydrate.
    /// </summary>
    private static async Task<string?> ReadOptionalStringAsync(System.Data.Common.DbDataReader reader, int ordinal)
        => await reader.IsDBNullAsync(ordinal).ConfigureAwait(false) ? null : reader.GetString(ordinal);

    /// <summary>
    /// Reads account_type with checking default when null/empty.
    /// Use: High (list paths). Scope: RecipientRepository row hydrate.
    /// </summary>
    private static async Task<string> ReadAccountTypeAsync(System.Data.Common.DbDataReader reader, int ordinal)
    {
        if (await reader.IsDBNullAsync(ordinal).ConfigureAwait(false))
        {
            return DefaultAccountType;
        }

        var value = reader.GetString(ordinal);
        return string.IsNullOrEmpty(value) ? DefaultAccountType : value;
    }
}
