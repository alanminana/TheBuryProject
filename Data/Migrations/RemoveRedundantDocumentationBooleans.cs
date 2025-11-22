using Microsoft.EntityFrameworkCore.Migrations;

namespace TheBuryProject.Data.Migrations
{
    public partial class RemoveRedundantDocumentationBooleans : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TieneReciboSueldo",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "TieneVeraz",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "TieneImpuesto",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "TieneServicioLuz",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "TieneServicioGas",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "TieneServicioAgua",
                table: "Clientes");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "TieneReciboSueldo",
                table: "Clientes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "TieneVeraz",
                table: "Clientes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "TieneImpuesto",
                table: "Clientes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "TieneServicioLuz",
                table: "Clientes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "TieneServicioGas",
                table: "Clientes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "TieneServicioAgua",
                table: "Clientes",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}