// <copyright file="RecipientRepository.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Persist;

/// <summary>ACH / payee recipient stored on device.</summary>
public sealed record AchRecipientRow(
    string Id,
    string Name,
    string? AccountMask,
    string? RoutingMask,
    string? AccountFull,
    DateTimeOffset CreatedAt);

/// <summary>SQLite ACH recipients repo (Cora recipientsRepo).</summary>
public interface IRecipientRepository
{
    Task<IReadOnlyList<AchRecipientRow>> ListAsync();

    Task UpsertAsync(AchRecipientRow row);

    Task SeedDefaultsIfEmptyAsync();
}

/// <inheritdoc />
public sealed class RecipientRepository : IRecipientRepository
{
    private readonly ILocalDb _db;

    public RecipientRepository(ILocalDb db) => _db = db;

    public async Task<IReadOnlyList<AchRecipientRow>> ListAsync()
    {
        await using var conn = _db.Open();
        await conn.OpenAsync().ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, name, account_mask, routing_mask, account_full, created_at FROM recipients ORDER BY name";
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
                DateTimeOffset.Parse(reader.GetString(5), System.Globalization.CultureInfo.InvariantCulture)));
        }

        return list;
    }

    public async Task UpsertAsync(AchRecipientRow row)
    {
        await using var conn = _db.Open();
        await conn.OpenAsync().ConfigureAwait(false);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO recipients (id, name, account_mask, routing_mask, account_full, created_at)
            VALUES ($id, $name, $am, $rm, $af, $created)
            ON CONFLICT(id) DO UPDATE SET
              name=$name, account_mask=$am, routing_mask=$rm, account_full=$af
            """;
        cmd.Parameters.AddWithValue("$id", row.Id);
        cmd.Parameters.AddWithValue("$name", row.Name);
        cmd.Parameters.AddWithValue("$am", (object?)row.AccountMask ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$rm", (object?)row.RoutingMask ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$af", (object?)row.AccountFull ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$created", row.CreatedAt.ToString("O"));
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
            "•••• 8821",
            "•••• 0210",
            null,
            DateTimeOffset.UtcNow)).ConfigureAwait(false);
        await UpsertAsync(new AchRecipientRow(
            Guid.NewGuid().ToString("N"),
            "Utilities Co",
            "•••• 4410",
            "•••• 1100",
            null,
            DateTimeOffset.UtcNow)).ConfigureAwait(false);
    }
}
