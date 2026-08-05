using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockManufactura.Infrastructure.Migrations
{
    public partial class AddRecursoProveedores : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RecursoProveedores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RecursoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProveedorId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Precio = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    EsPrioritario = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    Observaciones = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false, defaultValue: ""),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecursoProveedores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecursoProveedores_Recursos_RecursoId",
                        column: x => x.RecursoId,
                        principalTable: "Recursos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecursoProveedores_Proveedores_ProveedorId",
                        column: x => x.ProveedorId,
                        principalTable: "Proveedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecursoProveedores_RecursoId_ProveedorId",
                table: "RecursoProveedores",
                columns: new[] { "RecursoId", "ProveedorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecursoProveedores_ProveedorId",
                table: "RecursoProveedores",
                column: "ProveedorId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "RecursoProveedores");
        }
    }
}
