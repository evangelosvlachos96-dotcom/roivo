using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Roivo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessTaxProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "EstimatedPropertyValue",
                table: "Businesses",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VatFrequency",
                table: "Businesses",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                // "Quarterly", not the generated "": every business that
                // predates this column files quarterly, and an empty string
                // would fail to parse back into VatFrequency on read.
                defaultValue: "Quarterly");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstimatedPropertyValue",
                table: "Businesses");

            migrationBuilder.DropColumn(
                name: "VatFrequency",
                table: "Businesses");
        }
    }
}
