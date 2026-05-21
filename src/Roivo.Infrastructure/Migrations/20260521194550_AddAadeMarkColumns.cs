using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Roivo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAadeMarkColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "LastAadeIncomingMark",
                table: "Businesses",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "LastAadeOutgoingMark",
                table: "Businesses",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastAadeIncomingMark",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "LastAadeOutgoingMark",
                table: "Businesses");
        }
    }
}
