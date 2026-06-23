using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UGEL_BoletasWeb.Migrations
{
    /// <inheritdoc />
    public partial class RefactorUsuarioSistema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NombreCompleto",
                table: "UGEL_Usuario_Sistema");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NombreCompleto",
                table: "UGEL_Usuario_Sistema",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }
    }
}
