using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheBuryProject.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentosClienteModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EvaluacionesCredito_Clientes_ClienteId",
                table: "EvaluacionesCredito");

            migrationBuilder.DropForeignKey(
                name: "FK_EvaluacionesCredito_Creditos_CreditoId",
                table: "EvaluacionesCredito");

            migrationBuilder.AlterColumn<decimal>(
                name: "RelacionCuotaIngreso",
                table: "EvaluacionesCredito",
                type: "decimal(5,4)",
                precision: 5,
                scale: 4,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "PuntajeRiesgoCliente",
                table: "EvaluacionesCredito",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AlterColumn<decimal>(
                name: "PuntajeFinal",
                table: "EvaluacionesCredito",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.CreateTable(
                name: "DocumentosCliente",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    TipoDocumento = table.Column<int>(type: "int", nullable: false),
                    NombreArchivo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    RutaArchivo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TipoMIME = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TamanoBytes = table.Column<long>(type: "bigint", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    FechaSubida = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaVencimiento = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaVerificacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    VerificadoPor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    MotivoRechazo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentosCliente", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentosCliente_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "Categorias",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 8, 17, 42, 30, 258, DateTimeKind.Utc).AddTicks(3140));

            migrationBuilder.UpdateData(
                table: "Categorias",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 8, 17, 42, 30, 258, DateTimeKind.Utc).AddTicks(3152));

            migrationBuilder.UpdateData(
                table: "Marcas",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 8, 17, 42, 30, 262, DateTimeKind.Utc).AddTicks(5200));

            migrationBuilder.UpdateData(
                table: "Marcas",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 8, 17, 42, 30, 262, DateTimeKind.Utc).AddTicks(5204));

            migrationBuilder.UpdateData(
                table: "Marcas",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 8, 17, 42, 30, 262, DateTimeKind.Utc).AddTicks(5206));

            migrationBuilder.CreateIndex(
                name: "IX_EvaluacionesCredito_FechaEvaluacion",
                table: "EvaluacionesCredito",
                column: "FechaEvaluacion");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosCliente_ClienteId",
                table: "DocumentosCliente",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosCliente_Estado",
                table: "DocumentosCliente",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosCliente_FechaSubida",
                table: "DocumentosCliente",
                column: "FechaSubida");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosCliente_FechaVencimiento",
                table: "DocumentosCliente",
                column: "FechaVencimiento");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosCliente_TipoDocumento",
                table: "DocumentosCliente",
                column: "TipoDocumento");

            migrationBuilder.AddForeignKey(
                name: "FK_EvaluacionesCredito_Clientes_ClienteId",
                table: "EvaluacionesCredito",
                column: "ClienteId",
                principalTable: "Clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_EvaluacionesCredito_Creditos_CreditoId",
                table: "EvaluacionesCredito",
                column: "CreditoId",
                principalTable: "Creditos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EvaluacionesCredito_Clientes_ClienteId",
                table: "EvaluacionesCredito");

            migrationBuilder.DropForeignKey(
                name: "FK_EvaluacionesCredito_Creditos_CreditoId",
                table: "EvaluacionesCredito");

            migrationBuilder.DropTable(
                name: "DocumentosCliente");

            migrationBuilder.DropIndex(
                name: "IX_EvaluacionesCredito_FechaEvaluacion",
                table: "EvaluacionesCredito");

            migrationBuilder.AlterColumn<decimal>(
                name: "RelacionCuotaIngreso",
                table: "EvaluacionesCredito",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,4)",
                oldPrecision: 5,
                oldScale: 4,
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "PuntajeRiesgoCliente",
                table: "EvaluacionesCredito",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.AlterColumn<decimal>(
                name: "PuntajeFinal",
                table: "EvaluacionesCredito",
                type: "decimal(18,2)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(5,2)",
                oldPrecision: 5,
                oldScale: 2);

            migrationBuilder.UpdateData(
                table: "Categorias",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 8, 16, 42, 2, 133, DateTimeKind.Utc).AddTicks(166));

            migrationBuilder.UpdateData(
                table: "Categorias",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 8, 16, 42, 2, 133, DateTimeKind.Utc).AddTicks(169));

            migrationBuilder.UpdateData(
                table: "Marcas",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 8, 16, 42, 2, 134, DateTimeKind.Utc).AddTicks(1855));

            migrationBuilder.UpdateData(
                table: "Marcas",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 8, 16, 42, 2, 134, DateTimeKind.Utc).AddTicks(1859));

            migrationBuilder.UpdateData(
                table: "Marcas",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 8, 16, 42, 2, 134, DateTimeKind.Utc).AddTicks(1862));

            migrationBuilder.AddForeignKey(
                name: "FK_EvaluacionesCredito_Clientes_ClienteId",
                table: "EvaluacionesCredito",
                column: "ClienteId",
                principalTable: "Clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_EvaluacionesCredito_Creditos_CreditoId",
                table: "EvaluacionesCredito",
                column: "CreditoId",
                principalTable: "Creditos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
