using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TheBuryProject.Migrations
{
    /// <inheritdoc />
    public partial class AddMarcasTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Marcas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ParentId = table.Column<int>(type: "int", nullable: true),
                    PaisOrigen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Marcas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Marcas_Marcas_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Marcas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "Categorias",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 30, 5, 20, 35, 839, DateTimeKind.Utc).AddTicks(8049));

            migrationBuilder.UpdateData(
                table: "Categorias",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 30, 5, 20, 35, 839, DateTimeKind.Utc).AddTicks(8072));

            migrationBuilder.InsertData(
                table: "Marcas",
                columns: new[] { "Id", "Codigo", "CreatedAt", "CreatedBy", "Descripcion", "IsDeleted", "Nombre", "PaisOrigen", "ParentId", "UpdatedAt", "UpdatedBy" },
                values: new object[,]
                {
                    { 1, "SAM", new DateTime(2025, 10, 30, 5, 20, 35, 839, DateTimeKind.Utc).AddTicks(8187), "System", "Electrónica y electrodomésticos", false, "Samsung", "Corea del Sur", null, null, null },
                    { 2, "LG", new DateTime(2025, 10, 30, 5, 20, 35, 839, DateTimeKind.Utc).AddTicks(8190), "System", "Electrónica y electrodomésticos", false, "LG", "Corea del Sur", null, null, null },
                    { 3, "WHI", new DateTime(2025, 10, 30, 5, 20, 35, 839, DateTimeKind.Utc).AddTicks(8191), "System", "Electrodomésticos", false, "Whirlpool", "Estados Unidos", null, null, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Marcas_Codigo",
                table: "Marcas",
                column: "Codigo",
                unique: true,
                filter: "IsDeleted = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Marcas_ParentId",
                table: "Marcas",
                column: "ParentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Marcas");

            migrationBuilder.UpdateData(
                table: "Categorias",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 29, 23, 10, 6, 614, DateTimeKind.Utc).AddTicks(4548));

            migrationBuilder.UpdateData(
                table: "Categorias",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 29, 23, 10, 6, 614, DateTimeKind.Utc).AddTicks(4550));
        }
    }
}
