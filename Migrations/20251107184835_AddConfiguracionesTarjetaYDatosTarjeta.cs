using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheBuryProject.Migrations
{
    /// <inheritdoc />
    public partial class AddConfiguracionesTarjetaYDatosTarjeta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EstadoAutorizacion",
                table: "Ventas",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaAutorizacion",
                table: "Ventas",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaConfirmacion",
                table: "Ventas",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaEntrega",
                table: "Ventas",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaSolicitudAutorizacion",
                table: "Ventas",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotivoAutorizacion",
                table: "Ventas",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotivoRechazo",
                table: "Ventas",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiereAutorizacion",
                table: "Ventas",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UsuarioAutoriza",
                table: "Ventas",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UsuarioSolicita",
                table: "Ventas",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ConfiguracionesPago",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TipoPago = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    PermiteDescuento = table.Column<bool>(type: "bit", nullable: false),
                    PorcentajeDescuentoMaximo = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    TieneRecargo = table.Column<bool>(type: "bit", nullable: false),
                    PorcentajeRecargo = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionesPago", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DatosCheque",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VentaId = table.Column<int>(type: "int", nullable: false),
                    NumeroCheque = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Banco = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Titular = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CUIT = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    FechaEmision = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaVencimiento = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatosCheque", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DatosCheque_Ventas_VentaId",
                        column: x => x.VentaId,
                        principalTable: "Ventas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConfiguracionesTarjeta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConfiguracionPagoId = table.Column<int>(type: "int", nullable: false),
                    NombreTarjeta = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TipoTarjeta = table.Column<int>(type: "int", nullable: false),
                    Activa = table.Column<bool>(type: "bit", nullable: false),
                    PermiteCuotas = table.Column<bool>(type: "bit", nullable: false),
                    CantidadMaximaCuotas = table.Column<int>(type: "int", nullable: true),
                    TipoCuota = table.Column<int>(type: "int", nullable: true),
                    TasaInteresesMensual = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    TieneRecargoDebito = table.Column<bool>(type: "bit", nullable: false),
                    PorcentajeRecargoDebito = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionesTarjeta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConfiguracionesTarjeta_ConfiguracionesPago_ConfiguracionPagoId",
                        column: x => x.ConfiguracionPagoId,
                        principalTable: "ConfiguracionesPago",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DatosTarjeta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VentaId = table.Column<int>(type: "int", nullable: false),
                    ConfiguracionTarjetaId = table.Column<int>(type: "int", nullable: true),
                    NombreTarjeta = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TipoTarjeta = table.Column<int>(type: "int", nullable: false),
                    CantidadCuotas = table.Column<int>(type: "int", nullable: true),
                    TipoCuota = table.Column<int>(type: "int", nullable: true),
                    TasaInteres = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    MontoCuota = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MontoTotalConInteres = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    RecargoAplicado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    NumeroAutorizacion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DatosTarjeta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DatosTarjeta_ConfiguracionesTarjeta_ConfiguracionTarjetaId",
                        column: x => x.ConfiguracionTarjetaId,
                        principalTable: "ConfiguracionesTarjeta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DatosTarjeta_Ventas_VentaId",
                        column: x => x.VentaId,
                        principalTable: "Ventas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Categorias",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 7, 18, 48, 34, 658, DateTimeKind.Utc).AddTicks(9441));

            migrationBuilder.UpdateData(
                table: "Categorias",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 7, 18, 48, 34, 658, DateTimeKind.Utc).AddTicks(9445));

            migrationBuilder.UpdateData(
                table: "Marcas",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 7, 18, 48, 34, 659, DateTimeKind.Utc).AddTicks(3304));

            migrationBuilder.UpdateData(
                table: "Marcas",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 7, 18, 48, 34, 659, DateTimeKind.Utc).AddTicks(3307));

            migrationBuilder.UpdateData(
                table: "Marcas",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 7, 18, 48, 34, 659, DateTimeKind.Utc).AddTicks(3309));

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracionesPago_TipoPago",
                table: "ConfiguracionesPago",
                column: "TipoPago",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConfiguracionesTarjeta_ConfiguracionPagoId_NombreTarjeta",
                table: "ConfiguracionesTarjeta",
                columns: new[] { "ConfiguracionPagoId", "NombreTarjeta" });

            migrationBuilder.CreateIndex(
                name: "IX_DatosCheque_NumeroCheque",
                table: "DatosCheque",
                column: "NumeroCheque");

            migrationBuilder.CreateIndex(
                name: "IX_DatosCheque_VentaId",
                table: "DatosCheque",
                column: "VentaId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DatosTarjeta_ConfiguracionTarjetaId",
                table: "DatosTarjeta",
                column: "ConfiguracionTarjetaId");

            migrationBuilder.CreateIndex(
                name: "IX_DatosTarjeta_VentaId",
                table: "DatosTarjeta",
                column: "VentaId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DatosCheque");

            migrationBuilder.DropTable(
                name: "DatosTarjeta");

            migrationBuilder.DropTable(
                name: "ConfiguracionesTarjeta");

            migrationBuilder.DropTable(
                name: "ConfiguracionesPago");

            migrationBuilder.DropColumn(
                name: "EstadoAutorizacion",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "FechaAutorizacion",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "FechaConfirmacion",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "FechaEntrega",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "FechaSolicitudAutorizacion",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "MotivoAutorizacion",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "MotivoRechazo",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "RequiereAutorizacion",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "UsuarioAutoriza",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "UsuarioSolicita",
                table: "Ventas");

            migrationBuilder.UpdateData(
                table: "Categorias",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 7, 16, 35, 52, 681, DateTimeKind.Utc).AddTicks(9640));

            migrationBuilder.UpdateData(
                table: "Categorias",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 7, 16, 35, 52, 681, DateTimeKind.Utc).AddTicks(9643));

            migrationBuilder.UpdateData(
                table: "Marcas",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 7, 16, 35, 52, 681, DateTimeKind.Utc).AddTicks(9958));

            migrationBuilder.UpdateData(
                table: "Marcas",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 7, 16, 35, 52, 681, DateTimeKind.Utc).AddTicks(9962));

            migrationBuilder.UpdateData(
                table: "Marcas",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 7, 16, 35, 52, 681, DateTimeKind.Utc).AddTicks(9964));
        }
    }
}
