using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockManufactura.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCodigoProveedorToRecursoProveedor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RecursoProveedores_ProveedorId",
                table: "RecursoProveedores");

            migrationBuilder.AddColumn<string>(
                name: "CodigoProveedor",
                table: "RecursoProveedores",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_RecursoProveedores_ProveedorId_CodigoProveedor",
                table: "RecursoProveedores",
                columns: new[] { "ProveedorId", "CodigoProveedor" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RecursoProveedores_ProveedorId_CodigoProveedor",
                table: "RecursoProveedores");

            migrationBuilder.DropColumn(
                name: "CodigoProveedor",
                table: "RecursoProveedores");

            migrationBuilder.CreateIndex(
                name: "IX_RecursoProveedores_ProveedorId",
                table: "RecursoProveedores",
                column: "ProveedorId");
        }
    }
}
