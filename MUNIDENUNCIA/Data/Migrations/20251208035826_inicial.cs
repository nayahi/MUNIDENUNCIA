using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MUNIDENUNCIA.Data.Migrations
{
    /// <inheritdoc />
    public partial class inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Cedula",
                table: "AspNetUsers",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Departamento",
                table: "AspNetUsers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Discriminator",
                table: "AspNetUsers",
                type: "nvarchar(21)",
                maxLength: 21,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaRegistro",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NombreCompleto",
                table: "AspNetUsers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiereCambioContrasena",
                table: "AspNetUsers",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UltimoAcceso",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Success = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SolicitudesPermisos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CedulaPropietario = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NombreCompletoPropietario = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EmailPropietario = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TelefonoPropietario = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Distrito = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DireccionCompleta = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    PlanoCatastrado = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    TipoConstruccion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AreaConstruccionM2 = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    NumeroPlantas = table.Column<int>(type: "int", nullable: false),
                    DescripcionProyecto = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    PresupuestoEstimado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Pendiente"),
                    FechaSolicitud = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    FechaRevision = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaAprobacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevisadoPor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitudesPermisos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Comentarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SolicitudPermisoId = table.Column<int>(type: "int", nullable: false),
                    NombreFuncionario = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CargoFuncionario = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TextoComentario = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    FechaComentario = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    EsAprobacion = table.Column<bool>(type: "bit", nullable: false),
                    EsRechazo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Comentarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Comentarios_SolicitudesPermisos_SolicitudPermisoId",
                        column: x => x.SolicitudPermisoId,
                        principalTable: "SolicitudesPermisos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_EventType",
                table: "AuditLogs",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Timestamp",
                table: "AuditLogs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Comentarios_FechaComentario",
                table: "Comentarios",
                column: "FechaComentario");

            migrationBuilder.CreateIndex(
                name: "IX_Comentarios_SolicitudPermisoId",
                table: "Comentarios",
                column: "SolicitudPermisoId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesPermisos_CedulaPropietario",
                table: "SolicitudesPermisos",
                column: "CedulaPropietario");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesPermisos_Estado",
                table: "SolicitudesPermisos",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesPermisos_Estado_Fecha",
                table: "SolicitudesPermisos",
                columns: new[] { "Estado", "FechaSolicitud" });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesPermisos_FechaSolicitud",
                table: "SolicitudesPermisos",
                column: "FechaSolicitud");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitudesPermisos_PlanoCatastrado",
                table: "SolicitudesPermisos",
                column: "PlanoCatastrado");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "Comentarios");

            migrationBuilder.DropTable(
                name: "SolicitudesPermisos");

            migrationBuilder.DropColumn(
                name: "Cedula",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Departamento",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Discriminator",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "FechaRegistro",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "NombreCompleto",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "RequiereCambioContrasena",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "UltimoAcceso",
                table: "AspNetUsers");
        }
    }
}
