using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockManufactura.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStockMinimoToProducto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "StockMinimo",
                table: "Productos",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StockMinimo",
                table: "Productos");
        }
    }
}
