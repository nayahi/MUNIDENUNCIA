using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MUNIDENUNCIA.Data.Migrations
{
    /// <inheritdoc />
    public partial class MuniDenunciaCifrado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Denuncias_Cedula",
                table: "Denuncias");

            migrationBuilder.DropColumn(
                name: "Cedula",
                table: "Denuncias");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "Denuncias");

            migrationBuilder.DropColumn(
                name: "Telefono",
                table: "Denuncias");

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaResolucion",
                table: "Denuncias",
                type: "datetime2",
                nullable: true,
                comment: "Fecha en que se resolvió la denuncia",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaCreacion",
                table: "Denuncias",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()",
                comment: "Fecha de creación de la denuncia",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETUTCDATE()");

            migrationBuilder.AlterColumn<string>(
                name: "Estado",
                table: "Denuncias",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Recibida",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldDefaultValue: "Pendiente");

            migrationBuilder.AlterColumn<int>(
                name: "Categoria",
                table: "Denuncias",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<DateTime>(
                name: "ArchivoFechaSubida",
                table: "Denuncias",
                type: "datetime2",
                nullable: true,
                comment: "Fecha en que se subió el archivo PDF");

            migrationBuilder.AddColumn<string>(
                name: "ArchivoNombreOriginal",
                table: "Denuncias",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true,
                comment: "Nombre original del archivo PDF subido");

            migrationBuilder.AddColumn<string>(
                name: "ArchivoNombreServidor",
                table: "Denuncias",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true,
                comment: "Nombre aleatorio del archivo en el servidor");

            migrationBuilder.AddColumn<string>(
                name: "ArchivoRuta",
                table: "Denuncias",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                comment: "Ruta completa donde se almacenó el archivo");

            migrationBuilder.AddColumn<long>(
                name: "ArchivoTamanoBytes",
                table: "Denuncias",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ArchivoTipoMime",
                table: "Denuncias",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                comment: "Tipo MIME del archivo (application/pdf)");

            migrationBuilder.AddColumn<string>(
                name: "AsignadoAUserId",
                table: "Denuncias",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CedulaCifrada",
                table: "Denuncias",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                comment: "Cédula del ciudadano cifrada con Data Protection API");

            migrationBuilder.AddColumn<string>(
                name: "EmailCifrado",
                table: "Denuncias",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                comment: "Email del ciudadano cifrado con Data Protection API");

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaActualizacion",
                table: "Denuncias",
                type: "datetime2",
                nullable: true,
                comment: "Fecha de última actualización");

            migrationBuilder.AddColumn<string>(
                name: "TelefonoCifrado",
                table: "Denuncias",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                comment: "Teléfono del ciudadano cifrado con Data Protection API");

            migrationBuilder.CreateIndex(
                name: "IX_Denuncias_AsignadoA",
                table: "Denuncias",
                column: "AsignadoAUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Denuncias_Estado_Fecha",
                table: "Denuncias",
                columns: new[] { "Estado", "FechaCreacion" });

            migrationBuilder.AddForeignKey(
                name: "FK_Denuncias_AspNetUsers_AsignadoAUserId",
                table: "Denuncias",
                column: "AsignadoAUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Denuncias_AspNetUsers_AsignadoAUserId",
                table: "Denuncias");

            migrationBuilder.DropIndex(
                name: "IX_Denuncias_AsignadoA",
                table: "Denuncias");

            migrationBuilder.DropIndex(
                name: "IX_Denuncias_Estado_Fecha",
                table: "Denuncias");

            migrationBuilder.DropColumn(
                name: "ArchivoFechaSubida",
                table: "Denuncias");

            migrationBuilder.DropColumn(
                name: "ArchivoNombreOriginal",
                table: "Denuncias");

            migrationBuilder.DropColumn(
                name: "ArchivoNombreServidor",
                table: "Denuncias");

            migrationBuilder.DropColumn(
                name: "ArchivoRuta",
                table: "Denuncias");

            migrationBuilder.DropColumn(
                name: "ArchivoTamanoBytes",
                table: "Denuncias");

            migrationBuilder.DropColumn(
                name: "ArchivoTipoMime",
                table: "Denuncias");

            migrationBuilder.DropColumn(
                name: "AsignadoAUserId",
                table: "Denuncias");

            migrationBuilder.DropColumn(
                name: "CedulaCifrada",
                table: "Denuncias");

            migrationBuilder.DropColumn(
                name: "EmailCifrado",
                table: "Denuncias");

            migrationBuilder.DropColumn(
                name: "FechaActualizacion",
                table: "Denuncias");

            migrationBuilder.DropColumn(
                name: "TelefonoCifrado",
                table: "Denuncias");

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaResolucion",
                table: "Denuncias",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true,
                oldComment: "Fecha en que se resolvió la denuncia");

            migrationBuilder.AlterColumn<DateTime>(
                name: "FechaCreacion",
                table: "Denuncias",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETUTCDATE()",
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValueSql: "GETUTCDATE()",
                oldComment: "Fecha de creación de la denuncia");

            migrationBuilder.AlterColumn<string>(
                name: "Estado",
                table: "Denuncias",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Pendiente",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldDefaultValue: "Recibida");

            migrationBuilder.AlterColumn<string>(
                name: "Categoria",
                table: "Denuncias",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "Cedula",
                table: "Denuncias",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Denuncias",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Telefono",
                table: "Denuncias",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Denuncias_Cedula",
                table: "Denuncias",
                column: "Cedula");
        }
    }
}
