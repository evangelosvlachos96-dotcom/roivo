using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Roivo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAadeConnectionStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AadeConnectionStatus",
                table: "Businesses",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "NotConnected");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AadeConnectionStatus",
                table: "Businesses");
        }
    }
}
