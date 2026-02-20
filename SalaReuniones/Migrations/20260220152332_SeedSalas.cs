using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SalaReuniones.Migrations
{
    /// <inheritdoc />
    public partial class SeedSalas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Salas",
                columns: new[] { "Id", "Nombre" },
                values: new object[,]
                {
                    { 1, "Sala Principal" },
                    { 2, "Sala Secundaria" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Salas",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Salas",
                keyColumn: "Id",
                keyValue: 2);
        }
    }
}
