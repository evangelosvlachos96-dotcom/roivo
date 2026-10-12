using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Roivo.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReconciliationAndCashflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsReconciled",
                table: "Invoices",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReconciledAt",
                table: "Invoices",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsReconciled",
                table: "BankTransactions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReconciledAt",
                table: "BankTransactions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CashflowCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    IsRecurring = table.Column<bool>(type: "boolean", nullable: false),
                    RecurringDay = table.Column<int>(type: "integer", nullable: true),
                    AverageAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashflowCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashflowCategories_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CashflowForecasts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    ForecastDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PredictedInflow = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PredictedOutflow = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PredictedBalance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ConfidenceLow = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ConfidenceHigh = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ActualInflow = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    ActualOutflow = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    ActualBalance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashflowForecasts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CashflowForecasts_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReconciliationMatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    BankTransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MatchConfidence = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MatchedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MatchedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReconciliationMatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReconciliationMatches_BankTransactions_BankTransactionId",
                        column: x => x.BankTransactionId,
                        principalTable: "BankTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReconciliationMatches_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReconciliationMatches_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ReconciliationRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    RuleName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AmountTolerancePercent = table.Column<decimal>(type: "numeric(6,3)", precision: 6, scale: 3, nullable: false),
                    DateToleranceDays = table.Column<int>(type: "integer", nullable: false),
                    CounterpartyPattern = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReconciliationRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReconciliationRules_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaxObligations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<Guid>(type: "uuid", nullable: false),
                    TaxType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EstimatedAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ActualAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    IsPaid = table.Column<bool>(type: "boolean", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Period = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxObligations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaxObligations_Businesses_BusinessId",
                        column: x => x.BusinessId,
                        principalTable: "Businesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CashflowCategories_BusinessId_Type",
                table: "CashflowCategories",
                columns: new[] { "BusinessId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_CashflowForecasts_BusinessId_ForecastDate",
                table: "CashflowForecasts",
                columns: new[] { "BusinessId", "ForecastDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationMatches_BankTransactionId",
                table: "ReconciliationMatches",
                column: "BankTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationMatches_BusinessId_Status",
                table: "ReconciliationMatches",
                columns: new[] { "BusinessId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationMatches_InvoiceId",
                table: "ReconciliationMatches",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationMatches_InvoiceId_BankTransactionId",
                table: "ReconciliationMatches",
                columns: new[] { "InvoiceId", "BankTransactionId" },
                unique: true,
                filter: "\"Status\" <> 'Rejected'");

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationRules_BusinessId_IsActive",
                table: "ReconciliationRules",
                columns: new[] { "BusinessId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxObligations_BusinessId_DueDate",
                table: "TaxObligations",
                columns: new[] { "BusinessId", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxObligations_BusinessId_TaxType_Period",
                table: "TaxObligations",
                columns: new[] { "BusinessId", "TaxType", "Period" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CashflowCategories");

            migrationBuilder.DropTable(
                name: "CashflowForecasts");

            migrationBuilder.DropTable(
                name: "ReconciliationMatches");

            migrationBuilder.DropTable(
                name: "ReconciliationRules");

            migrationBuilder.DropTable(
                name: "TaxObligations");

            migrationBuilder.DropColumn(
                name: "IsReconciled",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ReconciledAt",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "IsReconciled",
                table: "BankTransactions");

            migrationBuilder.DropColumn(
                name: "ReconciledAt",
                table: "BankTransactions");
        }
    }
}
