using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TheBuryProject.Migrations
{
    /// <inheritdoc />
    public partial class AddSistemaAutorizaciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AlertasStock",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Prioridad = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    Mensaje = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    StockActual = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    StockMinimo = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CantidadSugeridaReposicion = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    FechaAlerta = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaResolucion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsuarioResolucion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NotificacionUrgente = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertasStock", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AlertasStock_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SolicitudesAutorizacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioSolicitante = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RolSolicitante = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TipoUmbral = table.Column<int>(type: "int", nullable: false),
                    ValorSolicitado = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ValorPermitido = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TipoOperacion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReferenciaOperacionId = table.Column<int>(type: "int", nullable: true),
                    Justificacion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    UsuarioAutorizador = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FechaResolucion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ComentarioResolucion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitudesAutorizacion", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UmbralesAutorizacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Rol = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TipoUmbral = table.Column<int>(type: "int", nullable: false),
                    ValorMaximo = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UmbralesAutorizacion", x => x.Id);
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_AlertasStock_Estado",
                table: "AlertasStock",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_AlertasStock_FechaAlerta",
                table: "AlertasStock",
                column: "FechaAlerta");

            migrationBuilder.CreateIndex(
                name: "IX_AlertasStock_NotificacionUrgente",
                table: "AlertasStock",
                column: "NotificacionUrgente");

            migrationBuilder.CreateIndex(
                name: "IX_AlertasStock_Prioridad",
                table: "AlertasStock",
                column: "Prioridad");

            migrationBuilder.CreateIndex(
                name: "IX_AlertasStock_ProductoId",
                table: "AlertasStock",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertasStock_Tipo",
                table: "AlertasStock",
                column: "Tipo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlertasStock");

            migrationBuilder.DropTable(
                name: "SolicitudesAutorizacion");

            migrationBuilder.DropTable(
                name: "UmbralesAutorizacion");

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
        }
    }
}
