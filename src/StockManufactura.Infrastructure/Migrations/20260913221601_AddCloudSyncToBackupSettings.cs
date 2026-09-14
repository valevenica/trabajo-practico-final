using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockManufactura.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCloudSyncToBackupSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CarpetaNube",
                table: "BackupSettings",
                type: "TEXT",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProveedorNube",
                table: "BackupSettings",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "Google Drive");

            migrationBuilder.AddColumn<bool>(
                name: "SincronizacionBidireccional",
                table: "BackupSettings",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UltimaSincronizacionNube",
                table: "BackupSettings",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CarpetaNube",
                table: "BackupSettings");

            migrationBuilder.DropColumn(
                name: "ProveedorNube",
                table: "BackupSettings");

            migrationBuilder.DropColumn(
                name: "SincronizacionBidireccional",
                table: "BackupSettings");

            migrationBuilder.DropColumn(
                name: "UltimaSincronizacionNube",
                table: "BackupSettings");
        }
    }
}
