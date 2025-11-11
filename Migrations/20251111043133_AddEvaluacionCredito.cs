using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheBuryProject.Migrations
{
    /// <inheritdoc />
    public partial class AddEvaluacionCredito : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EvaluacionesCredito",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreditoId = table.Column<int>(type: "int", nullable: false),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    Resultado = table.Column<int>(type: "int", nullable: false),
                    PuntajeRiesgoCliente = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MontoSolicitado = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SueldoCliente = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    RelacionCuotaIngreso = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TieneDocumentacionCompleta = table.Column<bool>(type: "bit", nullable: false),
                    TieneIngresosSuficientes = table.Column<bool>(type: "bit", nullable: false),
                    TieneBuenHistorial = table.Column<bool>(type: "bit", nullable: false),
                    TieneGarante = table.Column<bool>(type: "bit", nullable: false),
                    PuntajeFinal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    FechaEvaluacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluacionesCredito", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EvaluacionesCredito_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EvaluacionesCredito_Creditos_CreditoId",
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
                value: new DateTime(2025, 11, 11, 4, 31, 32, 646, DateTimeKind.Utc).AddTicks(5139));

            migrationBuilder.UpdateData(
                table: "Categorias",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 11, 4, 31, 32, 646, DateTimeKind.Utc).AddTicks(5143));

            migrationBuilder.UpdateData(
                table: "Marcas",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 11, 4, 31, 32, 647, DateTimeKind.Utc).AddTicks(8397));

            migrationBuilder.UpdateData(
                table: "Marcas",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 11, 4, 31, 32, 647, DateTimeKind.Utc).AddTicks(8402));

            migrationBuilder.UpdateData(
                table: "Marcas",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 11, 4, 31, 32, 647, DateTimeKind.Utc).AddTicks(8405));

            migrationBuilder.CreateIndex(
                name: "IX_EvaluacionesCredito_ClienteId",
                table: "EvaluacionesCredito",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluacionesCredito_CreditoId",
                table: "EvaluacionesCredito",
                column: "CreditoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EvaluacionesCredito");

            migrationBuilder.UpdateData(
                table: "Categorias",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 10, 23, 2, 38, 214, DateTimeKind.Utc).AddTicks(4522));

            migrationBuilder.UpdateData(
                table: "Categorias",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 10, 23, 2, 38, 214, DateTimeKind.Utc).AddTicks(4526));

            migrationBuilder.UpdateData(
                table: "Marcas",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 10, 23, 2, 38, 215, DateTimeKind.Utc).AddTicks(5732));

            migrationBuilder.UpdateData(
                table: "Marcas",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 10, 23, 2, 38, 215, DateTimeKind.Utc).AddTicks(5736));

            migrationBuilder.UpdateData(
                table: "Marcas",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 10, 23, 2, 38, 215, DateTimeKind.Utc).AddTicks(5739));
        }
    }
}
