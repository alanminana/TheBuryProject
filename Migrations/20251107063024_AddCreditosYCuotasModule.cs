using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheBuryProject.Migrations
{
    /// <inheritdoc />
    public partial class AddCreditosYCuotasModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AprobadoPor",
                table: "Creditos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CFTEA",
                table: "Creditos",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaPrimeraCuota",
                table: "Creditos",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GaranteId",
                table: "Creditos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiereGarante",
                table: "Creditos",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "SaldoPendiente",
                table: "Creditos",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalAPagar",
                table: "Creditos",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "Cuotas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreditoId = table.Column<int>(type: "int", nullable: false),
                    NumeroCuota = table.Column<int>(type: "int", nullable: false),
                    MontoCapital = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MontoInteres = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MontoTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FechaVencimiento = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaPago = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MontoPagado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MontoPunitorio = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MedioPago = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ComprobantePago = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cuotas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cuotas_Creditos_CreditoId",
                        column: x => x.CreditoId,
                        principalTable: "Creditos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Categorias",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 7, 6, 30, 24, 430, DateTimeKind.Utc).AddTicks(4185));

            migrationBuilder.UpdateData(
                table: "Categorias",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 7, 6, 30, 24, 430, DateTimeKind.Utc).AddTicks(4188));

            migrationBuilder.UpdateData(
                table: "Marcas",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 7, 6, 30, 24, 430, DateTimeKind.Utc).AddTicks(4359));

            migrationBuilder.UpdateData(
                table: "Marcas",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 7, 6, 30, 24, 430, DateTimeKind.Utc).AddTicks(4361));

            migrationBuilder.UpdateData(
                table: "Marcas",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 7, 6, 30, 24, 430, DateTimeKind.Utc).AddTicks(4364));

            migrationBuilder.CreateIndex(
                name: "IX_Creditos_GaranteId",
                table: "Creditos",
                column: "GaranteId");

            migrationBuilder.CreateIndex(
                name: "IX_Cuotas_CreditoId_NumeroCuota",
                table: "Cuotas",
                columns: new[] { "CreditoId", "NumeroCuota" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Creditos_Garantes_GaranteId",
                table: "Creditos",
                column: "GaranteId",
                principalTable: "Garantes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Creditos_Garantes_GaranteId",
                table: "Creditos");

            migrationBuilder.DropTable(
                name: "Cuotas");

            migrationBuilder.DropIndex(
                name: "IX_Creditos_GaranteId",
                table: "Creditos");

            migrationBuilder.DropColumn(
                name: "AprobadoPor",
                table: "Creditos");

            migrationBuilder.DropColumn(
                name: "CFTEA",
                table: "Creditos");

            migrationBuilder.DropColumn(
                name: "FechaPrimeraCuota",
                table: "Creditos");

            migrationBuilder.DropColumn(
                name: "GaranteId",
                table: "Creditos");

            migrationBuilder.DropColumn(
                name: "RequiereGarante",
                table: "Creditos");

            migrationBuilder.DropColumn(
                name: "SaldoPendiente",
                table: "Creditos");

            migrationBuilder.DropColumn(
                name: "TotalAPagar",
                table: "Creditos");

            migrationBuilder.UpdateData(
                table: "Categorias",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 6, 4, 20, 26, 819, DateTimeKind.Utc).AddTicks(4774));

            migrationBuilder.UpdateData(
                table: "Categorias",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 6, 4, 20, 26, 819, DateTimeKind.Utc).AddTicks(4778));

            migrationBuilder.UpdateData(
                table: "Marcas",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 6, 4, 20, 26, 819, DateTimeKind.Utc).AddTicks(4976));

            migrationBuilder.UpdateData(
                table: "Marcas",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 6, 4, 20, 26, 819, DateTimeKind.Utc).AddTicks(5015));

            migrationBuilder.UpdateData(
                table: "Marcas",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 6, 4, 20, 26, 819, DateTimeKind.Utc).AddTicks(5017));
        }
    }
}
