using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheBuryProject.Migrations
{
    /// <inheritdoc />
    public partial class CreateAlertasCobranza : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentosCliente");

            migrationBuilder.DropTable(
                name: "EvaluacionesCredito");

            migrationBuilder.CreateTable(
                name: "AlertasCobranza",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CuotaId = table.Column<int>(type: "int", nullable: false),
                    CreditoId = table.Column<int>(type: "int", nullable: false),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Mensaje = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Prioridad = table.Column<int>(type: "int", nullable: false),
                    Leida = table.Column<bool>(type: "bit", nullable: false),
                    Resuelta = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertasCobranza", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlertasCobranza_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AlertasCobranza_Creditos_CreditoId",
                        column: x => x.CreditoId,
                        principalTable: "Creditos",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AlertasCobranza_Cuotas_CuotaId",
                        column: x => x.CuotaId,
                        principalTable: "Cuotas",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ConfiguracionesMora",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TasaMoraDiaria = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    DiasGraciaMora = table.Column<int>(type: "int", nullable: false),
                    PorcentajeRecargoPrimerMes = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PorcentajeRecargoSegundoMes = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PorcentajeRecargoTercerMes = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiasAntesAlertaVencimiento = table.Column<int>(type: "int", nullable: false),
                    JobActivo = table.Column<bool>(type: "bit", nullable: false),
                    HoraEjecucion = table.Column<TimeSpan>(type: "time", nullable: false),
                    UltimaEjecucion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionesMora", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LogsMora",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FechaEjecucion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CuotasProcesadas = table.Column<int>(type: "int", nullable: false),
                    CuotasConMora = table.Column<int>(type: "int", nullable: false),
                    AlertasGeneradas = table.Column<int>(type: "int", nullable: false),
                    TotalMora = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalRecargosAplicados = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Exitoso = table.Column<bool>(type: "bit", nullable: false),
                    Errores = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DuracionSegundos = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogsMora", x => x.Id);
                });

            migrationBuilder.UpdateData(
                table: "Categorias",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 9, 4, 37, 27, 746, DateTimeKind.Utc).AddTicks(8627));

            migrationBuilder.UpdateData(
                table: "Categorias",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 9, 4, 37, 27, 746, DateTimeKind.Utc).AddTicks(8630));

            migrationBuilder.UpdateData(
                table: "Marcas",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 9, 4, 37, 27, 746, DateTimeKind.Utc).AddTicks(8767));

            migrationBuilder.UpdateData(
                table: "Marcas",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 9, 4, 37, 27, 746, DateTimeKind.Utc).AddTicks(8769));

            migrationBuilder.UpdateData(
                table: "Marcas",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 9, 4, 37, 27, 746, DateTimeKind.Utc).AddTicks(8771));

            migrationBuilder.CreateIndex(
                name: "IX_AlertasCobranza_ClienteId",
                table: "AlertasCobranza",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertasCobranza_CreditoId",
                table: "AlertasCobranza",
                column: "CreditoId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertasCobranza_CuotaId",
                table: "AlertasCobranza",
                column: "CuotaId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertasCobranza_Leida_Resuelta",
                table: "AlertasCobranza",
                columns: new[] { "Leida", "Resuelta" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlertasCobranza");

            migrationBuilder.DropTable(
                name: "ConfiguracionesMora");

            migrationBuilder.DropTable(
                name: "LogsMora");

            migrationBuilder.CreateTable(
                name: "DocumentosCliente",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    FechaSubida = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaVencimiento = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaVerificacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    MotivoRechazo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NombreArchivo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    RutaArchivo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    TamanoBytes = table.Column<long>(type: "bigint", nullable: false),
                    TipoDocumento = table.Column<int>(type: "int", nullable: false),
                    TipoMIME = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VerificadoPor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
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
                    MontoSolicitado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PuntajeFinal = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    PuntajeRiesgoCliente = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    RelacionCuotaIngreso = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: true),
                    Resultado = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    SueldoCliente = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
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
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EvaluacionesCredito_Creditos_CreditoId",
                        column: x => x.CreditoId,
                        principalTable: "Creditos",
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

            migrationBuilder.CreateIndex(
                name: "IX_EvaluacionesCredito_ClienteId",
                table: "EvaluacionesCredito",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluacionesCredito_CreditoId",
                table: "EvaluacionesCredito",
                column: "CreditoId");

            migrationBuilder.CreateIndex(
                name: "IX_EvaluacionesCredito_FechaEvaluacion",
                table: "EvaluacionesCredito",
                column: "FechaEvaluacion");
        }
    }
}
