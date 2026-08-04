using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockManufactura.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStockEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MotivoCambio",
                table: "ResourcePriceHistory",
                type: "TEXT",
                maxLength: 250,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "PrecioEquivalentePesos",
                table: "ResourcePriceHistory",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FechaHora = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Usuario = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Modulo = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Accion = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Entidad = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    IdEntidad = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Equipo = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BackupRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FechaHora = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    RutaArchivo = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    TamanoBytes = table.Column<long>(type: "INTEGER", nullable: false),
                    Exitoso = table.Column<bool>(type: "INTEGER", nullable: false),
                    Mensaje = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackupRecords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BackupSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CarpetaLocal = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Automatico = table.Column<bool>(type: "INTEGER", nullable: false),
                    MantenerUltimasCopias = table.Column<int>(type: "INTEGER", nullable: false),
                    UltimoBackupAutomatico = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IntervaloMinutos = table.Column<int>(type: "INTEGER", nullable: false),
                    GoogleDriveHabilitado = table.Column<bool>(type: "INTEGER", nullable: false),
                    GoogleDriveFolderId = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackupSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CostosIndirectos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    ValorMensual = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    FactorAplicacion = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    Observaciones = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostosIndirectos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Productos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Codigo = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 180, nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CostoFabricacionActual = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    MargenActual = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    PrecioSugeridoActual = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    FechaUltimoCalculo = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Observaciones = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Productos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductCostHistory",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Usuario = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    CostoAnterior = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CostoNuevo = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    PrecioSugeridoAnterior = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    PrecioSugeridoNuevo = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    MargenUtilizado = table.Column<decimal>(type: "decimal(18,6)", nullable: false),
                    CotizacionUtilizada = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    MotivoRecalculo = table.Column<string>(type: "TEXT", maxLength: 250, nullable: false),
                    Observaciones = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductCostHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductCostHistory_Productos_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductCostSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Fecha = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CostoTotal = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CotizacionUtilizada = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CostoFinal = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductCostSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductCostSnapshots_Productos_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecetaProductoItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RecursoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Cantidad = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CostoParcialManual = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Observaciones = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecetaProductoItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecetaProductoItems_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecetaProductoItems_Recursos_RecursoId",
                        column: x => x.RecursoId,
                        principalTable: "Recursos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Stocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProductoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UbicacionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CantidadDisponible = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CantidadReservada = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    UltimaActualizacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Stocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Stocks_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProductCostSnapshotItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RecursoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CantidadUtilizada = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    PrecioRecurso = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CotizacionUtilizada = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CostoParcial = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductCostSnapshotItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductCostSnapshotItems_ProductCostSnapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "ProductCostSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductCostSnapshotItems_Recursos_RecursoId",
                        column: x => x.RecursoId,
                        principalTable: "Recursos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_FechaHora",
                table: "AuditLogs",
                column: "FechaHora");

            migrationBuilder.CreateIndex(
                name: "IX_ProductCostHistory_ProductId",
                table: "ProductCostHistory",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductCostSnapshotItems_RecursoId",
                table: "ProductCostSnapshotItems",
                column: "RecursoId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductCostSnapshotItems_SnapshotId",
                table: "ProductCostSnapshotItems",
                column: "SnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductCostSnapshots_ProductId",
                table: "ProductCostSnapshots",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Productos_Codigo",
                table: "Productos",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecetaProductoItems_ProductoId_RecursoId",
                table: "RecetaProductoItems",
                columns: new[] { "ProductoId", "RecursoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecetaProductoItems_RecursoId",
                table: "RecetaProductoItems",
                column: "RecursoId");

            migrationBuilder.CreateIndex(
                name: "IX_Stocks_ProductoId_UbicacionId",
                table: "Stocks",
                columns: new[] { "ProductoId", "UbicacionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "BackupRecords");

            migrationBuilder.DropTable(
                name: "BackupSettings");

            migrationBuilder.DropTable(
                name: "CostosIndirectos");

            migrationBuilder.DropTable(
                name: "ProductCostHistory");

            migrationBuilder.DropTable(
                name: "ProductCostSnapshotItems");

            migrationBuilder.DropTable(
                name: "RecetaProductoItems");

            migrationBuilder.DropTable(
                name: "Stocks");

            migrationBuilder.DropTable(
                name: "ProductCostSnapshots");

            migrationBuilder.DropTable(
                name: "Productos");

            migrationBuilder.DropColumn(
                name: "MotivoCambio",
                table: "ResourcePriceHistory");

            migrationBuilder.DropColumn(
                name: "PrecioEquivalentePesos",
                table: "ResourcePriceHistory");
        }
    }
}
