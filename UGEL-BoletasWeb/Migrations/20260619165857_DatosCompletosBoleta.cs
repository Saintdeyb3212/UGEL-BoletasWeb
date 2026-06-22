using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UGEL_BoletasWeb.Migrations
{
    /// <inheritdoc />
    public partial class DatosCompletosBoleta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TipoPensionista",
                table: "UGEL_Boleta_Cabecera",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "TipoPension",
                table: "UGEL_Boleta_Cabecera",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Nombres",
                table: "UGEL_Boleta_Cabecera",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "CuentaBancaria",
                table: "UGEL_Boleta_Cabecera",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Cargo",
                table: "UGEL_Boleta_Cabecera",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Apellidos",
                table: "UGEL_Boleta_Cabecera",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<string>(
                name: "CodigoEsSalud",
                table: "UGEL_Boleta_Cabecera",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FechaNacimiento",
                table: "UGEL_Boleta_Cabecera",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FechasRegistro",
                table: "UGEL_Boleta_Cabecera",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LeyendaMensual",
                table: "UGEL_Boleta_Cabecera",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LeyendaPermanente",
                table: "UGEL_Boleta_Cabecera",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NivelMagisterial",
                table: "UGEL_Boleta_Cabecera",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TiempoServicio",
                table: "UGEL_Boleta_Cabecera",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodigoEsSalud",
                table: "UGEL_Boleta_Cabecera");

            migrationBuilder.DropColumn(
                name: "FechaNacimiento",
                table: "UGEL_Boleta_Cabecera");

            migrationBuilder.DropColumn(
                name: "FechasRegistro",
                table: "UGEL_Boleta_Cabecera");

            migrationBuilder.DropColumn(
                name: "LeyendaMensual",
                table: "UGEL_Boleta_Cabecera");

            migrationBuilder.DropColumn(
                name: "LeyendaPermanente",
                table: "UGEL_Boleta_Cabecera");

            migrationBuilder.DropColumn(
                name: "NivelMagisterial",
                table: "UGEL_Boleta_Cabecera");

            migrationBuilder.DropColumn(
                name: "TiempoServicio",
                table: "UGEL_Boleta_Cabecera");

            migrationBuilder.AlterColumn<string>(
                name: "TipoPensionista",
                table: "UGEL_Boleta_Cabecera",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "TipoPension",
                table: "UGEL_Boleta_Cabecera",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Nombres",
                table: "UGEL_Boleta_Cabecera",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "CuentaBancaria",
                table: "UGEL_Boleta_Cabecera",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Cargo",
                table: "UGEL_Boleta_Cabecera",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Apellidos",
                table: "UGEL_Boleta_Cabecera",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);
        }
    }
}
