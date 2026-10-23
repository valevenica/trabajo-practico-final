using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockManufactura.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UsuarioNombreUnico : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Antes de exigir unicidad se resuelven los nombres repetidos (sin distinguir mayúsculas): el usuario más
            // antiguo conserva el nombre y a los demás se les agrega su correo, que ya es único. No se elimina a nadie.
            migrationBuilder.Sql(@"
UPDATE Usuarios
SET Nombre = Nombre || ' (' || Email || ')'
WHERE Id IN (
    SELECT u.Id FROM Usuarios u
    WHERE EXISTS (
        SELECT 1 FROM Usuarios o
        WHERE lower(o.Nombre) = lower(u.Nombre)
          AND (o.CreatedAt < u.CreatedAt OR (o.CreatedAt = u.CreatedAt AND o.Id < u.Id))));");

            migrationBuilder.AlterColumn<string>(
                name: "Nombre",
                table: "Usuarios",
                type: "TEXT",
                maxLength: 150,
                nullable: false,
                collation: "NOCASE",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 150);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_Nombre",
                table: "Usuarios",
                column: "Nombre",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Usuarios_Nombre",
                table: "Usuarios");

            migrationBuilder.AlterColumn<string>(
                name: "Nombre",
                table: "Usuarios",
                type: "TEXT",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 150,
                oldCollation: "NOCASE");
        }
    }
}
