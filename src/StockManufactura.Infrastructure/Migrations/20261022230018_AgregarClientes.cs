using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockManufactura.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarClientes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ClienteId",
                table: "OrdenesProduccion",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Clientes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Documento = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Telefono = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Direccion = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    Ciudad = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Provincia = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Observaciones = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    FechaAlta = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clientes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesProduccion_ClienteId",
                table: "OrdenesProduccion",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_Documento",
                table: "Clientes",
                column: "Documento",
                unique: true,
                filter: "\"Documento\" <> ''");

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_Nombre",
                table: "Clientes",
                column: "Nombre");

            migrationBuilder.AddForeignKey(
                name: "FK_OrdenesProduccion_Clientes_ClienteId",
                table: "OrdenesProduccion",
                column: "ClienteId",
                principalTable: "Clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrdenesProduccion_Clientes_ClienteId",
                table: "OrdenesProduccion");

            migrationBuilder.DropTable(
                name: "Clientes");

            migrationBuilder.DropIndex(
                name: "IX_OrdenesProduccion_ClienteId",
                table: "OrdenesProduccion");

            migrationBuilder.DropColumn(
                name: "ClienteId",
                table: "OrdenesProduccion");
        }
    }
}
