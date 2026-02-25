using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SalaReuniones.Migrations
{
    /// <inheritdoc />
    public partial class AgregarUsuarioIdEnReserva : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Nombre",
                table: "Reservas");

            migrationBuilder.AddColumn<string>(
                name: "UsuarioId",
                table: "Reservas",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Reservas_UsuarioId",
                table: "Reservas",
                column: "UsuarioId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reservas_AspNetUsers_UsuarioId",
                table: "Reservas",
                column: "UsuarioId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reservas_AspNetUsers_UsuarioId",
                table: "Reservas");

            migrationBuilder.DropIndex(
                name: "IX_Reservas_UsuarioId",
                table: "Reservas");

            migrationBuilder.DropColumn(
                name: "UsuarioId",
                table: "Reservas");

            migrationBuilder.AddColumn<string>(
                name: "Nombre",
                table: "Reservas",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }
    }
}
