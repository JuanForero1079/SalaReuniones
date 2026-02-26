using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SalaReuniones.Migrations
{
    /// <inheritdoc />
    public partial class AddColorHexToSala : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ColorHex",
                table: "Salas",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Salas",
                keyColumn: "Id",
                keyValue: 1,
                column: "ColorHex",
                value: "#3788d8");

            migrationBuilder.UpdateData(
                table: "Salas",
                keyColumn: "Id",
                keyValue: 2,
                column: "ColorHex",
                value: "#3788d8");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ColorHex",
                table: "Salas");
        }
    }
}
