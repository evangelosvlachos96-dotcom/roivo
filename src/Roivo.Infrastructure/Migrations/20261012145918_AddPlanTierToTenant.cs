using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Roivo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanTierToTenant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PlanTier",
                table: "Tenants",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlanTier",
                table: "Tenants");
        }
    }
}
