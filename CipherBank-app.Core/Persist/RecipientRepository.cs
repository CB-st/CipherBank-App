// <copyright file="RecipientRepository.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Persist;

/// <summary>ACH / payee recipient stored on device.</summary>
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
        _schemaReady = true;
    }

#pragma warning disable CA2100 // Constant DDL strings only
    private static async Task TryAddRecipientColumnAsync(Microsoft.Data.Sqlite.SqliteConnection conn, string ddl)
    {
        try
        {
            await using var alter = conn.CreateCommand();
            alter.CommandText = ddl;
            await alter.ExecuteNonQueryAsync().ConfigureAwait(false);
        }
        catch
        {
            // Column already exists.
        }
    }
#pragma warning restore CA2100

    public async Task<IReadOnlyList<AchRecipientRow>> ListAsync()
    {
        await EnsureSchemaAsync().ConfigureAwait(false);
        await using var conn = _db.Open();
        await conn.OpenAsync().ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT id, name, holder, bank, routing, account, account_type, memo,
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
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetString(5),
                reader.IsDBNull(6) || string.IsNullOrEmpty(reader.GetString(6)) ? "checking" : reader.GetString(6),
                reader.IsDBNull(7) ? null : reader.GetString(7),
                reader.IsDBNull(8) ? null : reader.GetString(8),
                reader.IsDBNull(9) ? null : reader.GetString(9),
                DateTimeOffset.Parse(reader.GetString(10), System.Globalization.CultureInfo.InvariantCulture)));
        }

        return list;
    }

    public async Task UpsertAsync(AchRecipientRow row)
    {
        await EnsureSchemaAsync().ConfigureAwait(false);
        await using var conn = _db.Open();
        await conn.OpenAsync().ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO recipients (
              id, name, holder, bank, routing, account, account_type, memo,
              account_mask, routing_mask, created_at)
            VALUES ($id, $name, $holder, $bank, $routing, $account, $type, $memo, $am, $rm, $created)
            ON CONFLICT(id) DO UPDATE SET
              name=$name, holder=$holder, bank=$bank, routing=$routing, account=$account,
              account_type=$type, memo=$memo, account_mask=$am, routing_mask=$rm
            """;
        cmd.Parameters.AddWithValue("$id", row.Id);
        cmd.Parameters.AddWithValue("$name", row.Name);
        cmd.Parameters.AddWithValue("$holder", (object?)row.Holder ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$bank", (object?)row.Bank ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$routing", (object?)row.Routing ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$account", (object?)row.Account ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$type", row.AccountType);
        cmd.Parameters.AddWithValue("$memo", (object?)row.Memo ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$am", (object?)row.AccountMask ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$rm", (object?)row.RoutingMask ?? DBNull.Value);
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
            "•••• 0001",
            "•••• 0021",
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
            "•••• 2222",
            "•••• 0000",
            DateTimeOffset.UtcNow)).ConfigureAwait(false);
    }
}
