using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockManufactura.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddComponenteProductoToRecetaItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "RecursoId",
                table: "RecetaProductoItems",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AddColumn<Guid>(
                name: "ComponenteProductoId",
                table: "RecetaProductoItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecetaProductoItems_ComponenteProductoId",
                table: "RecetaProductoItems",
                column: "ComponenteProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_RecetaProductoItems_ProductoId_ComponenteProductoId",
                table: "RecetaProductoItems",
                columns: new[] { "ProductoId", "ComponenteProductoId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_RecetaProductoItems_Productos_ComponenteProductoId",
                table: "RecetaProductoItems",
                column: "ComponenteProductoId",
                principalTable: "Productos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RecetaProductoItems_Productos_ComponenteProductoId",
                table: "RecetaProductoItems");

            migrationBuilder.DropIndex(
                name: "IX_RecetaProductoItems_ComponenteProductoId",
                table: "RecetaProductoItems");

            migrationBuilder.DropIndex(
                name: "IX_RecetaProductoItems_ProductoId_ComponenteProductoId",
                table: "RecetaProductoItems");

            migrationBuilder.DropColumn(
                name: "ComponenteProductoId",
                table: "RecetaProductoItems");

            migrationBuilder.AlterColumn<Guid>(
                name: "RecursoId",
                table: "RecetaProductoItems",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);
        }
    }
}
