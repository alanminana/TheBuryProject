using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheBuryProject.Migrations
{
    /// <inheritdoc />
    public partial class AddSistemaNotificaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Notificaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioDestino = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Prioridad = table.Column<int>(type: "int", nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Mensaje = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IconoCss = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Leida = table.Column<bool>(type: "bit", nullable: false),
                    FechaLeida = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaNotificacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EntidadOrigen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EntidadOrigenId = table.Column<int>(type: "int", nullable: true),
                    DatosAdicionales = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notificaciones", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "Categorias",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 12, 14, 40, 48, 986, DateTimeKind.Utc).AddTicks(4876));

            migrationBuilder.UpdateData(
                table: "Categorias",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 12, 14, 40, 48, 986, DateTimeKind.Utc).AddTicks(4879));

            migrationBuilder.UpdateData(
                table: "Marcas",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 12, 14, 40, 48, 998, DateTimeKind.Utc).AddTicks(6425));

            migrationBuilder.UpdateData(
                table: "Marcas",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 12, 14, 40, 48, 998, DateTimeKind.Utc).AddTicks(6428));

            migrationBuilder.UpdateData(
                table: "Marcas",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 12, 14, 40, 48, 998, DateTimeKind.Utc).AddTicks(6431));

            migrationBuilder.CreateIndex(
                name: "IX_Notificaciones_FechaNotificacion",
                table: "Notificaciones",
                column: "FechaNotificacion");

            migrationBuilder.CreateIndex(
                name: "IX_Notificaciones_Leida",
                table: "Notificaciones",
                column: "Leida");

            migrationBuilder.CreateIndex(
                name: "IX_Notificaciones_Prioridad",
                table: "Notificaciones",
                column: "Prioridad");

            migrationBuilder.CreateIndex(
                name: "IX_Notificaciones_Tipo",
                table: "Notificaciones",
                column: "Tipo");

            migrationBuilder.CreateIndex(
                name: "IX_Notificaciones_UsuarioDestino",
                table: "Notificaciones",
                column: "UsuarioDestino");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notificaciones");

            migrationBuilder.UpdateData(
                table: "Categorias",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 12, 13, 39, 11, 131, DateTimeKind.Utc).AddTicks(9609));

            migrationBuilder.UpdateData(
                table: "Categorias",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 12, 13, 39, 11, 131, DateTimeKind.Utc).AddTicks(9616));

            migrationBuilder.UpdateData(
                table: "Marcas",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 12, 13, 39, 11, 139, DateTimeKind.Utc).AddTicks(7843));

            migrationBuilder.UpdateData(
                table: "Marcas",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 12, 13, 39, 11, 139, DateTimeKind.Utc).AddTicks(7847));

            migrationBuilder.UpdateData(
                table: "Marcas",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 12, 13, 39, 11, 139, DateTimeKind.Utc).AddTicks(7850));
        }
    }
}
