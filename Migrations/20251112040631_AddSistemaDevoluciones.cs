using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheBuryProject.Migrations
{
    /// <inheritdoc />
    public partial class AddSistemaDevoluciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Devoluciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VentaId = table.Column<int>(type: "int", nullable: false),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    NumeroDevolucion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FechaDevolucion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Motivo = table.Column<int>(type: "int", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    RequiereRMA = table.Column<bool>(type: "bit", nullable: false),
                    RMAId = table.Column<int>(type: "int", nullable: true),
                    TotalDevolucion = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NotaCreditoGenerada = table.Column<bool>(type: "bit", nullable: false),
                    NotaCreditoId = table.Column<int>(type: "int", nullable: true),
                    ObservacionesInternas = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AprobadoPor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FechaAprobacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devoluciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Devoluciones_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Devoluciones_Ventas_VentaId",
                        column: x => x.VentaId,
                        principalTable: "Ventas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Garantias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VentaDetalleId = table.Column<int>(type: "int", nullable: false),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    NumeroGarantia = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaVencimiento = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MesesGarantia = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    CondicionesGarantia = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    GarantiaExtendida = table.Column<bool>(type: "bit", nullable: false),
                    ObservacionesActivacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Garantias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Garantias_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Garantias_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Garantias_VentaDetalles_VentaDetalleId",
                        column: x => x.VentaDetalleId,
                        principalTable: "VentaDetalles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NotasCredito",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DevolucionId = table.Column<int>(type: "int", nullable: false),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    NumeroNotaCredito = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FechaEmision = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MontoTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    MontoUtilizado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FechaVencimiento = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_NotasCredito", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NotasCredito_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NotasCredito_Devoluciones_DevolucionId",
                        column: x => x.DevolucionId,
                        principalTable: "Devoluciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RMAs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProveedorId = table.Column<int>(type: "int", nullable: false),
                    DevolucionId = table.Column<int>(type: "int", nullable: false),
                    NumeroRMA = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FechaSolicitud = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    MotivoSolicitud = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    FechaAprobacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NumeroRMAProveedor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FechaEnvio = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NumeroGuiaEnvio = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FechaRecepcionProveedor = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TipoResolucion = table.Column<int>(type: "int", nullable: true),
                    FechaResolucion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DetalleResolucion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    MontoReembolso = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ObservacionesProveedor = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RMAs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RMAs_Devoluciones_DevolucionId",
                        column: x => x.DevolucionId,
                        principalTable: "Devoluciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RMAs_Proveedores_ProveedorId",
                        column: x => x.ProveedorId,
                        principalTable: "Proveedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DevolucionDetalles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DevolucionId = table.Column<int>(type: "int", nullable: false),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    Cantidad = table.Column<int>(type: "int", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EstadoProducto = table.Column<int>(type: "int", nullable: false),
                    TieneGarantia = table.Column<bool>(type: "bit", nullable: false),
                    GarantiaId = table.Column<int>(type: "int", nullable: true),
                    AccesoriosCompletos = table.Column<bool>(type: "bit", nullable: false),
                    AccesoriosFaltantes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ObservacionesTecnicas = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AccionRecomendada = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DevolucionDetalles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DevolucionDetalles_Devoluciones_DevolucionId",
                        column: x => x.DevolucionId,
                        principalTable: "Devoluciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DevolucionDetalles_Garantias_GarantiaId",
                        column: x => x.GarantiaId,
                        principalTable: "Garantias",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DevolucionDetalles_Productos_ProductoId",
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
                value: new DateTime(2025, 11, 12, 4, 6, 29, 833, DateTimeKind.Utc).AddTicks(5418));

            migrationBuilder.UpdateData(
                table: "Categorias",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 12, 4, 6, 29, 833, DateTimeKind.Utc).AddTicks(5422));

            migrationBuilder.UpdateData(
                table: "Marcas",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 12, 4, 6, 29, 844, DateTimeKind.Utc).AddTicks(9699));

            migrationBuilder.UpdateData(
                table: "Marcas",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 12, 4, 6, 29, 844, DateTimeKind.Utc).AddTicks(9704));

            migrationBuilder.UpdateData(
                table: "Marcas",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 12, 4, 6, 29, 844, DateTimeKind.Utc).AddTicks(9706));

            migrationBuilder.CreateIndex(
                name: "IX_DevolucionDetalles_DevolucionId",
                table: "DevolucionDetalles",
                column: "DevolucionId");

            migrationBuilder.CreateIndex(
                name: "IX_DevolucionDetalles_GarantiaId",
                table: "DevolucionDetalles",
                column: "GarantiaId");

            migrationBuilder.CreateIndex(
                name: "IX_DevolucionDetalles_ProductoId",
                table: "DevolucionDetalles",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_Devoluciones_ClienteId",
                table: "Devoluciones",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Devoluciones_Estado",
                table: "Devoluciones",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_Devoluciones_FechaDevolucion",
                table: "Devoluciones",
                column: "FechaDevolucion");

            migrationBuilder.CreateIndex(
                name: "IX_Devoluciones_NumeroDevolucion",
                table: "Devoluciones",
                column: "NumeroDevolucion",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Devoluciones_VentaId",
                table: "Devoluciones",
                column: "VentaId");

            migrationBuilder.CreateIndex(
                name: "IX_Garantias_ClienteId",
                table: "Garantias",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Garantias_Estado",
                table: "Garantias",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_Garantias_FechaInicio",
                table: "Garantias",
                column: "FechaInicio");

            migrationBuilder.CreateIndex(
                name: "IX_Garantias_FechaVencimiento",
                table: "Garantias",
                column: "FechaVencimiento");

            migrationBuilder.CreateIndex(
                name: "IX_Garantias_NumeroGarantia",
                table: "Garantias",
                column: "NumeroGarantia",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Garantias_ProductoId",
                table: "Garantias",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_Garantias_VentaDetalleId",
                table: "Garantias",
                column: "VentaDetalleId");

            migrationBuilder.CreateIndex(
                name: "IX_NotasCredito_ClienteId",
                table: "NotasCredito",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_NotasCredito_DevolucionId",
                table: "NotasCredito",
                column: "DevolucionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotasCredito_Estado",
                table: "NotasCredito",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_NotasCredito_FechaEmision",
                table: "NotasCredito",
                column: "FechaEmision");

            migrationBuilder.CreateIndex(
                name: "IX_NotasCredito_FechaVencimiento",
                table: "NotasCredito",
                column: "FechaVencimiento");

            migrationBuilder.CreateIndex(
                name: "IX_NotasCredito_NumeroNotaCredito",
                table: "NotasCredito",
                column: "NumeroNotaCredito",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RMAs_DevolucionId",
                table: "RMAs",
                column: "DevolucionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RMAs_Estado",
                table: "RMAs",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_RMAs_FechaSolicitud",
                table: "RMAs",
                column: "FechaSolicitud");

            migrationBuilder.CreateIndex(
                name: "IX_RMAs_NumeroRMA",
                table: "RMAs",
                column: "NumeroRMA",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RMAs_ProveedorId",
                table: "RMAs",
                column: "ProveedorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DevolucionDetalles");

            migrationBuilder.DropTable(
                name: "NotasCredito");

            migrationBuilder.DropTable(
                name: "RMAs");

            migrationBuilder.DropTable(
                name: "Garantias");

            migrationBuilder.DropTable(
                name: "Devoluciones");

            migrationBuilder.UpdateData(
                table: "Categorias",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 12, 2, 0, 41, 619, DateTimeKind.Utc).AddTicks(867));

            migrationBuilder.UpdateData(
                table: "Categorias",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 12, 2, 0, 41, 619, DateTimeKind.Utc).AddTicks(871));

            migrationBuilder.UpdateData(
                table: "Marcas",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 12, 2, 0, 41, 622, DateTimeKind.Utc).AddTicks(478));

            migrationBuilder.UpdateData(
                table: "Marcas",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 12, 2, 0, 41, 622, DateTimeKind.Utc).AddTicks(481));

            migrationBuilder.UpdateData(
                table: "Marcas",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2025, 11, 12, 2, 0, 41, 622, DateTimeKind.Utc).AddTicks(483));
        }
    }
}
