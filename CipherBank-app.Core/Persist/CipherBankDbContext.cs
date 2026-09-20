// <copyright file="CipherBankDbContext.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Globalization;
using CipherBank_app.Persist.Entities;
using Microsoft.EntityFrameworkCore;

namespace CipherBank_app.Persist;

/// <summary>EF Core model for the on-device, non-secret CipherBank database.</summary>
public sealed class CipherBankDbContext : DbContext
{
    public CipherBankDbContext(DbContextOptions<CipherBankDbContext> options)
        : base(options)
    {
    }

    internal DbSet<WalletEntity> Wallets => Set<WalletEntity>();

    internal DbSet<RecipientEntity> Recipients => Set<RecipientEntity>();

    internal DbSet<PreferenceEntity> Preferences => Set<PreferenceEntity>();

    internal DbSet<OhlcPointEntity> OhlcPoints => Set<OhlcPointEntity>();

    internal DbSet<RateSnapshotEntity> RateSnapshots => Set<RateSnapshotEntity>();

    internal DbSet<SyncMetadataEntity> SyncMetadata => Set<SyncMetadataEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<WalletEntity>(entity =>
        {
            entity.ToTable("wallets");
            entity.HasKey(value => value.Id);
            entity.Property(value => value.Id).HasColumnName("id");
            entity.Property(value => value.Symbol).HasColumnName("symbol").IsRequired();
            entity.Property(value => value.Label).HasColumnName("label");
            entity.Property(value => value.Address).HasColumnName("address");
            entity.Property(value => value.Path).HasColumnName("path");
            entity.Property(value => value.AccountIndex).HasColumnName("account_index");
            entity.Property(value => value.Kind).HasColumnName("kind").IsRequired();

            // SQLite has no datetime affinity; store ISO-8601 round-trip (O) and parse invariant.
            entity.Property(value => value.CreatedAt)
                .HasColumnName("created_at")
                .HasConversion(
                    value => value.ToString("O", CultureInfo.InvariantCulture),
                    value => DateTimeOffset.Parse(value, CultureInfo.InvariantCulture));
        });

        modelBuilder.Entity<RecipientEntity>(entity =>
        {
            entity.ToTable("recipients");
            entity.HasKey(value => value.Id);
            entity.Property(value => value.Id).HasColumnName("id");
            entity.Property(value => value.Name).HasColumnName("name").IsRequired();
            entity.Property(value => value.Holder).HasColumnName("holder");
            entity.Property(value => value.Bank).HasColumnName("bank");
            entity.Property(value => value.AccountType).HasColumnName("account_type").IsRequired();
            entity.Property(value => value.Memo).HasColumnName("memo");
            entity.Property(value => value.AccountMask).HasColumnName("account_mask");
            entity.Property(value => value.RoutingMask).HasColumnName("routing_mask");

            // SQLite has no datetime affinity; store ISO-8601 round-trip (O) and parse invariant.
            entity.Property(value => value.CreatedAt)
                .HasColumnName("created_at")
                .HasConversion(
                    value => value.ToString("O", CultureInfo.InvariantCulture),
                    value => DateTimeOffset.Parse(value, CultureInfo.InvariantCulture));
            entity.HasIndex(value => value.Name);
        });

        modelBuilder.Entity<PreferenceEntity>(entity =>
        {
            entity.ToTable("prefs");
            entity.HasKey(value => value.Key);
            entity.Property(value => value.Key).HasColumnName("key");
            entity.Property(value => value.Value).HasColumnName("value").IsRequired();
        });

        modelBuilder.Entity<OhlcPointEntity>(entity =>
        {
            entity.ToTable("ohlc");
            entity.HasKey(value => new { value.Symbol, value.Timestamp });
            entity.Property(value => value.Symbol).HasColumnName("symbol");
            entity.Property(value => value.Timestamp).HasColumnName("t");
            entity.Property(value => value.Value).HasColumnName("v");
        });

        modelBuilder.Entity<RateSnapshotEntity>(entity =>
        {
            entity.ToTable("rates_snapshot");
            entity.HasKey(value => value.Symbol);
            entity.Property(value => value.Symbol).HasColumnName("symbol");
            entity.Property(value => value.Usd).HasColumnName("usd");
            entity.Property(value => value.Change24H).HasColumnName("change24h");
            entity.Property(value => value.UpdatedAtMs).HasColumnName("updated_at");
        });

        modelBuilder.Entity<SyncMetadataEntity>(entity =>
        {
            entity.ToTable("sync_meta");
            entity.HasKey(value => value.Key);
            entity.Property(value => value.Key).HasColumnName("key");
            entity.Property(value => value.Value).HasColumnName("value").IsRequired();
            entity.Property(value => value.UpdatedAtMs).HasColumnName("updated_at");
        });
    }
}
