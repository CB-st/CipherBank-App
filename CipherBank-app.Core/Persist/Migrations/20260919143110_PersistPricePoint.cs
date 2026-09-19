using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CipherBank_app.Persist.Migrations
{
    /// <inheritdoc />
    public partial class PersistPricePoint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "v",
                table: "ohlc",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "REAL");

            migrationBuilder.AddColumn<decimal>(
                name: "volume",
                table: "ohlc",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "volume",
                table: "ohlc");

            migrationBuilder.AlterColumn<double>(
                name: "v",
                table: "ohlc",
                type: "REAL",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "TEXT");
        }
    }
}
