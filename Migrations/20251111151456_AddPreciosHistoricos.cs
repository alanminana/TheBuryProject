using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheBuryProject.Migrations
{
    /// <inheritdoc />
    public partial class AddPreciosHistoricos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EvaluacionesCredito");

            migrationBuilder.CreateTable(
                name: "PreciosHistoricos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    PrecioCompraAnterior = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PrecioCompraNuevo = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PrecioVentaAnterior = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PrecioVentaNuevo = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MotivoCambio = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PuedeRevertirse = table.Column<bool>(type: "bit", nullable: false),
                    FechaCambio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioModificacion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreciosHistoricos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PreciosHistoricos_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "Categorias",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 11, 15, 14, 55, 618, DateTimeKind.Utc).AddTicks(6734));

            migrationBuilder.UpdateData(
                table: "Categorias",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 11, 15, 14, 55, 618, DateTimeKind.Utc).AddTicks(6737));

            migrationBuilder.UpdateData(
                table: "Marcas",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 11, 15, 14, 55, 619, DateTimeKind.Utc).AddTicks(5097));

            migrationBuilder.UpdateData(
                table: "Marcas",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 11, 15, 14, 55, 619, DateTimeKind.Utc).AddTicks(5101));

            migrationBuilder.UpdateData(
                table: "Marcas",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 11, 15, 14, 55, 619, DateTimeKind.Utc).AddTicks(5103));

            migrationBuilder.CreateIndex(
                name: "IX_PreciosHistoricos_FechaCambio",
                table: "PreciosHistoricos",
                column: "FechaCambio");

            migrationBuilder.CreateIndex(
                name: "IX_PreciosHistoricos_ProductoId",
                table: "PreciosHistoricos",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_PreciosHistoricos_UsuarioModificacion",
                table: "PreciosHistoricos",
                column: "UsuarioModificacion");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PreciosHistoricos");

            migrationBuilder.CreateTable(
                name: "EvaluacionesCredito",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    CreditoId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaEvaluacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    MontoSolicitado = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PuntajeFinal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PuntajeRiesgoCliente = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RelacionCuotaIngreso = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Resultado = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    SueldoCliente = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TieneBuenHistorial = table.Column<bool>(type: "bit", nullable: false),
                    TieneDocumentacionCompleta = table.Column<bool>(type: "bit", nullable: false),
                    TieneGarante = table.Column<bool>(type: "bit", nullable: false),
                    TieneIngresosSuficientes = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true)
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
    }
}
