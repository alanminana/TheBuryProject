using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheBuryProject.Migrations
{
    /// <inheritdoc />
    public partial class AgregarModuloClientes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MovimientosStock_OrdenesCompra_OrdenCompraId",
                table: "MovimientosStock");

            migrationBuilder.DropForeignKey(
                name: "FK_MovimientosStock_Productos_ProductoId",
                table: "MovimientosStock");

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "MovimientosStock",
                type: "rowversion",
                rowVersion: true,
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "varbinary(max)",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "Clientes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TipoDocumento = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NumeroDocumento = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Apellido = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NombreCompleto = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FechaNacimiento = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EstadoCivil = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Telefono = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TelefonoAlternativo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Domicilio = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Localidad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Provincia = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CodigoPostal = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Empleador = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    TipoEmpleo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Sueldo = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    TelefonoLaboral = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TieneReciboSueldo = table.Column<bool>(type: "bit", nullable: false),
                    TieneVeraz = table.Column<bool>(type: "bit", nullable: false),
                    TieneImpuesto = table.Column<bool>(type: "bit", nullable: false),
                    TieneServicioLuz = table.Column<bool>(type: "bit", nullable: false),
                    TieneServicioGas = table.Column<bool>(type: "bit", nullable: false),
                    TieneServicioAgua = table.Column<bool>(type: "bit", nullable: false),
                    PuntajeRiesgo = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clientes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Creditos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    Numero = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MontoSolicitado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MontoAprobado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TasaInteres = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    CantidadCuotas = table.Column<int>(type: "int", nullable: false),
                    MontoCuota = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    FechaSolicitud = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaAprobacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaFinalizacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PuntajeRiesgoInicial = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Creditos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Creditos_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Garantes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    GaranteClienteId = table.Column<int>(type: "int", nullable: true),
                    TipoDocumento = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    NumeroDocumento = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Apellido = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Telefono = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Domicilio = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Relacion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Garantes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Garantes_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Garantes_Clientes_GaranteClienteId",
                        column: x => x.GaranteClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "Categorias",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "Descripcion", "Nombre" },
                values: new object[] { new DateTime(2025, 11, 6, 3, 45, 33, 755, DateTimeKind.Utc).AddTicks(6071), "Productos electr�nicos", "Electr�nica" });

            migrationBuilder.UpdateData(
                table: "Categorias",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "Nombre" },
                values: new object[] { new DateTime(2025, 11, 6, 3, 45, 33, 755, DateTimeKind.Utc).AddTicks(6073), "Refrigeraci�n" });

            migrationBuilder.UpdateData(
                table: "Marcas",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "Descripcion" },
                values: new object[] { new DateTime(2025, 11, 6, 3, 45, 33, 755, DateTimeKind.Utc).AddTicks(6562), "Electr�nica y electrodom�sticos" });

            migrationBuilder.UpdateData(
                table: "Marcas",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "Descripcion" },
                values: new object[] { new DateTime(2025, 11, 6, 3, 45, 33, 755, DateTimeKind.Utc).AddTicks(6565), "Electr�nica y electrodom�sticos" });

            migrationBuilder.UpdateData(
                table: "Marcas",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "Descripcion" },
                values: new object[] { new DateTime(2025, 11, 6, 3, 45, 33, 755, DateTimeKind.Utc).AddTicks(6567), "Electrodom�sticos" });

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_TipoDocumento_NumeroDocumento",
                table: "Clientes",
                columns: new[] { "TipoDocumento", "NumeroDocumento" },
                unique: true,
                filter: "IsDeleted = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Creditos_ClienteId",
                table: "Creditos",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Creditos_Numero",
                table: "Creditos",
                column: "Numero",
                unique: true,
                filter: "IsDeleted = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Garantes_ClienteId",
                table: "Garantes",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Garantes_GaranteClienteId",
                table: "Garantes",
                column: "GaranteClienteId");

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientosStock_OrdenesCompra_OrdenCompraId",
                table: "MovimientosStock",
                column: "OrdenCompraId",
                principalTable: "OrdenesCompra",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientosStock_Productos_ProductoId",
                table: "MovimientosStock",
                column: "ProductoId",
                principalTable: "Productos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MovimientosStock_OrdenesCompra_OrdenCompraId",
                table: "MovimientosStock");

            migrationBuilder.DropForeignKey(
                name: "FK_MovimientosStock_Productos_ProductoId",
                table: "MovimientosStock");

            migrationBuilder.DropTable(
                name: "Creditos");

            migrationBuilder.DropTable(
                name: "Garantes");

            migrationBuilder.DropTable(
                name: "Clientes");

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "MovimientosStock",
                type: "varbinary(max)",
                nullable: true,
                oldClrType: typeof(byte[]),
                oldType: "rowversion",
                oldRowVersion: true,
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "Categorias",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "Descripcion", "Nombre" },
                values: new object[] { new DateTime(2025, 11, 5, 17, 3, 11, 729, DateTimeKind.Utc).AddTicks(5654), "Productos electrónicos", "Electrónica" });

            migrationBuilder.UpdateData(
                table: "Categorias",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "Nombre" },
                values: new object[] { new DateTime(2025, 11, 5, 17, 3, 11, 729, DateTimeKind.Utc).AddTicks(5666), "Refrigeración" });

            migrationBuilder.UpdateData(
                table: "Marcas",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "Descripcion" },
                values: new object[] { new DateTime(2025, 11, 5, 17, 3, 11, 729, DateTimeKind.Utc).AddTicks(5897), "Electrónica y electrodomésticos" });

            migrationBuilder.UpdateData(
                table: "Marcas",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "Descripcion" },
                values: new object[] { new DateTime(2025, 11, 5, 17, 3, 11, 729, DateTimeKind.Utc).AddTicks(5900), "Electrónica y electrodomésticos" });

            migrationBuilder.UpdateData(
                table: "Marcas",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "Descripcion" },
                values: new object[] { new DateTime(2025, 11, 5, 17, 3, 11, 729, DateTimeKind.Utc).AddTicks(5902), "Electrodomésticos" });

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientosStock_OrdenesCompra_OrdenCompraId",
                table: "MovimientosStock",
                column: "OrdenCompraId",
                principalTable: "OrdenesCompra",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_MovimientosStock_Productos_ProductoId",
                table: "MovimientosStock",
                column: "ProductoId",
                principalTable: "Productos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
