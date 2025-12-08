using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MUNIDENUNCIA.Data.Migrations
{
    /// <inheritdoc />
    public partial class denunciasVUL : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Denuncias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Cedula = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NombreCompleto = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Telefono = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Categoria = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Ubicacion = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Pendiente"),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    FechaResolucion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Denuncias", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Denuncias_Categoria",
                table: "Denuncias",
                column: "Categoria");

            migrationBuilder.CreateIndex(
                name: "IX_Denuncias_Cedula",
                table: "Denuncias",
                column: "Cedula");

            migrationBuilder.CreateIndex(
                name: "IX_Denuncias_Estado",
                table: "Denuncias",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_Denuncias_FechaCreacion",
                table: "Denuncias",
                column: "FechaCreacion");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Denuncias");
        }
    }
}
