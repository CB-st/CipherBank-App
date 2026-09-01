// <copyright file="20260817134948_InitialCreate.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CipherBank_app.Persist.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ohlc",
                columns: table => new
                {
                    symbol = table.Column<string>(type: "TEXT", nullable: false),
                    t = table.Column<long>(type: "INTEGER", nullable: false),
                    v = table.Column<double>(type: "REAL", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ohlc", x => new { x.symbol, x.t });
                });

            migrationBuilder.CreateTable(
                name: "prefs",
                columns: table => new
                {
                    key = table.Column<string>(type: "TEXT", nullable: false),
                    value = table.Column<string>(type: "TEXT", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_prefs", x => x.key);
                });

            migrationBuilder.CreateTable(
                name: "rates_snapshot",
                columns: table => new
                {
                    symbol = table.Column<string>(type: "TEXT", nullable: false),
                    usd = table.Column<decimal>(type: "TEXT", nullable: false),
                    change24h = table.Column<decimal>(type: "TEXT", nullable: false),
                    updated_at = table.Column<long>(type: "INTEGER", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_rates_snapshot", x => x.symbol);
                });

            migrationBuilder.CreateTable(
                name: "recipients",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    holder = table.Column<string>(type: "TEXT", nullable: true),
                    bank = table.Column<string>(type: "TEXT", nullable: true),
                    account_type = table.Column<string>(type: "TEXT", nullable: false),
                    memo = table.Column<string>(type: "TEXT", nullable: true),
                    account_mask = table.Column<string>(type: "TEXT", nullable: true),
                    routing_mask = table.Column<string>(type: "TEXT", nullable: true),
                    created_at = table.Column<string>(type: "TEXT", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recipients", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sync_meta",
                columns: table => new
                {
                    key = table.Column<string>(type: "TEXT", nullable: false),
                    value = table.Column<string>(type: "TEXT", nullable: false),
                    updated_at = table.Column<long>(type: "INTEGER", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sync_meta", x => x.key);
                });

            migrationBuilder.CreateTable(
                name: "wallets",
                columns: table => new
                {
                    id = table.Column<string>(type: "TEXT", nullable: false),
                    symbol = table.Column<string>(type: "TEXT", nullable: false),
                    label = table.Column<string>(type: "TEXT", nullable: true),
                    address = table.Column<string>(type: "TEXT", nullable: true),
                    path = table.Column<string>(type: "TEXT", nullable: true),
                    account_index = table.Column<int>(type: "INTEGER", nullable: false),
                    kind = table.Column<string>(type: "TEXT", nullable: false),
                    created_at = table.Column<string>(type: "TEXT", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wallets", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_recipients_name",
                table: "recipients",
                column: "name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ohlc");

            migrationBuilder.DropTable(
                name: "prefs");

            migrationBuilder.DropTable(
                name: "rates_snapshot");

            migrationBuilder.DropTable(
                name: "recipients");

            migrationBuilder.DropTable(
                name: "sync_meta");

            migrationBuilder.DropTable(
                name: "wallets");
        }
    }
}
