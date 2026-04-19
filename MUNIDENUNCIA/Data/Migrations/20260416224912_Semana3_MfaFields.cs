using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MUNIDENUNCIA.Data.Migrations
{
    /// <inheritdoc />
    public partial class Semana3_MfaFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "MfaEnabledOn",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecoveryCodesHash",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TotpEnabled",
                table: "AspNetUsers",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TotpSecret",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MfaEnabledOn",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "RecoveryCodesHash",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "TotpEnabled",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "TotpSecret",
                table: "AspNetUsers");
        }
    }
}
