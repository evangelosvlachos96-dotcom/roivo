using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Roivo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Refactor_AuditLogIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "Timestamp",
                table: "AuditLogs",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now() at time zone 'utc'",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_TenantId_Timestamp",
                table: "AuditLogs",
                columns: new[] { "TenantId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId_Timestamp",
                table: "AuditLogs",
                columns: new[] { "UserId", "Timestamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_TenantId_Timestamp",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_UserId_Timestamp",
                table: "AuditLogs");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Timestamp",
                table: "AuditLogs",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "now() at time zone 'utc'");
        }
    }
}
