using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UGEL_BoletasWeb.Migrations
{
    /// <inheritdoc />
    public partial class InicializacionSistemaBD : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UGEL_Boleta_Cabecera",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DNI = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false),
                    Apellidos = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Nombres = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Cargo = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    TipoPensionista = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    TipoPension = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    CuentaBancaria = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    Mes = table.Column<string>(type: "char(2)", maxLength: 2, nullable: false),
                    Anio = table.Column<string>(type: "char(4)", maxLength: 4, nullable: false),
                    TotalIngresos = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    TotalDescuentos = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    MontoLiquido = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioCreacion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsuarioModificacion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EstadoActivo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UGEL_Boleta_Cabecera", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UGEL_Usuario_Sistema",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    Rol = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                    NombreCompleto = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioCreacion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsuarioModificacion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EstadoActivo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UGEL_Usuario_Sistema", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UGEL_Boleta_Detalle",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BoletaCabeceraId = table.Column<int>(type: "int", nullable: false),
                    CodigoConcepto = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    Descripcion = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    TipoConcepto = table.Column<string>(type: "char(1)", maxLength: 1, nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioCreacion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FechaModificacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsuarioModificacion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    EstadoActivo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UGEL_Boleta_Detalle", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UGEL_Boleta_Detalle_UGEL_Boleta_Cabecera_BoletaCabeceraId",
                        column: x => x.BoletaCabeceraId,
                        principalTable: "UGEL_Boleta_Cabecera",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BoletaCabecera_DNI",
                table: "UGEL_Boleta_Cabecera",
                column: "DNI");

            migrationBuilder.CreateIndex(
                name: "IX_BoletaCabecera_Periodo",
                table: "UGEL_Boleta_Cabecera",
                columns: new[] { "Mes", "Anio" });

            migrationBuilder.CreateIndex(
                name: "IX_UGEL_Boleta_Detalle_BoletaCabeceraId",
                table: "UGEL_Boleta_Detalle",
                column: "BoletaCabeceraId");

            migrationBuilder.CreateIndex(
                name: "IX_UsuarioSistema_Username_Unico",
                table: "UGEL_Usuario_Sistema",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UGEL_Boleta_Detalle");

            migrationBuilder.DropTable(
                name: "UGEL_Usuario_Sistema");

            migrationBuilder.DropTable(
                name: "UGEL_Boleta_Cabecera");
        }
    }
}
