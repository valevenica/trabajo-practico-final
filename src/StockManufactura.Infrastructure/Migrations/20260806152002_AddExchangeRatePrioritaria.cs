using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockManufactura.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddExchangeRatePrioritaria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "EsPrioritaria",
                table: "ExchangeRates",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            // Mark the most recent rate as prioritaria by default
            migrationBuilder.Sql(
                "UPDATE ExchangeRates SET EsPrioritaria = 1 WHERE Id = (SELECT Id FROM ExchangeRates ORDER BY Fecha DESC LIMIT 1)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EsPrioritaria",
                table: "ExchangeRates");
        }
    }
}
