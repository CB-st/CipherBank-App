// <copyright file="RecipientRepository.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using Microsoft.Data.Sqlite;

namespace CipherBank_app.Persist;

/// <summary>ACH / payee recipient stored on device.</summary>
/// <remarks>
/// Full account/routing digits are accepted on upsert only to compute masks; SQLite (the public
/// environment) persists masks and metadata — never cleartext PAN/routing.
/// </remarks>
public sealed record AchRecipientRow(
    string Id,
    string Name,
    string? Holder,
    string? Bank,
    string? Routing,
    string? Account,
    string AccountType,
    string? Memo,
    string? AccountMask,
    string? RoutingMask,
    DateTimeOffset CreatedAt);

/// <summary>SQLite ACH recipients repo (Cora recipientsRepo).</summary>
public interface IRecipientRepository
{
    Task EnsureSchemaAsync();

    Task<IReadOnlyList<AchRecipientRow>> ListAsync();

    Task UpsertAsync(AchRecipientRow row);

    Task DeleteAsync(string id);

    Task SeedDefaultsIfEmptyAsync();
}

/// <inheritdoc />
public sealed class RecipientRepository : IRecipientRepository
{
    private readonly ILocalDb _db;
    private bool _schemaReady;

    public RecipientRepository(ILocalDb db) => _db = db;

    public async Task EnsureSchemaAsync()
    {
        if (_schemaReady)
        {
            return;
        }

        await using var conn = _db.Open();
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

#pragma warning disable CA2100 // Constant DDL strings only
    /// <summary>
    /// Adds a column when missing; rethrows non-duplicate failures so schema-ready is not latched.
    /// Use: Low (first open). Scope: recipients table migration.
    /// </summary>
    private static async Task TryAddRecipientColumnAsync(SqliteConnection conn, string ddl)
    {
        try
        {
            await using var alter = conn.CreateCommand();
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
                await using var cmd = conn.CreateCommand();
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

    public async Task<IReadOnlyList<AchRecipientRow>> ListAsync()
    {
        await EnsureSchemaAsync().ConfigureAwait(false);
        await using var conn = _db.Open();
        await conn.OpenAsync().ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT id, name, holder, bank, account_type, memo,
                   account_mask, routing_mask, created_at
            FROM recipients ORDER BY name
            """;
        var list = new List<AchRecipientRow>();
        await using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
        while (await reader.ReadAsync().ConfigureAwait(false))
        {
            list.Add(new AchRecipientRow(
                reader.GetString(0),
                reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                Routing: null,
                Account: null,
                reader.IsDBNull(4) || string.IsNullOrEmpty(reader.GetString(4)) ? "checking" : reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetString(5),
                reader.IsDBNull(6) ? null : reader.GetString(6),
                reader.IsDBNull(7) ? null : reader.GetString(7),
                DateTimeOffset.Parse(reader.GetString(8), System.Globalization.CultureInfo.InvariantCulture)));
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

        await using var conn = _db.Open();
        await conn.OpenAsync().ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
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
        cmd.Parameters.AddWithValue("$created", row.CreatedAt.ToString("O"));
        await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    public async Task DeleteAsync(string id)
    {
        await EnsureSchemaAsync().ConfigureAwait(false);
        await using var conn = _db.Open();
        await conn.OpenAsync().ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM recipients WHERE id=$id";
        cmd.Parameters.AddWithValue("$id", id);
        await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    public async Task SeedDefaultsIfEmptyAsync()
    {
        var existing = await ListAsync().ConfigureAwait(false);
        if (existing.Count > 0)
        {
            return;
        }

        await UpsertAsync(new AchRecipientRow(
            Guid.NewGuid().ToString("N"),
            "Rent — 4th St LLC",
            "4th St LLC",
            "Demo Bank",
            "021000021",
            "88210001",
            "checking",
            "Rent",
            null,
            null,
            DateTimeOffset.UtcNow)).ConfigureAwait(false);
        await UpsertAsync(new AchRecipientRow(
            Guid.NewGuid().ToString("N"),
            "Utilities Co",
            "Utilities Co",
            "City Credit Union",
            "110000000",
            "44102222",
            "checking",
            null,
            null,
            null,
            DateTimeOffset.UtcNow)).ConfigureAwait(false);
    }
}
