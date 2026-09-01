// <copyright file="20260817134948_InitialCreate.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CipherBank_app.Persist.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        CreateOhlcTable(migrationBuilder);
        CreatePrefsTable(migrationBuilder);
        CreateRatesSnapshotTable(migrationBuilder);
        CreateRecipientsTable(migrationBuilder);
        CreateSyncMetaTable(migrationBuilder);
        CreateWalletsTable(migrationBuilder);
        CreateRecipientsNameIndex(migrationBuilder);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ohlc");
        migrationBuilder.DropTable(name: "prefs");
        migrationBuilder.DropTable(name: "rates_snapshot");
        migrationBuilder.DropTable(name: "recipients");
        migrationBuilder.DropTable(name: "sync_meta");
        migrationBuilder.DropTable(name: "wallets");
    }

    private static void CreateOhlcTable(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ohlc",
            columns: table => new
            {
                symbol = table.Column<string>(type: "TEXT"),
                t = table.Column<long>(type: "INTEGER"),
                v = table.Column<double>(type: "REAL"),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ohlc", x => new { x.symbol, x.t });
            });
    }

    private static void CreatePrefsTable(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "prefs",
            columns: table => new
            {
                key = table.Column<string>(type: "TEXT"),
                value = table.Column<string>(type: "TEXT"),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_prefs", x => x.key);
            });
    }

    private static void CreateRatesSnapshotTable(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "rates_snapshot",
            columns: table => new
            {
                symbol = table.Column<string>(type: "TEXT"),
                usd = table.Column<decimal>(type: "TEXT"),
                change24h = table.Column<decimal>(type: "TEXT"),
                updated_at = table.Column<long>(type: "INTEGER"),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_rates_snapshot", x => x.symbol);
            });
    }

    private static void CreateRecipientsTable(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "recipients",
            columns: table => new
            {
                id = table.Column<string>(type: "TEXT"),
                name = table.Column<string>(type: "TEXT"),
                holder = table.Column<string>(type: "TEXT", nullable: true),
                bank = table.Column<string>(type: "TEXT", nullable: true),
                account_type = table.Column<string>(type: "TEXT"),
                memo = table.Column<string>(type: "TEXT", nullable: true),
                account_mask = table.Column<string>(type: "TEXT", nullable: true),
                routing_mask = table.Column<string>(type: "TEXT", nullable: true),
                created_at = table.Column<string>(type: "TEXT"),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_recipients", x => x.id);
            });
    }

    private static void CreateSyncMetaTable(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "sync_meta",
            columns: table => new
            {
                key = table.Column<string>(type: "TEXT"),
                value = table.Column<string>(type: "TEXT"),
                updated_at = table.Column<long>(type: "INTEGER"),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_sync_meta", x => x.key);
            });
    }

    private static void CreateWalletsTable(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "wallets",
            columns: table => new
            {
                id = table.Column<string>(type: "TEXT"),
                symbol = table.Column<string>(type: "TEXT"),
                label = table.Column<string>(type: "TEXT", nullable: true),
                address = table.Column<string>(type: "TEXT", nullable: true),
                path = table.Column<string>(type: "TEXT", nullable: true),
                account_index = table.Column<int>(type: "INTEGER"),
                kind = table.Column<string>(type: "TEXT"),
                created_at = table.Column<string>(type: "TEXT"),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_wallets", x => x.id);
            });
    }

    private static void CreateRecipientsNameIndex(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_recipients_name",
            table: "recipients",
            column: "name");
    }
}
