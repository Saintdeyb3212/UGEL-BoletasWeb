using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UGEL_BoletasWeb.Migrations
{
    /// <inheritdoc />
    public partial class SoporteMontoImponible : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MontoImponible",
                table: "UGEL_Boleta_Cabecera",
                type: "decimal(12,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MontoImponible",
                table: "UGEL_Boleta_Cabecera");
        }
    }
}
