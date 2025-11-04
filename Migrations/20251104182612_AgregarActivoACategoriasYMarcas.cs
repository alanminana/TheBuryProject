using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheBuryProject.Migrations
{
    /// <inheritdoc />
    public partial class AgregarActivoACategoriasYMarcas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "Marcas",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "Categorias",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Categorias",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Activo", "CreatedAt" },
                values: new object[] { true, new DateTime(2025, 11, 4, 18, 26, 11, 863, DateTimeKind.Utc).AddTicks(3828) });

            migrationBuilder.UpdateData(
                table: "Categorias",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Activo", "CreatedAt" },
                values: new object[] { true, new DateTime(2025, 11, 4, 18, 26, 11, 863, DateTimeKind.Utc).AddTicks(3831) });

            migrationBuilder.UpdateData(
                table: "Marcas",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Activo", "CreatedAt" },
                values: new object[] { true, new DateTime(2025, 11, 4, 18, 26, 11, 863, DateTimeKind.Utc).AddTicks(4038) });

            migrationBuilder.UpdateData(
                table: "Marcas",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Activo", "CreatedAt" },
                values: new object[] { true, new DateTime(2025, 11, 4, 18, 26, 11, 863, DateTimeKind.Utc).AddTicks(4041) });

            migrationBuilder.UpdateData(
                table: "Marcas",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Activo", "CreatedAt" },
                values: new object[] { true, new DateTime(2025, 11, 4, 18, 26, 11, 863, DateTimeKind.Utc).AddTicks(4120) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Activo",
                table: "Marcas");

            migrationBuilder.DropColumn(
                name: "Activo",
                table: "Categorias");

            migrationBuilder.UpdateData(
                table: "Categorias",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 1, 4, 55, 7, 399, DateTimeKind.Utc).AddTicks(707));

            migrationBuilder.UpdateData(
                table: "Categorias",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 1, 4, 55, 7, 399, DateTimeKind.Utc).AddTicks(711));

            migrationBuilder.UpdateData(
                table: "Marcas",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 1, 4, 55, 7, 399, DateTimeKind.Utc).AddTicks(1009));

            migrationBuilder.UpdateData(
                table: "Marcas",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 1, 4, 55, 7, 399, DateTimeKind.Utc).AddTicks(1012));

            migrationBuilder.UpdateData(
                table: "Marcas",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 1, 4, 55, 7, 399, DateTimeKind.Utc).AddTicks(1015));
        }
    }
}
