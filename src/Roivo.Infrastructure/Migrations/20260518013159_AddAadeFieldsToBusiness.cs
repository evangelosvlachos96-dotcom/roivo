using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Roivo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAadeFieldsToBusiness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "AadeUserId",
                table: "Businesses",
                newName: "AadeUserIdEncrypted");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastAadeSyncAt",
                table: "Businesses",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastAadeSyncAt",
                table: "Businesses");

            migrationBuilder.RenameColumn(
                name: "AadeUserIdEncrypted",
                table: "Businesses",
                newName: "AadeUserId");
        }
    }
}
