using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SalaReuniones.Migrations
{
    /// <inheritdoc />
    public partial class InitialIdentityAndSalas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "1");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "2");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "3");

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "a1b2c3d4-e5f6-4711-aaaa-bbbbcccc0001", null, "Administrador", "ADMINISTRADOR" },
                    { "a1b2c3d4-e5f6-4711-aaaa-bbbbcccc0002", null, "Usuario", "USUARIO" },
                    { "a1b2c3d4-e5f6-4711-aaaa-bbbbcccc0003", null, "Visualizador", "VISUALIZADOR" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "a1b2c3d4-e5f6-4711-aaaa-bbbbcccc0001");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "a1b2c3d4-e5f6-4711-aaaa-bbbbcccc0002");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "a1b2c3d4-e5f6-4711-aaaa-bbbbcccc0003");

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "1", null, "Administrador", "ADMINISTRADOR" },
                    { "2", null, "Usuario", "USUARIO" },
                    { "3", null, "Visualizador", "VISUALIZADOR" }
                });
        }
    }
}
